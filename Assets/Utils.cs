using Unity.Mathematics;
using UnityEngine;

public static class Utils
{
    public static float ToSimulationLength(double length) => (float)(length / SimulationManager.LengthUnit);
    public static Vector3 ToSimulationLength(double3 length) => (length / SimulationManager.LengthUnit).AsVector3();
    public static double3 ToRealLength(Vector3 length) => length.AsDouble3() * SimulationManager.LengthUnit;
    
    public static Vector3 AsVector3(this double3 a) => new((float)a.x, (float)a.y, (float)a.z);
    public static double3 AsDouble3(this Vector3 a) => new(a.x, a.y, a.z);

    public static double3 CalculateGravitationalForce(in CelestialBodyData bodyA, in CelestialBodyData bodyB, double gravityConstant)
    {
        var delta = bodyB.Position - bodyA.Position;
        var distSqr = math.lengthsq(delta);
        var forceMagnitude = gravityConstant * (bodyA.Mass * bodyB.Mass) / distSqr;
        var force = math.normalize(delta) * forceMagnitude;

        return force;
    }
}