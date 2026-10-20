using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class M1PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float acceleration = 24f;
    public float braking = 34f;
    public float jumpHeight = 1.4f;
    public float dodgeSpeed = 10f;
    public float dodgeDuration = 0.22f;
    public Transform cameraTransform;
    CharacterController controller;
    Vector3 velocity, planarVelocity, dodgeDirection;
    float dodgeTimer, spiritCooldown;

    void Awake(){controller=GetComponent<CharacterController>();controller.minMoveDistance=0f;controller.stepOffset=.28f;}
    void Update()
    {
        var gm=M1GameManager.Instance;if(gm&&!gm.IsGameplayActive)return;
        float dt=Time.deltaTime;spiritCooldown-=dt;
        Vector3 forward=cameraTransform?cameraTransform.forward:Vector3.forward;
        Vector3 right=cameraTransform?cameraTransform.right:Vector3.right;
        forward.y=0;right.y=0;forward.Normalize();right.Normalize();
        float vertical=Input.GetAxisRaw("Vertical"),horizontal=Input.GetAxisRaw("Horizontal");
        Vector3 input=forward*vertical+right*horizontal;if(input.sqrMagnitude>1f)input.Normalize();
        // 按住 Shift 任意方向移动都能加速（原来只能向前）
        bool sprinting=Input.GetKey(KeyCode.LeftShift)&&input.sqrMagnitude>.01f&&!M1ChargeWeaponSystem.IsCharging;
        float speed=(sprinting?sprintSpeed:moveSpeed)*(M1ChargeWeaponSystem.IsCharging?.6f:1f); // 蓄力时减速走位
        Vector3 wanted=input*speed;
        planarVelocity=Vector3.MoveTowards(planarVelocity,wanted,(input.sqrMagnitude>.001f?acceleration:braking)*dt);
        if(dodgeTimer<=0f&&Input.GetKeyDown(KeyCode.LeftAlt)&&input.sqrMagnitude>.1f){dodgeDirection=input;dodgeTimer=dodgeDuration;var ca=GetComponent<M1CharacterAnimator>();if(ca)ca.Roll();}
        if(dodgeTimer>0f){planarVelocity=dodgeDirection*dodgeSpeed;dodgeTimer=Mathf.Max(0f,dodgeTimer-dt);}
        if(Input.GetButtonDown("Jump")&&controller.isGrounded)velocity.y=Mathf.Sqrt(jumpHeight*36f);
        velocity.y+=-18f*dt;
        controller.Move((planarVelocity+Vector3.up*velocity.y)*dt);
        if(controller.isGrounded&&velocity.y<0f)velocity.y=-2f;
        // 蓄力瞄准时面朝准星落点（参考帕鲁/宝可梦的瞄准姿态），否则面朝移动方向
        Vector3 face=input;
        if(M1ChargeWeaponSystem.IsCharging){Vector3 toAim=M1ChargeWeaponSystem.AimPoint-transform.position;toAim.y=0;if(toAim.sqrMagnitude>.25f)face=toAim.normalized;}
        if(face.sqrMagnitude>.03f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(face),1f-Mathf.Exp(-16f*dt));
        if(Input.GetKeyDown(KeyCode.Q)&&spiritCooldown<=0f){spiritCooldown=2f;gm?.CastSpiritMark(transform.position+transform.forward*1.2f);}
        if(Input.GetKeyDown(KeyCode.E))gm?.TryTame(transform.position);
    }
    void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(transform.position+Vector3.up+transform.forward*1.1f,1.15f);}
}