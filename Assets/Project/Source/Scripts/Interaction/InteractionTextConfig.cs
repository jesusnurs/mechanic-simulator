using UnityEngine;

namespace BigDreamLab.Interaction
{
    [CreateAssetMenu(menuName = "Mechanic Simulator/Interaction/Text Config", fileName = "InteractionTextConfig")]
    public sealed class InteractionTextConfig : ScriptableObject
    {
        public string fallbackPrompt = "ЛКМ / E - взаимодействовать";
        public string genericInteractionPrompt = "ЛКМ / E - взаимодействовать";
        public string unavailableReason = "Сейчас недоступно";
    }
}
