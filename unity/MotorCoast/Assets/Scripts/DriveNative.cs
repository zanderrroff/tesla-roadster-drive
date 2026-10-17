using System;
using System.Runtime.InteropServices;
namespace MotorCoast {
[StructLayout(LayoutKind.Sequential)] public struct DriveInput {
    public float steer, throttle, brake, handbrake, speed;
    public int requestedGear;
}
[StructLayout(LayoutKind.Sequential)] public struct DriveState { public float steer, throttle; public int gear; }
[StructLayout(LayoutKind.Sequential)] public struct DriveOutput {
    public float steerDegrees, driveTorque, brakeTorque, rearBrakeTorque, rearGrip;
    public int gear;
}
public static class DriveNative {
#if UNITY_IOS && !UNITY_EDITOR
    const string Library = "__Internal";
#else
    const string Library = "MotorCoastNative";
#endif
    [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] public static extern void MCReset(ref DriveState state);
    [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] public static extern void MCStep(ref DriveState state, ref DriveInput input, float dt, float power, out DriveOutput output);
}
}
