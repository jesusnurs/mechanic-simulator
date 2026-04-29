using BigDreamLab.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BigDreamLab.UI
{
    public sealed class MechanicPlayerHud : MonoBehaviour
    {
        [SerializeField] PlayerInteractionRaycaster interactionRaycaster;
        [SerializeField] MechanicHudView view;
        [SerializeField] MechanicHudTextConfig textConfig;
        [SerializeField] bool showWelcomeOnStart = true;
        [SerializeField] Color idleCrosshairColor = Color.white;
        [SerializeField] Color focusedCrosshairColor = new Color(1f, 0.82f, 0.2f);
        [SerializeField] Color blockedCrosshairColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] Color availableCrosshairColor = new Color(0.35f, 1f, 0.45f);

        DesktopFirstPersonController m_PlayerController;
        string m_TaskHeader;
        string m_TaskText;
        string m_ProgressText;
        string m_ToolName;
        bool m_WelcomeVisible;
        bool m_VictoryVisible;

        void Awake()
        {
            m_WelcomeVisible = showWelcomeOnStart;
            ResolveReferences();
            SubscribeView();
            ApplyTextConfig();
            ApplyVisibility();
            Refresh();
        }

        void OnEnable()
        {
            SubscribeView();
        }

        void OnDestroy()
        {
            UnsubscribeView();
        }

        void Update()
        {
            ResolveReferences();

            if (m_WelcomeVisible)
            {
                UpdateWelcomeInput();
                return;
            }

            if (m_VictoryVisible)
            {
                UpdateVictoryInput();
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

        public void ShowVictory()
        {
            m_VictoryVisible = true;
            m_WelcomeVisible = false;
            ApplyVisibility();
        }

        void ResolveReferences()
        {
            if (view == null)
                view = GetComponentInChildren<MechanicHudView>(true);

            if (interactionRaycaster == null)
                interactionRaycaster = GetComponentInParent<PlayerInteractionRaycaster>();

            if (interactionRaycaster == null)
                interactionRaycaster = FindFirstObjectByType<PlayerInteractionRaycaster>();

            if (m_PlayerController == null)
                m_PlayerController = GetComponentInParent<DesktopFirstPersonController>();

            if (m_PlayerController == null)
                m_PlayerController = FindFirstObjectByType<DesktopFirstPersonController>();
        }

        void ApplyTextConfig()
        {
            if (textConfig == null)
                return;

            m_TaskHeader = textConfig.taskHeader;
            m_TaskText = textConfig.taskText;
            m_ProgressText = textConfig.progressText;

            if (view != null)
                view.SetWelcomeText(textConfig.welcomeTitle, textConfig.welcomeText, textConfig.welcomeCloseText);
        }

        void Refresh()
        {
            if (view == null)
                return;

            view.SetTask(m_TaskHeader, m_TaskText, m_ProgressText);
            view.SetControlHint(textConfig != null ? textConfig.controlHintText : string.Empty);
            view.SetToolName(m_ToolName);

            var hasFocus = interactionRaycaster != null && interactionRaycaster.HasFocus;
            var canInteract = interactionRaycaster != null && interactionRaycaster.FocusedCanInteract;
            var prompt = interactionRaycaster != null ? interactionRaycaster.CurrentPrompt : string.Empty;
            var isBlockedInteractable = interactionRaycaster != null &&
                interactionRaycaster.FocusedInteractable != null &&
                !canInteract &&
                !string.IsNullOrWhiteSpace(prompt);

            var crosshairColor = canInteract
                ? availableCrosshairColor
                : isBlockedInteractable
                    ? blockedCrosshairColor
                    : hasFocus ? focusedCrosshairColor : idleCrosshairColor;

            view.SetCrosshairColor(crosshairColor);
            view.SetPrompt(prompt);
            view.SetHoldProgress(interactionRaycaster != null ? interactionRaycaster.HoldProgress : 0f);
        }

        void UpdateWelcomeInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                CloseWelcome();
        }

        void UpdateVictoryInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.rKey.wasPressedThisFrame)
                RestartLevel();
        }

        void ApplyVisibility()
        {
            var isUiBlockingGameplay = m_WelcomeVisible || m_VictoryVisible;

            if (view != null)
            {
                view.SetHudVisible(!isUiBlockingGameplay);
                view.SetWelcomeVisible(m_WelcomeVisible);
                view.SetVictoryVisible(m_VictoryVisible);
            }

            if (m_PlayerController != null)
            {
                m_PlayerController.SetInputBlocked(isUiBlockingGameplay);
                m_PlayerController.SetCursorLocked(!isUiBlockingGameplay);
            }
        }

        void SubscribeView()
        {
            ResolveReferences();
            if (view != null)
            {
                view.RestartRequested -= RestartLevel;
                view.RestartRequested += RestartLevel;
            }
        }

        void UnsubscribeView()
        {
            if (view != null)
                view.RestartRequested -= RestartLevel;
        }

        void RestartLevel()
        {
            Time.timeScale = 1f;

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else if (!string.IsNullOrWhiteSpace(activeScene.path))
                SceneManager.LoadScene(activeScene.path);
            else
                SceneManager.LoadScene(activeScene.name);
        }
    }
}
