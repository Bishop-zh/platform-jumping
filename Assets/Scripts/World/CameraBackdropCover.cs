using UnityEngine;

namespace Rustbound.World
{
    // Attach only to a SpriteRenderer directly beneath its orthographic camera.
    // Runs after WorldCamera has fitted the current pixel viewport.
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CameraBackdropCover : MonoBehaviour
    {
        Camera owner;
        SpriteRenderer artwork;

        void Awake()
        {
            owner=transform.parent!=null?transform.parent.GetComponent<Camera>():null;
            artwork=GetComponent<SpriteRenderer>();
        }

        void LateUpdate(){RefreshCover();}

        public void RefreshCover()
        {
            if(owner==null||artwork==null||artwork.sprite==null||!owner.orthographic)return;
            var bounds=artwork.sprite.bounds;
            if(bounds.size.x<=0||bounds.size.y<=0)return;
            float height=owner.orthographicSize*2;
            float scale=Mathf.Max(height*owner.aspect/bounds.size.x,height/bounds.size.y);
            transform.localScale=new Vector3(scale,scale,1);
            // Account for the sprite's actual pivot rather than assuming its
            // transform sits in the visual centre. Preserve the authored depth.
            var position=transform.localPosition;
            position.x=-bounds.center.x*scale;
            position.y=-bounds.center.y*scale;
            transform.localPosition=position;
        }
    }
}
