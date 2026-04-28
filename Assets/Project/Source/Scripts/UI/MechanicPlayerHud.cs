using BigDreamLab.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BigDreamLab.UI
{
    public sealed class MechanicPlayerHud : MonoBehaviour
    {
        [SerializeField] PlayerInteractionRaycaster interactionRaycaster;
        [SerializeField] MechanicHudTextConfig textConfig;
        [SerializeField] bool showWelcomeOnStart = true;
        [SerializeField] Color idleCrosshairColor = Color.white;
        [SerializeField] Color focusedCrosshairColor = new Color(1f, 0.82f, 0.2f);
        [SerializeField] Color blockedCrosshairColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] Color availableCrosshairColor = new Color(0.35f, 1f, 0.45f);

        Canvas m_Canvas;
        GameObject m_HudRoot;
        GameObject m_WelcomeRoot;
        DesktopFirstPersonController m_PlayerController;
        TextMeshProUGUI m_TaskHeaderText;
        TextMeshProUGUI m_TaskBodyText;
        TextMeshProUGUI m_PromptText;
        TextMeshProUGUI m_ToolText;
        Image m_Crosshair;
        Image m_HoldProgressBack;
        Image m_HoldProgressFill;
        string m_TaskHeader;
        string m_TaskText;
        string m_ProgressText;
        string m_ToolName;
        string m_WelcomeTitle;
        string m_WelcomeText;
        string m_WelcomeCloseText;
        bool m_WelcomeVisible;

        void Awake()
        {
            m_WelcomeVisible = showWelcomeOnStart;
            ApplyTextConfig();
            ResolveReferences();
            BuildHud();
        }

        void Update()
        {
            ResolveReferences();

            if (m_WelcomeVisible)
            {
                UpdateWelcomeInput();
                return;
            }

            Refresh();
        }

        void OnDisable()
        {
            if (m_PlayerController != null)
                m_PlayerController.SetInputBlocked(false);
        }

        public void SetTask(string header, string text, string progress = "")
        {
            m_TaskHeader = header;
            m_TaskText = text;
            m_ProgressText = progress;
            Refresh();
        }

        public void SetToolName(string toolName)
        {
            m_ToolName = toolName ?? string.Empty;
            Refresh();
        }

        public void CloseWelcome()
        {
            m_WelcomeVisible = false;
            ApplyVisibility();
            Refresh();
        }

        void ResolveReferences()
        {
            if (interactionRaycaster == null)
                interactionRaycaster = GetComponentInParent<PlayerInteractionRaycaster>();

            if (interactionRaycaster == null)
                interactionRaycaster = FindFirstObjectByType<PlayerInteractionRaycaster>();

            if (m_PlayerController == null)
                m_PlayerController = GetComponentInParent<DesktopFirstPersonController>();

            if (m_PlayerController == null)
                m_PlayerController = FindFirstObjectByType<DesktopFirstPersonController>();
        }

        void BuildHud()
        {
            if (m_Canvas != null)
                return;

            var canvasObject = new GameObject("Mechanic HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            m_Canvas = canvasObject.GetComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            m_HudRoot = CreateRoot("HUD Root", canvasObject.transform);
            m_WelcomeRoot = CreateRoot("Welcome Root", canvasObject.transform);

            var taskPanel = CreatePanel("Task Panel", m_HudRoot.transform, new Vector2(24f, -24f), new Vector2(520f, 128f), new Vector2(0f, 1f));
            m_TaskHeaderText = CreateText("Task Header", taskPanel.transform, m_TaskHeader, 26f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(m_TaskHeaderText.rectTransform, new Vector2(20f, -14f), new Vector2(480f, 36f), new Vector2(0f, 1f));
            m_TaskBodyText = CreateText("Task Body", taskPanel.transform, string.Empty, 21f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(m_TaskBodyText.rectTransform, new Vector2(20f, -52f), new Vector2(480f, 62f), new Vector2(0f, 1f));

            m_Crosshair = CreateImage("Crosshair", m_HudRoot.transform, idleCrosshairColor);
            SetRect(m_Crosshair.rectTransform, Vector2.zero, new Vector2(8f, 8f), new Vector2(0.5f, 0.5f));

            m_HoldProgressBack = CreateImage("Hold Progress Back", m_HudRoot.transform, new Color(0f, 0f, 0f, 0.55f));
            SetRect(m_HoldProgressBack.rectTransform, new Vector2(0f, -36f), new Vector2(132f, 8f), new Vector2(0.5f, 0.5f));
            m_HoldProgressFill = CreateImage("Hold Progress Fill", m_HoldProgressBack.transform, new Color(0.35f, 1f, 0.45f, 0.95f));
            m_HoldProgressFill.type = Image.Type.Filled;
            m_HoldProgressFill.fillMethod = Image.FillMethod.Horizontal;
            m_HoldProgressFill.fillOrigin = 0;
            SetRect(m_HoldProgressFill.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            m_HoldProgressFill.rectTransform.anchorMin = Vector2.zero;
            m_HoldProgressFill.rectTransform.anchorMax = Vector2.one;

            m_PromptText = CreateText("Interaction Prompt", m_HudRoot.transform, string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(m_PromptText.rectTransform, new Vector2(0f, -70f), new Vector2(720f, 42f), new Vector2(0.5f, 0.5f));

            m_ToolText = CreateText("Current Tool", m_HudRoot.transform, string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            m_ToolText.color = new Color(0.72f, 0.92f, 1f);
            SetRect(m_ToolText.rectTransform, new Vector2(0f, 42f), new Vector2(720f, 42f), new Vector2(0.5f, 0f));

            BuildWelcome();
            ApplyVisibility();
            Refresh();
        }

        void Refresh()
        {
            if (m_TaskHeaderText == null)
                return;

            m_TaskHeaderText.text = m_TaskHeader;
            m_TaskBodyText.text = string.IsNullOrWhiteSpace(m_ProgressText)
                ? m_TaskText
                : $"{m_TaskText}\n{m_ProgressText}";

            if (m_ToolText != null)
            {
                m_ToolText.text = m_ToolName;
                m_ToolText.enabled = !string.IsNullOrWhiteSpace(m_ToolName);
            }

            var hasFocus = interactionRaycaster != null && interactionRaycaster.HasFocus;
            var canInteract = interactionRaycaster != null && interactionRaycaster.FocusedCanInteract;
            var prompt = interactionRaycaster != null ? interactionRaycaster.CurrentPrompt : string.Empty;
            var isBlockedInteractable = interactionRaycaster != null &&
                interactionRaycaster.FocusedInteractable != null &&
                !canInteract &&
                !string.IsNullOrWhiteSpace(prompt);

            m_Crosshair.color = canInteract
                ? availableCrosshairColor
                : isBlockedInteractable
                    ? blockedCrosshairColor
                    : hasFocus ? focusedCrosshairColor : idleCrosshairColor;

            m_PromptText.text = prompt;
            m_PromptText.enabled = !string.IsNullOrWhiteSpace(prompt);

            var holdProgress = interactionRaycaster != null ? interactionRaycaster.HoldProgress : 0f;
            var showHold = holdProgress > 0.001f && holdProgress < 0.999f;
            m_HoldProgressBack.enabled = showHold;
            m_HoldProgressFill.enabled = showHold;
            m_HoldProgressFill.fillAmount = holdProgress;
        }

        void UpdateWelcomeInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if ((keyboard != null && (
                    keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.eKey.wasPressedThisFrame)) ||
                (mouse != null && mouse.leftButton.wasPressedThisFrame))
            {
                CloseWelcome();
            }
        }

        void BuildWelcome()
        {
            var dim = CreateImage("Welcome Dim", m_WelcomeRoot.transform, new Color(0f, 0f, 0f, 0.68f));
            StretchToParent(dim.rectTransform);

            var panel = CreatePanel("Welcome Panel", m_WelcomeRoot.transform, Vector2.zero, new Vector2(820f, 500f), new Vector2(0.5f, 0.5f));
            panel.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.05f, 0.94f);

            var title = CreateText("Welcome Title", panel.transform, m_WelcomeTitle, 42f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, -34f), new Vector2(740f, 60f), new Vector2(0.5f, 1f));

            var body = CreateText("Welcome Controls", panel.transform, m_WelcomeText, 25f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(body.rectTransform, new Vector2(0f, -132f), new Vector2(650f, 235f), new Vector2(0.5f, 1f));
            body.lineSpacing = 8f;

            var close = CreateText("Welcome Close Hint", panel.transform, m_WelcomeCloseText, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            close.color = new Color(0.35f, 1f, 0.45f);
            SetRect(close.rectTransform, new Vector2(0f, 44f), new Vector2(720f, 42f), new Vector2(0.5f, 0f));
        }

        void ApplyVisibility()
        {
            if (m_HudRoot != null)
                m_HudRoot.SetActive(!m_WelcomeVisible);

            if (m_WelcomeRoot != null)
                m_WelcomeRoot.SetActive(m_WelcomeVisible);

            if (m_PlayerController != null)
                m_PlayerController.SetInputBlocked(m_WelcomeVisible);
        }

        GameObject CreateRoot(string objectName, Transform parent)
        {
            var root = new GameObject(objectName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            StretchToParent(root.GetComponent<RectTransform>());
            return root;
        }

        void ApplyTextConfig()
        {
            m_TaskHeader = textConfig != null ? textConfig.taskHeader : "Этап 1: Подготовка рабочего места";
            m_TaskText = textConfig != null ? textConfig.taskText : "Открутите 5 болтов колеса";
            m_ProgressText = textConfig != null ? textConfig.progressText : "Прогресс: 0/5";
            m_WelcomeTitle = textConfig != null ? textConfig.welcomeTitle : "Добро пожаловать в мастерскую";
            m_WelcomeText = textConfig != null
                ? textConfig.welcomeText
                : "Управление:\nWASD / стрелки - движение\nМышь - осмотр\nShift - ускориться\nЛКМ / E - взаимодействовать\nEsc - освободить курсор";
            m_WelcomeCloseText = textConfig != null
                ? textConfig.welcomeCloseText
                : "Нажмите Enter / Space / E / ЛКМ, чтобы начать";
        }

        GameObject CreatePanel(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
        {
            var panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.color = new Color(0.05f, 0.055f, 0.06f, 0.78f);
            SetRect(panel.GetComponent<RectTransform>(), anchoredPosition, size, anchor);
            return panel;
        }

        TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var textComponent = textObject.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = style;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            return textComponent;
        }

        Image CreateImage(string objectName, Transform parent, Color color)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }
    }
}
