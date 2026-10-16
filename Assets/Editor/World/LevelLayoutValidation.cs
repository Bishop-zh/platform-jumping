using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rustbound.World;

public static class LevelLayoutValidation
{
    static readonly List<string> results=new List<string>();
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);results.Add("PASS: "+name);}
    public static void Run()
    {
        var previous=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;PlayerMotor.ManualSimulation=true;
        try
        {
            for(int id=1;id<=3;id++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Levels/Level0"+id+"_Map.prefab"));
                Physics2D.SyncTransforms();foreach(var composite in root.GetComponentsInChildren<CompositeCollider2D>())composite.GenerateGeometry();
                var session=root.GetComponentInChildren<LevelSession>();
                Check(session.level==id&&session.player!=null,"Level "+id+" runtime bindings");
                Check(root.GetComponentsInChildren<WorldHud>(true).Length==0,"Level "+id+" independent HUD");
                Check(root.GetComponentsInChildren<ChargeStation>().Length==(id==2?2:1),"Level "+id+" reduced charger count");
                Check(root.GetComponentsInChildren<WorldZone>().Count(z=>z.kind==ZoneKind.Exit)==1,"Level "+id+" single exit");
                foreach(var signal in root.GetComponentsInChildren<WorldSignal>())
                    for(int i=0;i<signal.onChanged.GetPersistentEventCount();i++)Check(signal.onChanged.GetPersistentTarget(i)!=null,"Level "+id+" signal event target");
                if(id!=3)continue;
                Check(root.GetComponentInChildren<WorldCamera>().followVertical,"Tower camera follows vertical movement");
                var exit=root.GetComponentsInChildren<WorldZone>().Single(z=>z.kind==ZoneKind.Exit);
                Check(Mathf.Abs(exit.transform.position.y-19.5f)<.01f,"Summit exit at height 19.5");
                foreach(float height in new[]{0f,6f,12f,19.5f})Check(root.transform.Find("Terrain").Cast<Transform>().Any(t=>Mathf.Abs(t.GetComponent<Collider2D>().bounds.max.y-height)<.01f),"Tower floor "+height);
                var motor=session.player;motor.SendMessage("Awake");Physics2D.SyncTransforms();
                var firstDeck=root.transform.Find("Terrain/上行踏板01").GetComponent<CompositeCollider2D>();
                var child=firstDeck.transform.Find("SurfaceCollision");var original=child.localPosition;float center=firstDeck.bounds.center.x;
                child.localPosition+=Vector3.right*2;Physics2D.SyncTransforms();firstDeck.GenerateGeometry();
                Check(Mathf.Abs(firstDeck.bounds.center.x-center-2)<.01f,"Child module moves parent composite geometry");
                child.localPosition=original;Physics2D.SyncTransforms();firstDeck.GenerateGeometry();
                Check(firstDeck.usedByEffector&&firstDeck.GetComponent<PlatformEffector2D>().useOneWay,"Composite surface bound to platform effector");
                Jump(motor,new Vector2(2.4f,0),new Vector2(4,1.5f));
                Jump(motor,new Vector2(5.3f,1.5f),new Vector2(7.5f,3));
                Jump(motor,new Vector2(8.8f,3),new Vector2(11,4.5f));
                Jump(motor,new Vector2(12.0f,4.5f),new Vector2(14,6));
                Jump(motor,new Vector2(12.1f,12),new Vector2(11,13.5f));
                Jump(motor,new Vector2(9.7f,13.5f),new Vector2(7.5f,15));
                Jump(motor,new Vector2(6.2f,15),new Vector2(4,16.5f));
                Jump(motor,new Vector2(5.3f,16.5f),new Vector2(7,18));
                Jump(motor,new Vector2(6.7f,18),new Vector2(5.4f,19.5f));
                var lift=root.GetComponentInChildren<MovingPlatform>();lift.SendMessage("Awake");lift.power.batteryCost=0;lift.power.TryActivate();
                motor.Enabled=false;motor.Body.simulated=false;
                for(int frame=0;frame<225;frame++){lift.Tick(.02f);Physics2D.Simulate(.02f);}
                Check(lift.GetComponent<BoxCollider2D>().bounds.max.y>11.5f,"Powered lift physically reaches upper landing");
                motor.Body.simulated=true;motor.Enabled=true;
                Jump(motor,new Vector2(18.1f,12),new Vector2(15.5f,12));
                var gate=root.GetComponentInChildren<GateController>();gate.inputA.batteryCost=0;gate.inputA.TryActivate();gate.Tick(.4f);
                Check(gate.IsOpen&&!gate.GetComponent<BoxCollider2D>().enabled,"Summit power switch opens gate collider");
            }
            Directory.CreateDirectory("Artifacts/World");File.WriteAllLines("Artifacts/World/layout-validation.txt",results);Debug.Log("LEVEL_LAYOUT_VALIDATION_OK "+results.Count);
        }
        finally{Physics2D.simulationMode=previous;PlayerMotor.ManualSimulation=false;}
    }
    static void Jump(PlayerMotor motor,Vector2 start,Vector2 finish)
    {
        var body=motor.Body;var shape=motor.GetComponent<BoxCollider2D>();body.simulated=false;body.velocity=Vector2.zero;
        motor.transform.position=start;
        var foot=motor.transform.TransformPoint(shape.offset+Vector2.down*shape.size.y/2);
        motor.transform.position+=Vector3.up*(start.y-foot.y+.015f);body.simulated=true;Physics2D.SyncTransforms();
        motor.Enabled=true;motor.Feed(0,false,false);
        for(int i=0;i<25;i++){motor.Tick(.02f);Physics2D.Simulate(.02f);}
        motor.RefreshGrounded();Check(motor.Grounded,"Jump origin grounded "+start+" actual="+body.position);
        motor.Feed(0,true,false);motor.Tick(.02f);Check(body.velocity.y>5,"Jump impulse "+start);Physics2D.Simulate(.02f);
        bool landed=false;float peak=shape.bounds.min.y;
        for(int i=0;i<100;i++)
        {
            float axis=i<5||Mathf.Abs(body.position.x-finish.x)<.05f?0:Mathf.Sign(finish.x-body.position.x);
            motor.Feed(axis,false,false);motor.Tick(.02f);Physics2D.Simulate(.02f);motor.RefreshGrounded();
            peak=Mathf.Max(peak,shape.bounds.min.y);
            if(motor.Grounded&&Mathf.Abs(shape.bounds.min.y-finish.y)<.06f){landed=true;break;}
        }
        Check(landed,"Reachable jump "+start+" -> "+finish+" final="+body.position+" feet="+shape.bounds.min.y+" peak="+peak+" velocity="+body.velocity);
    }
}

