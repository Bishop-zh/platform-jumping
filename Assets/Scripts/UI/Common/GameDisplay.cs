using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rustbound.UI
{
    /// <summary>Applies display changes once; ordinary window dragging never resets the user's size.</summary>
    public sealed class GameDisplay : MonoBehaviour
    {
        public const int NativeWidth=2560,NativeHeight=1440;
        static GameDisplay instance;
        Coroutine transition;
        public static bool IsFullscreen { get; private set; }=true;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){instance=null;IsFullscreen=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartDisplay()
        {
            if(Application.isEditor)return;
            instance=new GameObject("GameDisplay").AddComponent<GameDisplay>();DontDestroyOnLoad(instance.gameObject);
            SetFullscreen(PlayerPrefs.GetInt("Settings.Fullscreen",1)==1);
        }
        public static void SetFullscreen(bool enabled)
        {
            IsFullscreen=enabled;
            if(Application.isEditor)return;
            if(instance==null)return;
            if(instance.transition!=null)instance.StopCoroutine(instance.transition);
            instance.transition=instance.StartCoroutine(instance.Apply(enabled));
        }
        public static Vector2Int WindowSize(Vector2Int screen)=>new Vector2Int(Mathf.Max(1,Mathf.RoundToInt(screen.x*2f/3f)),Mathf.Max(1,Mathf.RoundToInt(screen.y*2f/3f)));
        IEnumerator Apply(bool enabled)
        {
            var desktop=DesktopRect();
            if(enabled)Screen.SetResolution(NativeWidth,NativeHeight,FullScreenMode.FullScreenWindow);
            else
            {
                var size=WindowSize(new Vector2Int(desktop.right-desktop.left,desktop.bottom-desktop.top));
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                // Unity applies the resolution at frame end. Wait for its window style and client size.
                float deadline=Time.realtimeSinceStartup+2f;
                do {yield return null;}while((Screen.fullScreen||Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<deadline);
                yield return new WaitForSecondsRealtime(.15f);
                CenterWindow(desktop,size);
            }
            transition=null;
        }
        struct Rect {public int left,top,right,bottom;}
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential)] struct MonitorInfo {public int size;public Rect monitor,work;public uint flags;}
        delegate bool WindowCallback(IntPtr window,IntPtr data);
        [DllImport("user32.dll")] static extern bool EnumWindows(WindowCallback callback,IntPtr data);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint command);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
        static IntPtr PlayerWindow()
        {
            IntPtr found=IntPtr.Zero;uint own=(uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            EnumWindows((window,data)=>{GetWindowThreadProcessId(window,out uint process);if(process!=own||!IsWindowVisible(window)||GetWindow(window,4)!=IntPtr.Zero)return true;found=window;return false;},IntPtr.Zero);
            return found;
        }
#endif
        static Rect DesktopRect()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var info=new MonitorInfo{size=Marshal.SizeOf(typeof(MonitorInfo))};
            if(GetMonitorInfo(MonitorFromWindow(PlayerWindow(),2),ref info))return info.monitor;
#endif
            return new Rect{right=Display.main.systemWidth>0?Display.main.systemWidth:Screen.currentResolution.width,bottom=Display.main.systemHeight>0?Display.main.systemHeight:Screen.currentResolution.height};
        }
        static void CenterWindow(Rect screen,Vector2Int clientSize)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var window=PlayerWindow();if(window==IntPtr.Zero||!GetWindowRect(window,out Rect outer))return;
            GetClientRect(window,out Rect client);
            int width=clientSize.x+(outer.right-outer.left)-(client.right-client.left);
            int height=clientSize.y+(outer.bottom-outer.top)-(client.bottom-client.top);
            int x=screen.left+((screen.right-screen.left)-width)/2;
            int y=screen.top+((screen.bottom-screen.top)-height)/2;
            SetWindowPos(window,IntPtr.Zero,x,y,width,height,0x0004|0x0010);
#endif
        }
    }
}
