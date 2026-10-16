using UnityEngine;
namespace Rustbound.World
{
    [DefaultExecutionOrder(-300)]
    public sealed class LevelMap : MonoBehaviour
    {
        public LevelSession session;
        public Transform spawnPoint;
        public bool useSpawnPoint;
        void Awake()
        {
            if(session!=null){var camera=GetComponentInChildren<WorldCamera>();if(camera!=null)camera.width=session.width;}
            if(session!=null && spawnPoint!=null && useSpawnPoint)
            {session.spawn=spawnPoint.position;if(session.player!=null)session.player.transform.position=spawnPoint.position;}
            else if(session!=null&&session.player!=null)session.spawn=session.player.transform.position;
        }
        void OnDrawGizmos(){if(spawnPoint!=null){Gizmos.color=Color.green;Gizmos.DrawWireSphere(spawnPoint.position,.3f);}}
    }
}
