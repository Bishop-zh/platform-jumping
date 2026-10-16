using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using Rustbound.World;

namespace Rustbound.UI
{
    // Opt-in QA command line only. Never runs during normal play or changes saved settings.
    public sealed class StandaloneDeliveryCheck : MonoBehaviour
    {
        static string output;readonly List<string> checks=new List<string>();int failures;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-jumpDeliveryCheck");
            if(Application.isEditor||index<0||index+1>=args.Length)return;
            Application.runInBackground=true;output=args[index+1];var go=new GameObject("Standalone delivery QA");DontDestroyOnLoad(go);go.AddComponent<StandaloneDeliveryCheck>();
        }
        void Check(bool condition,string name){checks.Add((condition?"PASS: ":"FAIL: ")+name);if(!condition)failures++;}
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(3);
            Check(Screen.fullScreen,"Default fullscreen startup");
            Check(Screen.width==2560&&Screen.height==1440,"2K rendering "+Screen.width+"x"+Screen.height);
            var modal=Instantiate(GameRuntimeAssets.Load().modal);modal.Show(ModalMode.Settings);
            var controller=modal.GetComponentInChildren<ModalSettingsController>();
            var toggle=(Toggle)typeof(ModalSettingsController).GetField("fullscreen",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(controller);
            LevelSession.Validation=true; // Avoid writing the user's volume/fullscreen preferences in QA.
            toggle.isOn=false;yield return new WaitForSecondsRealtime(3);
            Check(!Screen.fullScreen,"Settings toggle exits fullscreen");
            VerifyWindow();modal.Hide();Destroy(modal.gameObject);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level03");yield return new WaitForSecondsRealtime(2);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(900,700),new Vector2Int(720,1000)})
            {
                ResizeNative(size);yield return new WaitForSecondsRealtime(1);
                Check(Mathf.Abs(Screen.width-size.x)<=2&&Mathf.Abs(Screen.height-size.y)<=2,"Native window edge resize "+size);
                VerifyLayout(LevelSession.Current.hud.gameObject,"HUD");
                LevelSession.Current.OpenSettings();yield return new WaitForSecondsRealtime(.5f);
                var pause=FindObjectOfType<ModalDialogView>();VerifyLayout(pause.gameObject,"Pause");
                if(size.x==900){ScreenCapture.CaptureScreenshot(Path.Combine(output,"windowed-pause.png"));yield return new WaitForSecondsRealtime(.2f);}
                LevelSession.Current.HandleAction(ModalAction.Close);
            }
            GameDisplay.SetFullscreen(true);yield return new WaitForSecondsRealtime(2);
            Check(Screen.fullScreen,"Fullscreen restored after resize");VerifyLayout(LevelSession.Current.hud.gameObject,"HUD fullscreen");
            LevelSession.Current.OpenSettings();yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"fullscreen-pause.png"));
            yield return new WaitForSecondsRealtime(.2f);
            var current=FindObjectOfType<ModalDialogView>();current.transform.Find("Composition/SettingsContent/ReturnMainMenu").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(1);Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu"&&Time.timeScale==1,"Pause exit returns to main menu");
            Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,"standalone-validation.txt"),checks);
            File.WriteAllText(Path.Combine(output,"standalone-result.txt"),"Failures: "+failures+"\nChecks: "+checks.Count);
            Debug.Log("STANDALONE_DELIVERY_QA_COMPLETE failures="+failures);Application.Quit(failures==0?0:1);
        }
        void VerifyLayout(GameObject root,string name)
        {
            Canvas.ForceUpdateCanvases();bool inside=true;string outside="";var corners=new Vector3[4];
            var composition=root.transform.Find("Composition");
            foreach(var graphic in composition.GetComponentsInChildren<Graphic>())
            {
                if(!graphic.isActiveAndEnabled)continue;graphic.rectTransform.GetWorldCorners(corners);
                foreach(var p in corners)if(p.x< -3||p.y< -3||p.x>Screen.width+3||p.y>Screen.height+3){inside=false;outside+=graphic.name+" ";break;}
            }
            Check(inside,name+" fits "+Screen.width+"x"+Screen.height+" "+outside);
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        struct Rect{public int left,top,right,bottom;}
        [StructLayout(LayoutKind.Sequential)]struct Monitor{public int size;public Rect screen,work;public uint flags;}
        [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll")]static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
        [DllImport("user32.dll")]static extern bool GetMonitorInfo(IntPtr monitor,ref Monitor info);
        [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
        static IntPtr Window()=>System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
#endif
        void VerifyWindow()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var window=Window();var info=new Monitor{size=Marshal.SizeOf(typeof(Monitor))};GetMonitorInfo(MonitorFromWindow(window,2),ref info);
            GetWindowRect(window,out Rect outer);GetClientRect(window,out Rect client);
            var expected=GameDisplay.WindowSize(new Vector2Int(info.screen.right-info.screen.left,info.screen.bottom-info.screen.top));
            Check(Mathf.Abs(client.right-expected.x)<=2&&Mathf.Abs(client.bottom-expected.y)<=2,"Window client is screen two-thirds "+client.right+"x"+client.bottom);
            Check(Mathf.Abs((outer.left+outer.right)-(info.screen.left+info.screen.right))<=4&&Mathf.Abs((outer.top+outer.bottom)-(info.screen.top+info.screen.bottom))<=4,"Window centered on current monitor");
#endif
        }
        void ResizeNative(Vector2Int size)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var window=Window();GetWindowRect(window,out Rect outer);GetClientRect(window,out Rect client);
            SetWindowPos(window,IntPtr.Zero,0,0,size.x+outer.right-outer.left-client.right,size.y+outer.bottom-outer.top-client.bottom,0x0002|0x0004|0x0010);
#endif
        }
    }
}

