using UnityEngine;
namespace Rustbound.World
{
    public enum PickupKind { Fragment,Shield,JumpBoost,Battery }
    public sealed class WorldPickup : MonoBehaviour,IWorldReset
    {
        public PickupKind kind;
        public float visualScale=1.4f;
        public float floatHeight=.12f;
        public float floatSpeed=2f;
        Transform floatingVisual;
        Vector3 visualOrigin;
        float floatClock;
        void Start()
        {
            var source=GetComponent<SpriteRenderer>();if(source==null)return;
            var visual=new GameObject("FloatingVisual");floatingVisual=visual.transform;floatingVisual.SetParent(transform,false);
            var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=source.sprite;renderer.sharedMaterial=source.sharedMaterial;
            renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder;renderer.color=source.color;
            source.enabled=false;floatingVisual.localScale=Vector3.one*visualScale;
            // Keep the pickup collider fixed; only the displayed art floats.
            visualOrigin=Vector3.up*.08f;floatClock=transform.position.x;
        }
        void LateUpdate()
        {
            if(floatingVisual==null)return;floatClock+=Time.deltaTime*floatSpeed;
            floatingVisual.localPosition=visualOrigin+Vector3.up*Mathf.Sin(floatClock)*floatHeight;
            floatingVisual.GetComponent<SpriteRenderer>().sprite=GetComponent<SpriteRenderer>().sprite;
        }
        void OnTriggerEnter2D(Collider2D c)
        {
            var player=c.GetComponent<PlayerMotor>();if(player==null||!LevelSession.Current.Playing)return;
            if(kind==PickupKind.Fragment)LevelSession.Current.Collect();
            if(kind==PickupKind.Battery)LevelSession.Current.CollectBattery();
            if(kind==PickupKind.Shield)player.Shield=true;
            if(kind==PickupKind.JumpBoost)player.AddBoost();
            gameObject.SetActive(false);
        }
        public void ResetWorld(bool full){if(full||(kind!=PickupKind.Fragment&&kind!=PickupKind.Battery))gameObject.SetActive(true);}
    }
}
