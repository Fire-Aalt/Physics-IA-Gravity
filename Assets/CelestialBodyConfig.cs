using System;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class CelestialBodyConfig
{
    public GameObject celestialBodyPrefab;
    public double realPositionOffset;
    public double additionalOffset;
}