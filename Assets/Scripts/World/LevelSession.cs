using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Rustbound.UI;
using Rustbound.Progress;
namespace Rustbound.World
{
    public interface IWorldReset { void ResetWorld(bool full); }
    public sealed class LevelSession : MonoBehaviour
    {
        public static LevelSession Current;
        public static bool Validation;
        public static bool OpenSelection;
        public static LevelProgressRepository TestProgress;
        public int level=1;
        public float width=32;
        public Vector2 spawn=new Vector2(1,0);
        public PlayerMotor player;
        public ModalDialogView modalPrefab;
        public WorldHud hud;
        public bool standalone;
        Coroutine deathRoutine;
        public bool Playing { get; private set; }=true;
        public int Collected { get; private set; }
        public int Deaths { get; private set; }
        public float Seconds { get; private set; }
        public int Total { get; private set; }
        public int BatteriesCollected { get; private set; }
        public int BatteriesInBag { get; private set; }
        public int BatteryTotal { get; private set; }
        public Vector2 Checkpoint { get; private set; }
        ModalDialogView modal;
        LevelProgressRepository progress;
        bool ownsHud;
        void Awake()
        {
            Current=this; Time.timeScale=1; Checkpoint=spawn;
            progress=standalone?new LevelProgressRepository():Validation?(TestProgress??(TestProgress=new LevelProgressRepository())):LevelProgressRepository.Load();
            if(!standalone)progress.MarkEntered(level);
            if(FindObjectOfType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            if(modalPrefab==null){var assets=GameRuntimeAssets.Load();if(assets!=null)modalPrefab=assets.modal;}
            var pauseAssets=GameRuntimeAssets.Load();
            if(pauseAssets!=null&&pauseAssets.pauseModal!=null)modalPrefab=pauseAssets.pauseModal;
            if(modalPrefab!=null) { modal=Instantiate(modalPrefab);FullScreenCanvas.Attach(modal.gameObject); modal.Hide(); modal.ActionRequested.AddListener(HandleAction); }
        }
        void Start()
        {
            if(hud==null)hud=FindObjectOfType<WorldHud>();
            if(hud==null){var assets=GameRuntimeAssets.Load();if(assets!=null&&assets.gameplayHud!=null){hud=Instantiate(assets.gameplayHud);hud.name="GameplayHUD";ownsHud=true;}}
            if(hud!=null){FullScreenCanvas.Attach(hud.gameObject);hud.Bind(this);}
            PlayerHint.Attach(player);
            foreach(var p in transform.root.GetComponentsInChildren<WorldPickup>(true))
            {if(!p.gameObject.activeInHierarchy)continue;if(p.kind==PickupKind.Fragment)Total++;if(p.kind==PickupKind.Battery)BatteryTotal++;}
            BatteryTotal=FindObjectsOfType<ChargeStation>().Length;
            GameAudio.Ensure().PlayGameplay();
        }
        void OnDestroy(){if(ownsHud&&hud!=null)Destroy(hud.gameObject);if(modal!=null)Destroy(modal.gameObject);if(Current==this)Current=null;Time.timeScale=1;}
        void Update()
        {
            if(Playing)Seconds+=Time.deltaTime;
            if(!Validation && modal!=null && Input.GetKeyDown(KeyCode.Escape)) { if(Playing){Freeze();modal.Show(ModalMode.Settings);}else if(modal.Mode==ModalMode.Settings)Resume(); }
            if(hud!=null)hud.Refresh(this);
        }
        void Freeze() { Playing=false;player.Enabled=false;Time.timeScale=0; }
        void Resume() { if(modal!=null)modal.Hide();Time.timeScale=1;Playing=true;player.Enabled=true; }
        public void OpenSettings(){if(Playing&&modal!=null){Freeze();modal.Show(ModalMode.Settings);}}
        public void Die(bool voidHit=false) { if(!Playing||player.AbsorbHit(voidHit))return;Deaths++;player.BeginDeath();Freeze();deathRoutine=StartCoroutine(DeathSequence()); }
        System.Collections.IEnumerator DeathSequence(){yield return new WaitForSecondsRealtime(1.5f);deathRoutine=null;if(modal!=null)modal.Show(ModalMode.Failure);else Retry(false);}
        public void SetCheckpoint(Vector2 point) => Checkpoint=point;
        public void Collect() => Collected++;
        public void CollectBattery(){BatteriesCollected++;}
        public bool TrySpendBatteries(int count){var energy=player!=null?player.GetComponent<RobotEnergy>():null;return energy!=null&&energy.Spend(count);}
        public void Complete()
        {
            if(!Playing)return;
            int old=progress.UnlockedCount;
            progress.CompleteLevel(level,Mathf.Max(.02f,Seconds),Collected,Total);
            GameAudio.Ensure().PlayVictory();
            Freeze();if(modal!=null){modal.ShowVictory(Collected,Total,Seconds,Deaths,level<3,progress.UnlockedCount>old);modal.ShowBatterySummary(BatteriesCollected,BatteryTotal);}
        }
        public void Retry(bool full)
        {
            GameAudio.Ensure().PlayGameplay();
            if(deathRoutine!=null){StopCoroutine(deathRoutine);deathRoutine=null;}Time.timeScale=1;if(modal!=null)modal.Hide();
            if(full){Checkpoint=spawn;Collected=Deaths=0;Seconds=0;BatteriesCollected=BatteriesInBag=0;}
            if(full)player.GetComponent<RobotEnergy>()?.ResetCharge();
            var all=FindObjectsOfType<MonoBehaviour>(true);
            foreach(var b in all)if(b is WorldSignal s)s.ResetWorld(full);
            foreach(var b in all)if(b is IWorldReset r && !(b is WorldSignal))r.ResetWorld(full);
            player.Respawn(Checkpoint);Physics2D.SyncTransforms();Playing=true;
        }
        public void Interact(Vector2 point)
        {
            if(!Playing)return;
            WorldSignal nearest=null;float distance=1.5f;
            foreach(var s in FindObjectsOfType<WorldSignal>())if(!s.plate && Vector2.Distance(point,s.transform.position)<distance)
            {nearest=s;distance=Vector2.Distance(point,s.transform.position);}
            ChargeStation charger=null;foreach(var station in FindObjectsOfType<ChargeStation>())if(Vector2.Distance(point,station.transform.position)<distance){charger=station;distance=Vector2.Distance(point,station.transform.position);}
            if(charger!=null)charger.BeginCharge(player);else if(nearest!=null)nearest.Activate();
        }
        public void HandleAction(ModalAction action)
        {
            if(action==ModalAction.Close){Resume();return;}
            if(action==ModalAction.RetryCheckpoint){Retry(false);return;}
            if(action==ModalAction.RestartLevel||action==ModalAction.Replay){Retry(true);return;}
            Time.timeScale=1;
            if(action==ModalAction.NextLevel&&level<3)SceneManager.LoadScene("Level0"+(level+1));
            else {OpenSelection=action==ModalAction.ReturnLevelSelect;SceneManager.LoadScene("MainMenu");}
        }

    }
}
