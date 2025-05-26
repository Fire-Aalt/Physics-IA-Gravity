using System;
using NaughtyAttributes;
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
    public Vector3 relativePosition;
}