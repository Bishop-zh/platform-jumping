using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rustbound.Progress;

namespace Rustbound.UI
{
    public sealed class LevelCardView : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private int levelId;
        [SerializeField] private LevelSelectView owner;
        [SerializeField] private Button button;
        [SerializeField] private Image chassis;
        [SerializeField] private Sprite unlockedSprite, lockedSprite;
        [SerializeField] private GameObject unlockedContent, lockedContent, selectionOutline;
        [SerializeField] private SpriteNumberLabel levelNumber, bestTime, bestCollection;
        [SerializeField] private Image levelName;
        [SerializeField] private Image thumbnail;
        public int LevelId => levelId;
        public bool Unlocked => button.interactable;
        public bool Selected => selectionOutline.activeSelf;
        public Button Button => button;
        public void SelectThis() { if (Unlocked) owner.SelectLevel(levelId); }
        public void OnSelect(BaseEventData eventData) => SelectThis();

        public void Refresh(LevelProgressRepository progress, int selected)
        {
            bool unlocked = progress.IsUnlocked(levelId);
            button.interactable = unlocked;
            chassis.sprite = unlocked ? unlockedSprite : lockedSprite;
            chassis.color = unlocked ? Color.white : new Color(0.78f,0.78f,0.78f,1);
            thumbnail.color = unlocked ? Color.white : new Color(0.65f,0.65f,0.65f,1);
            unlockedContent.SetActive(unlocked);
            lockedContent.SetActive(!unlocked);
            SetSelected(selected == levelId && unlocked);
            levelNumber.SetValue(levelId.ToString("00"));
            levelNumber.SetTint(unlocked ? Color.white : new Color(0.5f,0.5f,0.5f,1));
            levelName.color = unlocked ? Color.white : new Color(0.62f,0.62f,0.62f,1);
            levelName.rectTransform.anchoredPosition = new Vector2(106, unlocked ? -310 : -365);
            var record = progress.GetRecord(levelId);
            bestTime.SetValue(record.completed ? FormatTime(record.bestSeconds) : "--:--");
            bestCollection.SetValue(record.completed ? record.bestCollected.ToString("00")+"/"+record.totalCollectibles.ToString("00") : "--/--");
        }

        public void SetSelected(bool selected) => selectionOutline.SetActive(selected);
        private static string FormatTime(float seconds)
        {
            int value = Mathf.FloorToInt(seconds);
            return (value/60).ToString("00")+":"+(value%60).ToString("00");
        }
#if UNITY_EDITOR
        public void Configure(int id, LevelSelectView parent, Button selectable, Image body, Sprite unlocked, Sprite locked,
            GameObject openContent, GameObject closedContent, GameObject outline, SpriteNumberLabel number,
            SpriteNumberLabel time, SpriteNumberLabel collection, Image name, Image picture)
        {
            levelId=id; owner=parent; button=selectable; chassis=body; unlockedSprite=unlocked; lockedSprite=locked;
            unlockedContent=openContent; lockedContent=closedContent; selectionOutline=outline;
            levelNumber=number; bestTime=time; bestCollection=collection; levelName=name;
            thumbnail=picture;
        }
#endif
    }
}
