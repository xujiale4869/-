using UnityEngine;

namespace Deepsea
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BoatController : MonoBehaviour
    {
        public GameDirector game;
        public Transform hull;
        public Transform lamp;
        public Transform stern;
        public float Fuel { get; private set; } = 1;
        public float Health { get; private set; } = 1;
        public float Speed { get { return body == null ? 0 : body.velocity.magnitude; } }
        public Vector3 Velocity { get { return body == null ? Vector3.zero : body.velocity; } }
        public Towable Tow { get; private set; }
        public bool LampOn { get; private set; } = true;
        public bool InputBlocked;
        public bool Moored { get; private set; }
        Rigidbody body;
        float throttle, rudder, bumpAt;
        Quaternion baseHullRotation;
        LineRenderer cable;
        // Test input remains inactive in normal standalone play.
        public bool AutomationInput;
        public float AutomationThrottle, AutomationRudder;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = 8;
            body.drag = .2f;
            body.angularDrag = 3;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        void Start()
        {
            baseHullRotation = hull.localRotation;
            cable = VisualFactory.Line("拖缆", transform, .035f, VisualFactory.Rope);
            cable.positionCount = 12;
            cable.enabled = false;
        }

        void Update()
        {
            throttle = InputBlocked ? 0 : (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            rudder = InputBlocked ? 0 : (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            if (AutomationInput && (Application.isEditor || GameDirector.VerificationMode)) { throttle = AutomationThrottle; rudder = AutomationRudder; }
            if (!InputBlocked && Input.GetKeyDown(KeyCode.L)) LampOn = !LampOn;
            if (!InputBlocked && Input.GetKeyDown(KeyCode.X) && Tow != null) ReleaseTow();
            Plane sea = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (lamp != null && sea.Raycast(game.Camera.ScreenPointToRay(Input.mousePosition), out distance))
            {
                Vector3 aim = game.Camera.ScreenPointToRay(Input.mousePosition).GetPoint(distance) - lamp.position;
                aim.y = 0;
                if (aim.sqrMagnitude > .1f) lamp.rotation = Quaternion.LookRotation(aim);
            }
            if (lamp != null) lamp.gameObject.SetActive(LampOn);
            float t = Time.time;
            Vector3 waterPosition=transform.position;
            float y=OceanSurface.Height(waterPosition,t)-OceanSurface.SeaLevel;
            float pitch=-Mathf.Atan2(OceanSurface.Height(waterPosition+transform.forward*1.5f,t)-OceanSurface.Height(waterPosition-transform.forward*1.5f,t),3)*Mathf.Rad2Deg;
            float roll=Mathf.Atan2(OceanSurface.Height(waterPosition+transform.right*.7f,t)-OceanSurface.Height(waterPosition-transform.right*.7f,t),1.4f)*Mathf.Rad2Deg;
            hull.localPosition=new Vector3(0,y,0);
            hull.localRotation=baseHullRotation*Quaternion.Euler(pitch,0,roll-rudder*Mathf.Min(Speed,4)*1.4f);
            if (Tow != null && cable != null)
            {
                cable.enabled = true;
                for (int i = 0; i < cable.positionCount; i++)
                {
                    float p = i / (float)(cable.positionCount - 1);
                    Vector3 v = Vector3.Lerp(stern.position, Tow.Bow.position, p);
                    v.y -= Mathf.Sin(p * Mathf.PI) * .18f;
                    cable.SetPosition(i, v);
                }
            }
            else if (cable != null) cable.enabled = false;
        }

        void FixedUpdate()
        {
            if (game == null || game.Paused || Moored) return;
            float load = 1f - .16f * (game.trip.CargoUsed+game.progress.shipParts) / Mathf.Max(1f, game.settings.cargoSlots);
            bool towAssist=game.progress.winch||game.progress.modules.Contains(ShipModule.TowAssist);
            float max = game.settings.topSpeed * load * Economy.TowSpeedMultiplier(towAssist, Tow != null);
            // Empty fuel still allows a slow return; failure never makes the save unrecoverable.
            if (Fuel <= 0) max = Mathf.Min(max, 1.3f);
            Vector3 fwd = transform.forward;
            float along = Vector3.Dot(body.velocity, fwd);
            float target = throttle >= 0 ? throttle * max : throttle * game.settings.reverseSpeed;
            float force = Mathf.Clamp((target - along) * 1.5f, -game.settings.acceleration, game.settings.acceleration);
            body.AddForce(fwd * force, ForceMode.Acceleration);
            Vector3 lateral = body.velocity - fwd * along;
            body.AddForce(-lateral * 2.7f, ForceMode.Acceleration);
            float steerFactor = Mathf.Clamp01(Mathf.Abs(along) / 2.3f) * (along < -.1f ? -1 : 1);
            float steering = game.settings.steering * (Tow == null ? 1 : game.progress.winch ? .82f : .62f);
            body.MoveRotation(body.rotation * Quaternion.Euler(0, rudder * steering * steerFactor * Time.fixedDeltaTime, 0));
            if (Mathf.Abs(throttle) > .05f && Fuel > 0)
                Fuel = Mathf.Max(0, Fuel - Time.fixedDeltaTime / game.settings.fuelSeconds * (.35f + .65f * Mathf.Abs(throttle))*(game.progress.technicianAssigned?.85f:1));
            if (body.position.sqrMagnitude > game.settings.seaLimit * game.settings.seaLimit)
                body.AddForce(-body.position.normalized * 1.5f, ForceMode.Acceleration);
        }

        public bool Attach(Towable target)
        {
            if (Tow != null || target == null || target.Attached) return false;
            Tow = target;
            target.Attach(this, game.progress.winch||game.progress.modules.Contains(ShipModule.TowAssist));
            return true;
        }

        public void ReleaseTow()
        {
            if (Tow == null) return;
            Tow.Detach();
            Tow = null;
            game.Notify("拖缆已解开。目标留在海上，可以回来接引。");
        }

        public Towable DeliverTow()
        {
            Towable delivered = Tow;
            if (delivered != null) delivered.Detach();
            Tow = null;
            return delivered;
        }

        public void Service() { Fuel = Health = 1; if(!body.isKinematic){body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero;} }
        public void Moor(Vector3 position,Quaternion rotation)
        {Service();Moored=true;body.isKinematic=true;body.position=position;body.rotation=rotation;transform.SetPositionAndRotation(position,rotation);}
        public void Unmoor(){Moored=false;body.isKinematic=false;}
        public void Refuel(float amount){Fuel=Mathf.Min(1,Fuel+amount);}
        public void Recover(Vector3 dock)
        {
            ReleaseTow();
            body.position = dock;
            body.rotation = Quaternion.identity;
            transform.SetPositionAndRotation(dock, Quaternion.identity);
            Service();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < 2 || Time.time - bumpAt < 1.5f) return;
            bumpAt = Time.time;
            Health = Mathf.Max(.15f, Health - collision.relativeVelocity.magnitude * .013f);
            if (game != null) game.Notify("碰到可见礁石，船体受损。返村后可免费基础维修。");
        }
    }
}

