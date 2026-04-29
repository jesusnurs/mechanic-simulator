using UnityEngine;

namespace BigDreamLab.UI
{
    [CreateAssetMenu(menuName = "Mechanic Simulator/UI/HUD Text Config", fileName = "MechanicHudTextConfig")]
    public sealed class MechanicHudTextConfig : ScriptableObject
    {
        [Header("Initial Task")]
        public string taskHeader;
        public string taskText;
        public string progressText;
        public string controlHintText;

        [Header("Welcome")]
        public string welcomeTitle;
        [TextArea(4, 8)] public string welcomeText;
        public string welcomeCloseText;
    }
}
