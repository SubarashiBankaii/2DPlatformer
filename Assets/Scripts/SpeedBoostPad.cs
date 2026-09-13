using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// Interactive Speed Boost Strip / Dash Pad.
    /// Accelerates player horizontally with a burst of impulse velocity.
    /// </summary>
    public class SpeedBoostPad : MonoBehaviour
    {
        [Header("Boost Settings")]
        [SerializeField] private float boostSpeed = 24f;
        [SerializeField] private Vector2 boostDirection = Vector2.right;

        private void OnTriggerEnter2D(Collider2D other)
        {
            ApplyBoost(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            ApplyBoost(collision.gameObject);
        }

        private void ApplyBoost(GameObject target)
        {
            var player = target.GetComponentInParent<PlayerController2D>();
            if (player != null)
            {
                player.ApplySpeedBoost(boostDirection.normalized * boostSpeed);
            }
        }
    }
}
