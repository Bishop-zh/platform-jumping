using UnityEngine;
namespace Rustbound.World
{
    public sealed class WorldCamera : MonoBehaviour
    {
        public PlayerMotor target;
        public float width=32;
        public bool bounded=true;
        public bool followVertical;
        float baseHeight;
        RenderTexture targetTexture;
        UnityEngine.UI.RawImage presentation;
        Camera worldCamera;
        GameObject screenCamera;
        void Start()
        {
            worldCamera=GetComponent<Camera>();
            // Unity Game View needs a camera targeting the display, even when the
            // actual world is rendered offscreen and shown by an Overlay Canvas.
            screenCamera=new GameObject("GameViewCamera");var display=screenCamera.AddComponent<Camera>();
            display.cullingMask=0;display.clearFlags=CameraClearFlags.SolidColor;display.backgroundColor=Color.black;display.depth=-100;display.targetDisplay=worldCamera.targetDisplay;
            if(target==null&&LevelSession.Current!=null)target=LevelSession.Current.player;
            baseHeight=target!=null?target.transform.position.y+2.5f:2.5f;
            var canvasObject=new GameObject("PixelViewport",typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvasObject.GetComponent<Canvas>().sortingOrder=-100;
            var image=new GameObject("PixelImage",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));image.transform.SetParent(canvasObject.transform,false);
            presentation=image.GetComponent<UnityEngine.UI.RawImage>();presentation.raycastTarget=false;
            var rect=presentation.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            ResizeViewport();if(target!=null)SetPosition(target.transform.position.x);
        }
        void ResizeViewport()
        {
            float aspect=(float)Mathf.Max(1,Screen.width)/Mathf.Max(1,Screen.height);
            int w=Mathf.Max(640,Mathf.RoundToInt(360*aspect));int h=Mathf.Max(360,Mathf.RoundToInt(640/aspect));
            if(targetTexture!=null&&targetTexture.width==w&&targetTexture.height==h)return;
            if(targetTexture!=null){worldCamera.targetTexture=null;targetTexture.Release();Destroy(targetTexture);}
            targetTexture=new RenderTexture(w,h,24){filterMode=FilterMode.Point};
            worldCamera.targetTexture=targetTexture;worldCamera.aspect=(float)w/h;worldCamera.orthographicSize=h/64f;
            if(presentation!=null)presentation.texture=targetTexture;
        }
        void LateUpdate(){if(worldCamera!=null)ResizeViewport();if(target!=null)SetPosition(target.transform.position.x);}
        void OnDestroy(){if(screenCamera!=null)Destroy(screenCamera);if(targetTexture!=null){targetTexture.Release();Destroy(targetTexture);}if(presentation!=null)Destroy(presentation.transform.parent.gameObject);}
        public void SetPosition(float x)
        {
            var cam=worldCamera!=null?worldCamera:GetComponent<Camera>();float half=cam!=null?cam.orthographicSize*cam.aspect:10;
            float center=bounded?(width<=half*2?width/2:Mathf.Clamp(x,half,width-half)):x;
            float y=followVertical&&target!=null?target.transform.position.y+2.5f:baseHeight;
            transform.position=new Vector3(Mathf.Round(center*32)/32,Mathf.Round(y*32)/32,-10);
        }
    }
}
