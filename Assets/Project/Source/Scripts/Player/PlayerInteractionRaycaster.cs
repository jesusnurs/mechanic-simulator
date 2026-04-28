using BigDreamLab.Interaction;
using UnityEngine;

namespace BigDreamLab.Player
{
    [DefaultExecutionOrder(-10)]
    public sealed class PlayerInteractionRaycaster : MonoBehaviour
    {
        [SerializeField] DesktopFirstPersonController playerInput;
        [SerializeField] Camera rayCamera;
        [SerializeField] float interactionDistance = 3f;
        [SerializeField] LayerMask interactionMask = ~0;
        [SerializeField] QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] bool drawDebugRay;
        [SerializeField] InteractionTextConfig textConfig;
        [SerializeField] bool autoUseProjectInteractionLayers = true;
        [SerializeField] string interactableLayerName = "Interactable";
        [SerializeField] string outlineLayerName = "Outline";

        IPlayerInteractable m_FocusedInteractable;
        IPlayerInteractable m_ActiveHoldInteractable;
        SelectionOutline m_FocusedOutline;
        readonly RaycastHit[] m_RaycastHits = new RaycastHit[32];
        RaycastHit m_CurrentHit;
        bool m_InteractionConsumed;
        int m_InteractableLayer = -1;
        int m_OutlineLayer = -1;

        public IPlayerInteractable FocusedInteractable => m_FocusedInteractable;
        public bool HasFocus => m_FocusedInteractable != null || m_FocusedOutline != null;
        public bool FocusedCanInteract { get; private set; }
        public string CurrentPrompt { get; private set; }
        public float HoldProgress { get; private set; }
        string FallbackPrompt => textConfig != null ? textConfig.fallbackPrompt : "ЛКМ / E - взаимодействовать";

        void Awake()
        {
            ConfigureProjectLayers();
            ResolveReferences();
        }

        void Update()
        {
            ResolveReferences();
            UpdateFocus();
            UpdateInteraction();
        }

        void ResolveReferences()
        {
            if (playerInput == null)
                playerInput = GetComponentInParent<DesktopFirstPersonController>();

            if (rayCamera == null)
            {
                var cameraTransform = playerInput != null ? playerInput.CameraTransform : null;
                if (cameraTransform != null)
                    rayCamera = cameraTransform.GetComponent<Camera>();
            }

            if (rayCamera == null)
                rayCamera = Camera.main;
        }

        void UpdateFocus()
        {
            var nextInteractable = FindInteractable(out var hit, out var nextOutline);
            if (!ReferenceEquals(nextInteractable, m_FocusedInteractable) ||
                !ReferenceEquals(nextOutline, m_FocusedOutline))
            {
                SendFocusExit();
                SetFocusedOutline(false);
                m_FocusedInteractable = nextInteractable;
                m_FocusedOutline = nextOutline;
                m_CurrentHit = hit;
                SetFocusedOutline(true);
                SendFocusEnter();
            }
            else
            {
                m_CurrentHit = hit;
            }

            if (drawDebugRay && rayCamera != null)
                Debug.DrawRay(rayCamera.transform.position, rayCamera.transform.forward * interactionDistance, HasFocus ? Color.green : Color.white);
        }

        void UpdateInteraction()
        {
            CurrentPrompt = string.Empty;
            FocusedCanInteract = false;

            if (playerInput == null)
            {
                ResetHold();
                return;
            }

            if (m_FocusedInteractable == null)
            {
                CurrentPrompt = m_FocusedOutline != null ? FallbackPrompt : string.Empty;
                ResetHold();
                return;
            }

            var context = CreateContext(playerInput.InteractHoldTime);
            if (!m_FocusedInteractable.CanInteract(context, out var unavailableReason))
            {
                CurrentPrompt = unavailableReason;
                ResetHold();
                return;
            }

            CurrentPrompt = m_FocusedInteractable.InteractionPrompt;
            FocusedCanInteract = true;

            if (playerInput.InteractPressedThisFrame)
            {
                m_ActiveHoldInteractable = m_FocusedInteractable;
                m_InteractionConsumed = false;

                if (!m_FocusedInteractable.RequiresHold)
                {
                    m_FocusedInteractable.OnInteract(context);
                    m_InteractionConsumed = true;
                    m_ActiveHoldInteractable = null;
                    return;
                }
            }

            if (playerInput.InteractHeld && ReferenceEquals(m_ActiveHoldInteractable, m_FocusedInteractable))
            {
                UpdateHold(context);
                return;
            }

            if (playerInput.InteractReleasedThisFrame)
                ResetHold();
        }

        IPlayerInteractable FindInteractable(out RaycastHit hit, out SelectionOutline outline)
        {
            hit = default;
            outline = null;
            if (rayCamera == null)
                return null;

            var ray = new Ray(rayCamera.transform.position, rayCamera.transform.forward);
            var hitCount = Physics.RaycastNonAlloc(ray, m_RaycastHits, interactionDistance, interactionMask, triggerInteraction);
            var closestDistance = float.PositiveInfinity;
            var closestIndex = -1;

            for (var i = 0; i < hitCount; i++)
            {
                var raycastHit = m_RaycastHits[i];
                if (raycastHit.collider == null || IsOwnCollider(raycastHit.collider))
                    continue;

                if (raycastHit.distance >= closestDistance)
                    continue;

                closestDistance = raycastHit.distance;
                closestIndex = i;
            }

            if (closestIndex < 0)
                return null;

            hit = m_RaycastHits[closestIndex];
            var behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>(true);
            IPlayerInteractable interactable = null;
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IPlayerInteractable playerInteractable)
                {
                    interactable = playerInteractable;
                    break;
                }
            }

            outline = ResolveOutline(hit.collider, interactable);
            if (interactable is IPlayerFocusVisibility focusVisibility &&
                !focusVisibility.ShouldShowFocus(new PlayerInteractionContext(playerInput, this, rayCamera, hit, playerInput != null ? playerInput.InteractHoldTime : 0f)))
            {
                outline = null;
                return null;
            }

            return interactable;
        }

        bool IsOwnCollider(Collider targetCollider)
        {
            return playerInput != null && targetCollider.transform.IsChildOf(playerInput.transform);
        }

        SelectionOutline ResolveOutline(Collider hitCollider, IPlayerInteractable interactable)
        {
            if (interactable is Component interactableComponent)
            {
                var outline = interactableComponent.GetComponentInParent<SelectionOutline>();
                if (outline != null)
                    return outline;
            }

            var existingOutline = hitCollider.GetComponentInParent<SelectionOutline>();
            if (existingOutline != null)
                return existingOutline;

            return null;
        }

        void SetFocusedOutline(bool isHighlighted)
        {
            if (m_FocusedOutline != null)
                m_FocusedOutline.ToggleOutline(isHighlighted);
        }

        void ConfigureProjectLayers()
        {
            m_InteractableLayer = LayerMask.NameToLayer(interactableLayerName);
            m_OutlineLayer = LayerMask.NameToLayer(outlineLayerName);

            if (!autoUseProjectInteractionLayers)
                return;

            if (interactionMask.value != -1 && interactionMask.value != 0)
                return;

            var mask = 0;
            if (m_InteractableLayer >= 0)
                mask |= 1 << m_InteractableLayer;

            if (m_OutlineLayer >= 0)
                mask |= 1 << m_OutlineLayer;

            if (mask != 0)
                interactionMask = mask;
        }

        void UpdateHold(PlayerInteractionContext context)
        {
            if (m_ActiveHoldInteractable == null || !m_ActiveHoldInteractable.RequiresHold)
                return;

            var holdDuration = Mathf.Max(0.01f, m_ActiveHoldInteractable.HoldDuration);
            HoldProgress = Mathf.Clamp01(playerInput.InteractHoldTime / holdDuration);
            m_ActiveHoldInteractable.OnHoldProgress(context, HoldProgress);

            if (HoldProgress < 1f || m_InteractionConsumed)
                return;

            m_ActiveHoldInteractable.OnInteract(context);
            m_InteractionConsumed = true;
        }

        void SendFocusEnter()
        {
            if (m_FocusedInteractable == null || playerInput == null)
                return;

            m_FocusedInteractable.OnFocusEnter(CreateContext(0f));
        }

        void SendFocusExit()
        {
            if (m_FocusedInteractable == null || playerInput == null)
                return;

            m_FocusedInteractable.OnFocusExit(CreateContext(0f));
        }

        void ResetHold()
        {
            if (m_ActiveHoldInteractable != null && playerInput != null)
                m_ActiveHoldInteractable.OnHoldProgress(CreateContext(0f), 0f);

            m_ActiveHoldInteractable = null;
            m_InteractionConsumed = false;
            HoldProgress = 0f;
        }

        PlayerInteractionContext CreateContext(float holdTime)
        {
            return new PlayerInteractionContext(playerInput, this, rayCamera, m_CurrentHit, holdTime);
        }
    }
}
