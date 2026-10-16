using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rustbound.UI
{
    public sealed class ModalSettingsController : MonoBehaviour
    {
        [SerializeField] private Slider music;
        [SerializeField] private Slider sfx;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private SpriteNumberLabel musicPercent;
        [SerializeField] private SpriteNumberLabel sfxPercent;
        [SerializeField] private UnityEvent settingsChanged = new UnityEvent();
        public float MusicVolume => music.value;
        public float SoundVolume => sfx.value;
        public UnityEvent SettingsChanged => settingsChanged;

        private void OnEnable()
        {
            music.SetValueWithoutNotify(PlayerPrefs.GetFloat("Settings.MusicVolume",0.7f));
            sfx.SetValueWithoutNotify(PlayerPrefs.GetFloat("Settings.SoundVolume",0.8f));
            fullscreen.SetIsOnWithoutNotify(Application.isPlaying&&!Application.isEditor?GameDisplay.IsFullscreen:PlayerPrefs.GetInt("Settings.Fullscreen",1)==1);
            Refresh();
        }
        public void MusicChanged(float value) { Refresh(); Save(); }
        public void SoundChanged(float value) { Refresh(); Save(); }
        public void FullscreenChanged(bool value)
        {
            if (Application.isPlaying) GameDisplay.SetFullscreen(value);
            Save();
        }
        public void RestoreDefaults()
        {
            music.SetValueWithoutNotify(0.7f); sfx.SetValueWithoutNotify(0.8f);
            fullscreen.SetIsOnWithoutNotify(true);
            Refresh();
            if (Application.isPlaying) GameDisplay.SetFullscreen(true);
            Save();
        }
        private void Refresh()
        {
            musicPercent.SetValue(Mathf.RoundToInt(music.value*100) + "%");
            sfxPercent.SetValue(Mathf.RoundToInt(sfx.value*100) + "%");
            if(Application.isPlaying)Rustbound.World.GameAudio.Ensure().ApplyVolumes(music.value,sfx.value);
        }
        private void Save()
        {
            if (Application.isPlaying && !Rustbound.World.LevelSession.Validation)
            {
                PlayerPrefs.SetFloat("Settings.MusicVolume",music.value);
                PlayerPrefs.SetFloat("Settings.SoundVolume",sfx.value);
                PlayerPrefs.SetInt("Settings.Fullscreen",fullscreen.isOn?1:0);
                PlayerPrefs.Save();
            }
            settingsChanged.Invoke();
        }
#if UNITY_EDITOR
        public void Configure(Slider musicSlider, Slider soundSlider, Toggle toggle, SpriteNumberLabel first, SpriteNumberLabel second)
        { music=musicSlider; sfx=soundSlider; fullscreen=toggle; musicPercent=first; sfxPercent=second; RestoreDefaults(); }
#endif
    }
}
