using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class DeliveryBuild
{
    [MenuItem("Rustbound/Delivery/Build Windows Game")]
    public static void Run()
    {
        PlayerSettings.defaultScreenWidth=2560;PlayerSettings.defaultScreenHeight=1440;
        PlayerSettings.defaultIsNativeResolution=false;PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow=true;PlayerSettings.allowFullscreenSwitch=false;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-jumpBuildOutput");
        string output=index>=0?args[index+1]:Path.GetFullPath("Deliverables/WindowsGame/jump.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.CompressWithLz4HC});
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-report.txt"),"Result: "+report.summary.result+"\nUnity: "+Application.unityVersion+"\nBytes: "+report.summary.totalSize+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\n");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        Debug.Log("WINDOWS_DELIVERY_BUILD_OK "+output);
    }
}
