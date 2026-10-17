using UnityEngine;
using UnityEngine.UI;
namespace Rustbound.World
{
    public sealed class PlayerHint : MonoBehaviour
    {
        PlayerMotor player;
        Canvas canvas;
        Text label;
        Font font;
        string notification;
        float expires;
        public string CurrentText=>label!=null?label.text:"";
        public static void Attach(PlayerMotor player){if(player!=null&&player.GetComponent<PlayerHint>()==null)player.gameObject.AddComponent<PlayerHint>();}
        public static void Show(string text,float duration=2)
        {
            if(LevelSession.Current==null||LevelSession.Current.player==null)return;
            var player=LevelSession.Current.player;Attach(player);var hint=player.GetComponent<PlayerHint>();hint.notification=text;hint.expires=Time.unscaledTime+duration;
        }
        void Awake()
        {
            player=GetComponent<PlayerMotor>();
            var root=new GameObject("PlayerHints",typeof(Canvas),typeof(CanvasScaler));canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(640,360);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var go=new GameObject("Hint",typeof(RectTransform),typeof(Text),typeof(Outline));go.transform.SetParent(root.transform,false);label=go.GetComponent<Text>();
#if UNITY_WEBGL && !UNITY_EDITOR
            font=Resources.Load<Font>("Portfolio/Fonts/NotoSansSC-Regular");
#else
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial Unicode MS"},18);
#endif
            label.font=font;label.fontSize=14;label.alignment=TextAnchor.MiddleCenter;label.color=new Color32(253,216,167,255);label.raycastTarget=false;
            go.GetComponent<Outline>().effectColor=new Color32(12,17,22,230);go.GetComponent<Outline>().effectDistance=new Vector2(1,-1);
            label.rectTransform.sizeDelta=new Vector2(300,40);label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(.5f,.5f);
        }
        public string ProximityText()
        {
            float nearest=1.5f;string text="";
            foreach(var station in FindObjectsOfType<ChargeStation>())
            {
                float distance=Vector2.Distance(player.transform.position,station.transform.position);
                if(distance>station.interactionDistance||distance>=nearest)continue;nearest=distance;
                var energy=player.GetComponent<RobotEnergy>();text=station.Charging?"正在充能…":energy!=null&&energy.Charge>=energy.capacity?"电量已充满":"按 E 充能";
            }
            foreach(var signal in FindObjectsOfType<WorldSignal>())
            {
                if(signal.plate)continue;float distance=Vector2.Distance(player.transform.position,signal.transform.position);if(distance>=nearest)continue;nearest=distance;
                if(signal.Active&&!signal.toggle){text="机关已开启";continue;}
                text=signal.ActivationEnergyCost>0&&!signal.Active?"按 E 消耗能量开启开关":"按 E 操作开关";
            }
            return text;
        }
        void LateUpdate()
        {
            if(player==null||player.IsDead||LevelSession.Current==null||!LevelSession.Current.Playing){label.text="";return;}
            label.text=Time.unscaledTime<expires?notification:ProximityText();
            var camera=FindObjectOfType<WorldCamera>();if(camera==null){label.text="";return;}
            var bounds=player.GetComponent<Collider2D>().bounds;var viewport=camera.GetComponent<Camera>().WorldToViewportPoint(new Vector3(bounds.center.x,bounds.max.y+.45f,0));
            if(viewport.z<=0){label.text="";return;}
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform,new Vector2(viewport.x*Screen.width,viewport.y*Screen.height),null,out var local);label.rectTransform.anchoredPosition=local;
        }
        void OnDestroy(){if(canvas!=null)Destroy(canvas.gameObject);if(font!=null)Destroy(font);}
    }
}
