#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using Rustbound.UI;
using Rustbound.Progress;
namespace Rustbound.World
{
    public sealed class NavigationValidation : MonoBehaviour
    {
        readonly List<string> checks=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-jumpNavigationTest")<0)return;LevelSession.Validation=true;LevelSession.TestProgress=new LevelProgressRepository();PlayerMotor.ManualSimulation=true;var go=new GameObject("NavigationValidation");DontDestroyOnLoad(go);go.AddComponent<NavigationValidation>();}
        void Check(bool condition,string name){checks.Add((condition?"PASS: ":"FAILED: ")+name);Debug.Log("NAVIGATION_CHECK: "+name);if(!condition){Save();EditorApplication.Exit(1);throw new Exception(name);}}
        void Save(){Directory.CreateDirectory("Artifacts/Handover");File.WriteAllLines("Artifacts/Handover/navigation-validation.txt",checks);}
        Button Button(GameObject root,string name){foreach(var button in root.GetComponentsInChildren<Button>(true))if(button.name==name&&button.gameObject.activeInHierarchy)return button;throw new Exception("Missing active button: "+name);}
        void ModalButton(string name){Button(FindObjectOfType<ModalDialogView>().gameObject,name).onClick.Invoke();}
        IEnumerator Start()
        {
            yield return null;yield return null;
            var menu=FindObjectOfType<MainMenuBootstrap>();Check(menu!=null&&menu.View.gameObject.activeSelf,"MainMenu initializes home UI");
            var actors=menu.View.GetComponentInChildren<MainMenuBackdropActors>(true);
            Check(actors!=null&&actors.transform.parent.name=="FullScreenBackground","Home actors belong to the background coordinate layer");
            Check(actors.robot.GetComponent<UIFrameAnimation>().FrameCount==4&&actors.chargingStation.GetComponent<UIFrameAnimation>().FrameCount==4,"Home actor prefab retains both animations");
            var menuTemplate=(MainMenuView)new SerializedObject(menu).FindProperty("menuPrefab").objectReferenceValue;
            var editedActors=menuTemplate.GetComponentInChildren<MainMenuBackdropActors>(true);
            Check(actors.robot.anchoredPosition==editedActors.robot.anchoredPosition&&actors.chargingStation.anchoredPosition==editedActors.chargingStation.anchoredPosition,"Home actors preserve user-edited positions");
            Check(actors.transform.lossyScale==actors.transform.parent.lossyScale,"Home actors inherit the same background scaling");
            Check(Button(menu.View.gameObject,"Settings").GetComponent<ButtonClickSound>()!=null,"Settings button automatically receives click audio binding");
            var gameAudio=GameAudio.Ensure();Check(gameAudio.Bank!=null&&gameAudio.Bank.gameplayMusic.length>30&&gameAudio.Bank.victory.length>2&&gameAudio.Bank.buttonClick.length>0,"Three generated audio clips import and bind successfully");
            Button(menu.View.gameObject,"Settings").onClick.Invoke();Check(FindObjectOfType<ModalDialogView>().Mode==ModalMode.Settings,"Home settings button opens settings");ModalButton("Return");Check(FindObjectOfType<ModalDialogView>()==null,"Home settings return closes dialog");
            Button(menu.View.gameObject,"LevelSelect").onClick.Invoke();yield return null;var select=FindObjectOfType<LevelSelectView>();Check(select!=null&&!menu.View.gameObject.activeSelf,"Home level-select button opens selection");Check(!select.SelectLevel(2)&&!select.SelectLevel(3),"Initial progress prevents selecting locked maps");Button(select.gameObject,"Back").onClick.Invoke();Check(menu.View.gameObject.activeSelf,"Selection back button restores home");
            Button(menu.View.gameObject,"StartGame").onClick.Invoke();yield return null;yield return null;Check(SceneManager.GetActiveScene().name=="Level01"&&LevelSession.Current.player!=null,"Start button loads default map and binds player");
            var session=LevelSession.Current;session.hud.settingsButton.onClick.Invoke();Check(!session.Playing&&Time.timeScale==0,"HUD settings pauses gameplay");ModalButton("Return");Check(session.Playing&&Time.timeScale==1,"Settings return resumes gameplay");session.hud.pauseButton.onClick.Invoke();Check(!session.Playing,"HUD pause button pauses gameplay");ModalButton("Return");
            session.Die(true);yield return new WaitForSecondsRealtime(1.6f);Check(FindObjectOfType<ModalDialogView>().Mode==ModalMode.Failure,"Death animation routes to failure UI");ModalButton("RetryCheckpoint");Check(session.Playing&&!session.player.IsDead&&session.Deaths==1,"Failure retry restores control and preserves death count");
            session.Die(true);yield return new WaitForSecondsRealtime(1.6f);ModalButton("RestartLevel");Check(session.Playing&&session.Deaths==0&&session.player.GetComponent<RobotEnergy>().Charge==20,"Failure restart resets run and built-in charge");
            session.Complete();Check(LevelSession.TestProgress.IsUnlocked(2),"Level 1 completion unlocks level 2");Check(FindObjectOfType<ModalDialogView>().Mode==ModalMode.Victory,"Completion opens victory UI");ModalButton("Replay");Check(session.Playing&&session.Seconds==0,"Victory replay resets current map");session.Complete();ModalButton("NextLevel");yield return null;yield return null;Check(SceneManager.GetActiveScene().name=="Level02"&&Time.timeScale==1,"Victory next button loads level 2 and resumes time");
            session=LevelSession.Current;session.Complete();Check(LevelSession.TestProgress.IsUnlocked(3),"Level 2 completion unlocks level 3");ModalButton("NextLevel");yield return null;yield return null;Check(SceneManager.GetActiveScene().name=="Level03","Victory next button loads level 3");
            session=LevelSession.Current;session.Complete();var modal=FindObjectOfType<ModalDialogView>();bool activeNext=false;foreach(var button in modal.GetComponentsInChildren<Button>(true))if(button.name=="NextLevel"&&button.gameObject.activeInHierarchy)activeNext=true;Check(!activeNext,"Final victory hides next-level button");ModalButton("ReturnLevels");yield return null;yield return null;select=FindObjectOfType<LevelSelectView>();Check(select!=null&&select.SelectedLevel==3,"Victory return-select opens selection with last map highlighted");Check(select.SelectLevel(2),"Unlocked map can be selected");Button(select.gameObject,"EnterLevel").onClick.Invoke();yield return null;yield return null;Check(LevelSession.Current.level==2,"Selection enter button loads chosen map");
            session=LevelSession.Current;session.Die(true);yield return new WaitForSecondsRealtime(1.6f);ModalButton("ReturnMainMenu");yield return null;yield return null;menu=FindObjectOfType<MainMenuBootstrap>();Check(menu.View.gameObject.activeSelf&&Time.timeScale==1,"Failure return-main button restores home and time");Button(menu.View.gameObject,"StartGame").onClick.Invoke();yield return null;yield return null;Check(LevelSession.Current.level==2,"Home start continues last played unlocked map");
            session=LevelSession.Current;session.Die(true);yield return new WaitForSecondsRealtime(1.6f);ModalButton("ReturnLevels");yield return null;yield return null;Check(FindObjectOfType<LevelSelectView>()!=null,"Failure return-select button opens selection");
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/SampleScene.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            Check(FindObjectOfType<MainMenuBootstrap>()!=null&&FindObjectOfType<MainMenuView>()!=null,"Empty scene Play automatically boots shared main menu");
            SceneManager.LoadScene("Level01");yield return null;yield return null;session=LevelSession.Current;
            Check(session!=null&&session.player!=null&&session.Playing,"Direct map Play binds playable session");
            Check(session.hud!=null&&session.hud.transform.parent==null&&session.hud.GetComponentInParent<LevelMap>()==null,"Gameplay HUD is dynamically instantiated outside map");
            Check(FindObjectsOfType<WorldHud>().Length==1,"Exactly one gameplay HUD exists");
            var template=GameRuntimeAssets.Load().gameplayHud;
            var expected=template.settingsButton.GetComponent<RectTransform>();var actual=session.hud.settingsButton.GetComponent<RectTransform>();
            Check(expected.anchoredPosition==actual.anchoredPosition&&expected.sizeDelta==actual.sizeDelta,"Edited HUD button layout is preserved at runtime");
            session.hud.settingsButton.onClick.Invoke();Check(!session.Playing,"Dynamically bound shared HUD settings responds");
            var settings=FindObjectOfType<ModalSettingsController>();var so=new SerializedObject(settings);
            var musicSlider=(Slider)so.FindProperty("music").objectReferenceValue;var effectSlider=(Slider)so.FindProperty("sfx").objectReferenceValue;
            musicSlider.value=.23f;effectSlider.value=.61f;
            Check(Mathf.Abs(gameAudio.Music.volume-.23f)<.001f&&Mathf.Abs(gameAudio.Effects.volume-.61f)<.001f,"Paused settings sliders immediately control separate music and sound sources");
            musicSlider.value=0;Check(gameAudio.Music.volume==0&&Mathf.Abs(gameAudio.Effects.volume-.61f)<.001f,"Muting music leaves click/victory volume unchanged");
            musicSlider.value=.23f;effectSlider.value=0;Check(gameAudio.Effects.volume==0&&Mathf.Abs(gameAudio.Music.volume-.23f)<.001f,"Muting sound effects leaves music volume unchanged");
            settings.RestoreDefaults();Check(Mathf.Abs(gameAudio.Music.volume-.7f)<.001f&&Mathf.Abs(gameAudio.Effects.volume-.8f)<.001f,"Restore defaults updates both live audio sources");
            ModalButton("Return");
            Check(gameAudio.Music.clip==gameAudio.Bank.gameplayMusic&&gameAudio.Music.loop,"Gameplay uses looping background music");
            session.Complete();Check(!gameAudio.Music.isPlaying,"Victory stops gameplay background music");
            ModalButton("Replay");Check(gameAudio.Music.clip==gameAudio.Bank.gameplayMusic&&gameAudio.Music.isPlaying,"Replay restarts the gameplay soundtrack");
            for(int id=1;id<=3;id++)
            {
                SceneManager.LoadScene("Level0"+id);yield return null;yield return null;session=LevelSession.Current;
                Check(session.hud.levelName.sprite==template.levelNames[id-1],"Map "+id+" dynamically chooses its level-name artwork");
                Check(session.hud.GetComponent<FullScreenCanvas>()!=null,"Map "+id+" attaches fullscreen HUD adaptation");
                bool hasDisplayCamera=false;foreach(var camera in Camera.allCameras)if(camera.enabled&&camera.targetTexture==null&&camera.targetDisplay==0)hasDisplayCamera=true;
                Check(hasDisplayCamera,"Map "+id+" has an enabled camera rendering to the Game View display");
                int listeners=0;foreach(var listener in FindObjectsOfType<AudioListener>())if(listener.enabled)listeners++;
                Check(listeners==1,"Map "+id+" has exactly one enabled audio listener");
                var viewport=FindObjectOfType<WorldCamera>().GetComponent<Camera>().targetTexture;
                Check(Mathf.Abs((float)viewport.width/viewport.height-(float)Screen.width/Screen.height)<.01f,"Map "+id+" world viewport fills screen aspect without letterboxing");
            }
            session=LevelSession.Current;session.Retry(true);var motor=session.player;var energy=motor.GetComponent<RobotEnergy>();
            WorldSignal pressure=null;foreach(var signal in FindObjectsOfType<WorldSignal>())if(signal.plate){pressure=signal;break;}
            var playerCollider=motor.GetComponent<Collider2D>();int previousCharge=energy.Charge;
            pressure.Press(playerCollider);Check(pressure.Active&&energy.Charge==previousCharge,"Player steps on pressure button without consuming energy");
            var box=FindObjectOfType<PushableCrate>();pressure.Press(box.GetComponent<Collider2D>());pressure.Release(playerCollider);
            Check(pressure.Active,"Crate keeps pressure button pressed after player leaves");pressure.Release(box.GetComponent<Collider2D>());
            Check(!pressure.Active,"Pressure button releases after all weights leave");
            motor.Shield=true;motor.Feed(0,false,false);motor.Tick(.02f);Check(motor.GetComponent<SpriteRenderer>().color==Color.white,"Shield/checkpoint route no longer tints player cyan");
            var hint=motor.GetComponent<PlayerHint>();Check(hint!=null,"Player receives a head-following text hint component");
            var charger=FindObjectOfType<ChargeStation>();motor.transform.position=charger.transform.position;motor.Body.position=charger.transform.position;
            Check(hint.ProximityText()=="按 E 充能","Charging station proximity displays the E charging instruction");
            WorldSignal paid=null;foreach(var signal in FindObjectsOfType<WorldSignal>())if(!signal.plate&&signal.batteryCost>0){paid=signal;break;}
            if(paid!=null){motor.transform.position=paid.transform.position;motor.Body.position=paid.transform.position;Check(hint.ProximityText()=="按 E 消耗能量开启开关","Energy device proximity displays its power interaction instruction");}
            PlayerHint.Show("存档成功");yield return null;Check(hint.CurrentText=="存档成功","Checkpoint notice appears as text above the player");
            var sprite=box.GetComponent<Collider2D>();Check(sprite.sharedMaterial!=null&&sprite.sharedMaterial.friction>=.6f&&box.GetComponent<Rigidbody2D>().drag>=4,"Crate uses gripping collision material and stronger damping");
            var ground=new GameObject("CrateBrakeTestGround");ground.transform.position=new Vector3(90,-.25f,0);var groundShape=ground.AddComponent<BoxCollider2D>();groundShape.size=new Vector2(10,.5f);
            var crateBody=box.GetComponent<Rigidbody2D>();box.left=85;box.right=95;crateBody.position=new Vector2(90,.48f);crateBody.velocity=Vector2.zero;Physics2D.SyncTransforms();
            var oldSimulation=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
            for(int i=0;i<20;i++){box.Tick();Physics2D.Simulate(.02f);}float startX=crateBody.position.x;crateBody.velocity=new Vector2(2,0);
            for(int i=0;i<20;i++){box.Tick();Physics2D.Simulate(.02f);}
            Check(Mathf.Abs(crateBody.velocity.x)<.1f&&crateBody.position.x-startX<.25f,"Crate quickly stops on ground after pushing ends");
            Physics2D.simulationMode=oldSimulation;Destroy(ground);session.Retry(true);
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1920,1200),new Vector2(2560,1080),new Vector2(1024,768)})
            {
                var reference=new Vector2(1672,941);float fit=FullScreenCanvas.FitScale(size,reference),cover=FullScreenCanvas.CoverScale(size,reference);
                Check(reference.x*fit<=size.x+.01f&&reference.y*fit<=size.y+.01f&&reference.x*cover>=size.x-.01f&&reference.y*cover>=size.y-.01f,"Fullscreen background covers and controls stay visible at "+size);
            }
            Save();Debug.Log("NAVIGATION_TEST_PASSED");EditorApplication.Exit(0);
        }
    }
}
#endif
