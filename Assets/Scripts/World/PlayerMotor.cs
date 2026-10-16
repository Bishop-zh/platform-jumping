using UnityEngine;
namespace Rustbound.World
{
    [RequireComponent(typeof(Rigidbody2D),typeof(BoxCollider2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public static bool ManualSimulation;
        public PlayerTuning tuning;
        public LayerMask groundMask=(1<<0)|(1<<8)|(1<<10)|(1<<11);
        public Rigidbody2D Body { get; private set; }
        public bool Grounded { get; private set; }
        public bool Shield;
        public bool Enabled = true;
        public float DashRemaining => Mathf.Max(0,cooldown);
        public bool IsDashing => dash>0;
        public bool IsDead {get;private set;}
        float axis, jumpBuffer, grace, dash, cooldown, boost, immunity;
        bool requestDash, airDash;
        int facing=1;
        BoxCollider2D shape;
        SpriteRenderer sprite;
        MovingPlatform support;
        readonly ContactPoint2D[] groundContacts=new ContactPoint2D[24];
        [Min(.01f)] public float groundProbeDistance=.1f;
        void Awake() { Body=GetComponent<Rigidbody2D>(); shape=GetComponent<BoxCollider2D>(); sprite=GetComponent<SpriteRenderer>();Body.gravityScale=(tuning!=null?tuning.gravity:28)/Mathf.Max(.01f,Mathf.Abs(Physics2D.gravity.y)); }
        void Update()
        {
            if(ManualSimulation || !Enabled) return;
            Feed(Input.GetAxisRaw("Horizontal"),Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.W),Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift));
            if(Input.GetKeyDown(KeyCode.E) && LevelSession.Current!=null) LevelSession.Current.Interact(transform.position);
        }
        public void Feed(float horizontal,bool jump,bool sprint) { axis=horizontal; if(jump)jumpBuffer=tuning!=null?tuning.jumpGrace:.1f; requestDash|=sprint; }
        void FixedUpdate() { if(!ManualSimulation) Tick(Time.fixedDeltaTime); }
        public void Tick(float dt)
        {
            if(!Enabled) return;
            RefreshGrounded();
            if(Grounded) { grace=tuning!=null?tuning.jumpGrace:.1f; airDash=false; } else grace-=dt;
            cooldown-=dt; boost-=dt; immunity-=dt;
            if(axis!=0)facing=axis>0?1:-1;
            if(sprite!=null) { sprite.flipX=facing<0; sprite.color=Color.white; }
            var velocity=Body.velocity;
            if(jumpBuffer>0 && grace>0) { velocity.y=Mathf.Sqrt(2*(tuning!=null?tuning.gravity:28)*(tuning!=null?tuning.jumpHeight:2.2f))*(boost>0?1.2f:1); grace=0; Grounded=false; jumpBuffer=0; }
            if(requestDash && cooldown<=0 && !airDash) { dash=tuning!=null?tuning.dashDuration:.15f; cooldown=tuning!=null?tuning.dashCooldown:1.2f; airDash=true; }
            requestDash=false;
            velocity.x=dash>0?facing*(tuning!=null?tuning.dashSpeed:10):axis*(tuning!=null?tuning.moveSpeed:4.5f);
            // Stop a dash against a crate rather than injecting a large impulse into it.
            if(dash>0) foreach(var hit in Physics2D.BoxCastAll(Body.position+shape.offset,new Vector2(.46f,1.2f),0,Vector2.right*facing,.12f))
                if(hit.collider!=null && hit.collider.GetComponent<PushableCrate>()!=null) { velocity.x=0; dash=0; }
            Body.velocity=velocity;
            if(support!=null && Grounded) Body.position+=support.Delta;
            dash-=dt; jumpBuffer-=dt;
            if(Body.position.y < -4 && LevelSession.Current!=null)LevelSession.Current.Die(true);
        }
        public void RefreshGrounded()
        {
            Grounded=false;support=null;
            if(shape==null||!shape.enabled||!Body.simulated||Body.velocity.y>.1f)return;
            // Solver contacts remain reliable after resizing the player or platforms.
            // A solid surface actually supporting this body is ground regardless of
            // the layer chosen while editing the map.
            int count=shape.GetContacts(groundContacts);
            for(int i=0;i<count;i++)
            {
                var contact=groundContacts[i];
                if(contact.normal.y<.6f)continue;
                var other=contact.collider==shape?contact.otherCollider:contact.collider;
                if(!IsGround(other))continue;
                Grounded=true;support=other.GetComponentInParent<MovingPlatform>();return;
            }
            // Use the live world bounds, including offset and Transform scale.
            var bounds=shape.bounds;
            var origin=new Vector2(bounds.center.x,bounds.min.y+.04f);
            var size=new Vector2(Mathf.Max(.01f,bounds.size.x-.04f),.04f);
            foreach(var hit in Physics2D.BoxCastAll(origin,size,0,Vector2.down,groundProbeDistance,groundMask))
                if(IsGround(hit.collider)&&hit.normal.y>.6f)
                {Grounded=true;support=hit.collider.GetComponentInParent<MovingPlatform>();return;}
        }
        bool IsGround(Collider2D other)
        {
            return other!=null&&other!=shape&&!other.isTrigger&&other.attachedRigidbody!=Body;
        }
        void OnDrawGizmosSelected()
        {
            var collider=GetComponent<BoxCollider2D>();if(collider==null)return;
            var bounds=collider.bounds;Gizmos.color=Grounded?Color.green:Color.yellow;
            Gizmos.DrawWireCube(new Vector3(bounds.center.x,bounds.min.y+.04f-groundProbeDistance/2,bounds.center.z),
                new Vector3(Mathf.Max(.01f,bounds.size.x-.04f),.04f+groundProbeDistance,0));
        }
        public bool AbsorbHit(bool voidHit) { if(immunity>0&&!voidHit)return true; if(Shield&&!voidHit){Shield=false;immunity=.8f;return true;}return false; }
        public void AddBoost() => boost=6;
        public void BeginDeath(){IsDead=true;Enabled=false;Body.velocity=Vector2.zero;Body.simulated=false;GetComponent<Animator>()?.SetBool("Dead",true);}
        public void Respawn(Vector2 point) { IsDead=false;Body.simulated=true;Body.position=point; Body.velocity=Vector2.zero; Enabled=true; Shield=false; boost=dash=cooldown=jumpBuffer=0; immunity=.25f; axis=0; airDash=false;GetComponent<Animator>()?.SetBool("Dead",false); }
    }
}
