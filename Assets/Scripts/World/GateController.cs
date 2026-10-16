using UnityEngine;
namespace Rustbound.World
{
    public sealed class GateController : MonoBehaviour,IWorldReset
    {
        public WorldSignal inputA,inputB;
        public Sprite closed,open;
        public bool IsOpen { get; private set; }
        float elapsed;
        bool eventPower;
        public void SetPowered(bool powered)=>eventPower=powered;
        public void Tick(float dt)
        {
            bool powered=inputA==null&&inputB==null?eventPower:(inputA==null||inputA.Active)&&(inputB==null||inputB.Active);
            elapsed=powered?elapsed+dt:0;
            IsOpen=elapsed>=.35f;
            GetComponent<BoxCollider2D>().enabled=!IsOpen;
            GetComponent<SpriteRenderer>().sprite=IsOpen?open:closed;
        }
        void FixedUpdate(){if(!PlayerMotor.ManualSimulation)Tick(Time.fixedDeltaTime);}
        public void ResetWorld(bool full){if(full)eventPower=false;elapsed=0;Tick(.35f);}
    }
}
