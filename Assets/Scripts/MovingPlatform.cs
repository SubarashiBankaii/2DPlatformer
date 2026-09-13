using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// Smooth Kinematic Moving Platform.
    /// Moves using Rigidbody2D.MovePosition in FixedUpdate for flawless physics.
    /// Exposes velocity so PlayerController2D can ride it smoothly without parenting bugs.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector2 moveDirection = Vector2.right;
        [SerializeField] private float moveDistance = 5f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float pauseTime = 0.3f;

        private Rigidbody2D rb;
        private Vector2 startPos;
        private Vector2 endPos;
        private float progress;
        private int direction = 1;
        private float pauseTimer;
        private Vector2 lastPos;

        public Vector2 Velocity { get; private set; }

        public Vector2 MoveDirection { get => moveDirection; set => moveDirection = value; }
        public float MoveDistance { get => moveDistance; set => moveDistance = value; }
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
        public float PauseTime { get => pauseTime; set => pauseTime = value; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Start()
        {
            startPos = transform.position;
            endPos = startPos + moveDirection.normalized * moveDistance;
            lastPos = rb.position;
        }

        private void FixedUpdate()
        {
            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.fixedDeltaTime;
                Velocity = Vector2.zero;
                return;
            }

            progress += direction * moveSpeed * Time.fixedDeltaTime / moveDistance;

            if (progress >= 1f)
            {
                progress = 1f;
                direction = -1;
                pauseTimer = pauseTime;
            }
            else if (progress <= 0f)
            {
                progress = 0f;
                direction = 1;
                pauseTimer = pauseTime;
            }

            // Smooth easing curve
            float eased = progress * progress * (3f - 2f * progress);
            Vector2 targetPos = Vector2.Lerp(startPos, endPos, eased);

            Velocity = (targetPos - rb.position) / Time.fixedDeltaTime;
            rb.MovePosition(targetPos);
        }
    }
}
