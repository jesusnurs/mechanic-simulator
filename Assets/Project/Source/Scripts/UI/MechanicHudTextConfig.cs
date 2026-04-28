using UnityEngine;

namespace BigDreamLab.UI
{
    [CreateAssetMenu(menuName = "Mechanic Simulator/UI/HUD Text Config", fileName = "MechanicHudTextConfig")]
    public sealed class MechanicHudTextConfig : ScriptableObject
    {
        [Header("Initial Task")]
        public string taskHeader = "Этап 1: Подготовка рабочего места";
        public string taskText = "Открутите 5 болтов колеса";
        public string progressText = "Прогресс: 0/5";

        [Header("Welcome")]
        public string welcomeTitle = "Добро пожаловать в мастерскую";
        [TextArea(4, 8)] public string welcomeText =
            "Управление:\n" +
            "WASD / стрелки - движение\n" +
            "Мышь - осмотр\n" +
            "Shift - ускориться\n" +
            "ЛКМ / E - взаимодействовать\n" +
            "1-9 - выбрать инструмент\n" +
            "F - убрать инструмент\n" +
            "Esc - освободить курсор";
        public string welcomeCloseText = "Нажмите Enter / Space / E / ЛКМ, чтобы начать";
    }
}
