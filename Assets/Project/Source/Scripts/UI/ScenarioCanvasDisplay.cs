using TMPro;
using UnityEngine;

namespace BigDreamLab.UI
{
    public sealed class ScenarioCanvasDisplay : MonoBehaviour
    {
        [SerializeField] ScenarioTaskData tasks;
        [SerializeField] TextMeshProUGUI headerDisplay;
        [SerializeField] TextMeshProUGUI textDisplay;
        [SerializeField] int currentTaskIndex;

        public int CurrentTaskIndex => currentTaskIndex;

        void Start()
        {
            Refresh();
        }

        public void NextTask()
        {
            if (tasks == null || tasks.Count == 0)
                return;

            currentTaskIndex = Mathf.Min(currentTaskIndex + 1, tasks.Count - 1);
            Refresh();
        }

        public void PreviousTask()
        {
            currentTaskIndex = Mathf.Max(currentTaskIndex - 1, 0);
            Refresh();
        }

        public void SetTaskIndex(int index)
        {
            currentTaskIndex = Mathf.Max(index, 0);
            Refresh();
        }

        public void SetTaskText(string header, string text)
        {
            if (headerDisplay != null)
                headerDisplay.text = header;

            if (textDisplay != null)
                textDisplay.text = text;
        }

        public void Refresh()
        {
            var task = tasks != null ? tasks.GetTask(currentTaskIndex) : ScenarioTaskEntry.Empty;
            SetTaskText(task.header, task.text);
        }
    }
}
