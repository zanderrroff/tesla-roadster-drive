using UnityEngine;
namespace MotorCoast {
[RequireComponent(typeof(CharacterController))]
public sealed class OnFootController:MonoBehaviour {
    public const int PlayerLayer=9;
    public MotorGame Game;public CharacterVisual Visual {get;private set;}
    public bool Crouched {get;private set;}public bool Prone {get;private set;}public bool Climbing {get;private set;}
    public bool Grounded=>capsule&&capsule.isGrounded;
    public float HorizontalSpeed=>new Vector2(velocity.x,velocity.z).magnitude;
    CharacterController capsule;Vector3 velocity,climbStart,climbEnd;float climbTime;bool jumpQueued;
    const float Standing=1.76f,Radius=.27f;
    public void Configure(MotorGame game,GameObject prefab){
        Game=game;capsule=GetComponent<CharacterController>();capsule.height=Standing;capsule.radius=Radius;capsule.center=Vector3.up*Standing/2;capsule.stepOffset=.30f;capsule.slopeLimit=45;capsule.skinWidth=.025f;capsule.minMoveDistance=0;
        gameObject.layer=PlayerLayer;ReplaceVisual(prefab);
    }
    public void ReplaceVisual(GameObject prefab){if(Visual){Visual.gameObject.SetActive(false);Destroy(Visual.gameObject);}Visual=Instantiate(prefab,transform,false).GetComponent<CharacterVisual>();Nodes.Layer(Visual.gameObject,PlayerLayer);Visual.Initialize();}
    public void Teleport(Vector3 position,Quaternion rotation){capsule.enabled=false;transform.SetPositionAndRotation(position,rotation);capsule.enabled=true;velocity=Vector3.zero;Climbing=false;jumpQueued=false;}
    public void BeginSeatTransition(){capsule.enabled=false;Crouched=Prone=Climbing=false;Visual.Emote=null;Visual.Crouching=Visual.Crawling=Visual.Airborne=Visual.Climbing=false;velocity=Vector3.zero;}
    public void EndSeatTransition(Vector3 feet,Quaternion rotation){Visual.EndTransition();Teleport(feet,rotation);}
    public void Jump(){jumpQueued=true;}
    public void CancelInput(){jumpQueued=false;}
    public void ToggleCrouch(){Crouched=!Crouched;Prone=false;}
    public void ToggleProne(){Prone=!Prone;Crouched=Prone;}
    public void Emote(string value){if(Grounded&&HorizontalSpeed<.2f)Visual.Emote=value;}
    public static bool HasRoom(Vector3 feet,float height,int mask)=>!Physics.CheckCapsule(feet+Vector3.up*(Radius+.04f),feet+Vector3.up*(height-Radius),Radius*.94f,mask,QueryTriggerInteraction.Ignore);
    public static bool FindExit(VehicleController car,out Vector3 feet){
        // Test both doors, then the rear. The ray establishes the actual ramp or
        // terrain height; the capsule rejects walls and ceilings before exit.
        float side=car.Spec.driverPosition.x<0?-1:1;
        foreach(var offset in new[]{new Vector3(side*(car.Spec.colliderSize.x/2+.65f),0,car.Spec.driverPosition.z),new Vector3(-side*(car.Spec.colliderSize.x/2+.65f),0,car.Spec.driverPosition.z),new Vector3(0,0,-car.Spec.colliderSize.z/2-.8f)}){
            var p=car.transform.TransformPoint(offset);
            if(Physics.Raycast(p+Vector3.up*1.4f,Vector3.down,out var hit,3.4f,~((1<<PlayerLayer)|(1<<VehicleController.VehicleLayer)),QueryTriggerInteraction.Ignore)&&hit.normal.y>.65f){
                feet=hit.point+Vector3.up*.04f;if(HasRoom(feet,Standing,~(1<<PlayerLayer)))return true;
            }
        }
        feet=default;return false;
    }
    void Update(){
        if(!Game||Game.IsPaused||Game.Input.Blocked||!Game.OnFoot)return;
        if(UnityEngine.Input.GetKeyDown(KeyCode.Space))Jump();
        if(UnityEngine.Input.GetKeyDown(KeyCode.LeftControl)||UnityEngine.Input.GetKeyDown(KeyCode.RightControl))ToggleCrouch();
        if(UnityEngine.Input.GetKeyDown(KeyCode.X))ToggleProne();
        if(Climbing){climbTime+=Time.deltaTime/ .7f;float t=Mathf.Clamp01(climbTime);Vector3 p=t<.5f?Vector3.Lerp(climbStart,new Vector3(climbStart.x,climbEnd.y+.12f,climbStart.z),Mathf.SmoothStep(0,1,t*2)):Vector3.Lerp(new Vector3(climbStart.x,climbEnd.y+.12f,climbStart.z),climbEnd,Mathf.SmoothStep(0,1,(t-.5f)*2));capsule.Move(p-transform.position);if(t>=1){Climbing=false;velocity=Vector3.zero;}Visual.Climbing=Climbing;return;}
        float dt=Mathf.Min(Time.deltaTime,.05f),wanted=Prone?.62f:Crouched?1.10f:Standing;
        if(wanted>capsule.height&&!HasRoom(transform.position,wanted,~(1<<PlayerLayer))){Crouched=true;Prone=!HasRoom(transform.position,1.1f,~(1<<PlayerLayer));wanted=Prone?.62f:1.1f;}
        capsule.height=Mathf.MoveTowards(capsule.height,wanted,dt*4);capsule.center=Vector3.up*capsule.height/2;capsule.stepOffset=Mathf.Min(.3f,capsule.height*.35f);
        var move=Game.Input.FootMove();var forward=Vector3.ProjectOnPlane(Game.Camera.transform.forward,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,forward);
        var direction=Vector3.ClampMagnitude(forward*move.y+right*move.x,1);bool run=Game.Input.TouchRun||UnityEngine.Input.GetKey(KeyCode.LeftShift)||UnityEngine.Input.GetKey(KeyCode.RightShift);
        float speed=Prone?.72f:Crouched?1.2f:run?5.3f:2.2f;
        var flat=Vector3.MoveTowards(new Vector3(velocity.x,0,velocity.z),direction*speed,(Grounded?16:5)*dt);velocity.x=flat.x;velocity.z=flat.z;
        if(direction.sqrMagnitude>.01f){Visual.Emote=null;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-12*dt));}
        if(Grounded&&velocity.y<0)velocity.y=-2;
        if(jumpQueued){jumpQueued=false;if(!Prone&&Grounded){if(TryClimb())return;if(!Crouched)velocity.y=Mathf.Sqrt(2*23*1.15f);}}
        velocity.y=Mathf.Max(-30,velocity.y-23*dt);var movement=velocity*dt;
        // A crawling body extends beyond the upright CharacterController's
        // footprint. Sweep that extra length so feet and head do not pass walls.
        var horizontal=new Vector3(movement.x,0,movement.z);
        if(Prone&&horizontal.sqrMagnitude>.000001f){var centre=transform.position+Vector3.up*.31f;if(Physics.CapsuleCast(centre-transform.forward*.65f,centre+transform.forward*.40f,.25f,horizontal.normalized,out var obstruction,horizontal.magnitude+.025f,~(1<<PlayerLayer),QueryTriggerInteraction.Ignore)){horizontal=horizontal.normalized*Mathf.Max(0,obstruction.distance-.025f);movement.x=horizontal.x;movement.z=horizontal.z;}}
        var flags=capsule.Move(movement);if((flags&CollisionFlags.Above)!=0&&velocity.y>0)velocity.y=0;
        Visual.Speed=HorizontalSpeed;Visual.Crouching=Crouched;Visual.Crawling=Prone;Visual.Airborne=!Grounded;Visual.Climbing=false;Visual.Animations=Game.AnimationsEnabled;
        if(transform.position.y< -20)Game.RecoverPerson();
    }
    bool TryClimb(){
        if(!Physics.SphereCast(transform.position+Vector3.up*.70f,.18f,transform.forward,out var front,.85f,~(1<<PlayerLayer),QueryTriggerInteraction.Ignore))return false;
        var ahead=front.point+transform.forward*.50f;
        if(!Physics.Raycast(new Vector3(ahead.x,transform.position.y+1.85f,ahead.z),Vector3.down,out var top,1.65f,~(1<<PlayerLayer),QueryTriggerInteraction.Ignore)||top.normal.y<.8f)return false;
        float rise=top.point.y-transform.position.y;if(rise<.35f||rise>1.5f)return false;
        var end=top.point+Vector3.up*.04f;if(!HasRoom(end,Standing,~(1<<PlayerLayer)))return false;
        climbStart=transform.position;climbEnd=end;climbTime=0;Climbing=true;Visual.Climbing=true;Visual.Emote=null;return true;
    }
    void OnDisable(){jumpQueued=false;velocity=Vector3.zero;if(Game!=null)Game.Input.Clear();}
}
}
