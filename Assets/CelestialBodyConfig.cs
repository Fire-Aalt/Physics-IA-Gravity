using System;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class CelestialBodyConfig
{
    public GameObject celestialBodyPrefab;
    
    public double3 realPositionKm;
    
    public bool addOffset;
    [ShowIf("addOffset")]
    [AllowNesting]
    [Range(0f, 360f)] public float angle;
    [ShowIf("addOffset")]
    [AllowNesting]
    public double offsetKm;
    
    public double realMassKg;
    public double realDiameterKm;
}