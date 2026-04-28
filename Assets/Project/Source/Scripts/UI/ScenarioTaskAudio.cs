using UnityEngine;

namespace BigDreamLab.UI
{
    public sealed class ScenarioTaskAudio : MonoBehaviour
    {
        [SerializeField] ScenarioTaskData tasks;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip taskCompleteAudio;

        void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        public void PlayTaskComplete()
        {
            if (audioSource == null || taskCompleteAudio == null)
                return;

            audioSource.PlayOneShot(taskCompleteAudio);
        }
    }
}
