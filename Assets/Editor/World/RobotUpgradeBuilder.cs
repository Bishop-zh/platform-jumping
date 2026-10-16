using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Rustbound.World;
public static class RobotUpgradeBuilder
{
    const string Art="Assets/Art/Pixel/Robot/",Anim="Assets/Animations/Robot/";
    static AnimatorController controller;
    static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
    static AnimationClip Clip(string name,string prefix,int start,int count,float fps,bool loop)
    {
        string path=Anim+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.frameRate=fps;
        var keys=new ObjectReferenceKeyframe[count+1];for(int i=0;i<=count;i++)keys[i]=new ObjectReferenceKeyframe{time=i/fps,value=Sprite(prefix+(start+Math.Min(i,count-1)).ToString("00"))};
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);return clip;
    }
    static void UpgradePlayer(GameObject go)
    {
        var animator=go.GetComponent<Animator>();if(animator==null)animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.updateMode=AnimatorUpdateMode.UnscaledTime;animator.applyRootMotion=false;
        go.GetComponent<SpriteRenderer>().sprite=Sprite("Idle00");
        if(go.GetComponent<RobotEnergy>()==null)go.AddComponent<RobotEnergy>();if(go.GetComponent<PlayerRigBootstrap>()==null)go.AddComponent<PlayerRigBootstrap>();
        var rb=go.GetComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Dynamic;rb.constraints=RigidbodyConstraints2D.FreezeRotation;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
    }
    static void Charger(GameObject go)
    {
        foreach(var animation in go.GetComponents<WorldSpriteAnimation>())UnityEngine.Object.DestroyImmediate(animation);
        foreach(var pickup in go.GetComponents<WorldPickup>())UnityEngine.Object.DestroyImmediate(pickup);
        var sr=go.GetComponent<SpriteRenderer>();if(sr==null)sr=go.AddComponent<SpriteRenderer>();sr.sprite=Sprite("Charge00");sr.sortingOrder=2;
        var station=go.GetComponent<ChargeStation>();if(station==null)station=go.AddComponent<ChargeStation>();station.frames=Enumerable.Range(0,4).Select(i=>Sprite("Charge"+i.ToString("00"))).ToArray();
        var collider=go.GetComponent<BoxCollider2D>();if(collider==null)collider=go.AddComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=new Vector2(1.4f,2.5f);collider.offset=new Vector2(0,1.25f);go.layer=13;
    }
    static void UpgradeHud(GameObject root)
    {
        foreach(var hud in root.GetComponentsInChildren<WorldHud>(true))
        {
            var time=new SerializedObject(hud.time);time.FindProperty("characterHeight").floatValue=9.5f;time.ApplyModifiedPropertiesWithoutUndo();hud.time.SetValue("00:00.00");
            var label=hud.transform.Find("Composition/BagLabel");if(label!=null)label.GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Gameplay/EnergyLabel.png");
            var key=hud.transform.Find("Composition/Controls/KeyC");if(key!=null){var text=key.GetComponent<Image>();text.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Gameplay/KeyE.png");}
        }
    }
    public static void Build()
    {
        Directory.CreateDirectory(Anim);AssetDatabase.Refresh();
        foreach(var file in Directory.GetFiles(Art,"*.png"))
        {
            var imp=(TextureImporter)AssetImporter.GetAtPath(file);imp.textureType=TextureImporterType.Sprite;imp.spritePixelsPerUnit=32;imp.spriteImportMode=SpriteImportMode.Single;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.BottomCenter;settings.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(settings);imp.alphaIsTransparency=true;imp.SaveAndReimport();
        }
        foreach(var name in new[]{"EnergyLabel","ChargerLabel","KeyE"}){var imp=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/UI/Gameplay/"+name+".png");imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();}
        string controllerPath=Anim+"Robot.controller";controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        if(controller.layers.Length==0)controller.AddLayer("Base Layer");
        var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);foreach(var transition in machine.anyStateTransitions)machine.RemoveAnyStateTransition(transition);
        controller.parameters=new AnimatorControllerParameter[0];controller.AddParameter("Mode",AnimatorControllerParameterType.Int);controller.AddParameter("Dead",AnimatorControllerParameterType.Bool);
        string[] names={"Idle","Run","Rise","Fall","Dash","Death","Charging","Land"};string[] prefixes={"Idle","Run","Jump","Jump","Run","Death","Idle","Jump"};int[] starts={0,0,4,11,0,0,0,19};int[] counts={24,24,7,8,6,24,12,5};float[] rates={8,18,16,14,32,16,8,25};
        for(int i=0;i<names.Length;i++)
        {
            var state=machine.AddState(names[i],new Vector3((i%4)*220,(i/4)*100));state.motion=Clip(names[i],prefixes[i],starts[i],counts[i],rates[i],i==0||i==1||i==4||i==6);if(i==0)machine.defaultState=state;
            var transition=machine.AddAnyStateTransition(state);transition.hasExitTime=false;transition.duration=0;transition.canTransitionToSelf=false;transition.AddCondition(AnimatorConditionMode.Equals,i,"Mode");
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        var player=PrefabUtility.LoadPrefabContents("Assets/Prefabs/World/Player.prefab");UpgradePlayer(player);PrefabUtility.SaveAsPrefabAsset(player,"Assets/Prefabs/World/Player.prefab");PrefabUtility.UnloadPrefabContents(player);
        var charger=new GameObject("ChargeStation");Charger(charger);PrefabUtility.SaveAsPrefabAsset(charger,"Assets/Prefabs/World/ChargeStation.prefab");UnityEngine.Object.DestroyImmediate(charger);
        var legacy=PrefabUtility.LoadPrefabContents("Assets/Prefabs/World/Battery.prefab");Charger(legacy);legacy.name="ChargeStation";PrefabUtility.SaveAsPrefabAsset(legacy,"Assets/Prefabs/World/Battery.prefab");PrefabUtility.UnloadPrefabContents(legacy);
        foreach(var file in Directory.GetFiles("Assets/Prefabs/Levels","*.prefab"))
        {
            var root=PrefabUtility.LoadPrefabContents(file);foreach(var motor in root.GetComponentsInChildren<PlayerMotor>(true))UpgradePlayer(motor.gameObject);
            foreach(var pickup in root.GetComponentsInChildren<WorldPickup>(true).Where(p=>p.kind==PickupKind.Battery).ToArray()){var go=pickup.gameObject;go.name="ChargeStation";go.transform.position+=Vector3.down*.6f;Charger(go);}
            UpgradeHud(root);PrefabUtility.SaveAsPrefabAsset(root,file);PrefabUtility.UnloadPrefabContents(root);
        }
        var hudRoot=PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/GameplayHUD.prefab");UpgradeHud(hudRoot);PrefabUtility.SaveAsPrefabAsset(hudRoot,"Assets/Prefabs/UI/GameplayHUD.prefab");PrefabUtility.UnloadPrefabContents(hudRoot);
        var modal=PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/SharedModal.prefab");modal.transform.Find("Composition/VictoryContent/BatteryLabel").GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Gameplay/ChargerLabel.png");PrefabUtility.SaveAsPrefabAsset(modal,"Assets/Prefabs/UI/SharedModal.prefab");PrefabUtility.UnloadPrefabContents(modal);
        AssetDatabase.SaveAssets();Debug.Log("ROBOT_UPGRADE_OK");EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");if(Array.IndexOf(Environment.GetCommandLineArgs(),"-jumpRobotTest")>=0)EditorApplication.isPlaying=true;else EditorApplication.Exit(0);
    }
}

