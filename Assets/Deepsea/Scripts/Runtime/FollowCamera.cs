using UnityEngine;

namespace Deepsea
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public BaySettings settings;
        public bool Overview;
        public bool Walking;
        public bool Scoping;
        Camera cameraComponent;
        Vector3 follow, velocity;
        float zoom;

        void Awake() { cameraComponent = GetComponent<Camera>(); }
        void Start() { follow = target.position; zoom = settings.cameraSize; }
        void LateUpdate()
        {
            if (target == null) return;
            if (!Overview && Time.timeScale > 0)
                zoom = Mathf.Clamp(zoom - Input.mouseScrollDelta.y * 2, Walking?8:15, Walking?20:40);
            if(Scoping&&(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.D)))Scoping=false;
            Vector3 wanted = Overview ? new Vector3(0, 0, 34) : target.position + (Walking?Vector3.zero:target.forward*2);
            if(Scoping&&!Overview){Vector2 aim=new Vector2(Input.mousePosition.x/Screen.width-.5f,Input.mousePosition.y/Screen.height-.5f);wanted=target.position+new Vector3(aim.x*90,0,aim.y*90);}
            follow = Vector3.SmoothDamp(follow, wanted, ref velocity, .28f, 100, Mathf.Max(.0001f,Time.unscaledDeltaTime));
            float size = Overview ? 101 : Scoping?5:zoom;
            cameraComponent.orthographicSize = Mathf.Lerp(cameraComponent.orthographicSize, size, 1 - Mathf.Exp(-4 * Time.unscaledDeltaTime));
            Quaternion rotation = CoastSlice.Requested?Quaternion.Euler(40,-12,0):Quaternion.Euler(settings.cameraPitch, 0, 0);
            transform.SetPositionAndRotation(follow - rotation * Vector3.forward * (CoastSlice.Requested?90:160), rotation);
        }
        public void SetWalking(Transform focus,bool walking){target=focus;Walking=walking;Overview=false;zoom=walking?13:settings.cameraSize;}
    }
}
