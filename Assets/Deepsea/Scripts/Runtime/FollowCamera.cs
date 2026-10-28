using UnityEngine;

namespace Deepsea
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public BaySettings settings;
        public bool Overview;
        Camera cameraComponent;
        Vector3 follow, velocity;
        float zoom;

        void Awake() { cameraComponent = GetComponent<Camera>(); }
        void Start() { follow = target.position; zoom = settings.cameraSize; }
        void LateUpdate()
        {
            if (target == null) return;
            if (!Overview && Time.timeScale > 0)
                zoom = Mathf.Clamp(zoom - Input.mouseScrollDelta.y * 2, 15, 40);
            Vector3 wanted = Overview ? new Vector3(0, 0, 34) : target.position + target.forward * 2;
            follow = Vector3.SmoothDamp(follow, wanted, ref velocity, .28f, 100, Time.unscaledDeltaTime);
            float size = Overview ? 101 : zoom;
            cameraComponent.orthographicSize = Mathf.Lerp(cameraComponent.orthographicSize, size, 1 - Mathf.Exp(-4 * Time.unscaledDeltaTime));
            Quaternion rotation = Quaternion.Euler(settings.cameraPitch, 0, 0);
            transform.SetPositionAndRotation(follow - rotation * Vector3.forward * 160, rotation);
        }
    }
}
