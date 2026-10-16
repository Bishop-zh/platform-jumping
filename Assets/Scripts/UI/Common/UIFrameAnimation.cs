using UnityEngine;
using UnityEngine.UI;

namespace Rustbound.UI
{
    /// <summary>Reusable UGUI frame animation. Fixed rects keep every frame on one baseline.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class UIFrameAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float[] durations;
        [SerializeField] private bool playOnEnable = true;
        private Image image;
        private int frameIndex;
        private float elapsed;

        public int FrameIndex => frameIndex;
        public int FrameCount => frames == null ? 0 : frames.Length;

        private void Awake() => image = GetComponent<Image>();
        private void OnEnable() => ShowFrame(0);

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float deltaSeconds)
        {
            if (!playOnEnable || FrameCount < 2) return;
            elapsed += Mathf.Max(0, deltaSeconds);
            // Consume long frames without skipping the timeline or getting stuck.
            while (elapsed >= Duration(frameIndex))
            {
                elapsed -= Duration(frameIndex);
                SetFrame((frameIndex + 1) % frames.Length);
            }
        }

        private float Duration(int index) => durations != null && index < durations.Length
            ? Mathf.Max(0.04f, durations[index]) : 0.5f;

        public void ShowFrame(int index)
        {
            elapsed = 0;
            SetFrame(index);
        }

        private void SetFrame(int index)
        {
            if (FrameCount == 0) return;
            if (image == null) image = GetComponent<Image>();
            frameIndex = Mathf.Clamp(index, 0, frames.Length - 1);
            image.sprite = frames[frameIndex];
        }

#if UNITY_EDITOR
        public void Configure(Sprite[] sprites, float[] seconds)
        {
            frames = sprites;
            durations = seconds;
            ShowFrame(0);
        }
#endif
    }
}
