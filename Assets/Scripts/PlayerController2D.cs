using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer2D
{
    /// <summary>
    /// Bulletproof 2D Platformer Player Controller.
    /// Features: Run, Sprint, Double Jump, Short Slide, Coyote Time, Variable Jump Cut,
    /// Wall-to-Wall Jump (wall kick climbing), and Mid-Air Dash.
    /// Integrates smoothly with Moving Platforms, Spring Pads, Speed Boosters, and Checkpoints.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float sprintMultiplier = 1.35f;
        [SerializeField] private float acceleration = 55f;
        [SerializeField] private float deceleration = 45f;
        [SerializeField] private float airControl = 0.8f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 14f;
        [SerializeField] private float doubleJumpForce = 12f;
        [SerializeField] private float gravityScale = 3f;
        [SerializeField] private float fallMultiplier = 1.5f;
        [SerializeField] private float jumpCutMultiplier = 0.4f;
        [SerializeField] private float coyoteTime = 0.1f;

        [Header("Wall Jump")]
        [SerializeField] private float wallJumpHorizontalForce = 11f;

        [Header("Air Dash")]
        [SerializeField] private float dashSpeed = 22f;
        [SerializeField] private float dashDuration = 0.16f;

        [Header("Slide")]
        [SerializeField] private float slideSpeed = 16f;
        [SerializeField] private float slideDuration = 0.4f;
        [SerializeField] private float slideHeightRatio = 0.5f;

        [Header("Ground (max angle from UP that counts as floor)")]
        [SerializeField] private float maxGroundAngle = 60f;

        [Header("Visual")]
        [SerializeField] private Transform visualModel;
        [SerializeField] private float leanAmount = 8f;

        [Header("Respawn & Checkpoints")]
        [SerializeField] private float fallDeathY = -8f;

        // Components
        private Rigidbody2D rb;
        private CapsuleCollider2D capsule;
        private PlayerVFX2D vfx;

        // Core State
        private float moveInput;
        private bool facingRight = true;
        private bool isGrounded;
        private bool canDoubleJump;
        private float coyoteTimer;

        // Wall Jump State
        private int touchingWallDir; // -1 for Left Wall, 1 for Right Wall, 0 for None
        private int lastWallJumpDir;  // Tracks direction of last wall jumped from (-1 or 1)

        // Mid-Air Dash State
        private bool canAirDash;
        private bool isDashing;
        private float dashTimer;
        private int dashDir;

        // Slide State
        private bool isSliding;
        private float slideTimer;
        private int slideDir;
        private Vector2 normalSize;
        private Vector2 normalOffset;

        // Ground contact tracking
        private HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
        private MovingPlatform activeMovingPlatform;
        private Vector3 activeCheckpointPos;

        // Visual
        private Vector3 baseScale = Vector3.one;
        private float currentLean;
        private Coroutine squashCo;

        // Public Accessors
        public bool IsGrounded => isGrounded;
        public bool IsSliding => isSliding;
        public bool IsDashing => isDashing;
        public int FacingDirection => facingRight ? 1 : -1;
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            vfx = GetComponent<PlayerVFX2D>();

            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // No-stick walls
            var mat = new PhysicsMaterial2D("NoFriction");
            mat.friction = 0f;
            mat.bounciness = 0f;
            capsule.sharedMaterial = mat;
            rb.sharedMaterial = mat;

            normalSize = capsule.size;
            normalOffset = capsule.offset;

            if (visualModel == null && transform.childCount > 0)
                visualModel = transform.GetChild(0);
            if (visualModel != null)
                baseScale = visualModel.localScale;

            activeCheckpointPos = transform.position;
        }

        public void SetCheckpoint(Vector3 checkpointPos)
        {
            activeCheckpointPos = checkpointPos;
        }

        // ── UPDATE ──

        private void Update()
        {
            // Check fall death -> respawn at last valid checkpoint
            if (transform.position.y < fallDeathY)
            {
                RespawnAtCheckpoint();
                return;
            }

            // Ground state from collision contacts
            bool wasGrounded = isGrounded;
            isGrounded = groundContacts.Count > 0;

            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
                canDoubleJump = true;
                canAirDash = true;
                isDashing = false;
                touchingWallDir = 0;
                lastWallJumpDir = 0; // Reset wall jump tracking on ground!
                if (!wasGrounded && rb.linearVelocity.y < -1f)
                    OnLand();
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
                activeMovingPlatform = null;
            }

            // Update Dash Timer
            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f)
                {
                    isDashing = false;
                    rb.linearVelocity = new Vector2(dashDir * moveSpeed, rb.linearVelocity.y);
                }
            }

            ReadInput();
            UpdateSlide();
            UpdateVisuals();
        }

        private void RespawnAtCheckpoint()
        {
            if (isSliding) StopSlide();
            isDashing = false;
            touchingWallDir = 0;
            transform.position = activeCheckpointPos;
            rb.linearVelocity = Vector2.zero;
            groundContacts.Clear();
            activeMovingPlatform = null;
            coyoteTimer = 0f;
            isGrounded = true;
            canAirDash = true;
        }

        private void FixedUpdate()
        {
            if (isDashing)
            {
                rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
                return;
            }

            Vector2 platformVelocity = Vector2.zero;
            if (isGrounded && activeMovingPlatform != null)
            {
                platformVelocity = activeMovingPlatform.Velocity;
            }

            // Horizontal movement
            if (!isSliding)
            {
                bool sprint = false;
                var kbd = Keyboard.current;
                if (kbd != null && (kbd.leftShiftKey.isPressed || kbd.rightShiftKey.isPressed))
                    sprint = true;

                float target = moveInput * moveSpeed * (sprint ? sprintMultiplier : 1f);
                float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
                if (!isGrounded) rate *= airControl;

                float vx = Mathf.MoveTowards(rb.linearVelocity.x - platformVelocity.x, target, rate * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(vx + platformVelocity.x, rb.linearVelocity.y);
            }
            else
            {
                // Slide velocity on moving platform
                rb.linearVelocity = new Vector2(slideDir * slideSpeed + platformVelocity.x, rb.linearVelocity.y);
            }

            // Fall gravity boost
            rb.gravityScale = rb.linearVelocity.y < 0f
                ? gravityScale * fallMultiplier
                : gravityScale;
        }

        // ── COLLISION GROUND & WALL DETECTION ──

        private bool IsGroundNormal(Collision2D col)
        {
            for (int i = 0; i < col.contactCount; i++)
            {
                if (Vector2.Angle(col.GetContact(i).normal, Vector2.up) <= maxGroundAngle)
                    return true;
            }
            return false;
        }

        private void UpdateWallContact(Collision2D col)
        {
            if (isGrounded)
            {
                touchingWallDir = 0;
                return;
            }

            for (int i = 0; i < col.contactCount; i++)
            {
                Vector2 normal = col.GetContact(i).normal;
                if (normal.x > 0.5f)
                {
                    touchingWallDir = -1; // Collided with Left Wall
                    return;
                }
                else if (normal.x < -0.5f)
                {
                    touchingWallDir = 1; // Collided with Right Wall
                    return;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (IsGroundNormal(col))
            {
                groundContacts.Add(col.collider);
                if (col.gameObject.TryGetComponent<MovingPlatform>(out var platform))
                {
                    activeMovingPlatform = platform;
                }
            }
            else
            {
                UpdateWallContact(col);
            }
        }

        private void OnCollisionStay2D(Collision2D col)
        {
            if (IsGroundNormal(col))
            {
                groundContacts.Add(col.collider);
                if (col.gameObject.TryGetComponent<MovingPlatform>(out var platform))
                {
                    activeMovingPlatform = platform;
                }
            }
            else
            {
                groundContacts.Remove(col.collider);
                UpdateWallContact(col);
            }
        }

        private void OnCollisionExit2D(Collision2D col)
        {
            groundContacts.Remove(col.collider);
            if (col.gameObject.TryGetComponent<MovingPlatform>(out var platform) && platform == activeMovingPlatform)
            {
                activeMovingPlatform = null;
            }
            touchingWallDir = 0;
        }

        // ── INPUT ──

        private void ReadInput()
        {
            Keyboard kbd = Keyboard.current;
            Gamepad gp = Gamepad.current;

            // Move
            moveInput = 0f;
            if (kbd != null)
            {
                if (kbd.dKey.isPressed || kbd.rightArrowKey.isPressed) moveInput += 1f;
                if (kbd.aKey.isPressed || kbd.leftArrowKey.isPressed) moveInput -= 1f;
            }
            if (gp != null)
            {
                float s = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(s) > Mathf.Abs(moveInput)) moveInput = s;
            }

            // Mid-Air Dash Input (E Key, Right Shift, or Gamepad Shoulder/Trigger)
            bool dashPressed = false;
            if (kbd != null && (kbd.eKey.wasPressedThisFrame || kbd.rightShiftKey.wasPressedThisFrame)) dashPressed = true;
            if (gp != null && (gp.rightShoulder.wasPressedThisFrame || gp.rightTrigger.wasPressedThisFrame)) dashPressed = true;

            if (dashPressed && !isGrounded && canAirDash && !isDashing)
            {
                StartDash();
                return;
            }

            // Jump
            bool jumpDown = false;
            if (kbd != null && kbd.spaceKey.wasPressedThisFrame) jumpDown = true;
            if (gp != null && gp.buttonSouth.wasPressedThisFrame) jumpDown = true;

            if (jumpDown)
            {
                if (isSliding)
                {
                    int dir = slideDir;
                    StopSlide();
                    DoJump(jumpForce);
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x + dir * 4f, rb.linearVelocity.y);
                }
                else if (!isGrounded && touchingWallDir != 0 && touchingWallDir != lastWallJumpDir)
                {
                    // Wall Jump! Kick off away from wall towards opposite wall
                    DoWallJump();
                }
                else if (coyoteTimer > 0f)
                {
                    DoJump(jumpForce);
                    canDoubleJump = true;
                }
                else if (canDoubleJump)
                {
                    DoJump(doubleJumpForce);
                    canDoubleJump = false;
                }
            }

            // Variable jump height
            bool jumpUp = false;
            if (kbd != null && kbd.spaceKey.wasReleasedThisFrame) jumpUp = true;
            if (gp != null && gp.buttonSouth.wasReleasedThisFrame) jumpUp = true;
            if (jumpUp && rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            // Slide
            bool slideDown = false;
            if (kbd != null && (kbd.cKey.wasPressedThisFrame || kbd.leftCtrlKey.wasPressedThisFrame
                || kbd.sKey.wasPressedThisFrame || kbd.downArrowKey.wasPressedThisFrame))
                slideDown = true;
            if (gp != null && gp.buttonEast.wasPressedThisFrame) slideDown = true;

            if (slideDown && isGrounded && !isSliding)
                StartSlide();

            // Facing
            if (!isSliding)
            {
                if (moveInput > 0.05f && !facingRight) SetFacing(true);
                else if (moveInput < -0.05f && facingRight) SetFacing(false);
            }
        }

        // ── AIR DASH ──

        private void StartDash()
        {
            isDashing = true;
            canAirDash = false;
            dashTimer = dashDuration;
            dashDir = facingRight ? 1 : -1;
            if (Mathf.Abs(moveInput) > 0.1f) dashDir = (int)Mathf.Sign(moveInput);

            if (vfx != null) vfx.PlayDashVFX();
            Squash(new Vector3(1.4f, 0.6f, 1f), 0.12f);
        }

        // ── WALL JUMP ──

        private void DoWallJump()
        {
            int wallSide = touchingWallDir;
            lastWallJumpDir = wallSide; // Lock out jumping off this same wall direction again until touching ground or opposite wall
            touchingWallDir = 0;

            // Launch UP and AWAY from wall
            float kickX = -wallSide * wallJumpHorizontalForce;
            rb.linearVelocity = new Vector2(kickX, jumpForce * 1.05f);

            SetFacing(wallSide < 0); // Face direction of flight
            coyoteTimer = 0f;
            canDoubleJump = true; // Refresh double jump so player can wall-jump again onto the next wall!
            canAirDash = true;    // Refresh air dash on wall kick

            if (vfx != null) vfx.PlayJumpVFX();
            Squash(new Vector3(0.7f, 1.4f, 1f), 0.12f);
        }

        // ── JUMP ──

        private void DoJump(float force)
        {
            coyoteTimer = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            groundContacts.Clear();

            if (vfx != null) vfx.PlayJumpVFX();
            Squash(new Vector3(0.7f, 1.4f, 1f), 0.12f);
        }

        // ── GIMMICK INTERACTION HELPERS ──

        public void LaunchUpward(float force)
        {
            DoJump(force);
            canDoubleJump = true;
            canAirDash = true;
        }

        public void ApplySpeedBoost(Vector2 impulse)
        {
            rb.linearVelocity = impulse;
            canAirDash = true;
        }

        // ── SLIDE ──

        private void StartSlide()
        {
            isSliding = true;
            slideTimer = slideDuration;
            slideDir = facingRight ? 1 : -1;

            if (Mathf.Abs(rb.linearVelocity.x) > 1f)
                slideDir = (int)Mathf.Sign(rb.linearVelocity.x);

            capsule.size = new Vector2(normalSize.x, normalSize.y * slideHeightRatio);
            capsule.offset = new Vector2(normalOffset.x,
                normalOffset.y - normalSize.y * (1f - slideHeightRatio) * 0.5f);

            if (vfx != null) vfx.PlaySlideEffect(true);
            Squash(new Vector3(1.3f, 0.6f, 1f), 0.12f);
        }

        private void UpdateSlide()
        {
            if (!isSliding) return;

            slideTimer -= Time.deltaTime;

            if (slideTimer <= 0f && CanStandUp())
                StopSlide();
        }

        private void StopSlide()
        {
            isSliding = false;
            capsule.size = normalSize;
            capsule.offset = normalOffset;
            if (vfx != null) vfx.PlaySlideEffect(false);
            Squash(Vector3.one, 0.1f);
        }

        private bool CanStandUp()
        {
            Vector2 start = (Vector2)transform.position + capsule.offset;
            float dist = normalSize.y * (1f - slideHeightRatio) + 0.1f;
            RaycastHit2D[] hits = Physics2D.RaycastAll(start, Vector2.up, dist);
            foreach (var h in hits)
            {
                if (h.collider != null && h.collider.gameObject != gameObject
                    && !h.collider.transform.IsChildOf(transform))
                    return false;
            }
            return true;
        }

        // ── LANDING ──

        private void OnLand()
        {
            if (vfx != null) vfx.PlayLandVFX();
            Squash(new Vector3(1.3f, 0.7f, 1f), 0.1f);
        }

        // ── FACING ──

        private void SetFacing(bool right)
        {
            facingRight = right;
            ApplyRotation();
        }

        // ── VISUALS ──

        private void UpdateVisuals()
        {
            if (visualModel == null || isSliding) return;
            float target = -moveInput * leanAmount;
            currentLean = Mathf.Lerp(currentLean, target, Time.deltaTime * 12f);
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            if (visualModel == null) return;
            float y = facingRight ? 0f : 180f;
            float z = facingRight ? currentLean : -currentLean;
            visualModel.localRotation = Quaternion.Euler(0f, y, z);
        }

        // ── SQUASH & STRETCH ──

        private void Squash(Vector3 target, float dur)
        {
            if (visualModel == null) return;
            if (squashCo != null) StopCoroutine(squashCo);
            squashCo = StartCoroutine(SquashAnim(target, dur));
        }

        private IEnumerator SquashAnim(Vector3 target, float dur)
        {
            Vector3 from = visualModel.localScale;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                visualModel.localScale = Vector3.Lerp(from, Vector3.Scale(baseScale, target), t / dur);
                yield return null;
            }
            from = visualModel.localScale;
            t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                visualModel.localScale = Vector3.Lerp(from, baseScale, t / dur);
                yield return null;
            }
            visualModel.localScale = baseScale;
        }
    }
}
