using UnityEngine;
namespace Rustbound.UI
{
    /// <summary>Actors share the background coordinate system, not the foreground controls.</summary>
    public sealed class MainMenuBackdropActors : MonoBehaviour
    {
        public RectTransform robot;
        public RectTransform chargingStation;
        public float platformTop=681;
        public void AlignFeet()
        {
            foreach(var rect in new[]{robot,chargingStation})
            {
                if(rect==null)continue;
                rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-platformTop+rect.sizeDelta.y*(1-rect.pivot.y));
            }
        }
#if UNITY_EDITOR
        [ContextMenu("将角色脚底对齐平台高度")]
        void AlignInEditor()=>AlignFeet();
#endif
    }
}
