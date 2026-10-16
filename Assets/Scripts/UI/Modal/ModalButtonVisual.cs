using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rustbound.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ModalButtonVisual : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Sprite normal;
        [SerializeField] private Sprite selected;
        [SerializeField] private Image label;
        [SerializeField] private Material normalLabelMaterial;
        private bool hovered;
        private bool focused;
        private void OnEnable() { hovered = false; focused = false; Paint(false); }
        private void OnDisable() { hovered = false; focused = false; Paint(false); }
        public void OnSelect(BaseEventData data) { focused = true; Paint(true); }
        public void OnDeselect(BaseEventData data) { focused = false; Paint(hovered); }
        public void OnPointerEnter(PointerEventData data)
        {
            hovered = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
            Paint(true);
        }
        public void OnPointerExit(PointerEventData data)
        {
            hovered = false;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);
            Paint(focused);
        }
        private void Paint(bool highlight)
        {
            GetComponent<Image>().sprite = highlight ? selected : normal;
            if (label != null && normalLabelMaterial != null)
                label.material = highlight ? null : normalLabelMaterial;
        }
#if UNITY_EDITOR
        public void Configure(Sprite plain, Sprite active, bool isPrimary) { normal=plain; selected=active; Paint(false); }
        public void ConfigureLabels(Image target, Material plain)
        { label=target; normalLabelMaterial=plain; Paint(false); }
#endif
    }
}
