#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace Rustbound.World
{
    public sealed class RobotValidation : MonoBehaviour
    {
        readonly List<string> checks=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-jumpRobotTest")<0)return;LevelSession.Validation=true;PlayerMotor.ManualSimulation=true;Physics2D.simulationMode=SimulationMode2D.Script;var go=new GameObject("RobotValidation");DontDestroyOnLoad(go);go.AddComponent<RobotValidation>();}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);Debug.Log("ROBOT_CHECK: "+message);}
        void Step(int count,float axis=0,bool jump=false,bool dash=false)
        {
            var session=LevelSession.Current;for(int i=0;i<count;i++){foreach(var station in FindObjectsOfType<ChargeStation>())station.Tick(.02f);foreach(var signal in FindObjectsOfType<GateController>())signal.Tick(.02f);session.player.Feed(axis,jump&&i==0,dash&&i==0);session.player.Tick(.02f);Physics2D.SyncTransforms();Physics2D.Simulate(.02f);session.player.GetComponent<PlayerPresentation>().Refresh(.02f);session.player.GetComponent<Animator>().Update(.02f);}
        }
        IEnumerator Start()
        {
            yield return null;bool failed=false;
            for(int id=1;id<=3;id++)
            {
                if(id>1){SceneManager.LoadScene("Level0"+id);yield return null;yield return null;}
                try
                {
                    var s=LevelSession.Current;var p=s.player;var energy=p.GetComponent<RobotEnergy>();var animator=p.GetComponent<Animator>();
                    Check(energy.Charge==20,"Scene "+id+" starts with built-in low charge");Check(FindObjectsOfType<ChargeStation>().Length==(id==1?2:3),"Scene "+id+" charger replacements preserve map layout");Check(Array.TrueForAll(FindObjectsOfType<WorldPickup>(),v=>v.kind!=PickupKind.Battery),"No disposable battery pickups remain in map");
                    Step(15);Check(p.Grounded,"Dynamic rigidbody lands on ground");Step(10);Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"Idle animation state");Step(3,-1);Check(p.GetComponent<SpriteRenderer>().flipX&&animator.GetCurrentAnimatorStateInfo(0).IsName("Run"),"Left input flips normalized right-facing movement");Step(1,1);Check(!p.GetComponent<SpriteRenderer>().flipX,"Right input restores facing");
                    Step(2,0,true);Check(p.Body.velocity.y>0&&animator.GetCurrentAnimatorStateInfo(0).IsName("Rise"),"Jump input drives ascending animation and rigidbody");Step(24);Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"),"Falling animation state");p.Respawn(s.spawn);Step(15);Step(2,1,false,true);Check(p.IsDashing&&animator.GetCurrentAnimatorStateInfo(0).IsName("Dash"),"Shift-equivalent input drives dash animation");p.Respawn(s.spawn);Step(15);
                    var signal=Array.Find(FindObjectsOfType<WorldSignal>(),v=>!v.plate&&v.batteryCost>0)??FindObjectOfType<WorldSignal>();Check(!signal.TryActivate(),"Low charge cannot power mechanism");
                    var stations=FindObjectsOfType<ChargeStation>();Array.Sort(stations,(a,b)=>a.transform.position.x.CompareTo(b.transform.position.x));var station=stations[0];p.Respawn(station.transform.position);Step(15);s.Interact(p.transform.position);Step(5);Check(station.Charging&&energy.Charge>20,"E-equivalent interaction starts progressive charging");Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Charging"),"Charging animation state");Step(90);Check(energy.Charge==100&&!station.Charging,"Charger fills built-in battery to capacity");Check(s.BatteriesCollected==1,"Charging station discovery counted once");s.Interact(p.transform.position);Step(2);Check(s.BatteriesCollected==1,"Repeat charging does not duplicate discovery statistics");
                    Check(signal.TryActivate()&&energy.Charge==60,"Mechanism consumes charge rather than inventory item");signal.Activate();Check(energy.Charge==60,"Latched mechanism does not consume charge twice");Step(25);
                    Capture("Level0"+id+"_Robot",s.player.transform.position.x);
                    s.Die(true);Check(p.IsDead&&!p.Body.simulated&&!s.Playing,"Death disables physics and control");p.GetComponent<PlayerPresentation>().Refresh(.02f);animator.Update(.02f);Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Death"),"Death state starts immediately");animator.Update(1.49f);Capture("Level0"+id+"_Death",p.transform.position.x);
                    s.Retry(false);Check(!p.IsDead&&p.Body.simulated&&energy.Charge==60&&signal.Active,"Checkpoint retry restores control and preserves installed charge");s.Retry(true);Check(energy.Charge==20&&!signal.Active,"Full restart resets built-in charge and mechanisms");
                }
                catch(Exception e){failed=true;checks.Add("FAILED: "+e.Message);Debug.LogError("ROBOT_TEST_FAILED: "+e);break;}
            }
            if(!failed)
            {
                LevelSession.Current.Die(true);yield return new WaitForSecondsRealtime(1.6f);
                Check(FindObjectOfType<Rustbound.UI.ModalDialogView>().Mode==Rustbound.UI.ModalMode.Failure,"Death animation completes before failure dialog is shown");LevelSession.Current.Retry(false);
                // A fresh, empty scene proves the drag-in prefab has no session/camera dependency.
                var oldScene=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("StandaloneRobotTest");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(oldScene);yield return null;
                var robot=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Player.prefab"),new Vector3(75,12),Quaternion.identity);yield return null;yield return null;
                try{Check(LevelSession.Current!=null&&LevelSession.Current.player==robot.GetComponent<PlayerMotor>(),"Dragged robot creates standalone session and binds itself");Check(LevelSession.Current.spawn==new Vector2(75,12),"Placed position becomes spawn point");Check(Camera.main!=null&&Mathf.Abs(Camera.main.transform.position.x-75)<.1f&&Camera.main.transform.position.y>12,"Camera immediately views remote spawn position");}
                catch(Exception e){failed=true;checks.Add("FAILED: "+e.Message);Debug.LogError(e);}
            }
            Directory.CreateDirectory("Artifacts/Robot");File.WriteAllLines("Artifacts/Robot/validation.txt",checks);Debug.Log(failed?"ROBOT_TEST_FAILED":"ROBOT_TEST_PASSED");EditorApplication.Exit(failed?1:0);
        }
        void Capture(string name,float x)
        {
            Directory.CreateDirectory("Artifacts/Robot");var cam=Camera.main;cam.GetComponent<WorldCamera>().SetPosition(x);var old=cam.targetTexture;var rt=new RenderTexture(640,360,24);cam.targetTexture=rt;cam.Render();var active=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(640,360,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,360),0,0);tex.Apply();File.WriteAllBytes("Artifacts/Robot/"+name+".png",tex.EncodeToPNG());cam.targetTexture=old;RenderTexture.active=active;Destroy(tex);rt.Release();Destroy(rt);
        }
    }
}
#endif
