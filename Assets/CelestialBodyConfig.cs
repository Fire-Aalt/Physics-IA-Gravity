using System;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class CelestialBodyConfig
{
    public GameObject celestialBodyPrefab;

    public bool setInitialPosition;

    [AllowNesting]
    [ShowIf("setInitialPosition")]
    public double realPositionOffset;
}