using UnityEngine;
namespace Rustbound.World
{
    [DefaultExecutionOrder(100)]
    public sealed class SceneryParallax : MonoBehaviour
    {
        [Range(0,1)] public float scrollRatio=.25f;
        // 1 keeps the layer at its authored height; 0 follows the camera fully.
        [Range(0,1)] public float verticalScrollRatio=1f;
        Vector3 origin,cameraOrigin;
        Transform cameraTransform;
        void Start(){origin=transform.position;if(Camera.main!=null){cameraTransform=Camera.main.transform;cameraOrigin=cameraTransform.position;}}
        void LateUpdate()
        {
            if(cameraTransform==null)return;
            var position=origin+(cameraTransform.position-cameraOrigin)*(1-scrollRatio);
            position.y=origin.y+(cameraTransform.position.y-cameraOrigin.y)*(1-verticalScrollRatio);
            position.z=origin.z;position.x=Mathf.Round(position.x*32)/32;
            position.y=Mathf.Round(position.y*32)/32;transform.position=position;
        }
    }
}
