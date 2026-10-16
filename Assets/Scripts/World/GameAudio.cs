using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Rustbound.World
{
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance {get;private set;}
        public AudioSource Music {get;private set;}
        public AudioSource Effects {get;private set;}
        public GameAudioBank Bank {get;private set;}
        float nextScan;
        AudioListener listener;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetInstance(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){Ensure();}
        public static GameAudio Ensure()
        {
            if(Instance==null){var existing=FindObjectOfType<GameAudio>();if(existing!=null)Instance=existing;else new GameObject("GameAudio").AddComponent<GameAudio>();}
            return Instance;
        }
        void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);Bank=Resources.Load<GameAudioBank>("GameAudioBank");
            Music=gameObject.AddComponent<AudioSource>();Music.playOnAwake=false;Music.loop=true;Music.spatialBlend=0;Music.ignoreListenerPause=true;
            Effects=gameObject.AddComponent<AudioSource>();Effects.playOnAwake=false;Effects.spatialBlend=0;Effects.ignoreListenerPause=true;
            listener=gameObject.AddComponent<AudioListener>();
            ApplyVolumes(PlayerPrefs.GetFloat("Settings.MusicVolume",.7f),PlayerPrefs.GetFloat("Settings.SoundVolume",.8f));
            SceneManager.sceneLoaded+=SceneLoaded;
        }
        void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            EnsureListener();nextScan=0;
            if(FindObjectOfType<LevelSession>()!=null)PlayGameplay();else Music.Stop();
        }
        void EnsureListener()
        {
            foreach(var other in FindObjectsOfType<AudioListener>())if(other!=listener)other.enabled=false;
            listener.enabled=true;AudioListener.volume=1;
        }
        void Update()
        {
            if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+.5f;
            foreach(var button in FindObjectsOfType<Button>(true))if(button.GetComponent<ButtonClickSound>()==null)button.gameObject.AddComponent<ButtonClickSound>();
        }
        public void ApplyVolumes(float music,float sfx){Music.volume=Mathf.Clamp01(music);Effects.volume=Mathf.Clamp01(sfx);}
        public void Click(){if(Bank!=null&&Bank.buttonClick!=null)Effects.PlayOneShot(Bank.buttonClick);}
        public void PlayGameplay(){if(Bank==null||Bank.gameplayMusic==null)return;Music.clip=Bank.gameplayMusic;if(!Music.isPlaying)Music.Play();}
        public void PlayVictory(){Music.Stop();if(Bank!=null&&Bank.victory!=null)Effects.PlayOneShot(Bank.victory);}
        void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;if(Instance==this)Instance=null;}
    }
    public sealed class ButtonClickSound : MonoBehaviour
    {
        Button button;
        void OnEnable(){button=GetComponent<Button>();if(button!=null)button.onClick.AddListener(Play);}
        void Play()=>GameAudio.Ensure().Click();
        void OnDisable(){if(button!=null)button.onClick.RemoveListener(Play);}
    }
}
