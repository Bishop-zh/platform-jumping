using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rustbound.Progress;

namespace Rustbound.UI
{
    public sealed class LevelSelectView : MonoBehaviour
    {
        [SerializeField] private LevelCardView[] cards;
        [SerializeField] private Button enterButton;
        [SerializeField] private UnityEvent backRequested = new UnityEvent();
        [SerializeField] private UnityEvent<int> levelRequested = new UnityEvent<int>();
        private LevelProgressRepository progress;
        public UnityEvent BackRequested => backRequested;
        public UnityEvent<int> LevelRequested => levelRequested;
        public int SelectedLevel { get; private set; } = 1;
        public LevelCardView[] Cards => cards;

        private void OnEnable()
        {
            if (Application.isPlaying && progress == null) Show(LevelProgressRepository.Load());
        }
        public void Show(LevelProgressRepository repository)
        {
            progress = repository;
            gameObject.SetActive(true);
            SelectedLevel = progress.PreferredLevel;
            foreach (var card in cards) card.Refresh(progress, SelectedLevel);
            enterButton.interactable = progress.IsUnlocked(SelectedLevel);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(cards[SelectedLevel-1].gameObject);
        }
        public bool SelectLevel(int id)
        {
            if (progress == null || !progress.IsUnlocked(id)) return false;
            SelectedLevel=id;
            foreach (var card in cards) card.SetSelected(card.LevelId==id);
            enterButton.interactable=true;
            return true;
        }
        public void RequestEnter()
        {
            if (progress != null && progress.MarkEntered(SelectedLevel)) levelRequested.Invoke(SelectedLevel);
        }
        public void RequestBack() => backRequested.Invoke();
        public void Hide() => gameObject.SetActive(false);
#if UNITY_EDITOR
        public void Configure(LevelCardView[] levelCards, Button enter) { cards=levelCards; enterButton=enter; }
#endif
    }
}
