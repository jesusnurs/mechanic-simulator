using System.Collections.Generic;
using BigDreamLab.Interaction;
using BigDreamLab.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigDreamLab.Scenario
{
    public enum BrakeDiscScenarioStep
    {
        RaiseCarWithJack = 0,
        RemoveHubcap = 1,
        PlaceHubcapOnTable = 2,
        RemoveWheelBolts = 3,
        PlaceWheelBoltsOnTable = 4,
        RemoveWheel = 5,
        PlaceWheelOnTable = 6,
        RemoveCaliperBolts = 7,
        PlaceCaliperBoltsOnTable = 8,
        RemoveCaliper = 9,
        PlaceCaliperOnTable = 10,
        RemoveBrakePads = 11,
        PlaceBrakePadsOnTable = 12,
        RemoveCaliperBracketBolts = 13,
        PlaceCaliperBracketBoltsOnTable = 14,
        RemoveCaliperBracket = 15,
        PlaceCaliperBracketOnTable = 16,
        RemoveOldBrakeDisc = 19,
        PlaceOldBrakeDiscOnTable = 20,
        TakeNewBrakeDiscFromTable = 21,
        InstallNewBrakeDisc = 22,
        TakeCaliperBracketFromTable = 25,
        InstallCaliperBracket = 26,
        TakeCaliperBracketBoltsFromTable = 27,
        TightenCaliperBracketBolts = 28,
        TakeBrakePadsFromTable = 29,
        InstallBrakePads = 30,
        TakeCaliperFromTable = 31,
        InstallCaliper = 32,
        TakeCaliperBoltsFromTable = 33,
        TightenCaliperBolts = 34,
        TakeWheelFromTable = 35,
        InstallWheel = 36,
        TakeWheelBoltsFromTable = 37,
        TightenWheelBolts = 38,
        TakeHubcapFromTable = 39,
        InstallHubcap = 40,
        Complete = 41,
    }

    [DefaultExecutionOrder(-5)]
    public sealed class BrakeDiscScenarioManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] MechanicPlayerHud hud;
        [SerializeField] BrakeDiscScenarioTextConfig textConfig;
        [SerializeField] MechanicToolController toolController;
        [SerializeField] List<ScenarioStepInteractable> actions = new List<ScenarioStepInteractable>();

        [Header("Flow")]
        [SerializeField] BrakeDiscScenarioStep initialStep = BrakeDiscScenarioStep.RaiseCarWithJack;
        [SerializeField] bool resetActionsOnStart = true;
        [SerializeField] bool toggleSpaceToHighlightCurrentTargets = true;
        [SerializeField] bool forceEnableCurrentActionColliders = true;

        public BrakeDiscScenarioStep CurrentStep { get; private set; }
        public string ScenarioNotReadyReason => textConfig != null ? textConfig.scenarioNotReadyReason : "Сценарий ещё не готов";
        public string DifferentTaskReason => textConfig != null ? textConfig.differentTaskReason : "Сейчас нужно выполнить другую задачу";
        public string AlreadyDoneReason => textConfig != null ? textConfig.alreadyDoneReason : "Это действие уже выполнено";

        readonly List<SelectionOutline> m_HintOutlines = new List<SelectionOutline>();
        bool m_IsHintVisible;

        void OnValidate()
        {
            SortActionsForInspector();
        }

        void Awake()
        {
            CurrentStep = initialStep;
            ConfigureActions();

            if (resetActionsOnStart)
                ResetActions();

            RefreshActionFocus();
            RefreshHintTargets();
            RefreshHud();
        }

        void OnEnable()
        {
            if (toolController != null)
                toolController.ToolChanged += RefreshHud;
        }

        void OnDisable()
        {
            if (toolController != null)
                toolController.ToolChanged -= RefreshHud;

            SetHintVisible(false);
        }

        void Update()
        {
            UpdateHintInput();
        }

        public bool IsCurrentTarget(ScenarioStepInteractable action)
        {
            return action != null && !action.IsCompleted && action.Step == CurrentStep;
        }

        public bool IsRequiredToolSelected(string requiredToolId)
        {
            if (string.IsNullOrWhiteSpace(requiredToolId))
                return true;

            if (BrakeDiscScenarioTextConfig.IsEmptyHandsToolId(requiredToolId))
                return toolController == null || !toolController.HasSelectedTool;

            return toolController != null && toolController.HasTool(requiredToolId);
        }

        public string GetWrongToolReason(string requiredToolId)
        {
            if (BrakeDiscScenarioTextConfig.IsEmptyHandsToolId(requiredToolId))
                return textConfig != null ? textConfig.emptyHandsRequiredReason : "Уберите инструмент: нажмите F";

            var toolName = toolController != null ? toolController.GetToolName(requiredToolId) : requiredToolId;
            return textConfig != null
                ? textConfig.FormatWrongToolReason(toolName)
                : $"Нужен инструмент: {toolName}";
        }

        public string GetRequiredToolId(ScenarioStepInteractable action)
        {
            if (action != null && !string.IsNullOrWhiteSpace(action.RequiredToolId))
                return action.RequiredToolId;

            return action != null && textConfig != null ? textConfig.GetRequiredToolId(action.Step) : string.Empty;
        }

        public string GetPrompt(BrakeDiscScenarioStep step)
        {
            return textConfig != null ? textConfig.GetPrompt(step) : "ЛКМ - взаимодействовать";
        }

        public void NotifyActionCompleted(ScenarioStepInteractable action)
        {
            if (action == null || action.Step != CurrentStep)
                return;

            if (IsCurrentStepComplete())
                AdvanceToNextStepWithActions();

            RefreshActionFocus();
            RefreshHintTargets();
            RefreshHud();
        }

        public void SetStep(BrakeDiscScenarioStep step)
        {
            CurrentStep = step;
            RefreshActionFocus();
            RefreshHintTargets();
            RefreshHud();
        }

        void ConfigureActions()
        {
            actions.RemoveAll(action => action == null);
            foreach (var sceneAction in FindObjectsByType<ScenarioStepInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sceneAction != null && !actions.Contains(sceneAction))
                    actions.Add(sceneAction);
            }

            SortActionsForInspector();

            foreach (var action in actions)
                action.Configure(this);
        }

        void SortActionsForInspector()
        {
            actions.RemoveAll(action => action == null);
            actions.Sort((left, right) => left.Step.CompareTo(right.Step));
        }

        void ResetActions()
        {
            foreach (var action in actions)
                action.ResetCompletion();
        }

        void AdvanceToNextStepWithActions()
        {
            var nextStep = (BrakeDiscScenarioStep)((int)CurrentStep + 1);
            while ((int)nextStep < (int)BrakeDiscScenarioStep.Complete && CountActions(nextStep) == 0)
                nextStep = (BrakeDiscScenarioStep)((int)nextStep + 1);

            CurrentStep = nextStep;
        }

        bool IsCurrentStepComplete()
        {
            var total = 0;
            var completed = 0;

            foreach (var action in actions)
            {
                if (action.Step != CurrentStep)
                    continue;

                total++;
                if (action.IsCompleted)
                    completed++;
            }

            return total > 0 && completed >= total;
        }

        int CountActions(BrakeDiscScenarioStep step)
        {
            var count = 0;
            foreach (var action in actions)
            {
                if (action != null && action.Step == step)
                    count++;
            }

            return count;
        }

        int CountCompletedActions(BrakeDiscScenarioStep step)
        {
            var count = 0;
            foreach (var action in actions)
            {
                if (action != null && action.Step == step && action.IsCompleted)
                    count++;
            }

            return count;
        }

        void RefreshActionFocus()
        {
            var currentColliders = new HashSet<Collider>();
            var allInteractionColliders = new HashSet<Collider>();
            foreach (var action in actions)
            {
                if (action == null)
                    continue;

                var actionColliders = action.GetInteractionColliders();
                foreach (var targetCollider in actionColliders)
                {
                    if (targetCollider != null)
                        allInteractionColliders.Add(targetCollider);
                }

                if (!IsCurrentTarget(action))
                    continue;

                foreach (var targetCollider in actionColliders)
                {
                    if (targetCollider != null)
                        currentColliders.Add(targetCollider);
                }
            }

            foreach (var action in actions)
            {
                if (action == null)
                    continue;

                var isCurrentTarget = IsCurrentTarget(action);
                action.SetScenarioFocusAllowed(isCurrentTarget);
            }

            foreach (var targetCollider in allInteractionColliders)
            {
                if (targetCollider != null)
                    targetCollider.enabled = currentColliders.Contains(targetCollider);
            }

            if (!forceEnableCurrentActionColliders)
            {
                foreach (var action in actions)
                {
                    if (action != null)
                        action.SetInteractionCollidersEnabled(IsCurrentTarget(action));
                }
            }
        }

        void RefreshHud()
        {
            if (hud == null)
                return;

            var completed = CountCompletedActions(CurrentStep);
            var total = CountActions(CurrentStep);
            var header = textConfig != null ? textConfig.GetHeader(CurrentStep) : "Сценарий";
            var task = textConfig != null ? textConfig.GetTask(CurrentStep) : CurrentStep.ToString();
            var progress = textConfig != null ? textConfig.FormatProgress(CurrentStep, completed, total) : string.Empty;
            var toolName = toolController != null && toolController.HasSelectedTool && CurrentStep != BrakeDiscScenarioStep.Complete
                ? toolController.ActiveToolName
                : string.Empty;

            hud.SetToolName(toolName);
            hud.SetTask(header, task, progress);

            if (CurrentStep == BrakeDiscScenarioStep.Complete)
                hud.ShowVictory();
        }

        void UpdateHintInput()
        {
            if (!toggleSpaceToHighlightCurrentTargets)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
                return;

            SetHintVisible(!m_IsHintVisible);
        }

        void SetHintVisible(bool isVisible)
        {
            if (m_IsHintVisible == isVisible)
                return;

            m_IsHintVisible = isVisible;
            RefreshHintTargets();
        }

        void RefreshHintTargets()
        {
            for (var i = m_HintOutlines.Count - 1; i >= 0; i--)
            {
                var outline = m_HintOutlines[i];
                if (outline != null)
                    outline.ToggleHintOutline(false);
            }

            m_HintOutlines.Clear();

            if (!m_IsHintVisible)
                return;

            foreach (var action in actions)
            {
                if (!IsCurrentTarget(action))
                    continue;

                var outline = action.GetComponentInParent<SelectionOutline>();
                if (outline == null || m_HintOutlines.Contains(outline))
                    continue;

                outline.ToggleHintOutline(true);
                m_HintOutlines.Add(outline);
            }
        }

    }
}
