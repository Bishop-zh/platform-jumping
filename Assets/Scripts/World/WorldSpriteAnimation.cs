using UnityEngine;
namespace Rustbound.World
{
    public sealed class WorldSpriteAnimation : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps=6;
        float clock;
        void Update(){if(frames==null||frames.Length==0)return;clock+=Time.deltaTime;GetComponent<SpriteRenderer>().sprite=frames[(int)(clock*fps)%frames.Length];}
    }
}
