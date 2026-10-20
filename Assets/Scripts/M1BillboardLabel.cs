using UnityEngine;
public class M1BillboardLabel : MonoBehaviour
{
    Transform cam;
    void LateUpdate(){if(!cam&&Camera.main)cam=Camera.main.transform;if(cam)transform.rotation=Quaternion.LookRotation(transform.position-cam.position,Vector3.up);}
}
