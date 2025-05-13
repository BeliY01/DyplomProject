using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using TMPro;

namespace PaulosDebug
{
    public class DebugMenu : MonoBehaviour
    {
        //public GraphicsSettingsMenu_URP graphicsMenu;
        [SerializeField]
        private bool debugOn;
        [SerializeField]
        private TMP_Text debugText;
        [SerializeField]
        private AudioMixer masterMixer;
        [SerializeField]
        private AudioSource fxsAudio, musicAudio;
        [SerializeField]
        private GameObject clearSaveObj;

        // Update is called once per frame
        void Update()
        {
            if (!fxsAudio.isPlaying)
                fxsAudio.Play();

            if (!musicAudio.isPlaying)
                musicAudio.Play();
        }

        private void Start()
        {
            debugText.gameObject.SetActive(debugOn);
            clearSaveObj.SetActive(debugOn);

            if (debugOn)
                StartCoroutine(RefreshDebug());
        }

        private IEnumerator RefreshDebug()
        {
            while (true)
            {
                debugText.text = "Engine Settings\n";

                debugText.text += QualitySettings.names[QualitySettings.GetQualityLevel()] + " : Quality\n";
                debugText.text += Screen.currentResolution + " : Resolution\n";
                debugText.text += QualitySettings.shadowResolution + " : ShadorResolution\n";
                debugText.text += Screen.fullScreenMode + " : ScreenMode\n";
                debugText.text += QualitySettings.vSyncCount + " : Vsync\n";
                debugText.text += QualitySettings.antiAliasing + " : AA\n";
                debugText.text += QualitySettings.masterTextureLimit + " : TextureQuality\n";
                debugText.text += QualitySettings.anisotropicFiltering + " : AnisoFilteringMode\n";
                //debugText.text += graphicsMenu.CurrentSettings.AnisotropicLevel + " : AnisoLevel\n";

                AudioConfiguration config = AudioSettings.GetConfiguration();

                masterMixer.GetFloat("mainVolume", out float val);
                debugText.text += val + " : Vol Main\n";
                masterMixer.GetFloat("fxVolume", out float val2);
                debugText.text += val2 + " : Vol FXs\n";
                masterMixer.GetFloat("musicVolume", out float val3);
                debugText.text += val3 + " : Vol Music\n";
                debugText.text += config.speakerMode + " : SpeakerMode\n";

                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
