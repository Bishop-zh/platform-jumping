using UnityEngine;
using UnityEngine.UI;

namespace Rustbound.UI
{
    /// <summary>A small sprite glyph font for dynamic numbers, not a substitute Chinese font.</summary>
    public sealed class SpriteNumberLabel : MonoBehaviour
    {
        [SerializeField] private string characters = "0123456789:/%-";
        [SerializeField] private Sprite[] glyphs;
        [SerializeField] private float characterWidth = 26;
        [SerializeField] private float characterHeight = 40;
        [SerializeField] private string value;
        public string Value => value;
        public void SetTint(Color tint)
        {
            foreach (var image in GetComponentsInChildren<Image>(true)) image.color = tint;
        }

        public void SetValue(string text)
        {
            value = text ?? string.Empty;
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
            for (int i = 0; i < value.Length; i++)
            {
                Image image;
                if (i < transform.childCount) image = transform.GetChild(i).GetComponent<Image>();
                else
                {
                    var go = new GameObject("Glyph", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(transform, false);
                    image = go.GetComponent<Image>();
                    image.raycastTarget = false;
                }
                int glyphIndex = characters.IndexOf(value[i]);
                image.gameObject.SetActive(glyphIndex >= 0 && glyphs != null && glyphIndex < glyphs.Length);
                if (!image.gameObject.activeSelf) continue;
                image.sprite = glyphs[glyphIndex];
                image.preserveAspect = true;
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                // Tight-cropped digits have different widths; preserve a common cap height/baseline.
                float width = characterHeight * image.sprite.rect.width / image.sprite.rect.height;
                float height = characterHeight;
                if (value[i] == '-')
                {
                    width = characterWidth * 0.75f;
                    height = width * image.sprite.rect.height / image.sprite.rect.width;
                }
                rect.anchoredPosition = new Vector2(i * characterWidth + (characterWidth-width)/2, -(characterHeight-height)/2);
                if(value[i]=='.'){width=characterWidth*.25f;height=width*image.sprite.rect.height/image.sprite.rect.width;rect.anchoredPosition=new Vector2(i*characterWidth+(characterWidth-width)/2,-(characterHeight-height));}
                rect.sizeDelta = new Vector2(width, height);
            }
        }

#if UNITY_EDITOR
        public void Configure(Sprite[] sprites, float width, float height, string text)
        {
            glyphs = sprites;
            characters = sprites.Length>14?"0123456789:/%-.":"0123456789:/%-";
            characterWidth = width;
            characterHeight = height;
            SetValue(text);
        }
#endif
    }
}
