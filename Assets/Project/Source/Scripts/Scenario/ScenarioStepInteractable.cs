using System.Collections;
using BigDreamLab.Interaction;
using BigDreamLab.Player;
using UnityEngine;
using UnityEngine.Events;

namespace BigDreamLab.Scenario
{
    public sealed class ScenarioStepInteractable : MonoBehaviour, IPlayerInteractable, IPlayerFocusVisibility
    {
        [Header("Scenario")]
        [SerializeField] BrakeDiscScenarioManager scenarioManager;
        [SerializeField] BrakeDiscScenarioStep step;
        [SerializeField] string requiredToolId;
        [SerializeField] bool completeOnlyOnce = true;

        [Header("Interaction")]
        [SerializeField] bool requiresHold;
        [SerializeField, Min(0.05f)] float holdDuration = 1.2f;

        [Header("Focus Visuals")]
        [SerializeField] bool hideRenderersUntilFocused;
        [SerializeField] Renderer[] focusRenderers;

        [Header("Animation")]
        [SerializeField] bool animateTransform;
        [SerializeField] Transform animatedTransform;
        [SerializeField] bool useTargetLocalPosition;
        [SerializeField] Vector3 targetLocalPosition;
        [SerializeField] Vector3 localPositionOffset;
        [SerializeField] Vector3 localRotationOffset;
        [SerializeField, Min(0.01f)] float animationDuration = 0.45f;

        [Header("Object Toggles")]
        [SerializeField] GameObject[] enableOnComplete;
        [SerializeField] GameObject[] disableOnComplete;
        [SerializeField] bool disableThisObjectOnComplete;

        [Header("Events")]
        [SerializeField] UnityEvent completed;

        SelectionOutline m_SelectionOutline;
        Coroutine m_CompletionRoutine;

        public BrakeDiscScenarioStep Step => step;
        public string RequiredToolId => requiredToolId;
        public bool IsCompleted { get; private set; }
        public string InteractionPrompt => scenarioManager != null ? scenarioManager.GetPrompt(step) : "ЛКМ - взаимодействовать";
        public bool RequiresHold => requiresHold;
        public float HoldDuration => holdDuration;

        void Awake()
        {
            m_SelectionOutline = GetComponent<SelectionOutline>();
            CacheFocusRenderers();

            if (hideRenderersUntilFocused)
                SetFocusRenderersVisible(false);
        }

        public void Configure(BrakeDiscScenarioManager manager)
        {
            scenarioManager = manager;
        }

        public void ResetCompletion()
        {
            IsCompleted = false;
        }

        public bool ShouldShowFocus(PlayerInteractionContext context)
        {
            return scenarioManager != null && scenarioManager.IsCurrentTarget(this);
        }

        public bool CanInteract(PlayerInteractionContext context, out string unavailableReason)
        {
            if (scenarioManager == null)
            {
                unavailableReason = "Сценарий ещё не готов";
                return false;
            }

            if (!scenarioManager.IsCurrentTarget(this))
            {
                unavailableReason = scenarioManager.DifferentTaskReason;
                return false;
            }

            if (completeOnlyOnce && IsCompleted)
            {
                unavailableReason = scenarioManager.AlreadyDoneReason;
                return false;
            }

            var requiredScenarioToolId = scenarioManager.GetRequiredToolId(this);
            if (!scenarioManager.IsRequiredToolSelected(requiredScenarioToolId))
            {
                unavailableReason = scenarioManager.GetWrongToolReason(requiredScenarioToolId);
                return false;
            }

            unavailableReason = string.Empty;
            return true;
        }

        public void OnFocusEnter(PlayerInteractionContext context)
        {
            if (hideRenderersUntilFocused)
                SetFocusRenderersVisible(true);
        }

        public void OnFocusExit(PlayerInteractionContext context)
        {
            if (hideRenderersUntilFocused)
                SetFocusRenderersVisible(false);
        }

        public void OnHoldProgress(PlayerInteractionContext context, float normalizedProgress)
        {
        }

        public void OnInteract(PlayerInteractionContext context)
        {
            if (m_CompletionRoutine != null)
                return;

            m_CompletionRoutine = StartCoroutine(CompleteRoutine());
        }

        public void SetScenarioFocusAllowed(bool isAllowed)
        {
            if (!isAllowed && m_SelectionOutline != null)
                m_SelectionOutline.ToggleOutline(false);

            if (!isAllowed && hideRenderersUntilFocused)
                SetFocusRenderersVisible(false);
        }

        IEnumerator CompleteRoutine()
        {
            if (animateTransform)
                yield return AnimateRoutine();

            IsCompleted = true;
            SetScenarioFocusAllowed(false);
            ApplyObjectToggles(disableSelf: false);
            completed?.Invoke();
            scenarioManager?.NotifyActionCompleted(this);
            m_CompletionRoutine = null;

            if (disableThisObjectOnComplete)
                gameObject.SetActive(false);
        }

        IEnumerator AnimateRoutine()
        {
            var target = animatedTransform != null ? animatedTransform : transform;
            var startPosition = target.localPosition;
            var startRotation = target.localRotation;
            var endPosition = useTargetLocalPosition ? targetLocalPosition : startPosition + localPositionOffset;
            var endRotation = startRotation * Quaternion.Euler(localRotationOffset);
            var elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / animationDuration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                target.localPosition = Vector3.Lerp(startPosition, endPosition, eased);
                target.localRotation = Quaternion.Slerp(startRotation, endRotation, eased);
                yield return null;
            }

            target.localPosition = endPosition;
            target.localRotation = endRotation;
        }

        void ApplyObjectToggles(bool disableSelf)
        {
            foreach (var target in enableOnComplete)
            {
                if (target != null)
                    target.SetActive(true);
            }

            foreach (var target in disableOnComplete)
            {
                if (target != null)
                    target.SetActive(false);
            }

            if (disableSelf && disableThisObjectOnComplete)
                gameObject.SetActive(false);
        }

        void CacheFocusRenderers()
        {
            if (focusRenderers != null && focusRenderers.Length > 0)
                return;

            focusRenderers = GetComponentsInChildren<Renderer>(true);
        }

        void SetFocusRenderersVisible(bool isVisible)
        {
            CacheFocusRenderers();
            foreach (var targetRenderer in focusRenderers)
            {
                if (targetRenderer != null)
                    targetRenderer.enabled = isVisible;
            }
        }
    }
}
