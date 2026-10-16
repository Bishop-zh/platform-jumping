using UnityEngine;
namespace Rustbound.World
{
    public sealed class PatrolEnemy : MonoBehaviour,IWorldReset
    {
        public float left=25,right=28;
        public float patrolDistance=3;
        Vector2 initial;
        public Sprite[] frames;
        bool forward=true;float clock;
        Rigidbody2D body;
        void Awake(){body=GetComponent<Rigidbody2D>();initial=transform.position;left=initial.x;right=left+patrolDistance;}
        public void Tick(float dt)
        {
            clock+=dt;var p=body!=null?(Vector3)body.position:transform.position;p.x=Mathf.MoveTowards(p.x,forward?right:left,dt);
            if(body!=null)body.MovePosition(p);else transform.position=p;
            if(Mathf.Abs(p.x-(forward?right:left))<.001f)forward=!forward;
            var sr=GetComponent<SpriteRenderer>();sr.flipX=!forward;if(frames!=null&&frames.Length>0)sr.sprite=frames[(int)(clock*6)%frames.Length];
        }
        void FixedUpdate(){if(!PlayerMotor.ManualSimulation)Tick(Time.fixedDeltaTime);}
        public void ResetWorld(bool full){if(body!=null){body.position=initial;body.velocity=Vector2.zero;}else transform.position=initial;forward=true;clock=0;}
    }
}
