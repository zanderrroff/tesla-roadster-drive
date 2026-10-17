using System;
using UnityEngine;
namespace MotorCoast {
[Serializable] public sealed class VehicleSpec {
    public string id, label, resource, highResource;
    public bool electric, hasInterior, fitLegs;
    public float power, mass, wheelRadius, driverScale, torsoLean, floorY, roofY, pedalZ, wheelTilt, steeringRadius;
    public Vector3[] wheelCenters;
    public Vector3 colliderSize, shellOffset, driverPosition, wheelPosition;
}
[Serializable] public sealed class VehicleCatalog { public VehicleSpec[] vehicles; }
public static class Nodes {
    public static Transform Find(Transform root, string name) {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.Replace(' ', '_') == name) return t;
        return null;
    }
    public static void Layer(GameObject root, int layer) {
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=layer;
    }
}
}
