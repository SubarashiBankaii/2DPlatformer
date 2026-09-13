using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer3D
{
    /// <summary>
    /// Senior-level 3D Platformer Player Controller.
    /// Fully optimized for Unity's New Input System (com.unity.inputsystem) with Legacy fallback.
    /// Features:
    /// - Smooth camera-relative movement.
    /// - Acceleration & deceleration damping.
    /// - Variable jump height, coyote time, and jump buffering.
    /// - Momentum sliding with slope speed boost.
    /// - Visual leaning & squash/stretch feedback.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Base walking movement speed in m/s.")]
        [SerializeField] private float moveSpeed = 9f;
        
        [Tooltip("Sprinting speed multiplier.")]
        [SerializeField] private float sprintMultiplier = 1.4f;
        
        [Tooltip("Time in seconds to reach full speed.")]
        [SerializeField] private float accelerationTime = 0.08f;
        
        [Tooltip("Time in seconds to come to a complete stop.")]
        [SerializeField] private float decelerationTime = 0.06f;
        
        [Tooltip("Time to rotate towards movement direction.")]
        [SerializeField] private float turnSmoothTime = 0.06f;

        [Header("Jump Settings")]
        [Tooltip("Maximum height of a full jump in meters.")]
        [SerializeField] private float jumpHeight = 2.8f;
        
        [Tooltip("Gravity acceleration multiplier.")]
        [SerializeField] private float gravityScale = 2.5f;
        
        [Tooltip("Multiplier applied to vertical velocity when jump button is released early.")]
        [SerializeField] private float jumpCutMultiplier = 0.4f;
        
        [Tooltip("Grace period (seconds) after leaving a ledge where jumping is allowed.")]
        [SerializeField] private float coyoteTime = 0.18f;
        
        [Tooltip("Grace period (seconds) for buffering jump input before hitting the ground.")]
        [SerializeField] private float jumpBufferTime = 0.18f;

        [Header("Slide Settings")]
        [Tooltip("Initial impulse speed boost when starting a slide.")]
        [SerializeField] private float slideInitialBoost = 10f;
        
        [Tooltip("Deceleration rate of slide speed on flat ground.")]
        [SerializeField] private float slideFriction = 6f;
        
        [Tooltip("Speed multiplier added when sliding down a slope.")]
        [SerializeField] private float slopeSlideBoost = 20f;
        
        [Tooltip("Height of character controller capsule when sliding.")]
        [SerializeField] private float slideHeight = 0.9f;
        
        [Tooltip("Minimum speed threshold before exiting slide automatically.")]
        [SerializeField] private float minSlideSpeed = 2.5f;

        [Header("Visual Feedback & Juice")]
        [Tooltip("Reference to child visual model container for leaning and squash/stretch.")]
        [SerializeField] private Transform visualModel;
        
        [Tooltip("Maximum tilt angle when turning.")]
        [SerializeField] private float maxLeanAngle = 12f;

        [Header("Ground & Environment Detection")]
        [Tooltip("Layer mask for walkable surfaces.")]
        [SerializeField] private LayerMask groundLayer = ~0;

        // Components & References
        private CharacterController controller;
        private Camera mainCamera;
        private PlayerVFX vfxSystem;

        // Velocity & Physics States
        private Vector3 targetMoveVelocity;
        private Vector3 currentMoveVelocity;
        private Vector3 velocityDamp;
        private float verticalVelocity;
        private float turnSmoothVelocity;

        // Slide State
        private bool isSliding;
        private Vector3 slideDirection;
        private float currentSlideSpeed;
        private float normalHeight;
        private Vector3 normalCenter;

        // Timers & Flags
        private bool isGrounded;
        private float coyoteTimer;
        private float jumpBufferTimer;

        // Juice Animation variables
        private Vector3 originalVisualScale = Vector3.one;
        private Coroutine squashStretchRoutine;

        public bool IsSliding => isSliding;
        public bool IsGrounded => isGrounded;
        public Vector3 Velocity => controller != null ? controller.velocity : Vector3.zero;
        public float CurrentSpeed => new Vector2(Velocity.x, Velocity.z).magnitude;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            mainCamera = Camera.main;
            vfxSystem = GetComponent<PlayerVFX>();

            if (controller != null)
            {
                normalHeight = controller.height;
                normalCenter = controller.center;
            }

            if (visualModel == null && transform.childCount > 0)
            {
                visualModel = transform.GetChild(0);
            }
            if (visualModel != null)
            {
                originalVisualScale = visualModel.localScale;
            }
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            CheckGroundStatus();
            HandleInput();
            HandleSlide();
            HandleMovement();
            HandleJump();
            ApplyGravity();
            ApplyFinalMovement();
            ApplyVisualJuice();
        }

        private void CheckGroundStatus()
        {
            bool wasGrounded = isGrounded;

            // CharacterController built-in check
            bool controllerGrounded = controller.isGrounded;

            // Raycast check from 0.3m above character bottom
            Vector3 rayOrigin = transform.position + Vector3.up * 0.3f;
            bool rayGrounded = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 0.45f, groundLayer, QueryTriggerInteraction.Ignore);

            isGrounded = controllerGrounded || rayGrounded;

            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
                if (!wasGrounded && verticalVelocity < -2f)
                {
                    OnLanding();
                }
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
            }

            if (jumpBufferTimer > 0)
            {
                jumpBufferTimer -= Time.deltaTime;
            }
        }

        #region Robust Input Handlers (New Input System Primary)

        private Vector2 GetMoveInput()
        {
            Vector2 move = Vector2.zero;

            // 1. Primary: New Input System Keyboard
            var kbd = Keyboard.current;
            if (kbd != null)
            {
                if (kbd.wKey.isPressed || kbd.upArrowKey.isPressed) move.y += 1f;
                if (kbd.sKey.isPressed || kbd.downArrowKey.isPressed) move.y -= 1f;
                if (kbd.dKey.isPressed || kbd.rightArrowKey.isPressed) move.x += 1f;
                if (kbd.aKey.isPressed || kbd.leftArrowKey.isPressed) move.x -= 1f;
            }

            // Gamepad Support
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > move.sqrMagnitude)
                {
                    move = stick;
                }
            }

            // 2. Fallback: Legacy Input (Safely wrapped)
            if (move.sqrMagnitude < 0.01f)
            {
                try
                {
                    float h = Input.GetAxisRaw("Horizontal");
                    float v = Input.GetAxisRaw("Vertical");
                    move = new Vector2(h, v);
                }
                catch { }
            }

            return move.normalized;
        }

        private bool GetJumpDown()
        {
            // 1. New Input System Keyboard
            var kbd = Keyboard.current;
            if (kbd != null && kbd.spaceKey.wasPressedThisFrame) return true;

            // Gamepad
            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) return true;

            // 2. Legacy Input Fallback
            try
            {
                if (Input.GetButtonDown("Jump")) return true;
            }
            catch { }

            return false;
        }

        private bool GetJumpUp()
        {
            var kbd = Keyboard.current;
            if (kbd != null && kbd.spaceKey.wasReleasedThisFrame) return true;

            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonSouth.wasReleasedThisFrame) return true;

            try
            {
                if (Input.GetButtonUp("Jump")) return true;
            }
            catch { }

            return false;
        }

        private bool IsSprinting()
        {
            var kbd = Keyboard.current;
            if (kbd != null && (kbd.leftShiftKey.isPressed || kbd.rightShiftKey.isPressed)) return true;

            var gamepad = Gamepad.current;
            if (gamepad != null && (gamepad.leftShoulder.isPressed || gamepad.leftTrigger.isPressed)) return true;

            try
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return true;
            }
            catch { }

            return false;
        }

        private bool GetSlideDown()
        {
            var kbd = Keyboard.current;
            if (kbd != null && (kbd.cKey.wasPressedThisFrame || kbd.leftCtrlKey.wasPressedThisFrame)) return true;

            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) return true;

            try
            {
                if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl)) return true;
            }
            catch { }

            return false;
        }

        #endregion

        private void HandleInput()
        {
            if (GetJumpDown())
            {
                jumpBufferTimer = jumpBufferTime;
            }

            if (GetJumpUp())
            {
                if (verticalVelocity > 0)
                {
                    verticalVelocity *= jumpCutMultiplier;
                }
            }

            if (GetSlideDown() && isGrounded && !isSliding)
            {
                StartSlide();
            }
        }

        private void HandleMovement()
        {
            if (isSliding) return;

            Vector2 input = GetMoveInput();
            Vector3 inputDir = new Vector3(input.x, 0f, input.y);

            if (inputDir.sqrMagnitude >= 0.01f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
                if (mainCamera != null)
                {
                    targetAngle += mainCamera.transform.eulerAngles.y;
                }

                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                float speed = moveSpeed * (IsSprinting() ? sprintMultiplier : 1f);
                targetMoveVelocity = moveDir * speed;
            }
            else
            {
                targetMoveVelocity = Vector3.zero;
            }

            float smoothTime = targetMoveVelocity.sqrMagnitude > 0.01f ? accelerationTime : decelerationTime;
            currentMoveVelocity = Vector3.SmoothDamp(currentMoveVelocity, targetMoveVelocity, ref velocityDamp, smoothTime);
        }

        private void HandleSlide()
        {
            if (!isSliding) return;

            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1.2f, groundLayer))
            {
                Vector3 normal = hit.normal;
                float slopeAngle = Vector3.Angle(normal, Vector3.up);

                if (slopeAngle > 5f)
                {
                    Vector3 slopeDown = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
                    slideDirection = Vector3.Slerp(slideDirection, slopeDown, Time.deltaTime * 6f);
                    currentSlideSpeed += slopeSlideBoost * Time.deltaTime * (slopeAngle / 45f);
                }
                else
                {
                    currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, 0f, slideFriction * Time.deltaTime);
                }
            }
            else
            {
                currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, 0f, slideFriction * 0.5f * Time.deltaTime);
            }

            Vector2 input = GetMoveInput();
            if (Mathf.Abs(input.x) > 0.1f && mainCamera != null)
            {
                Vector3 steerDir = mainCamera.transform.right * input.x;
                slideDirection = Vector3.Slerp(slideDirection, (slideDirection + steerDir * 0.35f).normalized, Time.deltaTime * 4f);
            }

            currentMoveVelocity = slideDirection * currentSlideSpeed;

            if (slideDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(new Vector3(slideDirection.x, 0, slideDirection.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12f);
            }

            if (currentSlideSpeed < minSlideSpeed)
            {
                if (HasOverheadClearance())
                {
                    StopSlide();
                }
            }
        }

        private void StartSlide()
        {
            isSliding = true;
            controller.height = slideHeight;
            controller.center = new Vector3(normalCenter.x, slideHeight * 0.5f, normalCenter.z);

            Vector3 horizVel = new Vector3(controller.velocity.x, 0, controller.velocity.z);
            if (horizVel.sqrMagnitude > 1f)
            {
                slideDirection = horizVel.normalized;
                currentSlideSpeed = horizVel.magnitude + slideInitialBoost;
            }
            else
            {
                slideDirection = transform.forward;
                currentSlideSpeed = moveSpeed + slideInitialBoost;
            }

            if (vfxSystem != null) vfxSystem.PlaySlideEffect(true);
            TriggerSquashStretch(new Vector3(1.2f, 0.6f, 1.2f), 0.15f);
        }

        private void StopSlide()
        {
            isSliding = false;
            controller.height = normalHeight;
            controller.center = normalCenter;

            if (vfxSystem != null) vfxSystem.PlaySlideEffect(false);
            TriggerSquashStretch(Vector3.one, 0.1f);
        }

        private bool HasOverheadClearance()
        {
            Vector3 rayStart = transform.position + Vector3.up * slideHeight;
            float rayLength = normalHeight - slideHeight + 0.1f;
            return !Physics.Raycast(rayStart, Vector3.up, rayLength, groundLayer);
        }

        private void HandleJump()
        {
            if (jumpBufferTimer > 0 && coyoteTimer > 0)
            {
                ExecuteJump();
            }
        }

        private void ExecuteJump()
        {
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;

            verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * (9.81f * gravityScale));

            if (isSliding)
            {
                currentMoveVelocity += slideDirection * 2.5f;
                StopSlide();
            }

            if (vfxSystem != null) vfxSystem.PlayJumpVFX();
            TriggerSquashStretch(new Vector3(0.75f, 1.35f, 0.75f), 0.15f);
        }

        private void ApplyGravity()
        {
            if (isGrounded && verticalVelocity <= 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity -= 9.81f * gravityScale * Time.deltaTime;
            }
        }

        private void ApplyFinalMovement()
        {
            Vector3 finalVelocity = currentMoveVelocity + Vector3.up * verticalVelocity;
            controller.Move(finalVelocity * Time.deltaTime);
        }

        private void OnLanding()
        {
            if (vfxSystem != null) vfxSystem.PlayLandVFX();
            TriggerSquashStretch(new Vector3(1.3f, 0.7f, 1.3f), 0.12f);
        }

        private void ApplyVisualJuice()
        {
            if (visualModel == null) return;

            if (!isSliding)
            {
                Vector2 input = GetMoveInput();
                float targetLean = -input.x * maxLeanAngle;
                Quaternion currentRot = visualModel.localRotation;
                Quaternion targetRot = Quaternion.Euler(0f, 0f, targetLean);
                visualModel.localRotation = Quaternion.Slerp(currentRot, targetRot, Time.deltaTime * 10f);
            }
        }

        private void TriggerSquashStretch(Vector3 targetScale, float duration)
        {
            if (visualModel == null) return;
            if (squashStretchRoutine != null) StopCoroutine(squashStretchRoutine);
            squashStretchRoutine = StartCoroutine(AnimateSquashStretch(targetScale, duration));
        }

        private IEnumerator AnimateSquashStretch(Vector3 targetScale, float duration)
        {
            Vector3 startScale = visualModel.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                visualModel.localScale = Vector3.Lerp(startScale, Vector3.Scale(originalVisualScale, targetScale), elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            startScale = visualModel.localScale;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                visualModel.localScale = Vector3.Lerp(startScale, originalVisualScale, elapsed / duration);
                yield return null;
            }
            visualModel.localScale = originalVisualScale;
        }
    }
}
