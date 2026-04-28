using System.Collections.Generic;
using UnityEngine;

namespace BigDreamLab.UI
{
    [CreateAssetMenu(menuName = "Mechanic Simulator/Scenario Tasks", fileName = "ScenarioTasks")]
    public sealed class ScenarioTaskData : ScriptableObject
    {
        [SerializeField] List<ScenarioTaskEntry> tasks = new List<ScenarioTaskEntry>();

        public int Count => tasks.Count;

        public ScenarioTaskEntry GetTask(int index)
        {
            if (tasks.Count == 0)
                return ScenarioTaskEntry.Empty;

            return tasks[Mathf.Clamp(index, 0, tasks.Count - 1)];
        }
    }

    [System.Serializable]
    public struct ScenarioTaskEntry
    {
        public string header;
        [TextArea(2, 6)] public string text;

        public static ScenarioTaskEntry Empty => new ScenarioTaskEntry
        {
            header = "Сценарий",
            text = "Текст сценария"
        };
    }
}
