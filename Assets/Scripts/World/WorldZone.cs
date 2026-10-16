using UnityEngine;
namespace Rustbound.World
{
    public enum ZoneKind { Hazard,Void,Checkpoint,Exit }
    public sealed class WorldZone : MonoBehaviour,IWorldReset
    {
        public ZoneKind kind;
        public WorldSignal requiredA,requiredB;
        public Sprite off,on;
        public Transform respawnPoint;
        bool checkpointVisited;
        void OnTriggerEnter2D(Collider2D c) => Enter(c);
        void OnTriggerStay2D(Collider2D c){if(kind==ZoneKind.Exit||kind==ZoneKind.Hazard||kind==ZoneKind.Void)Enter(c);}
        void Enter(Collider2D c)
        {
            if(c.GetComponent<PlayerMotor>()==null||!LevelSession.Current.Playing)return;
            if(kind==ZoneKind.Hazard||kind==ZoneKind.Void)LevelSession.Current.Die(kind==ZoneKind.Void);
            else if(kind==ZoneKind.Checkpoint){LevelSession.Current.SetCheckpoint(respawnPoint!=null?(Vector2)respawnPoint.position:(Vector2)transform.position);GetComponent<SpriteRenderer>().sprite=on;if(!checkpointVisited){checkpointVisited=true;PlayerHint.Show("存档成功");}}
            else LevelSession.Current.Complete();
        }
        void Update(){if(kind==ZoneKind.Exit&&GetComponent<SpriteRenderer>()!=null)GetComponent<SpriteRenderer>().sprite=on;}
        public void ResetWorld(bool full){if(full&&kind==ZoneKind.Checkpoint){checkpointVisited=false;GetComponent<SpriteRenderer>().sprite=off;}}
    }
}
