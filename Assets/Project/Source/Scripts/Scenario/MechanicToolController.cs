using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigDreamLab.Scenario
{
    [DefaultExecutionOrder(-15)]
    public sealed class MechanicToolController : MonoBehaviour
    {
        [Serializable]
        public sealed class ToolSlot
        {
            public string toolId = "tool";
            public string displayName = "Инструмент";
            public GameObject handObject;
            public bool selectedOnStart;
        }

        [SerializeField] List<ToolSlot> tools = new List<ToolSlot>
        {
            new ToolSlot
            {
                toolId = "DynamometricWrench",
                displayName = "Динамометрический ключ",
            },
        };
        [SerializeField] string emptyToolName = "Руки";

        int m_ActiveToolIndex = -1;
        bool m_HasAppliedSelection;

        public bool HasSelectedTool => IsValidToolIndex(m_ActiveToolIndex);
        public string ActiveToolId => IsValidToolIndex(m_ActiveToolIndex) ? tools[m_ActiveToolIndex].toolId : string.Empty;
        public string ActiveToolName => IsValidToolIndex(m_ActiveToolIndex) ? tools[m_ActiveToolIndex].displayName : emptyToolName;

        public event Action ToolChanged;

        void Awake()
        {
            var selectedIndex = -1;
            for (var i = 0; i < tools.Count; i++)
            {
                if (tools[i] != null && tools[i].selectedOnStart)
                {
                    selectedIndex = i;
                    break;
                }
            }

            SelectTool(selectedIndex);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.fKey.wasPressedThisFrame)
            {
                ClearTool();
                return;
            }

            for (var i = 0; i < tools.Count && i < 9; i++)
            {
                if (WasNumberPressed(keyboard, i + 1))
                {
                    SelectTool(i);
                    return;
                }
            }
        }

        public bool HasTool(string requiredToolId)
        {
            return string.IsNullOrWhiteSpace(requiredToolId) ||
                string.Equals(ActiveToolId, requiredToolId, StringComparison.OrdinalIgnoreCase);
        }

        public string GetToolName(string toolId)
        {
            if (string.IsNullOrWhiteSpace(toolId))
                return emptyToolName;

            foreach (var tool in tools)
            {
                if (tool != null && string.Equals(tool.toolId, toolId, StringComparison.OrdinalIgnoreCase))
                    return tool.displayName;
            }

            return toolId;
        }

        public void ClearTool()
        {
            SelectTool(-1);
        }

        public void SelectTool(int index)
        {
            if (index < -1 || index >= tools.Count)
                return;

            if (m_ActiveToolIndex == index && m_HasAppliedSelection)
                return;

            m_ActiveToolIndex = index;
            m_HasAppliedSelection = true;
            for (var i = 0; i < tools.Count; i++)
            {
                var handObject = tools[i] != null ? tools[i].handObject : null;
                if (handObject != null)
                    handObject.SetActive(i == m_ActiveToolIndex);
            }

            ToolChanged?.Invoke();
        }

        bool IsValidToolIndex(int index)
        {
            return index >= 0 && index < tools.Count && tools[index] != null;
        }

        static bool WasNumberPressed(Keyboard keyboard, int number)
        {
            return number switch
            {
                1 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
                2 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
                3 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
                4 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
                5 => keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame,
                6 => keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame,
                7 => keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame,
                8 => keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame,
                9 => keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame,
                _ => false,
            };
        }
    }
}
