using UnityEngine;

namespace Deepsea
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ExplorerController : MonoBehaviour
    {
        public GameDirector game;
        public Transform model,tool;
        public bool Blocked;
        CharacterController controller;
        float vertical;
        #if UNITY_EDITOR
        public bool AutomationInput;
        public Vector3 AutomationDirection;
        public Vector3 LastMove, LastContactNormal;
        public CollisionFlags LastFlags;
        #endif
        void Awake()
        {
            controller=GetComponent<CharacterController>();
            controller.height=1.6f;controller.radius=.25f;controller.center=new Vector3(0,.8f,0);
            controller.stepOffset=.32f;controller.slopeLimit=52;
            // Keep small frame steps at high frame rates instead of dropping
            // them below CharacterController's default movement threshold.
            controller.minMoveDistance=0;
        }
        void Update()
        {
            if(Blocked||game.Paused)return;
            Vector3 direction=new Vector3((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),0,
                (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0)).normalized;
            float speed=Input.GetKey(KeyCode.LeftShift)?5:3.2f;
            #if UNITY_EDITOR
            if(AutomationInput){direction=AutomationDirection;speed=4.2f;}
            #endif
            vertical=controller.isGrounded?-1:Mathf.Max(-18,vertical-18*Time.deltaTime);
            Vector3 movement=(direction*speed+Vector3.up*vertical)*Time.deltaTime;
            CollisionFlags flags=controller.Move(movement);
            #if UNITY_EDITOR
            LastMove=movement;LastFlags=flags;
            #endif
            if(direction.sqrMagnitude>.1f)model.rotation=Quaternion.Slerp(model.rotation,Quaternion.LookRotation(direction),Time.deltaTime*10);
            model.localPosition=new Vector3(0,Mathf.Abs(Mathf.Sin(Time.time*9))*direction.magnitude*.035f,0);
            if(transform.position.y<-1.5f)game.Expedition.ReturnToLanding();
        }
        public void Place(Vector3 position)
        {controller.enabled=false;transform.position=position;vertical=0;controller.enabled=true;}
        public void ShowTool(HandTool selected)
        {
            if(tool!=null)Destroy(tool.gameObject);
            Color c=selected==HandTool.Observe?VisualFactory.Lamp:selected==HandTool.CutNet?new Color(.72f,.75f,.69f):VisualFactory.Rope;
            tool=VisualFactory.Box("手持工具",model,new Vector3(.31f,.77f,.24f),new Vector3(.12f,.34f,.13f),c);
            tool.localRotation=Quaternion.Euler(25,0,-22);
        }
        #if UNITY_EDITOR
        void OnControllerColliderHit(ControllerColliderHit hit){LastContactNormal=hit.normal;}
        #endif
    }

}
