using UnityEngine;
namespace MotorCoast {
public sealed class VehicleAudio : MonoBehaviour {
    VehicleController car;AudioSource engine,tires,wind,impact,horn;float collisionCooldown;
    public void Configure(VehicleController vehicle) {
        car=vehicle;engine=Loop(car.Spec.electric?Electric():Resources.Load<AudioClip>("Audio/engine-loop"),.08f);
        tires=Loop(Resources.Load<AudioClip>("Audio/tire-loop"),0);wind=Loop(Resources.Load<AudioClip>("Audio/ambience"),.12f);
        impact=gameObject.AddComponent<AudioSource>();impact.spatialBlend=.3f;impact.clip=Resources.Load<AudioClip>("Audio/impact");
        horn=gameObject.AddComponent<AudioSource>();horn.clip=HornClip();horn.volume=.2f;
        car.Impact+=OnImpact;
    }
    AudioSource Loop(AudioClip clip,float volume) {
        var s=gameObject.AddComponent<AudioSource>();s.clip=clip;s.loop=true;s.volume=volume;s.Play();return s;
    }
    void Update() {
        if(!car||Time.timeScale==0)return;
        var speed=car.Body.linearVelocity.magnitude;float dt=Time.deltaTime;
        engine.pitch=Mathf.Lerp(engine.pitch,car.Spec.electric?.65f+speed*.055f: .7f+Mathf.Repeat(speed*.095f,1.25f)+car.Throttle*.4f,1-Mathf.Exp(-5*dt));
        engine.volume=Mathf.Lerp(engine.volume,(car.Spec.electric?.065f:.17f)+car.Throttle*.17f,1-Mathf.Exp(-5*dt));
        tires.volume=Mathf.Lerp(tires.volume,car.Grounded>0?Mathf.Clamp01((car.Slip-.12f)*1.1f)*.38f:0,1-Mathf.Exp(-12*dt));
        tires.pitch=.85f+Mathf.Min(speed*.01f,.4f);wind.volume=.07f+Mathf.Min(speed*.004f,.22f);
    }
    public void Horn() { if(!horn.isPlaying)horn.Play(); }
    void OnImpact(float force) { if(Time.time>collisionCooldown) { collisionCooldown=Time.time+.2f;impact.PlayOneShot(impact.clip,Mathf.Clamp01(force/16)*.7f); } }
    static AudioClip Electric() {
        const int rate=44100;var data=new float[rate*2];
        for(int i=0;i<data.Length;i++){float t=i/(float)rate;data[i]=.22f*Mathf.Sin(2*Mathf.PI*110*t)+.08f*Mathf.Sin(2*Mathf.PI*330*t)+.025f*Mathf.Sin(2*Mathf.PI*880*t);}
        var c=AudioClip.Create("Electric drive",data.Length,1,rate,false);c.SetData(data,0);return c;
    }
    static AudioClip HornClip() {
        const int rate=22050;var data=new float[rate/2];
        for(int i=0;i<data.Length;i++){float t=i/(float)rate,envelope=Mathf.Min(1,t*40)*Mathf.Min(1,(.5f-t)*20);data[i]=envelope*(Mathf.Sin(t*2*Mathf.PI*350)+Mathf.Sin(t*2*Mathf.PI*440))*.22f;}
        var c=AudioClip.Create("Horn",data.Length,1,rate,false);c.SetData(data,0);return c;
    }
    void OnDestroy(){if(car)car.Impact-=OnImpact;if(engine&&car&&car.Spec.electric)Destroy(engine.clip);if(horn)Destroy(horn.clip);}
}
}
