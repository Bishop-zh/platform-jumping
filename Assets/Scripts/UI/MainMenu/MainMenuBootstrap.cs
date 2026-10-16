using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Rustbound.Progress;

namespace Rustbound.UI
{
    /// <summary>Example entry scene. Instantiates the prefab; no dependency on an editor builder.</summary>
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        [SerializeField] private MainMenuView menuPrefab;
        [SerializeField] private ModalDialogView dialogPrefab;
        [SerializeField] private LevelSelectView levelSelectPrefab;
        [SerializeField] private UnityEvent<int> levelRequested = new UnityEvent<int>();
        private ModalDialogView dialog;
        private LevelSelectView levelSelect;
        public UnityEvent<int> LevelRequested => levelRequested;
        public MainMenuView View { get; private set; }

        private void Awake() => Initialize();
        private void Initialize()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            if (menuPrefab == null)
            {
                Debug.LogError("MainMenuBootstrap needs the MainMenu prefab reference.", this);
                return;
            }
            View = Instantiate(menuPrefab);
            FullScreenCanvas.Attach(View.gameObject);
            View.name = "MainMenu";
            View.StartGameRequested.AddListener(StartLevel);
            if (levelSelectPrefab != null) View.LevelSelectRequested.AddListener(OpenLevels);
            if (dialogPrefab != null)
            {
                dialog = Instantiate(dialogPrefab);
                FullScreenCanvas.Attach(dialog.gameObject);
                dialog.Hide();
                View.SettingsRequested.AddListener(OpenSettings);
            }
            if(Application.isPlaying && Rustbound.World.LevelSession.OpenSelection)
            { Rustbound.World.LevelSession.OpenSelection=false; OpenLevels(); }
        }
        private void OpenSettings() => dialog.Show(ModalMode.Settings);
        private void OpenLevels()
        {
            if (levelSelect == null)
            {
                levelSelect=Instantiate(levelSelectPrefab);
                FullScreenCanvas.Attach(levelSelect.gameObject);
                levelSelect.BackRequested.AddListener(ReturnFromLevels);
                levelSelect.LevelRequested.AddListener(RequestLevel);
            }
            View.gameObject.SetActive(false);
            levelSelect.Show(CurrentProgress());
        }
        private void ReturnFromLevels()
        {
            levelSelect.Hide();
            View.gameObject.SetActive(true);
            View.SelectButton(1);
            var button=View.transform.Find("Composition/LevelSelect");
            if(EventSystem.current!=null && button!=null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        private LevelProgressRepository CurrentProgress() => Rustbound.World.LevelSession.Validation
            ? Rustbound.World.LevelSession.TestProgress : LevelProgressRepository.Load();
        private void StartLevel() => RequestLevel(CurrentProgress().PreferredLevel);
        private void RequestLevel(int id)
        {
            levelRequested.Invoke(id);
            if(Application.isPlaying)
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level0"+id);
        }
        private void OnDestroy()
        {
            if (View != null) View.SettingsRequested.RemoveListener(OpenSettings);
            if (View != null) View.LevelSelectRequested.RemoveListener(OpenLevels);
            if (dialog != null) Destroy(dialog.gameObject);
            if (levelSelect != null) Destroy(levelSelect.gameObject);
        }
#if UNITY_EDITOR
        public void Configure(MainMenuView prefab) => menuPrefab = prefab;
        public void ConfigureDialog(ModalDialogView prefab) => dialogPrefab = prefab;
        public void ConfigureLevelSelect(LevelSelectView prefab) => levelSelectPrefab = prefab;
        public void InitializeForValidation() => Initialize();
#endif
    }
}
