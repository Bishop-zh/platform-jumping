using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using Rustbound.World;

// Explicit asset migration; never runs automatically when the editor opens.
public static class LevelLayoutRevision
{
    const string Folder="Assets/Prefabs/Levels/";
    const string World="Assets/Prefabs/World/";
    static GameObject Load(int n)=>PrefabUtility.LoadPrefabContents(Folder+"Level0"+n+"_Map.prefab");
    static void Remove(UnityEngine.Object o){if(o!=null)UnityEngine.Object.DestroyImmediate(o);}
    public static void RunAndValidate(){Run();LevelLayoutValidation.Run();}
    public static void Run()
    {
        AssetDatabase.Refresh();
        var old1=Load(1);var old2=Load(2);var old3=Load(3);
        try
        {
            if(old1.transform.Find("旧第三关_推箱压板段")!=null)throw new Exception("关卡已整合，请直接编辑现有地图，不要重复执行迁移。");
            Merge(old1,old3,true,32);Merge(old2,old3,false,22);
            Configure(old1,1,46,1);Configure(old2,2,58.5f,2);
            // Fewer repeatable chargers, not an inventory of disposable batteries.
            KeepChargers(old1,new[]{4.5f});KeepChargers(old2,new[]{3f,24.7f});
            foreach(var signal in old2.GetComponentsInChildren<WorldSignal>(true))if(!signal.plate)signal.energyPerBattery=20;
            CreateExit(old1,new Vector2(45,0));
            BindTerrain(old1);BindTerrain(old2);
            Save(old1,1);Save(old2,2);
            BuildTower(old3);Save(old3,3);
            BindReusableModules();AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Artifacts/World");
            File.WriteAllText("Artifacts/World/layout-revision.txt","Level01: old 1 + old 3 crate/plate section, width 46, one charger.\nLevel02: old 2 + old 3 elevated/powered-gate section, width 58.5, two chargers.\nLevel03: floors 0 / 6 / 12 / 19.5, one charger, powered vertical lift and summit gate.\n");
            Debug.Log("LEVEL_LAYOUT_REVISION_OK");
        }
        finally{PrefabUtility.UnloadPrefabContents(old1);PrefabUtility.UnloadPrefabContents(old2);PrefabUtility.UnloadPrefabContents(old3);}
    }
    static void Save(GameObject root,int n)=>PrefabUtility.SaveAsPrefabAsset(root,Folder+"Level0"+n+"_Map.prefab");
    static void Merge(GameObject target,GameObject source,bool first,float dx)
    {
        foreach(var exit in target.GetComponentsInChildren<WorldZone>(true).Where(z=>z.kind==ZoneKind.Exit).ToArray())Remove(exit.gameObject);
        var part=UnityEngine.Object.Instantiate(source);part.name=first?"旧第三关_推箱压板段":"旧第三关_高台供电段";
        Remove(part.GetComponent<LevelMap>());
        foreach(Transform group in part.transform.Cast<Transform>().ToArray())
        {
            if(!new[]{"Terrain","Mechanisms","Pickups","Hazards","Background"}.Contains(group.name)){Remove(group.gameObject);continue;}
            foreach(Transform item in group.Cast<Transform>().ToArray())
            {
                var zone=item.GetComponent<WorldZone>();
                bool keep=(item.position.x<14)==first;
                if(item.name.Contains("Boundary")||(zone!=null&&zone.kind==ZoneKind.Void)||
                    (group.name=="Background"&&item.name!="SpikeArt")||(first&&zone!=null&&zone.kind==ZoneKind.Exit))keep=false;
                if(!keep)Remove(item.gameObject);
            }
        }
        part.transform.SetParent(target.transform,false);part.transform.localPosition=new Vector3(dx,0,0);
        foreach(var gate in part.GetComponentsInChildren<GateController>(true))
            if(gate.inputA==null&&gate.inputB!=null)gate.inputA=gate.inputB;
        // The second section starts half a unit higher than the preceding level.
        if(!first)Deck(target.transform.Find("Terrain"),36.25f,.25f,1,"合并连接踏步");
    }
    static void Configure(GameObject root,int level,float width,int chargers)
    {
        var session=root.GetComponentInChildren<LevelSession>(true);session.level=level;session.width=width;
        var camera=root.GetComponentInChildren<WorldCamera>(true);camera.width=width;
        foreach(var signal in root.GetComponentsInChildren<WorldSignal>(true))
            for(int i=signal.onChanged.GetPersistentEventCount()-1;i>=0;i--)
                if(signal.onChanged.GetPersistentTarget(i)==null)UnityEventTools.RemovePersistentListener(signal.onChanged,i);
        foreach(var box in root.GetComponentsInChildren<BoxCollider2D>(true))
        {
            if(box.name=="RightBoundary")box.transform.position=new Vector3(width+.5f,0);
            if(box.GetComponent<WorldZone>()?.kind==ZoneKind.Void){box.transform.position=new Vector3(width/2,-6);box.size=new Vector2(width+10,2);}
        }
    }
    static void KeepChargers(GameObject root,float[] positions)
    {
        var all=root.GetComponentsInChildren<ChargeStation>(true).ToList();
        foreach(float x in positions){var nearest=all.OrderBy(s=>Mathf.Abs(s.transform.position.x-x)).First();all.Remove(nearest);}
        foreach(var station in all)Remove(station.gameObject);
    }
    static GameObject Spawn(string name,Transform parent,Vector2 position)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(World+name+".prefab");if(prefab==null)throw new Exception("Missing prefab "+name);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.transform.position=position;return go;
    }
    static void CreateExit(GameObject root,Vector2 position)=>Spawn("Exit",root.transform.Find("Mechanisms"),position);
    static GameObject Deck(Transform parent,float x,float top,float width,string name)
    {
        // Art and collision live together, so editing this module moves both.
        var deck=new GameObject(name);deck.layer=8;deck.transform.SetParent(parent,false);deck.transform.position=new Vector3(x,top-.5f);
        var sr=deck.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Pixel/World/PlatformA.png");
        sr.drawMode=SpriteDrawMode.Tiled;sr.size=new Vector2(width,.5f);
        var surface=new GameObject("SurfaceCollision");surface.layer=8;surface.transform.SetParent(deck.transform,false);
        var collider=surface.AddComponent<BoxCollider2D>();collider.size=new Vector2(width,.25f);collider.offset=new Vector2(0,.375f);
        Composite(deck,new[]{collider},top>0&&top<19.5f);
        return deck;
    }
    static void Composite(GameObject root,BoxCollider2D[] shapes,bool oneWay)
    {
        var body=root.GetComponent<Rigidbody2D>();if(body==null){body=root.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Static;}
        var composite=root.GetComponent<CompositeCollider2D>();if(composite==null)composite=root.AddComponent<CompositeCollider2D>();composite.geometryType=CompositeCollider2D.GeometryType.Polygons;
        foreach(var shape in shapes)shape.usedByComposite=true;
        if(oneWay){var effector=root.GetComponent<PlatformEffector2D>();if(effector==null)effector=root.AddComponent<PlatformEffector2D>();effector.useOneWay=true;effector.useOneWayGrouping=true;effector.surfaceArc=160;composite.usedByEffector=true;}
    }
    static void BindReusableModules()
    {
        foreach(string path in Directory.GetFiles(World+"Modular","*.prefab"))
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(root.GetComponent<CompositeCollider2D>()!=null)continue;
                var original=root.GetComponents<BoxCollider2D>().Where(c=>!c.isTrigger).ToArray();if(original.Length==0)continue;
                var shapes=original.Select((source,index)=>
                {
                    var child=new GameObject("CollisionShape_"+index);child.layer=root.layer;child.transform.SetParent(root.transform,false);
                    var shape=child.AddComponent<BoxCollider2D>();shape.size=source.size;shape.offset=source.offset;shape.edgeRadius=source.edgeRadius;shape.sharedMaterial=source.sharedMaterial;return shape;
                }).ToArray();
                foreach(var source in original)Remove(source);
                Composite(root,shapes,false);PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
    static void BindTerrain(GameObject root)
    {
        foreach(var parentBox in root.GetComponentsInChildren<BoxCollider2D>(true).ToArray())
        {
            var parent=parentBox.gameObject;
            if(!parent.name.StartsWith("Ground_")&&parent.name!="RaisedPlatform")continue;
            var tiles=parent.transform.Cast<Transform>().Where(t=>t.name=="Surface"&&t.GetComponent<SpriteRenderer>()!=null).ToArray();if(tiles.Length==0)continue;
            bool thin=parentBox.size.y<=.251f;
            var shapes=tiles.Select(tile=>
            {
                var sr=tile.GetComponent<SpriteRenderer>();var bounds=sr.sprite.bounds;tile.gameObject.layer=8;
                var shape=tile.GetComponent<BoxCollider2D>();if(shape==null)shape=tile.gameObject.AddComponent<BoxCollider2D>();
                shape.size=new Vector2(bounds.size.x,thin?.25f:bounds.size.y);
                shape.offset=new Vector2(bounds.center.x,thin?bounds.max.y-.125f:bounds.center.y);return shape;
            }).ToArray();
            Remove(parentBox);Composite(parent,shapes,thin);
        }
    }
    static void BuildTower(GameObject root)
    {
        foreach(string groupName in new[]{"Terrain","Mechanisms","Pickups","Hazards"})
            foreach(Transform child in root.transform.Find(groupName).Cast<Transform>().ToArray())Remove(child.gameObject);
        var terrain=root.transform.Find("Terrain");var mechanisms=root.transform.Find("Mechanisms");var pickups=root.transform.Find("Pickups");
        var session=root.GetComponentInChildren<LevelSession>(true);session.level=3;session.width=22;session.spawn=new Vector2(2,0);
        session.player.transform.position=session.spawn;
        var map=root.GetComponent<LevelMap>();map.spawnPoint.position=session.spawn;map.useSpawnPoint=true;
        var camera=root.GetComponentInChildren<WorldCamera>(true);camera.width=22;camera.followVertical=true;
        Deck(terrain,11,0,22,"第0层_出发地面");
        Deck(terrain,4,1.5f,3,"上行踏板01");Deck(terrain,7.5f,3,3,"上行踏板02");Deck(terrain,11,4.5f,3,"上行踏板03");
        Deck(terrain,16,6,7,"第1层_升降机站");
        Deck(terrain,16,12,8,"第2层_高空中转站");
        // The elevator shaft is open; its upper landing is to the left.
        var upper=terrain.Find("第2层_高空中转站");upper.GetComponent<SpriteRenderer>().size=new Vector2(5,.5f);
        upper.GetComponentInChildren<BoxCollider2D>().size=new Vector2(5,.25f);upper.position=new Vector3(14,11.5f);
        Deck(terrain,11,13.5f,3,"上行踏板04");Deck(terrain,7.5f,15,3,"上行踏板05");Deck(terrain,4,16.5f,3,"上行踏板06");
        Deck(terrain,7,18,3,"上行踏板07");Deck(terrain,3,19.5f,6,"第3层_顶部通关站");
        Spawn("ChargeStation",mechanisms,new Vector2(2,0));
        Spawn("Checkpoint",mechanisms,new Vector2(14,6));Spawn("Checkpoint",mechanisms,new Vector2(14,12));
        var switchGo=Spawn("Lever",mechanisms,new Vector2(15,6));var signal=switchGo.GetComponent<WorldSignal>();signal.batteryCost=1;signal.energyPerBattery=20;signal.toggle=false;
        var lift=Spawn("MovingPlatform",mechanisms,new Vector2(19,5.5f));var mover=lift.GetComponent<MovingPlatform>();mover.travelOffset=new Vector2(0,6);mover.power=signal;
        UnityEventTools.AddPersistentListener(signal.onChanged,mover.SetPowered);
        var summitSignal=Spawn("Lever",mechanisms,new Vector2(5.3f,19.5f)).GetComponent<WorldSignal>();summitSignal.batteryCost=1;summitSignal.energyPerBattery=40;summitSignal.toggle=false;
        var gate=Spawn("PoweredGate",mechanisms,new Vector2(3.8f,19.5f)).GetComponent<GateController>();gate.inputA=summitSignal;gate.inputB=null;
        UnityEventTools.AddPersistentListener(summitSignal.onChanged,gate.SetPowered);
        CreateExit(root,new Vector2(1,19.5f));
        foreach(var point in new[]{new Vector2(4,2.1f),new Vector2(7.5f,3.6f),new Vector2(11,5.1f),new Vector2(16,6.7f),new Vector2(19,9),new Vector2(14,12.7f),new Vector2(11,14.1f),new Vector2(7.5f,15.6f),new Vector2(4,17.1f),new Vector2(1.5f,20.2f)})Spawn("Fragment",pickups,point);
        var voidGo=new GameObject("Void");voidGo.transform.SetParent(root.transform.Find("Hazards"),false);voidGo.transform.position=new Vector3(11,-6);voidGo.layer=12;
        var voidCollider=voidGo.AddComponent<BoxCollider2D>();voidCollider.isTrigger=true;voidCollider.size=new Vector2(32,2);voidGo.AddComponent<WorldZone>().kind=ZoneKind.Void;
        foreach(float x in new[]{-.5f,22.5f}){var boundary=new GameObject("TowerBoundary");boundary.layer=8;boundary.transform.SetParent(terrain,false);boundary.transform.position=new Vector3(x,10);boundary.AddComponent<BoxCollider2D>().size=new Vector2(1,32);}
    }
}
