using UnityEngine;
using UnityEngine.UI;
namespace Rustbound.UI
{
    /// <summary>Fit controls uniformly; extend only decorative backgrounds to cover the viewport.</summary>
    [RequireComponent(typeof(Canvas),typeof(CanvasScaler))]
    public sealed class FullScreenCanvas : MonoBehaviour
    {
        RectTransform composition,background;
        Vector2 reference;
        CanvasScaler scaler;
        public static float FitScale(Vector2 viewport,Vector2 reference)=>Mathf.Min(viewport.x/reference.x,viewport.y/reference.y);
        public static float CoverScale(Vector2 viewport,Vector2 reference)=>Mathf.Max(viewport.x/reference.x,viewport.y/reference.y);
        public static void Attach(GameObject root){if(root!=null&&root.GetComponent<Canvas>()!=null&&root.GetComponent<FullScreenCanvas>()==null)root.AddComponent<FullScreenCanvas>();}
        void Awake()
        {
            scaler=GetComponent<CanvasScaler>();composition=transform.Find("Composition") as RectTransform;
            reference=composition!=null?composition.sizeDelta:scaler.referenceResolution;
            if(reference.x<=0||reference.y<=0)reference=new Vector2(640,360);
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            if(composition!=null)
            {
                foreach(string name in new[]{"Sky","DistantFactory","LeftShade","ForegroundSteelwork","Background","Foreground"})
                {
                    var source=composition.Find(name);if(source==null)continue;var image=source.GetComponent<Image>();if(image==null)continue;
                    if(background==null)
                    {
                        background=new GameObject("FullScreenBackground",typeof(RectTransform)).GetComponent<RectTransform>();
                        background.SetParent(transform,false);background.SetAsFirstSibling();background.anchorMin=background.anchorMax=background.pivot=new Vector2(.5f,.5f);background.sizeDelta=reference;
                    }
                    var copy=Instantiate(source.gameObject,background,false);copy.name=name;copy.GetComponent<Image>().raycastTarget=false;image.enabled=false;
                }
            }
            var menu=GetComponent<MainMenuView>();
            if(menu!=null&&composition!=null)
            {
                var actors=composition.GetComponentInChildren<MainMenuBackdropActors>(true);
                if(actors!=null)actors.transform.SetParent(background!=null?background:composition,false);
                else if(menu.BackdropActorsPrefab!=null)Instantiate(menu.BackdropActorsPrefab,background!=null?background:composition,false).name="MainMenuBackdropActors";
            }
            Apply();
        }
        void LateUpdate()=>Apply();
        void Apply()
        {
            var viewport=new Vector2(Screen.width,Screen.height);float fit=FitScale(viewport,reference);
            scaler.scaleFactor=Mathf.Max(.01f,fit);
            if(background!=null){float cover=CoverScale(viewport,reference);background.localScale=Vector3.one*(cover/Mathf.Max(.01f,fit));}
        }
    }
}
