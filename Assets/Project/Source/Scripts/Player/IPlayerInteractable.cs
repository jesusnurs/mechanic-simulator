using UnityEngine;

namespace BigDreamLab.Player
{
    public readonly struct PlayerInteractionContext
    {
        public PlayerInteractionContext(
            DesktopFirstPersonController player,
            PlayerInteractionRaycaster interactor,
            Camera camera,
            RaycastHit hit,
            float holdTime)
        {
            Player = player;
            Interactor = interactor;
            Camera = camera;
            Hit = hit;
            HoldTime = holdTime;
        }

        public DesktopFirstPersonController Player { get; }
        public PlayerInteractionRaycaster Interactor { get; }
        public Camera Camera { get; }
        public RaycastHit Hit { get; }
        public float HoldTime { get; }
    }

    public interface IPlayerInteractable
    {
        string InteractionPrompt { get; }
        bool RequiresHold { get; }
        float HoldDuration { get; }

        bool CanInteract(PlayerInteractionContext context, out string unavailableReason);
        void OnFocusEnter(PlayerInteractionContext context);
        void OnFocusExit(PlayerInteractionContext context);
        void OnHoldProgress(PlayerInteractionContext context, float normalizedProgress);
        void OnInteract(PlayerInteractionContext context);
    }

    public interface IPlayerFocusVisibility
    {
        bool ShouldShowFocus(PlayerInteractionContext context);
    }
}
