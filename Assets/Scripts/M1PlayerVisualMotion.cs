using UnityEngine;

public class M1PlayerVisualMotion : MonoBehaviour
{
    public Transform rig, leftArm, rightArm, leftLeg, rightLeg;
    CharacterController controller;
    Vector3 rigStart;
    void Awake(){ controller=GetComponent<CharacterController>(); if(rig) rigStart=rig.localPosition; }
    void Update()
    {
        if(!rig) return;
        float speed=controller?new Vector3(controller.velocity.x,0,controller.velocity.z).magnitude:0f;
        float gait=Mathf.Clamp01(speed/3.2f);
        float t=Time.time;
        rig.localPosition=rigStart+Vector3.up*(Mathf.Sin(t*(gait>0.1f?11f:2.1f))*(gait>0.1f?.045f:.018f));
        float swing=Mathf.Sin(t*10f)*.52f*gait;
        if(leftArm) leftArm.localRotation=Quaternion.Euler(swing*48f,0,8f);
        if(rightArm) rightArm.localRotation=Quaternion.Euler(-swing*48f,0,-8f);
        if(leftLeg) leftLeg.localRotation=Quaternion.Euler(-swing*52f,0,0);
        if(rightLeg) rightLeg.localRotation=Quaternion.Euler(swing*52f,0,0);
    }
}
