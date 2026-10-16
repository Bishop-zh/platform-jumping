using UnityEngine;
namespace Rustbound.World
{
    [DefaultExecutionOrder(-400)]
    public sealed class PlayerRigBootstrap : MonoBehaviour
    {
        public Transform spawnOverride;
        void Awake()
        {
            var motor=GetComponent<PlayerMotor>();var chosen=motor;
            foreach(var candidate in FindObjectsOfType<PlayerMotor>())if(candidate.GetComponentInParent<LevelMap>()==null){chosen=candidate;break;}
            if(chosen!=motor){gameObject.SetActive(false);return;}
            foreach(var other in FindObjectsOfType<PlayerMotor>())if(other!=motor)other.gameObject.SetActive(false);
            if(spawnOverride!=null)transform.position=spawnOverride.position;
            var session=FindObjectOfType<LevelSession>();
            if(session==null)
            {
                var go=new GameObject("StandaloneLevelSession");go.SetActive(false);session=go.AddComponent<LevelSession>();session.player=motor;session.spawn=transform.position;session.standalone=true;go.SetActive(true);
            }
            else {session.player=motor;session.spawn=transform.position;}
            var camera=FindObjectOfType<WorldCamera>();
            if(camera==null)
            {
                var cam=Camera.main;if(cam==null){cam=new GameObject("WorldCamera").AddComponent<Camera>();cam.tag="MainCamera";}
                cam.orthographic=true;cam.orthographicSize=5.625f;cam.backgroundColor=new Color32(192,179,140,255);camera=cam.gameObject.AddComponent<WorldCamera>();camera.bounded=false;camera.followVertical=true;
            }
            camera.target=motor;if(transform.position.x<0||transform.position.x>camera.width)camera.bounded=false;
        }
    }
}
