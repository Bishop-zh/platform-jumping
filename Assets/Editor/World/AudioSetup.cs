using UnityEditor;
using UnityEngine;
using Rustbound.World;
public static class AudioSetup
{
    public static void Configure()
    {
        AssetDatabase.Refresh();
        foreach(string name in new[]{"UI_Click","Wasteland_Play_Loop","Victory"})
        {
            var importer=AssetImporter.GetAtPath("Assets/Audio/"+name+".wav") as AudioImporter;
            if(importer==null)throw new System.Exception("Missing audio: "+name);
            var settings=importer.defaultSampleSettings;settings.loadType=name=="Wasteland_Play_Loop"?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
        }
        var bank=AssetDatabase.LoadAssetAtPath<GameAudioBank>("Assets/Resources/GameAudioBank.asset");
        if(bank==null){bank=ScriptableObject.CreateInstance<GameAudioBank>();AssetDatabase.CreateAsset(bank,"Assets/Resources/GameAudioBank.asset");}
        bank.buttonClick=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI_Click.wav");
        bank.gameplayMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Wasteland_Play_Loop.wav");
        bank.victory=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Victory.wav");
        EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();Debug.Log("AUDIO_SETUP_PASSED");
    }
    public static void ConfigureAndValidate(){Configure();HandoverAudit.Run();}
}
