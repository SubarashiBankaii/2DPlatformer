using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// Trigger attached to the final goal flag/banner.
    /// Triggers WinManager2D when player reaches the finish line.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class GoalTrigger2D : MonoBehaviour
    {
        private bool isTriggered = false;

        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isTriggered) return;

            var player = collision.GetComponent<PlayerController2D>();
            if (player != null)
            {
                isTriggered = true;
                Debug.Log("<color=yellow><b>[GOAL REACHED!] Player reached the finish line!</b></color>");

                if (WinManager2D.Instance != null)
                {
                    WinManager2D.Instance.ShowWinUI();
                }
            }
        }
    }
}
