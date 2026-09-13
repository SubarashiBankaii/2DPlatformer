using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// Checkpoint trigger that saves player position.
    /// Changes color when activated to give clear visual feedback.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Checkpoint2D : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        [SerializeField] private Color activeColor = new Color(0.2f, 0.9f, 0.4f, 1.0f);

        private bool isActivated = false;
        private Renderer rend;

        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            rend = GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = inactiveColor;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isActivated) return;

            var player = collision.GetComponent<PlayerController2D>();
            if (player != null)
            {
                isActivated = true;
                if (rend != null)
                    rend.material.color = activeColor;

                // Spawn position slightly above checkpoint base so player lands safely
                Vector3 spawnPos = transform.position + new Vector3(0f, 0.5f, 0f);
                player.SetCheckpoint(spawnPos);
                Debug.Log($"<color=green>[CHECKPOINT] Activated at {spawnPos}</color>");
            }
        }

        public void ResetCheckpoint()
        {
            isActivated = false;
            if (rend != null)
                rend.material.color = inactiveColor;
        }
    }
}
