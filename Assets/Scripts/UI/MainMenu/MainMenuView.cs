using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rustbound.UI
{
    /// <summary>Presentation and UI events only; screen navigation belongs to the caller.</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private MainMenuBackdropActors backdropActorsPrefab;
        public MainMenuBackdropActors BackdropActorsPrefab=>backdropActorsPrefab;
        [SerializeField] private Button[] menuButtons;
        [SerializeField] private Image[] buttonLabels;
        [SerializeField] private Sprite normalButton;
        [SerializeField] private Sprite selectedButton;
        [SerializeField] private Sprite normalStartLabel;
        [SerializeField] private Sprite selectedStartLabel;
        [SerializeField] private RectTransform selectionArrow;
        [SerializeField] private UnityEvent startGameRequested = new UnityEvent();
        [SerializeField] private UnityEvent levelSelectRequested = new UnityEvent();
        [SerializeField] private UnityEvent settingsRequested = new UnityEvent();
        [SerializeField] private UnityEvent quitRequested = new UnityEvent();

        public UnityEvent StartGameRequested => startGameRequested;
        public UnityEvent LevelSelectRequested => levelSelectRequested;
        public UnityEvent SettingsRequested => settingsRequested;
        public UnityEvent QuitRequested => quitRequested;

        private void OnEnable()
        {
            if (menuButtons == null || menuButtons.Length != 4) return;
            menuButtons[0].onClick.AddListener(RequestStart);
            menuButtons[1].onClick.AddListener(RequestLevels);
            menuButtons[2].onClick.AddListener(RequestSettings);
            menuButtons[3].onClick.AddListener(RequestQuit);
            SelectButton(0);
        }

        private void Start()
        {
            if (EventSystem.current != null && menuButtons != null && menuButtons.Length > 0)
                EventSystem.current.SetSelectedGameObject(menuButtons[0].gameObject);
        }

        private void OnDisable()
        {
            if (menuButtons == null || menuButtons.Length != 4) return;
            menuButtons[0].onClick.RemoveListener(RequestStart);
            menuButtons[1].onClick.RemoveListener(RequestLevels);
            menuButtons[2].onClick.RemoveListener(RequestSettings);
            menuButtons[3].onClick.RemoveListener(RequestQuit);
        }

        private void RequestStart() => startGameRequested.Invoke();
        private void RequestLevels() => levelSelectRequested.Invoke();
        private void RequestSettings() => settingsRequested.Invoke();
        private void RequestQuit()
        {
            quitRequested.Invoke();
#if !UNITY_EDITOR
            Application.Quit();
#endif
        }

        public void SelectButton(int index)
        {
            if (menuButtons == null || index < 0 || index >= menuButtons.Length) return;
            for (int i = 0; i < menuButtons.Length; i++)
            {
                menuButtons[i].image.sprite = i == index ? selectedButton : normalButton;
                // Supplied label artwork remains independent from button interaction.
                if (i == 0)
                {
                    buttonLabels[i].sprite = index == 0 ? selectedStartLabel : normalStartLabel;
                    buttonLabels[i].color = Color.white;
                }
                else buttonLabels[i].color = i == index ? new Color32(89, 243, 237, 255) : Color.white;
            }
            var rect = (RectTransform)menuButtons[index].transform;
            selectionArrow.anchoredPosition = new Vector2(80, rect.anchoredPosition.y - rect.sizeDelta.y / 2f);
        }

#if UNITY_EDITOR
        public void Configure(Button[] buttons, Image[] labels, Sprite normal, Sprite selected, RectTransform arrow, Sprite startLabel)
        {
            menuButtons = buttons;
            buttonLabels = labels;
            normalButton = normal;
            selectedButton = selected;
            normalStartLabel = labels[0].sprite;
            selectedStartLabel = startLabel;
            selectionArrow = arrow;
            SelectButton(0);
        }
#endif
    }
}
