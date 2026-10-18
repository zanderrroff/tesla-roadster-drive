using UnityEngine;
namespace MotorCoast {
public sealed class VehicleDoor:MonoBehaviour {
    Transform panel;Quaternion closed;float angle,target,sign;Vector3 axis=Vector3.up;
    public bool Available=>panel;
    public void Configure(VehicleController car){panel=Nodes.Find(car.transform,"Door_Driver");if(panel){closed=panel.localRotation;sign=car.Spec.driverPosition.x<0?1:-1;if(car.Spec.id.Contains("300sl-gullwing")){axis=Vector3.forward;sign=-sign;}else if(car.Spec.id.Contains("aventador")||car.Spec.id.Contains("revuelto")||car.Spec.id.Contains("diablo")){axis=Vector3.right;sign=1;}}}
    public void Open(bool value){target=value?68:0;}
    public void CloseImmediately(){target=angle=0;if(panel)panel.localRotation=closed;}
    void Update(){if(!panel||Time.timeScale==0)return;angle=Mathf.MoveTowards(angle,target,Time.deltaTime*145);panel.localRotation=closed*Quaternion.AngleAxis(angle*sign,axis);}
}
}
