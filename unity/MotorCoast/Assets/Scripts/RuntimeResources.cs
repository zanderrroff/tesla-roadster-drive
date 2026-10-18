using System;
using System.Collections.Generic;
using UnityEngine;
namespace MotorCoast {
// Procedural meshes/materials are native allocations and outlive their renderer
// unless explicitly released. Asset-backed textures and imported models are
// deliberately never registered here.
public sealed class RuntimeResources:MonoBehaviour {
    static RuntimeResources current;
    readonly HashSet<UnityEngine.Object> owned=new HashSet<UnityEngine.Object>();
    public static IDisposable Begin(GameObject root){var previous=current;current=root.GetComponent<RuntimeResources>();if(!current)current=root.AddComponent<RuntimeResources>();return new Scope(previous);}
    public static T Own<T>(T value) where T:UnityEngine.Object {if(current&&value)current.owned.Add(value);return value;}
    sealed class Scope:IDisposable {readonly RuntimeResources previous;public Scope(RuntimeResources value){previous=value;}public void Dispose(){current=previous;}}
    void OnDestroy(){foreach(var value in owned)if(value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}owned.Clear();}
}
}
