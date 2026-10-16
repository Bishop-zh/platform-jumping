using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Rustbound.UI
{
    public enum ModalMode { Settings, Victory, Failure }
    public enum ModalAction { NextLevel, Replay, ReturnLevelSelect, ReturnMainMenu, RetryCheckpoint, RestartLevel, Close }
    [Serializable] public sealed class ModalActionEvent : UnityEvent<ModalAction> { }

    /// <summary>One shared modal shell; only content and title switch between modes.</summary>
    public sealed class ModalDialogView : MonoBehaviour
    {
        [SerializeField] private GameObject settingsContent;
        [SerializeField] private GameObject victoryContent;
        [SerializeField] private GameObject failureContent;
        [SerializeField] private Image title;
        [SerializeField] private Image titleIcon;
        [SerializeField] private Sprite settingsTitle;
        [SerializeField] private Sprite victoryTitle;
        [SerializeField] private Sprite failureTitle;
        [SerializeField] private Sprite victoryIcon;
        [SerializeField] private Sprite failureIcon;
        [SerializeField] private SpriteNumberLabel collected;
        [SerializeField] private SpriteNumberLabel time;
        [SerializeField] private SpriteNumberLabel deaths;
        [SerializeField] private SpriteNumberLabel batteries;
        [SerializeField] private GameObject unlockedNotice;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private TextMeshProUGUI pauseTitle;
        [SerializeField] private ModalActionEvent actionRequested = new ModalActionEvent();
        private GameObject previousSelection;
        private bool hasSelectionSnapshot;
        private Selectable firstSelectable;
        private RectTransform[] victoryButtons;
        private Vector2[] victoryButtonPositions;
        public ModalMode Mode { get; private set; }
        public ModalActionEvent ActionRequested => actionRequested;

        public void Show(ModalMode mode)
        {
            if (!hasSelectionSnapshot && EventSystem.current != null)
            {
                previousSelection = EventSystem.current.currentSelectedGameObject;
                hasSelectionSnapshot = true;
            }
            gameObject.SetActive(true);
            Mode = mode;
            settingsContent.SetActive(mode == ModalMode.Settings);
            victoryContent.SetActive(mode == ModalMode.Victory);
            failureContent.SetActive(mode == ModalMode.Failure);
            title.sprite = mode == ModalMode.Settings ? settingsTitle : mode == ModalMode.Victory ? victoryTitle : failureTitle;
            title.enabled=pauseTitle==null||mode!=ModalMode.Settings;
            if(pauseTitle!=null)pauseTitle.gameObject.SetActive(mode==ModalMode.Settings);
            titleIcon.gameObject.SetActive(mode != ModalMode.Settings);
            titleIcon.sprite = mode == ModalMode.Failure ? failureIcon : victoryIcon;
            var titleRect = title.rectTransform;
            if(pauseTitle==null)
            {
                titleRect.anchoredPosition = mode == ModalMode.Settings ? new Vector2(747, -177) : new Vector2(735, -177);
                titleRect.sizeDelta = mode == ModalMode.Settings ? new Vector2(178, 74) : new Vector2(323, 74);
            }
            GetComponent<ModalSlideIn>()?.Play();
            if (EventSystem.current != null)
            {
                var content = mode == ModalMode.Settings ? settingsContent : mode == ModalMode.Victory ? victoryContent : failureContent;
                firstSelectable = content.GetComponentsInChildren<Selectable>().FirstInteractable();
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void Update()
        {
            // Open with neutral buttons; keyboard users can begin navigation immediately.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
                firstSelectable != null && firstSelectable.IsActive() && firstSelectable.IsInteractable() &&
                (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.UpArrow) ||
                 Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)))
                EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }

        public void ShowVictory(int current, int total, float seconds, int deathCount, bool nextAvailable, bool newlyUnlocked)
        {
            int elapsed = Mathf.Max(0, Mathf.FloorToInt(seconds));
            collected.SetValue(Mathf.Max(0,current).ToString("00") + "/" + Mathf.Max(0,total).ToString("00"));
            time.SetValue((elapsed / 60).ToString("00") + ":" + (elapsed % 60).ToString("00"));
            deaths.SetValue(Mathf.Max(0,deathCount).ToString());
            nextLevelButton.gameObject.SetActive(nextAvailable);
            LayoutVictoryButtons(nextAvailable);
            unlockedNotice.SetActive(newlyUnlocked && nextAvailable);
            Show(ModalMode.Victory);
        }

        private void LayoutVictoryButtons(bool nextAvailable)
        {
            if (victoryButtons == null)
            {
                victoryButtons = new[] { (RectTransform)nextLevelButton.transform,
                    victoryContent.transform.Find("Replay") as RectTransform,
                    victoryContent.transform.Find("ReturnLevels") as RectTransform };
                if (victoryButtons[1] == null || victoryButtons[2] == null) { victoryButtons = null; return; }
                victoryButtonPositions = new Vector2[3];
                for (int i = 0; i < 3; i++) victoryButtonPositions[i] = victoryButtons[i].anchoredPosition;
            }
            for (int i = 0; i < 3; i++) victoryButtons[i].anchoredPosition = victoryButtonPositions[i];
            if (nextAvailable) return;
            float left = victoryButtonPositions[0].x - victoryButtons[0].pivot.x * victoryButtons[0].rect.width;
            float right = victoryButtonPositions[2].x + (1 - victoryButtons[2].pivot.x) * victoryButtons[2].rect.width;
            float replayWidth = victoryButtons[1].rect.width;
            float levelsWidth = victoryButtons[2].rect.width;
            float gap = victoryButtonPositions[2].x - victoryButtons[2].pivot.x * levelsWidth -
                (victoryButtonPositions[1].x + (1 - victoryButtons[1].pivot.x) * replayWidth);
            float start = (left + right - replayWidth - levelsWidth - gap) * 0.5f;
            victoryButtons[1].anchoredPosition = new Vector2(start + victoryButtons[1].pivot.x * replayWidth, victoryButtonPositions[1].y);
            victoryButtons[2].anchoredPosition = new Vector2(start + replayWidth + gap + victoryButtons[2].pivot.x * levelsWidth, victoryButtonPositions[2].y);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            hasSelectionSnapshot = false;
            previousSelection = null;
        }
        public void ShowBatterySummary(int current,int total)
        {if(batteries!=null)batteries.SetValue(Mathf.Max(0,current).ToString("00")+"/"+Mathf.Max(0,total).ToString("00"));}

        public void Request(ModalAction action)
        {
            actionRequested.Invoke(action);
            if (action == ModalAction.Close) Hide();
        }

#if UNITY_EDITOR
        public void ConfigurePauseTitle(TextMeshProUGUI text)=>pauseTitle=text;
        public void ConfigureBatterySummary(SpriteNumberLabel label)=>batteries=label;
        public void Configure(GameObject settings, GameObject victory, GameObject failure, Image headerTitle,
            Image icon, Sprite settingsSprite, Sprite victorySprite, Sprite failureSprite, Sprite successIcon,
            Sprite offlineIcon, SpriteNumberLabel collectedLabel, SpriteNumberLabel timeLabel,
            SpriteNumberLabel deathLabel, GameObject notice, Button next)
        {
            settingsContent = settings; victoryContent = victory; failureContent = failure;
            title = headerTitle; titleIcon = icon;
            settingsTitle = settingsSprite; victoryTitle = victorySprite; failureTitle = failureSprite;
            victoryIcon = successIcon; failureIcon = offlineIcon;
            collected = collectedLabel; time = timeLabel; deaths = deathLabel;
            unlockedNotice = notice; nextLevelButton = next;
            ShowVictory(8,12,155,3,true,true);
        }
#endif
    }

    internal static class ModalSelectableExtensions
    {
        public static Selectable FirstInteractable(this Selectable[] elements)
        {
            foreach (var element in elements) if (element.IsActive() && element.IsInteractable()) return element;
            return null;
        }
    }
}
