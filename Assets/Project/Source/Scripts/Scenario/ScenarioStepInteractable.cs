using System.Collections;
using System.Collections.Generic;
using BigDreamLab.Interaction;
using BigDreamLab.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("showRenderersBeforeAnimation")]
        [SerializeField] bool showRenderersOnInteractionStart;
        [SerializeField] bool showRenderersOnComplete;
        [SerializeField] bool hideRenderersOnComplete;
        [SerializeField] Material hiddenRendererMaterial;

        [Header("Animation")]
        [SerializeField] bool animateTransform;
        [SerializeField] Transform animatedTransform;
        [SerializeField] Vector3 animationFromLocalPosition;
        [FormerlySerializedAs("targetLocalPosition")]
        [SerializeField] Vector3 animationToLocalPosition;
        [SerializeField, Min(0.01f)] float animationDuration = 0.45f;

        [HideInInspector, FormerlySerializedAs("useTargetLocalPosition")]
        [SerializeField] bool legacyUseTargetLocalPosition;
        [HideInInspector, FormerlySerializedAs("localPositionOffset")]
        [SerializeField] Vector3 legacyLocalPositionOffset;
        [HideInInspector, FormerlySerializedAs("localRotationOffset")]
        [SerializeField] Vector3 legacyLocalRotationOffset;

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
        Collider[] m_InteractionColliders;
        bool[] m_DefaultColliderEnabledStates;
        bool m_ColliderStateCached;
        bool m_WarnedAboutMissingHiddenMaterial;
        static readonly Dictionary<Renderer, Material[]> s_DefaultMaterialsByRenderer = new Dictionary<Renderer, Material[]>();
        static readonly Dictionary<Renderer, bool> s_DefaultEnabledByRenderer = new Dictionary<Renderer, bool>();
        static Material s_RuntimeHiddenMaterial;

        public BrakeDiscScenarioStep Step => step;
        public string RequiredToolId => requiredToolId;
        public bool IsCompleted { get; private set; }
        public string InteractionPrompt => scenarioManager != null ? scenarioManager.GetPrompt(step) : "ЛКМ - взаимодействовать";
        public bool RequiresHold => requiresHold;
        public float HoldDuration => holdDuration;

        void OnValidate()
        {
            MigrateLegacyAnimationSettings();
        }

        void Awake()
        {
            m_SelectionOutline = GetComponent<SelectionOutline>();
            CacheFocusRenderers();
            CacheRendererState();
            CacheColliderState();
            MigrateLegacyAnimationSettings();

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

        public Collider[] GetInteractionColliders()
        {
            CacheColliderState();
            return m_InteractionColliders;
        }

        public void SetInteractionCollidersEnabled(bool isEnabled)
        {
            SetInteractionCollidersEnabled(isEnabled, true);
        }

        public void SetInteractionCollidersEnabled(bool isEnabled, bool respectInitialEnabledState)
        {
            CacheColliderState();
            if (m_InteractionColliders == null)
                return;

            for (var i = 0; i < m_InteractionColliders.Length; i++)
            {
                var targetCollider = m_InteractionColliders[i];
                if (targetCollider == null)
                    continue;

                targetCollider.enabled = isEnabled &&
                    (!respectInitialEnabledState ||
                    m_DefaultColliderEnabledStates == null ||
                    i >= m_DefaultColliderEnabledStates.Length ||
                    m_DefaultColliderEnabledStates[i]);
            }
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
            if (showRenderersOnInteractionStart)
                SetRenderersHidden(false);

            if (animateTransform)
                yield return AnimateRoutine();

            IsCompleted = true;
            SetScenarioFocusAllowed(false);
            ApplyObjectToggles(disableSelf: false);
            if (showRenderersOnComplete)
                SetRenderersHidden(false);

            if (hideRenderersOnComplete)
                SetRenderersHidden(true);

            completed?.Invoke();
            scenarioManager?.NotifyActionCompleted(this);
            m_CompletionRoutine = null;

            if (disableThisObjectOnComplete)
            {
                ResetAnimatedTransformToFrom();
                gameObject.SetActive(false);
            }
        }

        IEnumerator AnimateRoutine()
        {
            var target = animatedTransform != null ? animatedTransform : transform;
            var elapsed = 0f;
            target.localPosition = animationFromLocalPosition;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / animationDuration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                target.localPosition = Vector3.Lerp(animationFromLocalPosition, animationToLocalPosition, eased);
                yield return null;
            }

            target.localPosition = animationToLocalPosition;
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
                gameObject.SetActive(false);
        }

        void CacheFocusRenderers()
        {
            if (focusRenderers != null && focusRenderers.Length > 0)
            {
                focusRenderers = FilterOwnedRenderers(focusRenderers);
                return;
            }

            focusRenderers = FilterOwnedRenderers(GetComponentsInChildren<Renderer>(true));
        }

        Renderer[] FilterOwnedRenderers(Renderer[] renderers)
        {
            var ownedRenderers = new List<Renderer>();
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer != null && IsOwnedRenderer(targetRenderer))
                    ownedRenderers.Add(targetRenderer);
            }

            return ownedRenderers.ToArray();
        }

        bool ShouldHideRenderersOnStart()
        {
            if (!hideRenderersOnStart)
                return false;

            var siblingActions = GetComponents<ScenarioStepInteractable>();
            if (siblingActions.Length <= 1)
                return true;

            foreach (var siblingAction in siblingActions)
            {
                if (siblingAction == null || ReferenceEquals(siblingAction, this))
                    continue;

                if (!siblingAction.hideRenderersOnStart && SharesRendererWith(siblingAction))
                    return false;
            }

            return true;
        }

        bool SharesRendererWith(ScenarioStepInteractable other)
        {
            if (other == null)
                return false;

            CacheFocusRenderers();
            other.CacheFocusRenderers();

            foreach (var targetRenderer in focusRenderers)
            {
                if (targetRenderer == null)
                    continue;

                foreach (var otherRenderer in other.focusRenderers)
                {
                    if (ReferenceEquals(targetRenderer, otherRenderer))
                        return true;
                }
            }

            return false;
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

                if (!s_DefaultMaterialsByRenderer.ContainsKey(targetRenderer))
                {
                    s_DefaultMaterialsByRenderer.Add(targetRenderer, targetRenderer.sharedMaterials);
                    s_DefaultEnabledByRenderer.Add(targetRenderer, targetRenderer.enabled);
                }

                m_DefaultRendererMaterials[i] = s_DefaultMaterialsByRenderer[targetRenderer];
                m_DefaultRendererEnabledStates[i] = s_DefaultEnabledByRenderer[targetRenderer];
            }
        }

        void CacheColliderState()
        {
            if (m_ColliderStateCached)
                return;

            m_ColliderStateCached = true;
            var ownedColliders = new List<Collider>();
            foreach (var targetCollider in GetComponentsInChildren<Collider>(true))
            {
                if (targetCollider != null && IsOwnedInteractionCollider(targetCollider))
                    ownedColliders.Add(targetCollider);
            }

            m_InteractionColliders = ownedColliders.ToArray();
            m_DefaultColliderEnabledStates = new bool[m_InteractionColliders.Length];

            for (var i = 0; i < m_InteractionColliders.Length; i++)
            {
                var targetCollider = m_InteractionColliders[i];
                if (targetCollider != null)
                    m_DefaultColliderEnabledStates[i] = targetCollider.enabled;
            }
        }

        bool IsOwnedInteractionCollider(Collider targetCollider)
        {
            var current = targetCollider.transform;
            while (current != null && current.IsChildOf(transform))
            {
                var interactables = current.GetComponents<ScenarioStepInteractable>();
                if (interactables.Length > 0)
                {
                    foreach (var interactable in interactables)
                    {
                        if (ReferenceEquals(interactable, this))
                            return true;
                    }

                    return false;
                }

                if (current == transform)
                    break;

                current = current.parent;
            }

            return false;
        }

        bool IsOwnedRenderer(Renderer targetRenderer)
        {
            var current = targetRenderer.transform;
            while (current != null && current.IsChildOf(transform))
            {
                var interactables = current.GetComponents<ScenarioStepInteractable>();
                if (interactables.Length > 0)
                {
                    foreach (var interactable in interactables)
                    {
                        if (ReferenceEquals(interactable, this))
                            return true;
                    }

                    return false;
                }

                if (current == transform)
                    break;

                current = current.parent;
            }

            return false;
        }

        void ResetAnimatedTransformToFrom()
        {
            if (!animateTransform)
                return;

            var target = animatedTransform != null ? animatedTransform : transform;
            target.localPosition = animationFromLocalPosition;
        }

        void MigrateLegacyAnimationSettings()
        {
            _ = legacyLocalRotationOffset;

            if (!animateTransform)
                return;

            var target = animatedTransform != null ? animatedTransform : transform;
            if (animationFromLocalPosition == Vector3.zero)
                animationFromLocalPosition = target.localPosition;

            if (animationToLocalPosition != Vector3.zero || legacyUseTargetLocalPosition)
                return;

            if (legacyLocalPositionOffset != Vector3.zero)
                animationToLocalPosition = animationFromLocalPosition + legacyLocalPositionOffset;
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
                if (targetRenderer != null && IsOwnedByTarget(target, targetRenderer.transform))
                    targetRenderer.enabled = !isHidden;
            }
        }

        static bool IsOwnedByTarget(GameObject target, Transform targetTransform)
        {
            var current = targetTransform;
            while (current != null && current.IsChildOf(target.transform))
            {
                if (current != target.transform &&
                    current.GetComponents<ScenarioStepInteractable>().Length > 0)
                    return false;

                if (current == target.transform)
                    break;

                current = current.parent;
            }

            return true;
        }
    }
}
