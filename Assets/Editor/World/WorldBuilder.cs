using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Rustbound.World;
using Rustbound.UI;
using UnityEditor.Events;
public static class WorldBuilder
{
    static readonly string Art="Assets/Art/Pixel/World/";
    static readonly string Pref="Assets/Prefabs/World/";
    static LevelSession session;
    static PhysicsMaterial2D smooth;
    static PlayerTuning tuning;
    static readonly List<Vector3> groundSpans=new List<Vector3>();
    static Sprite S(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+n+".png");
    [Serializable] class Catalog {public CatalogItem[] items;}
    [Serializable] class CatalogItem {public string name;public float width,height,interactionX;public bool decor,front;public CatalogBox[] colliders;}
    [Serializable] class CatalogBox {public float x,y,w,h;}
    static void BuildCatalog()
    {
        Directory.CreateDirectory(Pref+"Modular");Directory.CreateDirectory(Pref+"Scenery");
        var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText("ArtSource/World/Catalog/catalog.json"));
        foreach(var item in catalog.items)
        {
            var go=Visual(item.name,item.name,0,0,item.decor?(item.front?-5:-25):0);
            if(item.decor)go.AddComponent<SceneryParallax>().scrollRatio=item.front?.8f:.25f;
            if(!item.decor)
            {
                Body(go,RigidbodyType2D.Static);
                foreach(var c in item.colliders)Box(go,c.w,c.h,c.x,c.y);
                if(item.name=="BridgeMovingDeck")
                {go.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;go.layer=11;go.AddComponent<MovingPlatform>();}
                if(item.name=="CheckpointPlatform"||item.name=="BridgeCheckpointBase"||item.name=="ExitPlatform"||item.name=="TerminalPlatform"||item.name=="GroundExitBase")
                {
                    bool checkpoint=item.name.Contains("Checkpoint");float floor=item.colliders[0].y+item.colliders[0].h/2;
                    var zone=go.AddComponent<WorldZone>();zone.kind=checkpoint?ZoneKind.Checkpoint:ZoneKind.Exit;zone.off=zone.on=S(item.name);
                    Box(go,.65f,1.4f,item.interactionX,floor+.7f,true);go.layer=8;
                    var point=new GameObject("InteractionPoint");point.transform.SetParent(go.transform,false);point.transform.localPosition=new Vector3(item.interactionX,floor,0);zone.respawnPoint=point.transform;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(go,Pref+(item.decor?"Scenery/":"Modular/")+item.name+".prefab");UnityEngine.Object.DestroyImmediate(go);
        }
        foreach(string stem in new[]{"SpikeLeft","SpikeMiddle","SpikeRight"})
        {
            var go=Visual(stem,stem,0,0,1);Box(go,1,.35f,0,.27f,true);go.layer=12;
            go.AddComponent<WorldZone>().kind=ZoneKind.Hazard;SavePrefab(go,stem);UnityEngine.Object.DestroyImmediate(go);
        }
        // Two self-contained examples demonstrate the same event wiring used in the maps.
        foreach(bool crate in new[]{false,true})
        {
            var kit=new GameObject(crate?"CratePlateGateKit":"BatterySocketGateKit");
            var source=Visual("Input",crate?"WeightPlateOff":"WallButtonOff",0,0,2);source.transform.SetParent(kit.transform);
            var signal=source.AddComponent<WorldSignal>();signal.plate=crate;signal.batteryCost=crate?0:1;signal.off=S(crate?"WeightPlateOff":"WallButtonOff");signal.on=S(crate?"WeightPlateOn":"WallButtonOn");
            if(!crate)SavePrefab(source,"BatterySocket");
            if(crate)Box(source,.95f,.25f,0,.18f,true);
            var gate=Gate(2,0,null);gate.transform.SetParent(kit.transform);UnityEventTools.AddPersistentListener(signal.onChanged,gate.SetPowered);
            PrefabUtility.SaveAsPrefabAsset(kit,Pref+kit.name+".prefab");UnityEngine.Object.DestroyImmediate(kit);
        }
    }
    static void AddScenery(Transform root)
    {
        var group=new GameObject("SceneryLayers");group.transform.SetParent(root,false);
        for(int i=0;i<3;i++){var g=Visual("FarCity","FarCity",8+i*16,-3,-25);g.AddComponent<SceneryParallax>().scrollRatio=.25f;g.transform.SetParent(group.transform,true);}
        foreach(var p in new[]{new Vector3(3,-2,0),new Vector3(session.width-4,-2,1)})
        {var g=Visual("NearDecoration",p.z==0?"NearBrokenTank":"NearScrapHeap",p.x,p.y,-5);g.AddComponent<SceneryParallax>().scrollRatio=.8f;g.transform.SetParent(group.transform,true);}
    }
    static void ExtendVictory()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/SharedModal.prefab");
        var content=root.transform.Find("Composition/VictoryContent");
        var old=content.Find("Batteries");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        old=content.Find("BatteryLabel");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var label=UnityEngine.Object.Instantiate(content.Find("Collected").gameObject,content);label.name="Batteries";
        var number=label.GetComponent<SpriteNumberLabel>();((RectTransform)label.transform).anchoredPosition=new Vector2(966,-330);number.SetValue("00/00");
        root.GetComponent<ModalDialogView>().ConfigureBatterySummary(number);
        var batteryLabel=Rect("BatteryLabel",content,650,332,130,34).gameObject.AddComponent<Image>();batteryLabel.sprite=U("BatteryLabel");batteryLabel.preserveAspect=true;batteryLabel.raycastTarget=false;
        string[] rowNames={"Collected","Time","Deaths"};float[] ys={390,450,510};
        for(int i=0;i<3;i++){var r=(RectTransform)content.Find(rowNames[i]);r.anchoredPosition=new Vector2(966,-ys[i]);}
        foreach(var image in content.GetComponentsInChildren<Image>(true))
        {
            string n=image.name;if(n=="CollectIcon"||n=="CollectLabel"){var r=image.rectTransform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,-390);if(n=="CollectLabel"){image.sprite=U("CrystalLabel");image.preserveAspect=true;image.color=Color.white;r.sizeDelta=new Vector2(130,34);}if(n=="CollectIcon")image.sprite=S("Fragment0");}
            if(n=="TimeIcon"||n=="TimeLabel"){var r=image.rectTransform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,-450);}
            if(n=="DeathIcon"||n=="DeathLabel"){var r=image.rectTransform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,-510);}
        }
        var icon=content.Find("BatteryIcon");if(icon!=null)UnityEngine.Object.DestroyImmediate(icon.gameObject);
        var batteryIcon=Rect("BatteryIcon",content,572,330,43,43).gameObject.AddComponent<Image>();batteryIcon.sprite=U("Battery");batteryIcon.preserveAspect=true;batteryIcon.raycastTarget=false;
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/UI/SharedModal.prefab");PrefabUtility.UnloadPrefabContents(root);
    }
    public static void BuildAndCapture()
    {
        Directory.CreateDirectory(Pref);Directory.CreateDirectory("Artifacts/World");
        var tagSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers=tagSettings.FindProperty("layers");string[] layerNames={"WorldGround","WorldPlayer","WorldCrate","WorldPlatform","WorldHazard","WorldTrigger"};
        for(int i=0;i<layerNames.Length;i++){var slot=layers.GetArrayElementAtIndex(8+i);if(!string.IsNullOrEmpty(slot.stringValue)&&slot.stringValue!=layerNames[i])throw new Exception("Layer slot already occupied: "+(8+i));slot.stringValue=layerNames[i];}
        tagSettings.ApplyModifiedProperties();
        foreach(string f in Directory.GetFiles(Art,"*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(f);importer.textureType=TextureImporterType.Sprite;
            importer.spritePixelsPerUnit=32;importer.spriteImportMode=SpriteImportMode.Single;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.BottomCenter;importer.SetTextureSettings(settings);
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true;importer.SaveAndReimport();
        }
        foreach(string f in Directory.GetFiles("Assets/Art/UI/Gameplay","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(f);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        }
        smooth=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Pref+"NoFriction.physicsMaterial2D");
        if(smooth==null){smooth=new PhysicsMaterial2D("NoFriction"){friction=0,bounciness=0};AssetDatabase.CreateAsset(smooth,Pref+"NoFriction.physicsMaterial2D");}
        Directory.CreateDirectory("Assets/Data");tuning=AssetDatabase.LoadAssetAtPath<PlayerTuning>("Assets/Data/PlayerTuning.asset");
        if(tuning==null){tuning=ScriptableObject.CreateInstance<PlayerTuning>();AssetDatabase.CreateAsset(tuning,"Assets/Data/PlayerTuning.asset");}
        ExtendVictory();BuildCatalog();
        for(int i=1;i<=3;i++)Build(i);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Level01.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Level02.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Level03.unity",true)};
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        Debug.Log("WORLD_BUILD_OK: three serialized scenes and reusable world prefabs");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-jumpWorldTest")>=0)EditorApplication.isPlaying=true;
        else EditorApplication.Exit(0);
    }
    static GameObject Visual(string name,string sprite,float x,float y,int order=0)
    {
        var go=new GameObject(name);go.transform.position=new Vector3(x,y,0);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=S(sprite);sr.sortingOrder=order;return go;
    }
    static BoxCollider2D Box(GameObject go,float w,float h,float ox=0,float oy=0,bool trigger=false)
    {go.layer=trigger?13:8;var c=go.AddComponent<BoxCollider2D>();c.size=new Vector2(w,h);c.offset=new Vector2(ox,oy);c.isTrigger=trigger;c.sharedMaterial=smooth;return c;}
    static Rigidbody2D Body(GameObject go,RigidbodyType2D type)
    {var b=go.AddComponent<Rigidbody2D>();b.bodyType=type;b.gravityScale=28/9.81f;b.constraints=RigidbodyConstraints2D.FreezeRotation;b.collisionDetectionMode=CollisionDetectionMode2D.Continuous;return b;}
    static void SavePrefab(GameObject go,string name){PrefabUtility.SaveAsPrefabAsset(go,Pref+name+".prefab");}
    static void Ground(float a,float b,float top,bool floating=false)
    {
        var root=new GameObject(floating?"RaisedPlatform":"Ground_"+a);root.transform.position=new Vector3(a,top,0);
        Box(root,b-a,floating?.25f:3,(b-a)/2,floating?-.125f:-1.5f);
        Body(root,RigidbodyType2D.Static);
        if(!floating)groundSpans.Add(new Vector3(a,b,top));
        for(float x=a;x<b-.001f;x+=2)
        {
            float w=Mathf.Min(2,b-x);int pixels=Mathf.RoundToInt(w*32);
            string stem=floating?"PlatformA":"GroundModule"+((int)((x-a)/2)%3+1);
            if(x==a)stem=floating?"PlatformLeft":"GroundModule0";
            else if(Mathf.Abs(x+2-b)<.001f)stem=floating?"PlatformRight":"GroundModule4";
            if(pixels<64)stem+="_"+pixels;
            var tile=Visual("Surface",stem,x+w/2,top-(floating?.5f:2));tile.transform.SetParent(root.transform,true);
        }
        if(floating && top>.75f)
        {
            int count=b-a<=3?1:2;
            for(int i=0;i<count;i++)
            {
                float x=count==1?(a+b)/2:i==0?a+.5f:b-.5f;
                float floor=-2;
                foreach(var span in groundSpans)if(x>=span.x&&x<=span.y&&span.z<top-.5f)floor=Mathf.Max(floor,span.z);
                Support(root.transform,x,floor,top-.5f);
            }
        }
    }
    static void Support(Transform root,float x,float floor,float ceiling)
    {
        // One half-unit column under each chosen bearing; end exactly at the underside.
        int height=Mathf.RoundToInt((ceiling-floor)*32);if(height<=0)return;
        var go=Visual("AlignedSupport","SupportHeight_"+height,x,floor,-2);go.transform.SetParent(root,true);
    }
    static WorldSignal Signal(float x,float y,bool plate,bool wall=false)
    {
        string prefix=plate?"WeightPlate":wall?"WallButton":"Lever";
        var g=Visual(prefix,prefix+"Off",x,y,2);var s=g.AddComponent<WorldSignal>();s.plate=plate;s.off=S(prefix+"Off");s.on=S(prefix+"On");
        if(plate)Box(g,.95f,.25f,0,.18f,true);
        if(!plate){var marker=new GameObject("Interact_C");marker.transform.SetParent(g.transform,false);marker.transform.localPosition=new Vector3(0,1.15f,0);var text=marker.AddComponent<TextMesh>();text.text="C";text.fontSize=32;text.characterSize=.1f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.cyan;marker.GetComponent<MeshRenderer>().sortingOrder=6;}
        SavePrefab(g,prefix);return s;
    }
    static void Crate(float x,float y)
    {var g=Visual("PushableCrate","CrateA",x,y,2);Box(g,.94f,.94f,0,.47f);g.layer=10;var rb=Body(g,RigidbodyType2D.Dynamic);rb.mass=2;rb.drag=2;var p=g.AddComponent<PushableCrate>();p.initial=new Vector2(x,y);p.left=x-1;p.right=x+3;SavePrefab(g,"Crate");}
    static GateController Gate(float x,float y,WorldSignal a,WorldSignal b=null)
    {var g=Visual("PoweredGate","GateClosed",x,y,3);Box(g,.84f,3,0,1.5f);var c=g.AddComponent<GateController>();c.closed=S("GateClosed");c.open=S("GateOpen");SavePrefab(g,"PoweredGate");c.inputA=a;c.inputB=b;if(a!=null)UnityEventTools.AddPersistentListener(a.onChanged,c.SetPowered);return c;}
    static WorldZone Zone(string name,ZoneKind kind,float x,float y,float w,float h,string prefix=null)
    {
        var g=prefix==null?new GameObject(name):Visual(name,prefix+"Off",x,y,1);g.transform.position=new Vector3(x,y,0);
        Box(g,w,h,0,h/2,true);g.layer=kind==ZoneKind.Hazard||kind==ZoneKind.Void?12:13;var z=g.AddComponent<WorldZone>();z.kind=kind;if(prefix!=null){z.off=S(prefix+"Off");z.on=S(prefix+"On");SavePrefab(g,prefix);}return z;
    }
    static void Pit(float a,float b)
    {
        Zone("Spikes",ZoneKind.Hazard,(a+b)/2,-1.5f,b-a,.45f);
        Ground(a,b,-1.5f);
        for(float x=a;x<b;x+=1){float w=Mathf.Min(1,b-x);string n=x==a?"SpikeLeft":x+1>=b?"SpikeRight":"SpikeMiddle";if(w<1)n+="_"+Mathf.RoundToInt(w*32);Visual("SpikeArt",n,x+w/2,-1.5f,1);}
    }
    static void Pickup(float x,float y,PickupKind kind=PickupKind.Fragment)
    {string n=kind==PickupKind.Fragment?"Fragment0":kind==PickupKind.Battery?"BatteryWorld":kind==PickupKind.Shield?"Shield0":"JumpBoost0";var g=Visual(kind.ToString(),n,x,y,4);Box(g,.5f,.5f,0,.25f,true);g.AddComponent<WorldPickup>().kind=kind;var animation=g.AddComponent<WorldSpriteAnimation>();animation.frames=kind==PickupKind.Fragment?new[]{S("Fragment0"),S("Fragment1"),S("Fragment2"),S("Fragment3")}:kind==PickupKind.Battery?new[]{S(n)}:new[]{S(n),S(kind==PickupKind.Shield?"Shield1":"JumpBoost1")};SavePrefab(g,kind.ToString());}
    static void Ceiling(float a,float b,float bottom)
    {
        Ground(a,b,bottom+.5f,true);
        var all=UnityEngine.Object.FindObjectsOfType<BoxCollider2D>();
        foreach(var c in all)if(c.name=="RaisedPlatform"&&Mathf.Abs(c.transform.position.x-a)<.01f&&Mathf.Abs(c.transform.position.y-bottom-.5f)<.01f)
        {c.size=new Vector2(b-a,.5f);c.offset=new Vector2((b-a)/2,-.25f);}
    }
    static void Build(int id)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        groundSpans.Clear();
        session=new GameObject("LevelSession").AddComponent<LevelSession>();session.level=id;session.width=id==1?32:id==2?36:36.5f;
        session.modalPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SharedModal.prefab").GetComponent<ModalDialogView>();
        var player=Visual("Player","Player0",1,0,5);Box(player,.5f,1.25f,0,.625f);player.layer=9;Body(player,RigidbodyType2D.Dynamic);session.player=player.AddComponent<PlayerMotor>();session.player.tuning=tuning;
        var presentation=player.AddComponent<PlayerPresentation>();presentation.idle=S("Player0");presentation.blink=S("Player1");presentation.dashFrames=new[]{S("DashFX0"),S("DashFX1"),S("DashFX2"),S("DashFX3")};SavePrefab(player,"Player");
        var camera=new GameObject("WorldCamera",typeof(Camera));camera.tag="MainCamera";camera.transform.position=new Vector3(10,2.5f,-10);
        var cam=camera.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=5.625f;cam.backgroundColor=new Color32(192,179,140,255);cam.clearFlags=CameraClearFlags.SolidColor;
        var follow=camera.AddComponent<WorldCamera>();follow.target=session.player;follow.width=session.width;
        Backdrop();session.hud=Hud();
        Zone("Void",ZoneKind.Void,session.width/2,-6,session.width+10,2);
        var wall=new GameObject("LeftBoundary");wall.transform.position=new Vector3(-.5f,0);Box(wall,1,20,0,4);
        wall=new GameObject("RightBoundary");wall.transform.position=new Vector3(session.width+.5f,0);Box(wall,1,20,0,4);
        WorldSignal a=null,b=null;
        if(id==1)
        {
            Ground(0,6,0);Ground(3,4,.5f,true);Pit(6,8);Ground(8,18,0);Crate(10,0);a=Signal(12.5f,0,true);a.batteryCost=1;Gate(15,0,a);Ceiling(12.5f,16,3);
            Zone("Checkpoint",ZoneKind.Checkpoint,16.5f,0,.6f,1.6f,"Checkpoint");Pit(18,21.25f);Ground(21.25f,32,0);Ground(25,27,1.5f,true);
            foreach(var p in new[]{new Vector2(2,.8f),new Vector2(3.5f,1.3f),new Vector2(6.7f,1.5f),new Vector2(8.5f,.8f),new Vector2(16.5f,1.9f),new Vector2(19.5f,1.5f),new Vector2(25.5f,2.2f),new Vector2(26.5f,2.2f)})Pickup(p.x,p.y);
            Pickup(4.5f,.6f,PickupKind.Battery);Pickup(17,.6f,PickupKind.Battery);
            b=Signal(22.5f,0,false,true);b.batteryCost=1;Gate(24,0,b);Ceiling(23,25.5f,3);
        }
        if(id==2)
        {
            Ground(0,7,0);a=Signal(4,0,false);a.batteryCost=1;Pit(7,13);Ground(13,19,0);Zone("Checkpoint",ZoneKind.Checkpoint,16,0,.6f,1.6f,"Checkpoint");Pickup(17,.7f,PickupKind.Shield);
            var m=Visual("MovingPlatform","MovingPlatform",8.75f,-.5f,2);Box(m,2.5f,.25f,0,.375f);m.layer=11;Body(m,RigidbodyType2D.Kinematic);var move=m.AddComponent<MovingPlatform>();move.from=new Vector2(8.75f,-.5f);move.to=new Vector2(12.75f,-.5f);SavePrefab(m,"MovingPlatform");move.power=a;UnityEventTools.AddPersistentListener(a.onChanged,move.SetPowered);
            Pit(19,22.25f);Ground(22.25f,36,0);
            Pickup(3,.6f,PickupKind.Battery);Pickup(17.5f,.6f,PickupKind.Battery);Pickup(24.7f,.6f,PickupKind.Battery);
            var s2=Signal(22.8f,0,false,true);s2.batteryCost=1;Gate(24,0,s2);Ceiling(23,25.5f,3);
            var s3=Signal(30.5f,0,false,true);s3.batteryCost=1;Gate(32,0,s3);Ceiling(31,33.5f,3);
            var enemy=Visual("PatrolEnemy","Patrol0",25,0,3);Box(enemy,.65f,.75f,0,.375f,true);enemy.layer=12;Body(enemy,RigidbodyType2D.Kinematic);enemy.AddComponent<WorldZone>().kind=ZoneKind.Hazard;var patrol=enemy.AddComponent<PatrolEnemy>();patrol.frames=new[]{S("Patrol0"),S("Patrol1"),S("Patrol2"),S("Patrol3")};SavePrefab(enemy,"PatrolEnemy");
            foreach(var p in new[]{new Vector2(2,.8f),new Vector2(5,.8f),new Vector2(8.5f,1),new Vector2(11,1),new Vector2(14,.8f),new Vector2(18,.8f),new Vector2(20.5f,1.5f),new Vector2(24,.8f),new Vector2(29.5f,1.5f),new Vector2(30.5f,1.5f)})Pickup(p.x,p.y);
        }
        if(id==3)
        {
            Ground(0,14,0);Crate(3,0);a=Signal(5.5f,0,true);a.batteryCost=1;Gate(8.5f,0,a);Ceiling(6,10,3);Zone("Checkpoint",ZoneKind.Checkpoint,11,0,.6f,1.6f,"Checkpoint");
            Pit(14,17);Ground(17,23.5f,.5f);Ground(18.5f,20.5f,2.25f,true);Pickup(18,1.2f,PickupKind.JumpBoost);b=Signal(21,1.5f,false,true);b.batteryCost=1;
            Pit(23.5f,26.75f);Ground(26.75f,36.5f,.5f);Gate(32.5f,.5f,a,b);Ceiling(30.5f,34,3.5f);
            Pickup(2.8f,.6f,PickupKind.Battery);Pickup(12,.6f,PickupKind.Battery);Pickup(22.5f,1.1f,PickupKind.Battery);
            var s3=Signal(27.5f,.5f,false,true);s3.batteryCost=1;Gate(29,.5f,s3);Ceiling(28,30.5f,3.5f);
            foreach(var p in new[]{new Vector2(2,.8f),new Vector2(4,.8f),new Vector2(7,1.4f),new Vector2(10.5f,.8f),new Vector2(12.5f,.8f),new Vector2(15.3f,1.8f),new Vector2(18,1.4f),new Vector2(19,3),new Vector2(20,3),new Vector2(22,1.3f),new Vector2(25,2),new Vector2(29,1.3f)})Pickup(p.x,p.y);
        }
        float ex=id==1?30.5f:id==2?34.5f:35;
        var exit=Zone("ExitTerminal",ZoneKind.Exit,ex,id==3?.5f:0,.8f,2,"Exit");exit.requiredA=a;exit.requiredB=b;
        Organize(id);
        var mapRoot=session.transform.root.gameObject;var map=mapRoot.AddComponent<LevelMap>();map.session=session;
        var spawn=new GameObject("SpawnPoint");spawn.transform.SetParent(mapRoot.transform,false);spawn.transform.position=session.spawn;map.spawnPoint=spawn.transform;
        AddScenery(mapRoot.transform);
        Directory.CreateDirectory("Assets/Prefabs/Levels");PrefabUtility.SaveAsPrefabAssetAndConnect(mapRoot,"Assets/Prefabs/Levels/Level0"+id+"_Map.prefab",InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/Level0"+id+".unity");
    }
    static void Organize(int id)
    {
        var objects=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var root=new GameObject("Level0"+id);var groups=new Dictionary<string,Transform>();
        foreach(string n in new[]{"Systems","Terrain","Background","Mechanisms","Pickups","Hazards","Actors","UI"}){var g=new GameObject(n);g.transform.SetParent(root.transform,false);groups[n]=g.transform;}
        foreach(var g in objects)
        {
            string category="Systems";
            if(g.GetComponent<PlayerMotor>()!=null)category="Actors";
            else if(g.GetComponent<WorldPickup>()!=null)category="Pickups";
            else if(g.GetComponent<WorldZone>()!=null) {var z=g.GetComponent<WorldZone>();category=z.kind==ZoneKind.Hazard||z.kind==ZoneKind.Void?"Hazards":"Mechanisms";}
            else if(g.GetComponent<WorldSignal>()!=null||g.GetComponent<GateController>()!=null||g.GetComponent<MovingPlatform>()!=null||g.GetComponent<PushableCrate>()!=null)category="Mechanisms";
            else if(g.GetComponent<BoxCollider2D>()!=null)category="Terrain";
            else if(g.GetComponent<WorldHud>()!=null)category="UI";
            else if(g.GetComponent<SpriteRenderer>()!=null)category="Background";
            g.transform.SetParent(groups[category],true);
        }
    }
    static void Backdrop()
    {
        var background=S("Backdrop");
        if(background!=null){var go=new GameObject("DistantFactory");var sr=go.AddComponent<SpriteRenderer>();sr.sprite=background;sr.sortingOrder=-30;sr.color=Color.white;go.transform.SetParent(Camera.main.transform,false);go.transform.localPosition=new Vector3(0,-5.625f,20);}
    }
    static RectTransform Rect(string n,Transform parent,float x,float y,float w,float h)
    {var g=new GameObject(n,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static WorldHud Hud()
    {
        var go=new GameObject("GameplayHUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;
        var composition=Rect("Composition",go.transform,0,0,640,360);composition.anchorMin=composition.anchorMax=composition.pivot=new Vector2(.5f,.5f);composition.anchoredPosition=Vector2.zero;
        var hud=go.AddComponent<WorldHud>();var glyph=new Sprite[15];string[] names={"Digit0","Digit1","Digit2","Digit3","Digit4","Digit5","Digit6","Digit7","Digit8","Digit9","Colon","Slash","Percent","Minus"};for(int i=0;i<14;i++)glyph[i]=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Modal/"+names[i]+".png");glyph[14]=U("Dot");
        var collect=ArtImage("CollectFrame",go.transform,"CollectFrame",6,6,120,35);
        ArtImage("Battery",go.transform,"Battery",20,11,8,24);
        ArtImage("Multiply",go.transform,"Multiply",41,20,7,7);
        hud.fragments=Rect("Fragments",go.transform,56,18,65,16).gameObject.AddComponent<SpriteNumberLabel>();hud.fragments.Configure(glyph,11,16,"00/08");
        hud.batteries=Rect("BatteryCount",go.transform,56,18,65,16).gameObject.AddComponent<SpriteNumberLabel>();hud.batteries.Configure(glyph,11,16,"00/02");hud.fragments.GetComponent<RectTransform>().anchoredPosition=new Vector2(153,-44);
        var crystalIcon=Rect("CrystalIcon",go.transform,130,43,13,16).gameObject.AddComponent<Image>();crystalIcon.sprite=S("Fragment0");crystalIcon.preserveAspect=true;crystalIcon.raycastTarget=false;
        hud.bag=Rect("BatteryBag",go.transform,44,43,26,12).gameObject.AddComponent<SpriteNumberLabel>();hud.bag.Configure(glyph,9,12,"00");
        ArtImage("BagLabel",go.transform,"BagLabel",8,43,30,12);
        var levelFrame=ArtImage("LevelFrame",go.transform,"LevelFrame",128,6,149,30);
        hud.level=Rect("Level",go.transform,143,16,23,14).gameObject.AddComponent<SpriteNumberLabel>();hud.level.Configure(glyph,10,14,"01");hud.level.SetTint(new Color32(232,117,43,255));
        hud.levelName=Rect("LevelName",go.transform,169,16,94,13).gameObject.AddComponent<Image>();hud.levelName.preserveAspect=true;hud.levelName.raycastTarget=false;
        hud.levelNames=new Sprite[3];for(int i=0;i<3;i++)hud.levelNames[i]=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/LevelSelect/LevelName"+(i+1)+".png");hud.levelName.sprite=hud.levelNames[session.level-1];
        ArtImage("TimeFrame",go.transform,"TimeFrame",467,6,104,30);ArtImage("Clock",go.transform,"Clock",480,14,16,16);
        hud.time=Rect("Time",go.transform,503,17,64,14).gameObject.AddComponent<SpriteNumberLabel>();hud.time.Configure(glyph,7.5f,13,"00:00.00");
        hud.settingsButton=ArtImage("Settings",go.transform,"SettingsButton",574,6,27,30).gameObject.AddComponent<Button>();hud.settingsButton.targetGraphic=hud.settingsButton.GetComponent<Image>();
        hud.pauseButton=ArtImage("Pause",go.transform,"PauseButton",605,6,27,30).gameObject.AddComponent<Button>();hud.pauseButton.targetGraphic=hud.pauseButton.GetComponent<Image>();
        // Keep the complete numeral-backed statistics, but the reference HUD has no death counter.
        hud.deaths=Rect("Deaths",go.transform,0,0,0,0).gameObject.AddComponent<SpriteNumberLabel>();hud.deaths.Configure(glyph,10,14,"0");hud.deaths.gameObject.SetActive(false);
        var bar=Rect("DashCooldown",go.transform,8,60,118,3).gameObject.AddComponent<Image>();bar.color=new Color32(43,226,220,255);bar.type=Image.Type.Filled;bar.fillMethod=Image.FillMethod.Horizontal;hud.dashBar=bar;
        var help=Rect("Controls",go.transform,8,335,283,21);help.gameObject.AddComponent<Image>().color=new Color(.04f,.06f,.08f,.9f);
        ArtImage("KeyA",help,"KeyA",4,3,15,15);ArtImage("KeyD",help,"KeyD",23,3,15,15);ArtImage("MoveHelp",help,"MoveLabel",43,4,30,12);
        ArtImage("KeySpace",help,"KeySpace",101,3,41,15);ArtImage("JumpHelp",help,"JumpLabel",149,4,30,12);
        ArtImage("KeyC",help,"KeyC",205,3,15,15);ArtImage("Interact",help,"InteractLabel",225,4,30,12);
        LabelSprite("DashHelp",go.transform,"DashHint",306,342,91,12);
        var children=new List<Transform>();foreach(Transform child in go.transform)if(child!=composition)children.Add(child);
        foreach(var child in children)child.SetParent(composition,false);
        SavePrefab(go,"GameplayHUD");return hud;
    }
    static SpriteNumberLabel Number(string n,Transform parent,float x,Sprite[] sprites,string value)
    {var r=Rect(n,parent,x,5,100,24);var label=r.gameObject.AddComponent<SpriteNumberLabel>();label.Configure(sprites,14,23,value);return label;}
    static void TextLabel(string n,Transform p,string text,float x,float y,float w,float h,int size)
    {var t=Rect(n,p,x,y,w,h).gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=text;t.fontSize=size;t.color=new Color(.85f,.86f,.8f);t.raycastTarget=false;}
    static void LabelSprite(string n,Transform p,string sprite,float x,float y,float w,float h)
    {var image=Rect(n,p,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Modal/"+sprite+".png");image.preserveAspect=true;image.raycastTarget=false;}
    static Sprite U(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Gameplay/"+n+".png");
    static RectTransform ArtImage(string n,Transform p,string sprite,float x,float y,float w,float h)
    {var r=Rect(n,p,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.sprite=U(sprite);image.raycastTarget=n=="Settings"||n=="Pause";return r;}
}
