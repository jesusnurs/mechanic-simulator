using BigDreamLab.Player;
using UnityEngine;

namespace BigDreamLab.Scenario
{
    public sealed class WheelBoltInteractable : MonoBehaviour, IPlayerInteractable, IPlayerFocusVisibility
    {
        [SerializeField] ScenarioStepInteractable scenarioAction;

        ScenarioStepInteractable Action
        {
            get
            {
                if (scenarioAction == null)
                    scenarioAction = GetComponent<ScenarioStepInteractable>();

                return scenarioAction;
            }
        }

        public string InteractionPrompt => Action != null ? Action.InteractionPrompt : "ЛКМ - открутить болт";
        public bool RequiresHold => Action != null && Action.RequiresHold;
        public float HoldDuration => Action != null ? Action.HoldDuration : 0f;

        public bool ShouldShowFocus(PlayerInteractionContext context)
        {
            return Action != null && Action.ShouldShowFocus(context);
        }

        public bool CanInteract(PlayerInteractionContext context, out string unavailableReason)
        {
            if (Action != null)
                return Action.CanInteract(context, out unavailableReason);

            unavailableReason = "Сценарий ещё не готов";
            return false;
        }

        public void OnFocusEnter(PlayerInteractionContext context)
        {
            Action?.OnFocusEnter(context);
        }

        public void OnFocusExit(PlayerInteractionContext context)
        {
            Action?.OnFocusExit(context);
        }

        public void OnHoldProgress(PlayerInteractionContext context, float normalizedProgress)
        {
            Action?.OnHoldProgress(context, normalizedProgress);
        }

        public void OnInteract(PlayerInteractionContext context)
        {
            Action?.OnInteract(context);
        }
    }
}
