using Unity.Mathematics;
using UnityEngine;

public static class Utils
{
    public static float ToSimulationLength(float length) => length / SimulationManager.LengthUnit;
    public static Vector3 ToSimulationLength(Vector3 length) => length / SimulationManager.LengthUnit;
    public static Vector3 ToRealLength(Vector3 length) => length * SimulationManager.LengthUnit;
    public static float ToSimulationMass(float mass) => mass / SimulationManager.MassUnit;
    
    public static float ToSimulationTime(float time) => time / SimulationManager.TimeUnit;
}