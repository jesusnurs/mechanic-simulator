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

        [Header("Renderer Visibility")]
        [SerializeField] bool hideRenderersOnStart;
        [SerializeField] bool showRenderersBeforeAnimation;
        [SerializeField] bool showRenderersOnComplete;
        [SerializeField] bool hideRenderersOnComplete = true;
        [SerializeField] Material hiddenRendererMaterial;

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
        Material[][] m_DefaultRendererMaterials;
        bool[] m_DefaultRendererEnabledStates;
        bool m_RendererStateCached;
        bool m_RenderersHidden;
        bool m_WarnedAboutMissingHiddenMaterial;
        static Material s_RuntimeHiddenMaterial;

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
            CacheRendererState();

            if (hideRenderersOnStart)
                SetRenderersHidden(true);

            if (hideRenderersUntilFocused && !m_RenderersHidden)
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
            if (hideRenderersUntilFocused && !m_RenderersHidden)
                SetFocusRenderersVisible(true);
        }

        public void OnFocusExit(PlayerInteractionContext context)
        {
            if (hideRenderersUntilFocused && !m_RenderersHidden)
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

            if (!isAllowed && hideRenderersUntilFocused && !m_RenderersHidden)
                SetFocusRenderersVisible(false);
        }

        IEnumerator CompleteRoutine()
        {
            if (showRenderersBeforeAnimation)
                SetRenderersHidden(false);

            if (animateTransform)
                yield return AnimateRoutine();

            IsCompleted = true;
            SetScenarioFocusAllowed(false);
            ApplyObjectToggles(disableSelf: false);
            if (showRenderersOnComplete)
                SetRenderersHidden(false);

            completed?.Invoke();
            scenarioManager?.NotifyActionCompleted(this);
            m_CompletionRoutine = null;

            if (disableThisObjectOnComplete)
            {
                if (hideRenderersOnComplete)
                    SetRenderersHidden(true);
                else
                    gameObject.SetActive(false);
            }
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
            if (enableOnComplete != null)
            {
                foreach (var target in enableOnComplete)
                {
                    if (target != null)
                    {
                        target.SetActive(true);
                        SetTargetRenderersHidden(target, false);
                    }
                }
            }

            if (disableOnComplete != null)
            {
                foreach (var target in disableOnComplete)
                {
                    if (target != null)
                        SetTargetRenderersHidden(target, true);
                }
            }

            if (disableSelf && disableThisObjectOnComplete)
            {
                if (hideRenderersOnComplete)
                    SetRenderersHidden(true);
                else
                    gameObject.SetActive(false);
            }
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

        public void SetRenderersHidden(bool isHidden)
        {
            CacheFocusRenderers();
            CacheRendererState();

            if (m_RenderersHidden == isHidden)
                return;

            var hiddenMaterial = ResolveHiddenRendererMaterial();
            for (var i = 0; i < focusRenderers.Length; i++)
            {
                var targetRenderer = focusRenderers[i];
                if (targetRenderer == null)
                    continue;

                if (isHidden)
                {
                    if (hiddenMaterial != null)
                    {
                        var hiddenMaterials = new Material[targetRenderer.sharedMaterials.Length];
                        for (var materialIndex = 0; materialIndex < hiddenMaterials.Length; materialIndex++)
                            hiddenMaterials[materialIndex] = hiddenMaterial;

                        targetRenderer.sharedMaterials = hiddenMaterials;
                        targetRenderer.enabled = true;
                        continue;
                    }

                    WarnMissingHiddenMaterial();
                    targetRenderer.enabled = false;
                    continue;
                }

                if (m_DefaultRendererMaterials != null && i < m_DefaultRendererMaterials.Length)
                    targetRenderer.sharedMaterials = m_DefaultRendererMaterials[i];

                if (m_DefaultRendererEnabledStates != null && i < m_DefaultRendererEnabledStates.Length)
                    targetRenderer.enabled = m_DefaultRendererEnabledStates[i];
                else
                    targetRenderer.enabled = true;
            }

            m_RenderersHidden = isHidden;
        }

        void CacheRendererState()
        {
            if (m_RendererStateCached)
                return;

            m_RendererStateCached = true;
            CacheFocusRenderers();
            m_DefaultRendererMaterials = new Material[focusRenderers.Length][];
            m_DefaultRendererEnabledStates = new bool[focusRenderers.Length];

            for (var i = 0; i < focusRenderers.Length; i++)
            {
                var targetRenderer = focusRenderers[i];
                if (targetRenderer == null)
                    continue;

                m_DefaultRendererMaterials[i] = targetRenderer.sharedMaterials;
                m_DefaultRendererEnabledStates[i] = targetRenderer.enabled;
            }
        }

        Material ResolveHiddenRendererMaterial()
        {
            if (hiddenRendererMaterial != null)
                return hiddenRendererMaterial;

            if (s_RuntimeHiddenMaterial != null)
                return s_RuntimeHiddenMaterial;

            var hiddenShader = Shader.Find("Mechanic Simulator/Invisible Interactable");
            if (hiddenShader == null)
                return null;

            s_RuntimeHiddenMaterial = new Material(hiddenShader)
            {
                name = "Runtime Invisible Interactable Material",
            };
            return s_RuntimeHiddenMaterial;
        }

        void WarnMissingHiddenMaterial()
        {
            if (m_WarnedAboutMissingHiddenMaterial)
                return;

            m_WarnedAboutMissingHiddenMaterial = true;
            Debug.LogWarning($"{nameof(ScenarioStepInteractable)} could not find hidden renderer material. Hidden objects will not be outlineable until a material using 'Mechanic Simulator/Invisible Interactable' is assigned.", this);
        }

        static void SetTargetRenderersHidden(GameObject target, bool isHidden)
        {
            var scenarioActions = target.GetComponents<ScenarioStepInteractable>();
            if (scenarioActions.Length > 0)
            {
                foreach (var scenarioAction in scenarioActions)
                {
                    if (scenarioAction != null)
                        scenarioAction.SetRenderersHidden(isHidden);
                }

                return;
            }

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer != null)
                    targetRenderer.enabled = !isHidden;
            }
        }
    }
}
