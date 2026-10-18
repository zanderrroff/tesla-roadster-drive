using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.EventSystems;
namespace MotorCoast.Tests {
public sealed class InputTests {
    [UnityTearDown] public IEnumerator ReturnToEditor(){if(Application.isPlaying)yield return new ExitPlayMode();}
    [UnityTest] public IEnumerator PedalsOwnTheirTouchesAndCancelIndependently() {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        yield return new EnterPlayMode();
        var events=new GameObject("Events",typeof(EventSystem));var gas=new GameObject("Gas",typeof(HoldControl));var brake=new GameObject("Brake",typeof(HoldControl));
        try {
            bool throttle=false,braking=false;var g=gas.GetComponent<HoldControl>();var b=brake.GetComponent<HoldControl>();g.Changed=v=>throttle=v;b.Changed=v=>braking=v;
            var first=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=10,position=Vector2.zero};var second=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=11,position=Vector2.zero};
            g.OnPointerDown(first);b.OnPointerDown(second);Assert.IsTrue(throttle&&braking);
            g.OnPointerUp(second);Assert.IsTrue(throttle,"Another finger cannot release the accelerator");
            first.position=new Vector2(-5000,5000);g.OnPointerUp(first);Assert.IsFalse(throttle,"Release outside the control must release the accelerator");Assert.IsTrue(braking);
            b.OnCancel(second);Assert.IsFalse(braking);g.OnPointerDown(first);gas.SetActive(false);Assert.IsFalse(throttle);
            gas.SetActive(true);g.OnPointerDown(second);Assert.IsTrue(throttle,"Disable must not leave a stale pointer owner");
        }finally{Object.DestroyImmediate(gas);Object.DestroyImmediate(brake);Object.DestroyImmediate(events);}
    }
    [UnityTest] public IEnumerator SteeringTracksRotationAndReleasesOnFocusLoss() {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        yield return new EnterPlayMode();
        var events=new GameObject("Events",typeof(EventSystem));var obj=new GameObject("Wheel",typeof(RectTransform),typeof(WheelPad));
        try {
            var wheel=obj.GetComponent<WheelPad>();wheel.Input=new PlayerInput();
            var touch=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=1,position=new Vector2(100,0)};
            wheel.OnPointerDown(touch);touch.position=new Vector2(0,-100);wheel.OnDrag(touch);Assert.Greater(wheel.Input.TouchSteer,.5f);
            wheel.SendMessage("OnApplicationFocus",false);Assert.AreEqual(0,wheel.Input.TouchSteer);
            touch.pointerId=2;wheel.OnPointerDown(touch);touch.position=new Vector2(100,0);wheel.OnDrag(touch);Assert.Less(wheel.Input.TouchSteer,-.5f);
            wheel.OnCancel(touch);Assert.AreEqual(0,wheel.Input.TouchSteer);
        }finally{Object.DestroyImmediate(obj);Object.DestroyImmediate(events);}
    }
}
}
