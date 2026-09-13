using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// A door that opens and closes on a timer.
    /// When closed, it blocks the player. When open, the player can pass.
    /// Visual: scales Y to 0 when open, full height when closed.
    /// </summary>
    public class TimingDoor : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("How long the door stays OPEN (seconds).")]
        [SerializeField] private float openTime = 1.5f;

        [Tooltip("How long the door stays CLOSED (seconds).")]
        [SerializeField] private float closeTime = 2.5f;

        [Tooltip("Time offset so doors don't all sync (seconds).")]
        [SerializeField] private float startDelay = 0f;

        [Header("Animation")]
        [SerializeField] private float animSpeed = 6f;

        private BoxCollider2D col;
        private Vector3 closedScale;
        private Vector3 openScale;
        private float timer;
        private bool isOpen;

        // Warning indicator
        private SpriteRenderer warningLight;

        public float OpenTime { get => openTime; set => openTime = value; }
        public float CloseTime { get => closeTime; set => closeTime = value; }
        public float StartDelay { get => startDelay; set => startDelay = value; }

        private void Awake()
        {
            col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();

            closedScale = transform.localScale;
            openScale = new Vector3(closedScale.x, 0.05f, closedScale.z);

            timer = -startDelay; // Negative timer acts as delay
            isOpen = false;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer < 0f) return; // Still in start delay

            float cycleTime = openTime + closeTime;
            float cyclePos = timer % cycleTime;

            bool shouldBeOpen = cyclePos < openTime;

            if (shouldBeOpen != isOpen)
            {
                isOpen = shouldBeOpen;
                if (col != null) col.enabled = !isOpen;
            }

            // Smooth open/close animation
            Vector3 target = isOpen ? openScale : closedScale;
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * animSpeed);

            // Color feedback: green = open, red = closed, yellow = about to change
            float timeLeft;
            if (isOpen)
                timeLeft = openTime - cyclePos;
            else
                timeLeft = cycleTime - cyclePos;

            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                if (isOpen)
                    rend.material.color = timeLeft < 0.5f ? new Color(1f, 0.8f, 0f) : new Color(0.2f, 0.9f, 0.3f);
                else
                    rend.material.color = timeLeft < 0.5f ? new Color(1f, 0.8f, 0f) : new Color(0.9f, 0.2f, 0.2f);
            }
        }
    }
}
