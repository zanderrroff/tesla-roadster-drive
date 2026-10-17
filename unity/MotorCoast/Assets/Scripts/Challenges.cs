using UnityEngine;
namespace MotorCoast {
public sealed class Challenges:MonoBehaviour {
    public VehicleController Car;public CoastWorld World;
    public string Status {get;private set;}="СВОБОДНОЕ ВОЖДЕНИЕ";
    float drift,comboTimer,airTime,announcementUntil;Vector3 takeoff;int gate;float slalomStart=-1;string announcement;
    void Update() {
        if(!Car||Time.timeScale==0)return;
        float dt=Time.deltaTime;var local=Car.transform.InverseTransformDirection(Car.Body.linearVelocity);
        float angle=Mathf.Abs(Mathf.Atan2(local.x,Mathf.Abs(local.z))*Mathf.Rad2Deg);
        if(Car.Grounded>1&&Car.Kph>24&&angle>12&&angle<75) {drift+=angle*dt*Car.Kph*.06f;comboTimer=2;}
        else comboTimer-=dt;
        if(comboTimer<0&&drift>10){float best=PlayerPrefs.GetFloat("bestDrift",0);if(drift>best)PlayerPrefs.SetFloat("bestDrift",drift);Announce("ДРИФТ  +"+Mathf.FloorToInt(drift));drift=0;}
        if(Car.Grounded==0){if(airTime==0)takeoff=Car.transform.position;airTime+=dt;}
        else if(airTime>0){float distance=Vector2.Distance(new Vector2(takeoff.x,takeoff.z),new Vector2(Car.transform.position.x,Car.transform.position.z));if(airTime>.5f&&distance>4)Announce("ПРЫЖОК  "+distance.ToString("0.0")+" М");airTime=0;}
        if(World.Slalom.Count>gate&&Vector3.Distance(Car.transform.position,World.Slalom[gate]+Vector3.up*.6f)<5){
            if(gate==0)slalomStart=Time.time;gate++;
            if(gate==World.Slalom.Count){float t=Time.time-slalomStart;float record=PlayerPrefs.GetFloat("bestSlalom",9999);if(t<record)PlayerPrefs.SetFloat("bestSlalom",t);Announce("СЛАЛОМ  "+t.ToString("0.0")+" СЕК");gate=0;slalomStart=-1;}
        }
        Status=Time.time<announcementUntil?announcement:drift>5?"ДРИФТ  "+Mathf.FloorToInt(drift):slalomStart>=0?"СЛАЛОМ  "+gate+" / "+World.Slalom.Count+"    "+(Time.time-slalomStart).ToString("0.0")+" СЕК":"ДРИФТ-ПЛОЩАДКА   /   РАМПЫ   /   СЛАЛОМ";
    }
    public void ResetRun(){drift=comboTimer=airTime=0;gate=0;slalomStart=-1;}
    void Announce(string value){announcement=value;announcementUntil=Time.time+4;PlayerPrefs.Save();}
}
}
