using UnityEngine;
using Rustbound.UI;
namespace Rustbound.World
{
    public sealed class GameRuntimeAssets : ScriptableObject
    {
        public WorldHud gameplayHud;
        public ModalDialogView modal;
        public ModalDialogView pauseModal;
        public MainMenuBootstrap mainMenu;
        public static GameRuntimeAssets Load()=>Resources.Load<GameRuntimeAssets>("GameRuntimeAssets");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){LevelSession.Current=null;LevelSession.OpenSelection=false;LevelSession.Validation=false;LevelSession.TestProgress=null;PlayerMotor.ManualSimulation=false;Time.timeScale=1;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterEntry()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded-=OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded+=OnSceneLoaded;
        }
        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode)=>EnsureEntry();
        static void EnsureEntry()
        {
            if(Object.FindObjectOfType<LevelSession>()!=null||Object.FindObjectOfType<PlayerMotor>()!=null||Object.FindObjectOfType<MainMenuBootstrap>()!=null)return;
            var assets=Load();if(assets!=null&&assets.mainMenu!=null)Object.Instantiate(assets.mainMenu).name="MainMenuRuntime";
        }
    }
}
