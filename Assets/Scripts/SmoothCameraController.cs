using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer3D
{
    /// <summary>
    /// Senior-level 3D Third-Person Orbit Camera Controller.
    /// Fully optimized for Unity's New Input System (com.unity.inputsystem).
    /// Features:
    /// - Mouse orbit controls with smooth damping.
    /// - Collision detection (prevents camera clipping through geometry).
    /// - Dynamic FOV based on player velocity and slide state.
    /// - Auto target discovery.
    /// </summary>
    public class SmoothCameraController : MonoBehaviour
    {
        [Header("Target & Offset Settings")]
        [Tooltip("Target transform to follow (usually the Player).")]
        [SerializeField] private Transform target;
        
        [Tooltip("Height offset above player position to orbit around.")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);
        
        [Tooltip("Default distance behind player.")]
        [SerializeField] private float defaultDistance = 5.5f;

        [Header("Orbit & Rotation Sensitivity")]
        [Tooltip("Horizontal mouse look sensitivity.")]
        [SerializeField] private float sensitivityX = 2.5f;
        
        [Tooltip("Vertical mouse look sensitivity.")]
        [SerializeField] private float sensitivityY = 2.0f;
        
        [Tooltip("Minimum pitch angle (looking up).")]
        [SerializeField] private float minPitch = -25f;
        
        [Tooltip("Maximum pitch angle (looking down).")]
        [SerializeField] private float maxPitch = 70f;
        
        [Tooltip("Smooth time for camera rotation damping.")]
        [SerializeField] private float rotationSmoothTime = 0.05f;
        
        [Tooltip("Smooth time for camera position damping.")]
        [SerializeField] private float positionSmoothTime = 0.04f;

        [Header("Obstacle Collision Detection")]
        [Tooltip("Layers camera will collide with (e.g. Ground, Buildings).")]
        [SerializeField] private LayerMask collisionLayers = ~0;
        
        [Tooltip("Radius of spherecast used to test for obstacles.")]
        [SerializeField] private float collisionRadius = 0.25f;
        
        [Tooltip("Minimum distance camera can get to target when obstructed.")]
        [SerializeField] private float minDistance = 1.0f;

        [Header("Dynamic FOV & Speed Effects")]
        [Tooltip("Normal Field of View.")]
        [SerializeField] private float normalFOV = 60f;
        
        [Tooltip("Field of View when sliding or sprinting.")]
        [SerializeField] private float maxSpeedFOV = 70f;
        
        [Tooltip("Speed at which FOV transitions.")]
        [SerializeField] private float fovDampSpeed = 5f;

        private Camera cam;
        private PlayerController playerController;

        private float currentYaw;
        private float currentPitch = 15f;

        private Vector3 currentPosVelocity;
        private float currentDistance;
        private float distanceDampVelocity;

        public Transform Target { get => target; set => target = value; }

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            currentDistance = defaultDistance;

            // Lock and hide cursor for smooth third-person controls
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            FindTargetIfMissing();
        }

        private void FindTargetIfMissing()
        {
            if (target == null)
            {
                var player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (target != null)
            {
                playerController = target.GetComponentInParent<PlayerController>();
                currentYaw = target.eulerAngles.y;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTargetIfMissing();
                if (target == null) return;
            }

            HandleInput();
            CalculateCameraTransform();
            HandleDynamicFOV();
        }

        private Vector2 GetMouseInput()
        {
            Vector2 delta = Vector2.zero;

            // 1. Primary: New Input System Mouse
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 rawDelta = mouse.delta.ReadValue();
                delta.x = rawDelta.x * 0.08f * sensitivityX;
                delta.y = rawDelta.y * 0.08f * sensitivityY;
            }

            // 2. Fallback: Legacy Input (Safely wrapped)
            if (delta.sqrMagnitude < 0.0001f)
            {
                try
                {
                    delta.x = Input.GetAxis("Mouse X") * sensitivityX;
                    delta.y = Input.GetAxis("Mouse Y") * sensitivityY;
                }
                catch { }
            }

            return delta;
        }

        private void HandleInput()
        {
            // Toggle cursor lock with ESC using New Input System primary
            bool escPressed = false;
            var kbd = Keyboard.current;
            if (kbd != null && kbd.escapeKey.wasPressedThisFrame)
            {
                escPressed = true;
            }
            else
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
                }
                catch { }
            }

            if (escPressed)
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseDelta = GetMouseInput();
                currentYaw += mouseDelta.x;
                currentPitch -= mouseDelta.y;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }
        }

        private void CalculateCameraTransform()
        {
            Vector3 focusPoint = target.position + targetOffset;

            // Calculate rotation Quaternion from Pitch & Yaw angles
            Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            // Compute ideal position behind focus point
            Vector3 desiredPosition = focusPoint - (targetRotation * Vector3.forward * defaultDistance);

            // SphereCast Collision check between focus point and desired position
            float targetDistance = defaultDistance;
            Vector3 rayDir = (desiredPosition - focusPoint).normalized;

            if (Physics.SphereCast(focusPoint, collisionRadius, rayDir, out RaycastHit hit, defaultDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                // Pull camera in closer to prevent clipping into walls
                targetDistance = Mathf.Clamp(hit.distance - 0.1f, minDistance, defaultDistance);
            }

            // Smooth distance transition
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceDampVelocity, 0.04f);

            // Calculate final smoothed camera position
            Vector3 finalPosition = focusPoint - (targetRotation * Vector3.forward * currentDistance);
            transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref currentPosVelocity, positionSmoothTime);

            // Look directly at target focus point
            transform.rotation = Quaternion.LookRotation(focusPoint - transform.position);
        }

        private void HandleDynamicFOV()
        {
            if (cam == null) return;

            float targetFOV = normalFOV;
            if (playerController != null)
            {
                if (playerController.IsSliding || playerController.CurrentSpeed > 10f)
                {
                    targetFOV = maxSpeedFOV;
                }
            }

            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovDampSpeed);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                playerController = target.GetComponentInParent<PlayerController>();
                currentYaw = target.eulerAngles.y;
            }
        }
    }
}
