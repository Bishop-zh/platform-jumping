#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
namespace Rustbound.World
{
    public sealed class WorldValidation : MonoBehaviour
    {
        readonly List<string> checks=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-jumpWorldTest")<0)return;
            LevelSession.Validation=true;PlayerMotor.ManualSimulation=true;
            Physics2D.simulationMode=SimulationMode2D.Script;
            var go=new GameObject("WorldPhysicsValidation");DontDestroyOnLoad(go);go.AddComponent<WorldValidation>();
        }
        void Check(bool condition,string name){if(!condition)throw new Exception(name);checks.Add(name);Debug.Log("WORLD_CHECK: "+name);}
        void Step(int count,float axis=0,int jump=-1,int dash=-1)
        {
            for(int i=0;i<count;i++)
            {
                if(!LevelSession.Current.Playing)break;
                foreach(var m in FindObjectsOfType<MovingPlatform>())m.Tick(.02f);
                foreach(var g in FindObjectsOfType<GateController>())g.Tick(.02f);
                foreach(var e in FindObjectsOfType<PatrolEnemy>())e.Tick(.02f);
                foreach(var box in FindObjectsOfType<PushableCrate>())box.Tick();
                LevelSession.Current.player.Feed(axis,i==jump,i==dash);LevelSession.Current.player.Tick(.02f);
                Physics2D.SyncTransforms();Physics2D.Simulate(.02f);
            }
        }
        IEnumerator Start()
        {
            yield return null;
            bool failed=false;
            for(int id=1;id<=3;id++)
            {
                if(id>1){if(SceneManager.GetActiveScene().name!="Level0"+id)SceneManager.LoadScene("Level0"+id);yield return null;yield return null;}
                try
                {
                    var s=LevelSession.Current;var p=s.player;
                    Check(s.level==id,"Scene "+id+" loads with player/session");Check(s.Total==(id==1?8:id==2?10:12),"Scene "+id+" collectible total");
                    Check(s.BatteryTotal==(id==1?2:3),"Scene "+id+" battery total derived from editable map");
                    s.hud.settingsButton.onClick.Invoke();Check(!s.Playing,"HUD settings button pauses scene");s.HandleAction(Rustbound.UI.ModalAction.Close);Check(s.Playing,"Closing settings resumes scene");
                    Step(10);Check(p.Grounded,"Scene "+id+" grounded collider");
                    Capture(id,10,"Start");
                    if(id==1)
                    {
                        var relocated=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Crate.prefab"),new Vector3(100,10),Quaternion.identity).GetComponent<PushableCrate>();
                        relocated.ResetWorld(false);Check(relocated.initial==new Vector2(100,10)&&relocated.left==99&&relocated.right==103,"Crate prefab derives reset position and bounds from its placement");DestroyImmediate(relocated.gameObject);
                        var relocatedPlatform=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/MovingPlatform.prefab"),new Vector3(120,10),Quaternion.identity).GetComponent<MovingPlatform>();
                        Check(relocatedPlatform.from==new Vector2(120,10)&&relocatedPlatform.to==new Vector2(124,10),"Platform prefab derives travel endpoints from its placement");DestroyImmediate(relocatedPlatform.gameObject);
                        p.Respawn(new Vector2(5.5f,.01f));Step(8);Step(38,1,0);Check(s.Playing&&p.Body.position.x>8,"Ordinary jump crosses 2-unit pit");
                        p.Respawn(new Vector2(17.5f,.01f));Step(8);Step(40,1,0,8);Check(s.Playing&&p.Body.position.x>21.25f,"Dash jump crosses 3.25-unit pit");
                        p.Respawn(new Vector2(17.5f,.01f));Step(8);Step(70,1,0);Check(!s.Playing,"Long pit cannot be crossed by the same ordinary jump");s.Retry(true);
                        p.Respawn(new Vector2(14,.01f));Step(45,1);Check(p.Body.position.x<14.5f,"Closed door physically blocks the player");
                        p.Respawn(new Vector2(7,-1.1f));Step(25);Check(!s.Playing,"Spike trigger causes death through actual physics contact");s.Retry(true);
                        var signal=Array.Find(FindObjectsOfType<WorldSignal>(),v=>v.plate);
                        Check(!signal.TryActivate()&&s.BatteriesInBag==0,"Battery-powered plate rejects activation without a battery");
                        s.Retry(true);p.Respawn(new Vector2(4.5f,.7f));Step(5);Check(s.BatteriesCollected==1&&s.BatteriesInBag==1,"Battery trigger fills inventory and collection count");
                        p.Respawn(new Vector2(9,.01f));Step(90,1);Check(signal.Active,"Physical crate push latches pressure plate");Step(20);Check(Array.Find(FindObjectsOfType<GateController>(),g=>g.inputA==signal).IsOpen,"Plate opens and disables gate collider");
                        Check(s.BatteriesInBag==0&&s.BatteriesCollected==1,"Power installation consumes inventory but preserves collection statistics");signal.Activate();Check(s.BatteriesInBag==0,"Repeated activation does not spend twice");
                        var toggle=Array.Find(FindObjectsOfType<WorldSignal>(),v=>!v.plate);s.CollectBattery();toggle.toggle=true;toggle.Activate();toggle.Activate();Check(!toggle.Active&&s.BatteriesInBag==0,"Toggle input turns off without refunding installed battery");toggle.Activate();Check(toggle.Active&&s.BatteriesInBag==0,"Installed toggle input turns on again without spending twice");toggle.toggle=false;
                        s.SetCheckpoint(new Vector2(16.5f,0));s.Collect();s.Die(true);Check(!s.Playing&&s.Deaths==1,"Void triggers failure once");s.Retry(false);Check(s.Collected>0&&signal.Active&&p.Body.position.x==16.5f,"Checkpoint keeps collection and latched puzzle");
                        s.Retry(true);Check(s.Collected==0&&!signal.Active&&s.BatteriesCollected==0&&s.BatteriesInBag==0,"Full restart clears puzzle and both battery counters");
                        p.Respawn(new Vector2(2,.7f));Step(5);int once=s.Collected;Step(5);Check(once==1&&s.Collected==once,"Fragment trigger scores once and disables pickup");
                    }
                    if(id==2)
                    {
                        var m=FindObjectOfType<MovingPlatform>();p.Respawn(new Vector2(3,.7f));Step(5);s.Interact(new Vector2(4,0));Check(m.power.Active,"Battery-fed lever interaction powers moving platform");
                        Vector2 initial=m.transform.position;m.Tick(0);Check(m.Delta==Vector2.zero,"Platform waiting frame has no passenger displacement");
                        p.Respawn(new Vector2(8.75f,.02f));Step(20);float start=p.Body.position.x;Step(70);Check(s.Playing&&p.Body.position.x>start+1,"Moving platform carries standing player");
                        p.Respawn(new Vector2(24,0));Step(15);p.Shield=true;s.Die();Check(s.Playing&&!p.Shield,"Shield absorbs one contact hit");s.Die(true);Check(!s.Playing,"Shield does not prevent void death");s.Retry(false);
                    }
                    if(id==3)
                    {
                        var signals=FindObjectsOfType<WorldSignal>();WorldSignal plate=null,button=null;foreach(var item in signals){if(item.plate)plate=item;else if(Mathf.Abs(item.transform.position.x-21)<.1f)button=item;}
                        GateController final=null;foreach(var g in FindObjectsOfType<GateController>())if(g.inputB!=null)final=g;
                        p.Respawn(new Vector2(2.8f,.7f));Step(5);p.Respawn(new Vector2(12,.7f));Step(5);
                        plate.Activate();Step(25);Check(!final.IsOpen,"Final gate remains closed with only one input");s.Interact(new Vector2(21,1.5f));Step(25);Check(button.Active&&final.IsOpen,"Final gate requires both inputs");
                        p.Respawn(new Vector2(13.5f,.01f));Step(8);Step(39,1,0,8);Check(s.Playing&&p.Body.position.x>17,"Dash jump reaches raised landing");
                    }
                    foreach(var pickup in FindObjectsOfType<WorldPickup>())if(pickup.kind==PickupKind.Battery){p.Respawn((Vector2)pickup.transform.position+new Vector2(0,.1f));Step(5);}
                    foreach(var sig in FindObjectsOfType<WorldSignal>())sig.Activate();Step(25);Capture(id,id==1?23:id==2?27:27,"Mechanisms");
                    var exit=Array.Find(FindObjectsOfType<WorldZone>(),z=>z.kind==ZoneKind.Exit);
                    p.Respawn(new Vector2(exit.transform.position.x,exit.transform.position.y+.01f));Step(2);
                    Check(!s.Playing,"Scene "+id+" exit trigger opens victory");Check(LevelSession.TestProgress.GetRecord(id).completed,"Scene "+id+" completion recorded");
                    Check(s.BatteriesInBag==0,"Victory is reachable with an empty battery bag after installation");
                }
                catch(Exception e){Debug.LogError("WORLD_VALIDATION_FAILED: "+e);checks.Add("FAILED: "+e.Message);failed=true;break;}
                yield return new WaitForSecondsRealtime(.3f);
                CaptureVictory(id);
                if(id<3)LevelSession.Current.HandleAction(Rustbound.UI.ModalAction.NextLevel);
            }
            if(!failed)
            {
                LevelSession.Current.HandleAction(Rustbound.UI.ModalAction.ReturnLevelSelect);yield return null;yield return null;
                var menu=FindObjectOfType<Rustbound.UI.MainMenuBootstrap>();var selection=FindObjectOfType<Rustbound.UI.LevelSelectView>();
                Check(SceneManager.GetActiveScene().name=="MainMenu"&&selection!=null&&!menu.View.gameObject.activeSelf,"Victory returns to the active level selection screen");
                selection.SelectLevel(1);selection.RequestEnter();yield return null;yield return null;
                Check(LevelSession.Current.level==1,"Level selection enter button loads the selected map scene");
                LevelSession.Current.HandleAction(Rustbound.UI.ModalAction.NextLevel);yield return null;yield return null;
                Check(LevelSession.Current.level==2,"Next level action loads scene 2");
                LevelSession.Current.HandleAction(Rustbound.UI.ModalAction.ReturnMainMenu);yield return null;yield return null;
                menu=FindObjectOfType<Rustbound.UI.MainMenuBootstrap>();Check(menu.View.gameObject.activeSelf,"Return main menu action restores the home screen");
                menu.View.StartGameRequested.Invoke();yield return null;yield return null;
                Check(LevelSession.Current.level==2,"Main menu start resumes the last played map");
            }
            File.WriteAllLines("Artifacts/World/physics-validation.txt",checks);
            Debug.Log(failed?"WORLD_TEST_FAILED":"WORLD_TEST_PASSED");EditorApplication.Exit(failed?1:0);
        }
        void Capture(int id,float x,string name)
        {
            var cam=Camera.main;cam.GetComponent<WorldCamera>().SetPosition(x);
            var hud=LevelSession.Current.hud.GetComponent<Canvas>();var originalMode=hud.renderMode;hud.renderMode=RenderMode.ScreenSpaceCamera;hud.worldCamera=cam;hud.planeDistance=1;
            var scaler=hud.GetComponent<UnityEngine.UI.CanvasScaler>();float originalScale=scaler.scaleFactor;scaler.scaleFactor=1;
            var originalTarget=cam.targetTexture;var rt=new RenderTexture(640,360,24);rt.filterMode=FilterMode.Point;cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(640,360,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,360),0,0);tex.Apply();
            File.WriteAllBytes("Artifacts/World/Level0"+id+"_"+name+".png",tex.EncodeToPNG());cam.targetTexture=originalTarget;hud.renderMode=originalMode;scaler.scaleFactor=originalScale;RenderTexture.active=old;Destroy(tex);rt.Release();Destroy(rt);
        }
        void CaptureVictory(int id)
        {
            var modal=FindObjectOfType<Rustbound.UI.ModalDialogView>();var canvas=modal.GetComponent<Canvas>();var cam=Camera.main;
            var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var target=cam.targetTexture;
            var rt=new RenderTexture(1672,941,24);cam.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.5f;
            Canvas.ForceUpdateCanvases();cam.Render();var active=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(1672,941,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1672,941),0,0);tex.Apply();
            File.WriteAllBytes("Artifacts/World/Level0"+id+"_Victory.png",tex.EncodeToPNG());RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=oldCamera;cam.targetTexture=target;Destroy(tex);rt.Release();Destroy(rt);
        }
    }
}
#endif
