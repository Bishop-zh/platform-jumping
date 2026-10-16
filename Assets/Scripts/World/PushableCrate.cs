using UnityEngine;
namespace Rustbound.World
{
    public sealed class PushableCrate : MonoBehaviour,IWorldReset
    {
        public Vector2 initial;
        public float left,right;
        public Vector2 travelLimits=new Vector2(-1,3);
        Rigidbody2D body;
        public float maxPushSpeed=2.4f,stopDeceleration=18,friction=.65f;
        readonly ContactPoint2D[] contacts=new ContactPoint2D[16];
        PhysicsMaterial2D grip;
        void Awake(){body=GetComponent<Rigidbody2D>();initial=body.position;left=initial.x+travelLimits.x;right=initial.x+travelLimits.y;body.mass=Mathf.Max(body.mass,3);body.drag=Mathf.Max(body.drag,4);grip=new PhysicsMaterial2D("CrateGrip"){friction=friction,bounciness=0};GetComponent<Collider2D>().sharedMaterial=grip;}
        void OnDestroy(){if(grip!=null)Destroy(grip);}
        void FixedUpdate(){if(!PlayerMotor.ManualSimulation)Tick();}
        public void Tick()
        {
            bool grounded=false,pushing=false;
            int count=body.GetContacts(contacts);
            for(int i=0;i<count;i++)
            {
                var contact=contacts[i];if(contact.normal.y>.5f)grounded=true;
                var player=contact.collider.GetComponentInParent<PlayerMotor>();if(player==null)player=contact.otherCollider.GetComponentInParent<PlayerMotor>();
                if(player!=null&&Mathf.Abs(contact.normal.x)>.5f&&Mathf.Abs(player.Body.velocity.x)>.1f&&player.Body.velocity.x*(body.position.x-player.Body.position.x)>0)pushing=true;
            }
            float speed=Mathf.Clamp(body.velocity.x,-maxPushSpeed,maxPushSpeed);
            if(grounded&&!pushing)speed=Mathf.MoveTowards(speed,0,stopDeceleration*Time.fixedDeltaTime);
            body.velocity=new Vector2(speed,body.velocity.y);
            if(right>left && (body.position.x<left||body.position.x>right))
            {body.position=new Vector2(Mathf.Clamp(body.position.x,left,right),body.position.y);body.velocity=new Vector2(0,body.velocity.y);}
        }
        public void ResetWorld(bool full){body.position=initial;body.velocity=Vector2.zero;body.angularVelocity=0;}
    }
}
