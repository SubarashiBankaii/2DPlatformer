using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Platformer2D
{
    /// <summary>
    /// Manages the "YOU WIN!" UI popup, Speedrun Time & Medal calculation,
    /// Personal Best persistent storage, and level restart.
    /// </summary>
    public class WinManager2D : MonoBehaviour
    {
        public static WinManager2D Instance { get; private set; }

        private GameObject winCanvas;
        private Text titleText;
        private Text timeSummaryText;
        private Text medalBadgeText;
        private Text pbText;

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
            CreateWinUI();
        }

        public void CreateWinUI()
        {
            if (winCanvas != null) return;

            // Ensure EventSystem exists and uses InputSystemUIInputModule
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
            }

            var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null) DestroyImmediate(legacyModule);

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            // Canvas setup
            winCanvas = new GameObject("WinCanvas");
            Canvas canvas = winCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = winCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            winCanvas.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 36);

            // Backdrop
            GameObject backdropObj = new GameObject("Backdrop");
            backdropObj.transform.SetParent(winCanvas.transform, false);
            Image backdropImg = backdropObj.AddComponent<Image>();
            backdropImg.color = new Color(0.04f, 0.06f, 0.1f, 0.85f);

            RectTransform backdropRect = backdropObj.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.sizeDelta = Vector2.zero;

            // Central Card
            GameObject cardObj = new GameObject("WinCard");
            cardObj.transform.SetParent(backdropObj.transform, false);
            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.color = new Color(0.11f, 0.15f, 0.22f, 0.95f);

            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(540f, 420f);
            cardRect.anchoredPosition = Vector2.zero;

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(cardObj.transform, false);
            titleText = titleObj.AddComponent<Text>();
            titleText.text = "COURSE CLEAR!";
            titleText.font = font;
            titleText.fontSize = 46;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1.0f, 0.85f, 0.15f);

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(500f, 60f);
            titleRect.anchoredPosition = new Vector2(0f, 150f);

            // Medal Badge
            GameObject medalObj = new GameObject("MedalBadgeText");
            medalObj.transform.SetParent(cardObj.transform, false);
            medalBadgeText = medalObj.AddComponent<Text>();
            medalBadgeText.text = "🥇 GOLD MEDAL";
            medalBadgeText.font = font;
            medalBadgeText.fontSize = 32;
            medalBadgeText.alignment = TextAnchor.MiddleCenter;
            medalBadgeText.color = new Color(1.0f, 0.9f, 0.2f);

            RectTransform medalRect = medalObj.GetComponent<RectTransform>();
            medalRect.sizeDelta = new Vector2(500f, 50f);
            medalRect.anchoredPosition = new Vector2(0f, 90f);

            // Time Summary
            GameObject summaryObj = new GameObject("TimeSummaryText");
            summaryObj.transform.SetParent(cardObj.transform, false);
            timeSummaryText = summaryObj.AddComponent<Text>();
            timeSummaryText.text = "TIME: 00:24.15";
            timeSummaryText.font = font;
            timeSummaryText.fontSize = 28;
            timeSummaryText.alignment = TextAnchor.MiddleCenter;
            timeSummaryText.color = Color.white;

            RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
            summaryRect.sizeDelta = new Vector2(500f, 45f);
            summaryRect.anchoredPosition = new Vector2(0f, 35f);

            // PB Text
            GameObject pbObj = new GameObject("PBText");
            pbObj.transform.SetParent(cardObj.transform, false);
            pbText = pbObj.AddComponent<Text>();
            pbText.text = "BEST RECORD: 00:24.15";
            pbText.font = font;
            pbText.fontSize = 22;
            pbText.alignment = TextAnchor.MiddleCenter;
            pbText.color = new Color(0.7f, 0.85f, 1.0f);

            RectTransform pbRect = pbObj.GetComponent<RectTransform>();
            pbRect.sizeDelta = new Vector2(500f, 40f);
            pbRect.anchoredPosition = new Vector2(0f, -15f);

            // Restart Button
            GameObject btnObj = new GameObject("RestartButton");
            btnObj.transform.SetParent(cardObj.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.75f, 0.45f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.25f, 0.88f, 0.55f);
            cb.pressedColor = new Color(0.1f, 0.55f, 0.32f);
            btn.colors = cb;

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(260f, 60f);
            btnRect.anchoredPosition = new Vector2(0f, -120f);

            // Button Text
            GameObject btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            Text btnText = btnTextObj.AddComponent<Text>();
            btnText.text = "PLAY AGAIN";
            btnText.font = font;
            btnText.fontSize = 26;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.sizeDelta = new Vector2(260f, 60f);
            btnTextRect.anchoredPosition = Vector2.zero;

            btn.onClick.AddListener(RestartGame);

            winCanvas.SetActive(false);
        }

        public void ShowWinUI()
        {
            if (winCanvas == null) CreateWinUI();

            float finalTime = 0f;
            if (SpeedrunTimer2D.Instance != null)
            {
                SpeedrunTimer2D.Instance.StopTimer();
                finalTime = SpeedrunTimer2D.Instance.CurrentRunTime;
            }

            bool isNewRecord = false;
            if (SpeedrunTimer2D.Instance != null && finalTime > 0f)
            {
                isNewRecord = SpeedrunTimer2D.Instance.SavePersonalBest(finalTime);
            }

            string formattedTime = SpeedrunTimer2D.FormatTime(finalTime);
            timeSummaryText.text = "TIME: " + formattedTime;

            float pb = SpeedrunTimer2D.Instance != null ? SpeedrunTimer2D.Instance.GetPersonalBest() : finalTime;
            pbText.text = "PERSONAL BEST: " + SpeedrunTimer2D.FormatTime(pb) + (isNewRecord ? "  ⭐ NEW RECORD!" : "");

            string medal = SpeedrunTimer2D.Instance != null ? SpeedrunTimer2D.Instance.GetMedalEarned(finalTime) : "CLEAR";
            if (medal == "GOLD")
            {
                medalBadgeText.text = "🥇 GOLD MEDAL";
                medalBadgeText.color = new Color(1.0f, 0.85f, 0.15f);
            }
            else if (medal == "SILVER")
            {
                medalBadgeText.text = "🥈 SILVER MEDAL";
                medalBadgeText.color = new Color(0.82f, 0.86f, 0.9f);
            }
            else if (medal == "BRONZE")
            {
                medalBadgeText.text = "🥉 BRONZE MEDAL";
                medalBadgeText.color = new Color(0.85f, 0.55f, 0.3f);
            }
            else
            {
                medalBadgeText.text = "🏁 STAGE CLEARED";
                medalBadgeText.color = new Color(0.4f, 0.85f, 1.0f);
            }

            winCanvas.SetActive(true);
            Time.timeScale = 0f; // Pause physics
        }

        public void RestartGame()
        {
            Time.timeScale = 1f; // Resume physics
            if (winCanvas != null) winCanvas.SetActive(false);

            var builder = Object.FindFirstObjectByType<PlaygroundBuilder2D>();
            if (builder != null)
            {
                builder.BuildLevel();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}
