using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigDreamLab.Scenario
{
    [CreateAssetMenu(menuName = "Mechanic Simulator/Scenario/Brake Disc Text Config", fileName = "BrakeDiscScenarioTextConfig")]
    public sealed class BrakeDiscScenarioTextConfig : ScriptableObject
    {
        public const string EmptyHandsToolId = "Hands";
        public const string DynamometricWrenchToolId = "DynamometricWrench";

        [Serializable]
        public sealed class StepText
        {
            public BrakeDiscScenarioStep step;
            public string header;
            [TextArea(2, 4)] public string task;
            public string prompt;
            public string progressFormat;
            public string requiredToolId = EmptyHandsToolId;
        }

        public List<StepText> steps = new List<StepText>
        {
            new StepText { step = BrakeDiscScenarioStep.RaiseCarWithJack, header = "Этап 1: Подготовка", task = "Нажмите на рычаг Carjack, чтобы поднять Caret.", prompt = "ЛКМ - поднять машину" },
            new StepText { step = BrakeDiscScenarioStep.RemoveHubcap, header = "Этап 1: Колесо", task = "Снимите крышку колеса (kalpak).", prompt = "ЛКМ - снять крышку" },
            new StepText { step = BrakeDiscScenarioStep.PlaceHubcapOnTable, header = "Этап 1: Колесо", task = "Положите крышку на отмеченное место на столе.", prompt = "ЛКМ - положить крышку" },
            new StepText { step = BrakeDiscScenarioStep.RemoveWheelBolts, header = "Этап 1: Колесо", task = "Открутите 5 болтов колеса.", prompt = "ЛКМ - открутить болт", progressFormat = "Болты сняты: {0}/{1}", requiredToolId = DynamometricWrenchToolId },
            new StepText { step = BrakeDiscScenarioStep.PlaceWheelBoltsOnTable, header = "Этап 1: Колесо", task = "Положите снятые болты колеса на отмеченные места на столе.", prompt = "ЛКМ - положить болт", progressFormat = "Болты на столе: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.RemoveWheel, header = "Этап 1: Колесо", task = "Снимите колесо со ступицы.", prompt = "ЛКМ - снять колесо" },
            new StepText { step = BrakeDiscScenarioStep.PlaceWheelOnTable, header = "Этап 1: Колесо", task = "Поставьте колесо на его место на столе.", prompt = "ЛКМ - поставить колесо" },
            new StepText { step = BrakeDiscScenarioStep.RemoveCaliperBolts, header = "Этап 2: Суппорт", task = "Открутите болты крепления суппорта.", prompt = "ЛКМ - открутить болт суппорта", progressFormat = "Болты суппорта сняты: {0}/{1}", requiredToolId = DynamometricWrenchToolId },
            new StepText { step = BrakeDiscScenarioStep.PlaceCaliperBoltsOnTable, header = "Этап 2: Суппорт", task = "Положите болты суппорта на отмеченные места на столе.", prompt = "ЛКМ - положить болт суппорта", progressFormat = "Болты суппорта на столе: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.RemoveCaliper, header = "Этап 2: Суппорт", task = "Снимите суппорт и освободите доступ к колодкам.", prompt = "ЛКМ - снять суппорт" },
            new StepText { step = BrakeDiscScenarioStep.PlaceCaliperOnTable, header = "Этап 2: Суппорт", task = "Положите суппорт на отмеченное место на столе.", prompt = "ЛКМ - положить суппорт" },
            new StepText { step = BrakeDiscScenarioStep.RemoveBrakePads, header = "Этап 2: Колодки", task = "Снимите тормозные колодки.", prompt = "ЛКМ - снять колодку", progressFormat = "Колодки сняты: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.PlaceBrakePadsOnTable, header = "Этап 2: Колодки", task = "Положите тормозные колодки на стол.", prompt = "ЛКМ - положить колодку", progressFormat = "Колодки на столе: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.RemoveCaliperBracketBolts, header = "Этап 2: Скоба", task = "Открутите болты скобы суппорта.", prompt = "ЛКМ - открутить болт скобы", progressFormat = "Болты скобы сняты: {0}/{1}", requiredToolId = DynamometricWrenchToolId },
            new StepText { step = BrakeDiscScenarioStep.PlaceCaliperBracketBoltsOnTable, header = "Этап 2: Скоба", task = "Положите болты скобы суппорта на стол.", prompt = "ЛКМ - положить болт скобы", progressFormat = "Болты скобы на столе: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.RemoveCaliperBracket, header = "Этап 2: Скоба", task = "Снимите скобу суппорта.", prompt = "ЛКМ - снять скобу" },
            new StepText { step = BrakeDiscScenarioStep.PlaceCaliperBracketOnTable, header = "Этап 2: Скоба", task = "Положите скобу суппорта на стол.", prompt = "ЛКМ - положить скобу" },
            new StepText { step = BrakeDiscScenarioStep.UnscrewBrakeDiscFixingScrew, header = "Этап 2: Диск", task = "Открутите фиксирующий винт тормозного диска.", prompt = "Удерживайте ЛКМ - открутить винт", requiredToolId = DynamometricWrenchToolId },
            new StepText { step = BrakeDiscScenarioStep.PlaceBrakeDiscFixingScrewOnTable, header = "Этап 2: Диск", task = "Положите фиксирующий винт на стол.", prompt = "ЛКМ - положить винт" },
            new StepText { step = BrakeDiscScenarioStep.RemoveOldBrakeDisc, header = "Этап 2: Диск", task = "Снимите старый тормозной диск.", prompt = "ЛКМ - снять старый диск" },
            new StepText { step = BrakeDiscScenarioStep.PlaceOldBrakeDiscOnTable, header = "Этап 2: Диск", task = "Положите старый тормозной диск на стол.", prompt = "ЛКМ - положить старый диск" },
            new StepText { step = BrakeDiscScenarioStep.InstallNewBrakeDisc, header = "Этап 3: Новый диск", task = "Установите новый тормозной диск со стола.", prompt = "ЛКМ - установить новый диск" },
            new StepText { step = BrakeDiscScenarioStep.TakeBrakeDiscFixingScrewFromTable, header = "Этап 3: Новый диск", task = "Возьмите фиксирующий винт со стола.", prompt = "ЛКМ - взять винт" },
            new StepText { step = BrakeDiscScenarioStep.InsertBrakeDiscFixingScrew, header = "Этап 3: Новый диск", task = "Вставьте фиксирующий винт в тормозной диск.", prompt = "ЛКМ - вставить винт" },
            new StepText { step = BrakeDiscScenarioStep.TightenBrakeDiscFixingScrew, header = "Этап 3: Новый диск", task = "Затяните фиксирующий винт.", prompt = "Удерживайте ЛКМ - затянуть винт", requiredToolId = DynamometricWrenchToolId },
            new StepText { step = BrakeDiscScenarioStep.TakeCaliperBracketFromTable, header = "Этап 3: Сборка", task = "Возьмите скобу суппорта со стола.", prompt = "ЛКМ - взять скобу" },
            new StepText { step = BrakeDiscScenarioStep.InstallCaliperBracket, header = "Этап 3: Сборка", task = "Установите скобу суппорта на место.", prompt = "ЛКМ - установить скобу" },
            new StepText { step = BrakeDiscScenarioStep.TakeCaliperBracketBoltsFromTable, header = "Этап 3: Сборка", task = "Возьмите болты скобы суппорта со стола.", prompt = "ЛКМ - взять болт скобы", progressFormat = "Болты взяты: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.TightenCaliperBracketBolts, header = "Этап 3: Сборка", task = "Поставьте и затяните болты скобы суппорта.", prompt = "Удерживайте ЛКМ - затянуть болт скобы", progressFormat = "Болты скобы затянуты: {0}/{1}", requiredToolId = DynamometricWrenchToolId },
            new StepText { step = BrakeDiscScenarioStep.TakeBrakePadsFromTable, header = "Этап 3: Сборка", task = "Возьмите тормозные колодки со стола.", prompt = "ЛКМ - взять колодку", progressFormat = "Колодки взяты: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.InstallBrakePads, header = "Этап 3: Сборка", task = "Установите тормозные колодки на место.", prompt = "ЛКМ - установить колодку", progressFormat = "Колодки установлены: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.TakeCaliperFromTable, header = "Этап 3: Сборка", task = "Возьмите суппорт со стола.", prompt = "ЛКМ - взять суппорт" },
            new StepText { step = BrakeDiscScenarioStep.InstallCaliper, header = "Этап 3: Сборка", task = "Установите суппорт на место.", prompt = "ЛКМ - установить суппорт" },
            new StepText { step = BrakeDiscScenarioStep.TakeCaliperBoltsFromTable, header = "Этап 3: Сборка", task = "Возьмите болты суппорта со стола.", prompt = "ЛКМ - взять болт суппорта", progressFormat = "Болты взяты: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.TightenCaliperBolts, header = "Этап 3: Сборка", task = "Поставьте и затяните болты крепления суппорта.", prompt = "Удерживайте ЛКМ - затянуть болт суппорта", progressFormat = "Болты суппорта затянуты: {0}/{1}", requiredToolId = DynamometricWrenchToolId },
            new StepText { step = BrakeDiscScenarioStep.TakeWheelFromTable, header = "Этап 4: Колесо", task = "Возьмите колесо со стола.", prompt = "ЛКМ - взять колесо" },
            new StepText { step = BrakeDiscScenarioStep.InstallWheel, header = "Этап 4: Колесо", task = "Установите колесо обратно на ступицу.", prompt = "ЛКМ - установить колесо" },
            new StepText { step = BrakeDiscScenarioStep.TakeWheelBoltsFromTable, header = "Этап 4: Колесо", task = "Возьмите болты колеса со стола.", prompt = "ЛКМ - взять болт колеса", progressFormat = "Болты взяты: {0}/{1}" },
            new StepText { step = BrakeDiscScenarioStep.TightenWheelBolts, header = "Этап 4: Колесо", task = "Поставьте и затяните 5 болтов колеса.", prompt = "Удерживайте ЛКМ - затянуть болт колеса", progressFormat = "Болты колеса затянуты: {0}/{1}", requiredToolId = DynamometricWrenchToolId },
            new StepText { step = BrakeDiscScenarioStep.TakeHubcapFromTable, header = "Этап 4: Колесо", task = "Возьмите колпак со стола.", prompt = "ЛКМ - взять колпак" },
            new StepText { step = BrakeDiscScenarioStep.InstallHubcap, header = "Этап 4: Колесо", task = "Поставьте колпак обратно на колесо.", prompt = "ЛКМ - поставить колпак" },
            new StepText { step = BrakeDiscScenarioStep.Complete, header = "Готово", task = "Ремонт завершён. Тормозной диск заменён.", prompt = string.Empty },
        };

        [Header("Blocked Reasons")]
        public string scenarioNotReadyReason = "Сценарий ещё не готов";
        public string differentTaskReason = "Сейчас нужно выполнить другую задачу";
        public string alreadyDoneReason = "Это действие уже выполнено";
        public string wrongToolReasonFormat = "Нужен инструмент: {0}";
        public string emptyHandsRequiredReason = "Уберите инструмент: нажмите F";

        public string GetHeader(BrakeDiscScenarioStep step)
        {
            return FindStep(step)?.header ?? "Сценарий";
        }

        public string GetTask(BrakeDiscScenarioStep step)
        {
            return FindStep(step)?.task ?? step.ToString();
        }

        public string GetPrompt(BrakeDiscScenarioStep step)
        {
            return FindStep(step)?.prompt ?? "ЛКМ - взаимодействовать";
        }

        public string GetRequiredToolId(BrakeDiscScenarioStep step)
        {
            return FindStep(step)?.requiredToolId ?? string.Empty;
        }

        public string FormatProgress(BrakeDiscScenarioStep step, int completed, int total)
        {
            if (total <= 1)
                return string.Empty;

            var format = FindStep(step)?.progressFormat;
            return string.IsNullOrWhiteSpace(format) ? $"{completed}/{total}" : string.Format(format, completed, total);
        }

        public string FormatWrongToolReason(string toolName)
        {
            return string.Format(wrongToolReasonFormat, toolName);
        }

        public static bool IsEmptyHandsToolId(string toolId)
        {
            return string.Equals(toolId, EmptyHandsToolId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(toolId, "EmptyHands", StringComparison.OrdinalIgnoreCase);
        }

        StepText FindStep(BrakeDiscScenarioStep step)
        {
            foreach (var stepText in steps)
            {
                if (stepText != null && stepText.step == step)
                    return stepText;
            }

            return null;
        }
    }
}
