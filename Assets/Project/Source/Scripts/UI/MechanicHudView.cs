using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigDreamLab.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MechanicHudView : MonoBehaviour
    {
        [SerializeField] UIDocument document;

        VisualElement m_HudRoot;
        VisualElement m_WelcomeRoot;
        VisualElement m_Crosshair;
        VisualElement m_HoldProgressBack;
        VisualElement m_HoldProgressFill;
        Label m_TaskHeaderText;
        Label m_TaskBodyText;
        Label m_PromptText;
        Label m_ToolText;
        Label m_ControlHintText;
        Label m_WelcomeTitleText;
        Label m_WelcomeBodyText;
        Label m_WelcomeCloseText;
        VisualElement m_VictoryRoot;
        Button m_RestartButton;

        public event Action RestartRequested;

        void Awake()
        {
            ResolveDocument();
            BindLayout();
        }

        public void SetHudVisible(bool isVisible)
        {
            SetDisplay(m_HudRoot, isVisible);
        }

        public void SetWelcomeVisible(bool isVisible)
        {
            SetDisplay(m_WelcomeRoot, isVisible);
        }

        public void SetTask(string header, string text, string progress)
        {
            if (m_TaskHeaderText != null)
                m_TaskHeaderText.text = header ?? string.Empty;

            if (m_TaskBodyText != null)
                m_TaskBodyText.text = string.IsNullOrWhiteSpace(progress)
                    ? text ?? string.Empty
                    : $"{text}";
        }

        public void SetPrompt(string prompt)
        {
            SetLabel(m_PromptText, prompt, hideWhenEmpty: true);
        }

        public void SetToolName(string toolName)
        {
            SetLabel(m_ToolText, toolName, hideWhenEmpty: true);
        }

        public void SetControlHint(string hint)
        {
            SetLabel(m_ControlHintText, hint, hideWhenEmpty: true);
        }

        public void SetWelcomeText(string title, string body, string closeHint)
        {
            SetLabel(m_WelcomeTitleText, title, hideWhenEmpty: false);
            SetLabel(m_WelcomeBodyText, body, hideWhenEmpty: false);
            SetLabel(m_WelcomeCloseText, closeHint, hideWhenEmpty: false);
        }

        public void SetCrosshairColor(Color color)
        {
            if (m_Crosshair != null)
                m_Crosshair.style.backgroundColor = color;
        }

        public void SetHoldProgress(float progress)
        {
            var showHold = progress > 0.001f && progress < 0.999f;
            SetDisplay(m_HoldProgressBack, showHold);

            if (m_HoldProgressFill != null)
                m_HoldProgressFill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
        }

        void ResolveDocument()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        void BindLayout()
        {
            if (document == null)
                return;

            var root = document.rootVisualElement;
            m_HudRoot = root.Q<VisualElement>("hud-root");
            m_WelcomeRoot = root.Q<VisualElement>("welcome-root");
            m_Crosshair = root.Q<VisualElement>("crosshair");
            m_HoldProgressBack = root.Q<VisualElement>("hold-progress-back");
            m_HoldProgressFill = root.Q<VisualElement>("hold-progress-fill");
            m_TaskHeaderText = root.Q<Label>("task-header");
            m_TaskBodyText = root.Q<Label>("task-body");
            m_PromptText = root.Q<Label>("interaction-prompt");
            m_ToolText = root.Q<Label>("current-tool");
            m_ControlHintText = root.Q<Label>("control-hint");
            m_WelcomeTitleText = root.Q<Label>("welcome-title");
            m_WelcomeBodyText = root.Q<Label>("welcome-body");
            m_WelcomeCloseText = root.Q<Label>("welcome-close");
            m_VictoryRoot = root.Q<VisualElement>("victory-root");
            m_RestartButton = root.Q<Button>("restart-button");

            if (m_RestartButton != null)
                m_RestartButton.clicked += OnRestartClicked;
        }

        void OnDestroy()
        {
            if (m_RestartButton != null)
                m_RestartButton.clicked -= OnRestartClicked;
        }

        public void SetVictoryVisible(bool isVisible)
        {
            SetDisplay(m_VictoryRoot, isVisible);
        }

        void OnRestartClicked()
        {
            RestartRequested?.Invoke();
        }

        static void SetDisplay(VisualElement element, bool isVisible)
        {
            if (element != null)
                element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static void SetLabel(Label label, string value, bool hideWhenEmpty)
        {
            if (label == null)
                return;

            label.text = value ?? string.Empty;
            if (hideWhenEmpty)
                label.style.display = string.IsNullOrWhiteSpace(value) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
