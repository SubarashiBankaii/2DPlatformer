using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// Senior-level 2D Side-Scrolling Platformer Camera Controller.
    /// Features:
    /// - Ultra-smooth X/Y target tracking with Vector3.SmoothDamp.
    /// - Dynamic directional lookahead (shifts ahead when player runs left/right).
    /// - Vertical offset & damping.
    /// - Dynamic Orthographic Zoom during sprint/slide.
    /// </summary>
    public class SmoothCamera2D : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Target transform to follow (usually Player).")]
        [SerializeField] private Transform target;
        
        [Tooltip("Offset relative to player center.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);

        [Header("Smooth Damping Settings")]
        [Tooltip("Time for camera to catch up to target horizontally.")]
        [SerializeField] private float smoothTimeX = 0.12f;
        
        [Tooltip("Time for camera to catch up to target vertically.")]
        [SerializeField] private float smoothTimeY = 0.18f;

        [Header("Directional Lookahead")]
        [Tooltip("Distance camera shifts ahead in player's facing direction.")]
        [SerializeField] private float lookAheadDistance = 2.5f;
        
        [Tooltip("Speed at which lookahead shifts direction.")]
        [SerializeField] private float lookAheadDampSpeed = 4f;

        [Header("Dynamic Orthographic Zoom")]
        [Tooltip("Default Orthographic Size.")]
        [SerializeField] private float defaultOrthoSize = 6.5f;
        
        [Tooltip("Orthographic Size when sprinting or sliding.")]
        [SerializeField] private float maxSpeedOrthoSize = 7.5f;
        
        [Tooltip("Zoom damp speed.")]
        [SerializeField] private float zoomDampSpeed = 3f;

        private Camera cam;
        private PlayerController2D playerController;

        private Vector3 currentVelocity;
        private float currentLookAheadX;
        private float targetLookAheadX;

        public Transform Target { get => target; set => target = value; }

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            if (cam != null && !cam.orthographic)
            {
                cam.orthographic = true;
                cam.orthographicSize = defaultOrthoSize;
            }

            FindTargetIfMissing();
        }

        private void FindTargetIfMissing()
        {
            if (target == null)
            {
                var player = Object.FindFirstObjectByType<PlayerController2D>();
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (target != null)
            {
                playerController = target.GetComponentInParent<PlayerController2D>();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTargetIfMissing();
                if (target == null) return;
            }

            CalculateCameraPosition();
            HandleDynamicZoom();
        }

        private void CalculateCameraPosition()
        {
            // Determine lookahead shift based on player facing / velocity
            int facingDir = playerController != null ? playerController.FacingDirection : 1;
            float moveX = playerController != null ? playerController.Velocity.x : 0f;

            if (Mathf.Abs(moveX) > 0.5f)
            {
                targetLookAheadX = Mathf.Sign(moveX) * lookAheadDistance;
            }
            else
            {
                targetLookAheadX = facingDir * lookAheadDistance * 0.5f;
            }

            currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAheadX, Time.deltaTime * lookAheadDampSpeed);

            Vector3 targetPos = target.position + offset + new Vector3(currentLookAheadX, 0f, 0f);

            // Separate X and Y smooth damping for ultra-stable platformer tracking
            float newX = Mathf.SmoothDamp(transform.position.x, targetPos.x, ref currentVelocity.x, smoothTimeX);
            float newY = Mathf.SmoothDamp(transform.position.y, targetPos.y, ref currentVelocity.y, smoothTimeY);

            transform.position = new Vector3(newX, newY, offset.z);
        }

        private void HandleDynamicZoom()
        {
            if (cam == null || !cam.orthographic) return;

            float targetSize = defaultOrthoSize;
            if (playerController != null)
            {
                if (playerController.IsSliding || Mathf.Abs(playerController.Velocity.x) > 11f)
                {
                    targetSize = maxSpeedOrthoSize;
                }
            }

            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomDampSpeed);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                playerController = target.GetComponentInParent<PlayerController2D>();
            }
        }
    }
}
