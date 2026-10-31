using EchoesOfAscent.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfAscent.Presentation
{
    public sealed class LevelCompleteUI : MonoBehaviour
    {
        const string VictoryMessage = "Вы прошли уровень!";

        [SerializeField] GameObject panelRoot;
        [SerializeField] bool pauseOnComplete = true;

        bool isShown;

        void Awake()
        {
            Time.timeScale = 1f;

            if (panelRoot == null)
                panelRoot = BuildUi();
            else
                panelRoot.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.LevelCompleted += Show;
        }

        void OnDisable()
        {
            GameEvents.LevelCompleted -= Show;
        }

        void Show()
        {
            if (isShown || panelRoot == null)
                return;

            isShown = true;
            panelRoot.SetActive(true);

            if (pauseOnComplete)
                Time.timeScale = 0f;
        }

        static GameObject BuildUi()
        {
            var canvasObject = new GameObject("LevelCompleteCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();

            var panelObject = new GameObject("VictoryPanel");
            panelObject.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);

            var textObject = new GameObject("VictoryText");
            textObject.transform.SetParent(panelObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(900f, 180f);
            textRect.anchoredPosition = Vector2.zero;

            Text label = textObject.AddComponent<Text>();
            label.text = VictoryMessage;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 48;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            panelObject.SetActive(false);
            return panelObject;
        }
    }
}
