using UnityEngine;
namespace Rustbound.World
{
    [DefaultExecutionOrder(-100)]
    public sealed class MovingPlatform : MonoBehaviour,IWorldReset
    {
        public Vector2 from,to;
        public Vector2 travelOffset=new Vector2(4,0);
        public WorldSignal power;
        public Vector2 Delta { get; private set; }
        bool forward=true;float wait=.6f;
        Rigidbody2D body;
        bool eventPower;
        public void SetPowered(bool powered)=>eventPower=powered;
        void Awake(){body=GetComponent<Rigidbody2D>();from=body.position;to=from+travelOffset;}
        public void Tick(float dt)
        {
            Delta=Vector2.zero;if(power!=null?!power.Active:!eventPower)return;
            if(wait>0){wait-=dt;return;}
            var next=Vector2.MoveTowards(body.position,forward?to:from,1.5f*dt);
            Delta=next-body.position;body.MovePosition(next);
            if(Vector2.Distance(next,forward?to:from)<.001f){forward=!forward;wait=.6f;}
        }
        void FixedUpdate(){if(!PlayerMotor.ManualSimulation)Tick(Time.fixedDeltaTime);}
        public void ResetWorld(bool full){if(full)eventPower=false;body.position=from;Delta=Vector2.zero;forward=true;wait=.6f;}
    }
}
