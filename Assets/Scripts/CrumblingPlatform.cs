using System.Collections;
using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// Fragile / Crumbling platform that shakes when stepped on,
    /// collapses after a brief delay, and respawns safely.
    /// </summary>
    public class CrumblingPlatform : MonoBehaviour
    {
        [Header("Crumble Settings")]
        [SerializeField] private float crumbleDelay = 0.4f;
        [SerializeField] private float respawnTime = 2.5f;
        [SerializeField] private float shakeIntensity = 0.08f;

        private Collider2D col2d;
        private Renderer rend;
        private Vector3 startPos;
        private bool isCrumbling;

        private void Awake()
        {
            col2d = GetComponent<Collider2D>();
            rend = GetComponent<Renderer>();
            startPos = transform.position;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isCrumbling) return;

            // Trigger when player lands from above
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponentInParent<PlayerController2D>() != null)
            {
                if (collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f)
                {
                    StartCoroutine(CrumbleSequence());
                }
            }
        }

        private IEnumerator CrumbleSequence()
        {
            isCrumbling = true;

            // Shake phase
            float elapsed = 0f;
            while (elapsed < crumbleDelay)
            {
                elapsed += Time.deltaTime;
                float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
                float offsetY = Random.Range(-shakeIntensity, shakeIntensity);
                transform.position = startPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            // Collapse phase
            transform.position = startPos;
            if (col2d != null) col2d.enabled = false;
            if (rend != null) rend.enabled = false;

            // Wait before respawning
            yield return new WaitForSeconds(respawnTime);

            // Respawn
            if (col2d != null) col2d.enabled = true;
            if (rend != null) rend.enabled = true;
            isCrumbling = false;
        }
    }
}
