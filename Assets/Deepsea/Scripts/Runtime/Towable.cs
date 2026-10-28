using UnityEngine;

namespace Deepsea
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Towable : MonoBehaviour
    {
        public Transform Bow;
        public bool heavy;
        public bool isBoat;
        public bool Attached { get { return joint != null; } }
        Rigidbody body;
        SpringJoint joint;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = heavy ? 5 : 3;
            body.drag = 1.4f;
            body.angularDrag = 2;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void Attach(BoatController boat, bool improved)
        {
            joint = gameObject.AddComponent<SpringJoint>();
            joint.connectedBody = boat.GetComponent<Rigidbody>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = transform.InverseTransformPoint(Bow.position);
            joint.connectedAnchor = boat.transform.InverseTransformPoint(boat.stern.position);
            joint.minDistance = 4;
            joint.maxDistance = 5;
            joint.spring = improved ? 100 : 58;
            joint.damper = improved ? 30 : 18;
            joint.enableCollision = true;
            joint.tolerance = .15f;
        }

        public void Detach()
        {
            if (joint != null)
            {
                joint.spring = 0;
                joint.damper = 0;
                joint.connectedBody = null;
                Destroy(joint);
            }
            joint = null;
            body.velocity *= .2f;
        }
    }
}
