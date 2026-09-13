using UnityEngine;
using UnityEngine.UI;

namespace Platformer2D
{
    /// <summary>
    /// Live Speedrun Stopwatch & Medal Evaluation Manager.
    /// Draws clean top-center HUD timer and tracks Personal Best persistent times.
    /// </summary>
    public class SpeedrunTimer2D : MonoBehaviour
    {
        public static SpeedrunTimer2D Instance { get; private set; }

        [Header("Medal Target Times (Seconds)")]
        [SerializeField] private float goldTargetTime = 28f;
        [SerializeField] private float silverTargetTime = 38f;
        [SerializeField] private float bronzeTargetTime = 50f;

        private float currentRunTime;
        private bool isRunning = true;

        // UI
        private GameObject hudCanvas;
        private Text timerText;
        private Text pbText;

        public float CurrentRunTime => currentRunTime;
        public float GoldTargetTime => goldTargetTime;
        public float SilverTargetTime => silverTargetTime;
        public float BronzeTargetTime => bronzeTargetTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CreateHUD();
            ResetTimer();
        }

        private void Update()
        {
            if (isRunning)
            {
                currentRunTime += Time.deltaTime;
                UpdateHUDText();
            }
        }

        public void ResetTimer()
        {
            currentRunTime = 0f;
            isRunning = true;
            UpdateHUDText();
        }

        public void StopTimer()
        {
            isRunning = false;
        }

        public static string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds <= 0f || timeInSeconds >= 9999f) return "--:--.--";
            int minutes = (int)(timeInSeconds / 60f);
            int seconds = (int)(timeInSeconds % 60f);
            int fraction = (int)((timeInSeconds * 100f) % 100f);
            return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, fraction);
        }

        public string GetMedalEarned(float runTime)
        {
            if (runTime <= goldTargetTime) return "GOLD";
            if (runTime <= silverTargetTime) return "SILVER";
            if (runTime <= bronzeTargetTime) return "BRONZE";
            return "CLEAR";
        }

        public float GetPersonalBest()
        {
            return PlayerPrefs.GetFloat("PB_Speedrun_Level1", 9999f);
        }

        public bool SavePersonalBest(float time)
        {
            float pb = GetPersonalBest();
            if (time < pb)
            {
                PlayerPrefs.SetFloat("PB_Speedrun_Level1", time);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        private void CreateHUD()
        {
            if (hudCanvas != null) return;

            hudCanvas = new GameObject("SpeedrunHUDCanvas");
            Canvas canvas = hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = hudCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);

            // Container Panel
            GameObject panelObj = new GameObject("TimerPanel");
            panelObj.transform.SetParent(hudCanvas.transform, false);
            Image panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.12f, 0.18f, 0.75f);

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -15f);
            panelRect.sizeDelta = new Vector2(260f, 65f);

            // Live Timer Text
            GameObject textObj = new GameObject("TimerText");
            textObj.transform.SetParent(panelObj.transform, false);
            timerText = textObj.AddComponent<Text>();
            timerText.font = font;
            timerText.fontSize = 28;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.color = new Color(1.0f, 0.9f, 0.2f); // Golden yellow

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(0f, 20f);
            textRect.offsetMax = Vector2.zero;

            // PB Subtext
            GameObject pbObj = new GameObject("PBText");
            pbObj.transform.SetParent(panelObj.transform, false);
            pbText = pbObj.AddComponent<Text>();
            pbText.font = font;
            pbText.fontSize = 16;
            pbText.alignment = TextAnchor.MiddleCenter;
            pbText.color = new Color(0.7f, 0.8f, 0.9f);

            RectTransform pbRect = pbObj.GetComponent<RectTransform>();
            pbRect.anchorMin = Vector2.zero;
            pbRect.anchorMax = Vector2.one;
            pbRect.offsetMin = Vector2.zero;
            pbRect.offsetMax = new Vector2(0f, -40f);

            UpdateHUDText();
        }

        private void UpdateHUDText()
        {
            if (timerText != null)
            {
                timerText.text = FormatTime(currentRunTime);
            }
            if (pbText != null)
            {
                float pb = GetPersonalBest();
                pbText.text = "BEST: " + FormatTime(pb);
            }
        }
    }
}
