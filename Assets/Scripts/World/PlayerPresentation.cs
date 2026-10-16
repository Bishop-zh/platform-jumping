using UnityEngine;
namespace Rustbound.World
{
    public sealed class PlayerPresentation : MonoBehaviour
    {
        public Sprite idle,blink;public Sprite[] dashFrames;
        SpriteRenderer body,effect;PlayerMotor motor;Animator animator;
        float clock,landing;bool wasGrounded;
        void Awake(){body=GetComponent<SpriteRenderer>();motor=GetComponent<PlayerMotor>();animator=GetComponent<Animator>();var go=new GameObject("DashTrail");go.transform.SetParent(transform,false);effect=go.AddComponent<SpriteRenderer>();effect.sortingOrder=body.sortingOrder-1;}
        public void Refresh(float dt)
        {
            clock+=dt;landing-=dt;if(motor.Grounded&&!wasGrounded)landing=.2f;wasGrounded=motor.Grounded;
            bool charging=false;foreach(var station in FindObjectsOfType<ChargeStation>())if(station.Charging&&Vector2.Distance(transform.position,station.transform.position)<station.interactionDistance)charging=true;
            int mode=motor.IsDead?5:motor.IsDashing?4:!motor.Grounded?(motor.Body.velocity.y>0?2:3):charging?6:landing>0?7:Mathf.Abs(motor.Body.velocity.x)>.1f?1:0;
            if(animator!=null){animator.SetInteger("Mode",mode);animator.SetBool("Dead",motor.IsDead);animator.speed=LevelSession.Current!=null&&!LevelSession.Current.Playing&&!motor.IsDead?0:1;}else if(idle!=null)body.sprite=clock%3>2.85f?blink:idle;
            effect.enabled=motor.IsDashing&&!motor.IsDead;if(effect.enabled&&dashFrames!=null&&dashFrames.Length>0){effect.sprite=dashFrames[(int)(clock*24)%dashFrames.Length];effect.flipX=body.flipX;effect.transform.localPosition=new Vector3(body.flipX?.45f:-.45f,.2f,0);}
        }
        void Update(){Refresh(Time.unscaledDeltaTime);}
    }
}
