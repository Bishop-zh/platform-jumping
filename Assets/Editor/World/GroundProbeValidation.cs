using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rustbound.World;

public static class GroundProbeValidation
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var previous=Physics2D.simulationMode;
        Physics2D.simulationMode=SimulationMode2D.Script;
        PlayerMotor.ManualSimulation=true;
        int checks=0;
        try
        {
            foreach(float scale in new[]{.65f,1f,1.6f})
            foreach(int layer in new[]{0,8,6})
            {
                var floor=new GameObject("EditedPlatform");floor.layer=layer;
                floor.transform.position=new Vector3(0,-.25f);
                var ground=floor.AddComponent<BoxCollider2D>();ground.size=new Vector2(10,.5f);
                var player=new GameObject("ResizedPlayer");player.transform.localScale=Vector3.one*scale;
                var shape=player.AddComponent<BoxCollider2D>();shape.size=new Vector2(.8f,1.8f);shape.offset=new Vector2(.12f,.9f);
                var body=player.AddComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeRotation;
                var motor=player.AddComponent<PlayerMotor>();motor.SendMessage("Awake");body.position=new Vector2(0,.3f);
                Physics2D.SyncTransforms();
                for(int i=0;i<60;i++){motor.Tick(.02f);Physics2D.Simulate(.02f);}
                motor.RefreshGrounded();Require(motor.Grounded,"Landing scale "+scale+" layer "+layer);checks++;
                motor.Feed(0,true,false);motor.Tick(.02f);
                Require(body.velocity.y>5&&!motor.Grounded,"Jump from edited collider");checks++;
                Physics2D.Simulate(.02f);motor.RefreshGrounded();
                Require(!motor.Grounded,"Ascending must stay airborne");checks++;
                UnityEngine.Object.DestroyImmediate(player);UnityEngine.Object.DestroyImmediate(floor);
            }
            var p=new GameObject("AirbornePlayer");var motorAir=p.AddComponent<PlayerMotor>();motorAir.SendMessage("Awake");
            p.transform.position=new Vector3(0,3);Physics2D.SyncTransforms();motorAir.RefreshGrounded();
            Require(!motorAir.Grounded,"No ground means airborne");checks++;
            UnityEngine.Object.DestroyImmediate(p);
            Directory.CreateDirectory("Artifacts/World");File.WriteAllText("Artifacts/World/ground-probe-validation.txt",checks+" checks passed: edited collider sizes, scale, default/world/custom layers, jumping and airborne detection.");
            Debug.Log("GROUND_PROBE_VALIDATION_OK "+checks);
        }
        finally{Physics2D.simulationMode=previous;PlayerMotor.ManualSimulation=false;}
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}
