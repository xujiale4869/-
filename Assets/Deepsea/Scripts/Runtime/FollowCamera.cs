using UnityEngine;

namespace Deepsea
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public BaySettings settings;
        public bool Overview;
        public bool Walking;
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
            Vector3 wanted = Overview ? new Vector3(0, 0, 34) : target.position + (Walking?Vector3.zero:target.forward*2);
            follow = Vector3.SmoothDamp(follow, wanted, ref velocity, .28f, 100, Mathf.Max(.0001f,Time.unscaledDeltaTime));
            float size = Overview ? 101 : zoom;
            cameraComponent.orthographicSize = Mathf.Lerp(cameraComponent.orthographicSize, size, 1 - Mathf.Exp(-4 * Time.unscaledDeltaTime));
            Quaternion rotation = Quaternion.Euler(settings.cameraPitch, 0, 0);
            transform.SetPositionAndRotation(follow - rotation * Vector3.forward * 160, rotation);
        }
        public void SetWalking(Transform focus,bool walking){target=focus;Walking=walking;Overview=false;zoom=walking?13:settings.cameraSize;}
    }
}
