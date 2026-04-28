using BigDreamLab.Player;
using UnityEngine;
using UnityEngine.Events;

namespace BigDreamLab.Interaction
{
    public sealed class SimplePlayerInteractable : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField] InteractionTextConfig textConfig;
        [SerializeField] bool requiresHold;
        [SerializeField, Min(0.05f)] float holdDuration = 1.2f;
        [SerializeField] bool canInteract = true;
        [SerializeField] UnityEvent interacted;
        [SerializeField] UnityEvent<float> holdProgressChanged;

        public string InteractionPrompt => textConfig != null ? textConfig.genericInteractionPrompt : "ЛКМ / E - взаимодействовать";
        public bool RequiresHold => requiresHold;
        public float HoldDuration => holdDuration;

        public bool CanInteract(PlayerInteractionContext context, out string reason)
        {
            reason = canInteract
                ? string.Empty
                : textConfig != null ? textConfig.unavailableReason : "Сейчас недоступно";
            return canInteract;
        }

        public void OnFocusEnter(PlayerInteractionContext context)
        {
        }

        public void OnFocusExit(PlayerInteractionContext context)
        {
            holdProgressChanged?.Invoke(0f);
        }

        public void OnHoldProgress(PlayerInteractionContext context, float normalizedProgress)
        {
            holdProgressChanged?.Invoke(normalizedProgress);
        }

        public void OnInteract(PlayerInteractionContext context)
        {
            interacted?.Invoke();
        }
    }
}
