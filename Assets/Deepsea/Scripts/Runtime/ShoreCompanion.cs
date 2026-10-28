using UnityEngine;

namespace Deepsea
{
    public sealed class ShoreCompanion : MonoBehaviour
    {
        public ExpeditionController expedition;
        public Transform model;
        public bool Following,Boarded;
        public string State { get { return Boarded?"已在工作艇上":Following?"正在同行":"东岸避风处等待"; } }
        public readonly string PersonId="linzhou";
        CharacterController controller;
        float vertical;
        void Awake()
        {controller=gameObject.AddComponent<CharacterController>();controller.height=1.45f;controller.radius=.24f;controller.center=new Vector3(0,.73f,0);controller.stepOffset=.32f;controller.slopeLimit=52;controller.minMoveDistance=0;}
        void Update()
        {
            if(!Following||Boarded||expedition.game.Paused||!expedition.OnFoot)return;
            Vector3 delta=expedition.Explorer.transform.position-transform.position;delta.y=0;
            float distance=delta.magnitude;
            // Follow a collision checked breadcrumb trail rather than cutting
            // through roofs, the net shortcut or a cliff when the player detours.
            Vector3 waypoint=expedition.CompanionWaypoint(transform.position);
            Vector3 to=waypoint-transform.position;to.y=0;
            Vector3 direction=distance>1.5f?to.normalized:Vector3.zero;
            vertical=controller.isGrounded?-1:Mathf.Max(-15,vertical-18*Time.deltaTime);
            controller.Move((direction*3.45f+Vector3.up*vertical)*Time.deltaTime);
            if(direction.sqrMagnitude>.1f)model.rotation=Quaternion.Slerp(model.rotation,Quaternion.LookRotation(direction),Time.deltaTime*6);
        }
        public void ResetAt(Vector3 position)
        {controller.enabled=false;transform.position=position;controller.enabled=true;Following=Boarded=false;gameObject.SetActive(true);}
        public void Board(){Boarded=true;Following=false;gameObject.SetActive(false);}
    }
}
