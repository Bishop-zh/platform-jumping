using UnityEngine;
using UnityEngine.EventSystems;

namespace Rustbound.UI
{
    public sealed class MenuButtonSelection : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        [SerializeField] private MainMenuView owner;
        [SerializeField] private int index;
        public void OnSelect(BaseEventData eventData) => owner.SelectButton(index);
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Keep keyboard selection and hover selection in sync.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
            else owner.SelectButton(index);
        }
#if UNITY_EDITOR
        public void Configure(MainMenuView view, int buttonIndex) { owner = view; index = buttonIndex; }
#endif
    }
}
