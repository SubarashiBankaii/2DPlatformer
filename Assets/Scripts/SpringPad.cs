using System.Collections;
using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// Interactive Spring / Trampoline Pad.
    /// Launches the player upward with bonus spring velocity and bouncy visual animation.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class SpringPad : MonoBehaviour
    {
        [Header("Spring Settings")]
        [SerializeField] private float bounceForce = 22f;
        [SerializeField] private Transform visualModel;

        private Vector3 originalScale = Vector3.one;
        private bool isBouncing;

        private void Awake()
        {
            if (visualModel == null && transform.childCount > 0)
                visualModel = transform.GetChild(0);

            if (visualModel != null)
                originalScale = visualModel.localScale;
            else
                originalScale = transform.localScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBounce(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Only trigger if player lands on top of the spring
            if (collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f)
            {
                TryBounce(collision.gameObject);
            }
        }

        private void TryBounce(GameObject target)
        {
            if (target.CompareTag("Player") || target.GetComponentInParent<PlayerController2D>() != null)
            {
                var player = target.GetComponentInParent<PlayerController2D>();
                if (player != null)
                {
                    player.LaunchUpward(bounceForce);
                    if (!isBouncing) StartCoroutine(BounceAnimation());
                }
            }
        }

        private IEnumerator BounceAnimation()
        {
            isBouncing = true;
            Transform t = visualModel != null ? visualModel : transform;

            // Compress
            float dur = 0.08f;
            float elapsed = 0f;
            Vector3 compressScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 0.4f, originalScale.z);
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(originalScale, compressScale, elapsed / dur);
                yield return null;
            }

            // Pop stretch
            dur = 0.12f;
            elapsed = 0f;
            Vector3 stretchScale = new Vector3(originalScale.x * 0.8f, originalScale.y * 1.4f, originalScale.z);
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(compressScale, stretchScale, elapsed / dur);
                yield return null;
            }

            // Return to normal
            dur = 0.1f;
            elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(stretchScale, originalScale, elapsed / dur);
                yield return null;
            }

            t.localScale = originalScale;
            isBouncing = false;
        }
    }
}
