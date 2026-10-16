using System.Collections;
using UnityEngine;

namespace Rustbound.UI
{
    public sealed class ModalSlideIn : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private float duration = 0.22f;
        private Coroutine transition;
        private Vector2 restingPosition;
        private bool captured;
        private void Awake(){CapturePosition();}
        private void CapturePosition(){if(!captured&&panel!=null){restingPosition=panel.anchoredPosition;captured=true;}}
        public void Play()
        {
            if(panel==null)return;
            CapturePosition();
            if (!Application.isPlaying) return;
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(Animate());
        }
        private IEnumerator Animate()
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(duration,0.01f));
                panel.anchoredPosition = restingPosition+new Vector2(0, 72 * Mathf.Pow(1-t,3));
                yield return null;
            }
            panel.anchoredPosition = restingPosition;
            transition = null;
        }
        private void OnDisable()
        {
            if (transition != null) StopCoroutine(transition);
            transition = null;
            if (panel != null&&captured) panel.anchoredPosition = restingPosition;
        }
#if UNITY_EDITOR
        public void Configure(RectTransform rect) => panel=rect;
#endif
    }
}
