using System;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class CelestialBodyConfig
{
    public CelestialBody celestialBody;

    public bool setInitialPosition;
    
    [AllowNesting]
    [ShowIf("setInitialPosition")]
    public CelestialBody relativeBody;
    
    [AllowNesting]
    [ShowIf("setInitialPosition")]
    public double3 relativePosition;

    [AllowNesting]
    [ReadOnly]
    public double3 realPosition;
}