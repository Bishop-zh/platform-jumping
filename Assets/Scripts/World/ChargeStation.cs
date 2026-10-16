using UnityEngine;
using UnityEngine.Events;
namespace Rustbound.World
{
    public sealed class ChargeStation : MonoBehaviour,IWorldReset
    {
        public Sprite[] frames;
        public float chargePerSecond=60;
        public float interactionDistance=1.5f;
        public UnityEvent onCharged=new UnityEvent();
        public bool Visited {get;private set;}
        PlayerMotor robot;float accumulator;
        public bool Charging=>robot!=null;
        public void BeginCharge(PlayerMotor player)
        {
            if(player==null||Vector2.Distance(player.transform.position,transform.position)>interactionDistance)return;
            robot=player;accumulator=0;
            if(!Visited){Visited=true;if(LevelSession.Current!=null)LevelSession.Current.CollectBattery();}
        }
        public void Tick(float dt)
        {
            if(robot==null)return;
            if((LevelSession.Current!=null&&!LevelSession.Current.Playing)||robot.IsDead||Vector2.Distance(robot.transform.position,transform.position)>interactionDistance){robot=null;return;}
            var energy=robot.GetComponent<RobotEnergy>();if(energy==null){robot=null;return;}
            accumulator+=chargePerSecond*dt;int amount=Mathf.FloorToInt(accumulator);accumulator-=amount;energy.Recharge(amount);
            if(frames!=null&&frames.Length>0)GetComponent<SpriteRenderer>().sprite=frames[Mathf.Clamp(Mathf.FloorToInt(energy.Charge/(float)energy.capacity*frames.Length),0,frames.Length-1)];
            if(energy.Charge>=energy.capacity){robot=null;onCharged.Invoke();}
        }
        void Update(){if(!PlayerMotor.ManualSimulation)Tick(Time.deltaTime);}
        public void ResetWorld(bool full){robot=null;accumulator=0;if(full)Visited=false;if(full&&frames!=null&&frames.Length>0)GetComponent<SpriteRenderer>().sprite=frames[0];}
    }
}
