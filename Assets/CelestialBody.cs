using System;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class CelestialBody : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool doNotScaleTrail;
    
    [HideInInspector] public TrailRenderer trailRenderer;
    
    public double RealRadius { get; private set; }
    
    public bool DoNotScaleTrail => doNotScaleTrail;

    private void Awake()
    {
        trailRenderer = GetComponentInChildren<TrailRenderer>();
    }

    private CelestialBodyData AsData(CelestialBodyConfig config)
    {
        var positionKm = config.realPositionKm;
        if (config.addOffset)
        {
            positionKm += config.offsetKm * new double3(
                math.sin(math.radians(config.angle)),
                0,
                math.cos(math.radians(config.angle)));
        }
        
        return new CelestialBodyData
        {
            HashCode = GetHashCode(),
            Mass = config.realMassKg,
            Position = positionKm * 1000.0
        };
    }
    
    public CelestialBodyData Initialize(CelestialBodyConfig config)
    {
        RealRadius = config.realDiameterKm * 1000.0 / 2.0;
        var data = AsData(config);
        transform.localScale = Utils.ToSimulationLength(RealRadius) * Vector3.one;
        ApplyPresentationValues(data);
        trailRenderer?.Clear();

        return data;
    }

    public void ApplyPresentationValues(CelestialBodyData celestialBodyData)
    {
        transform.position = Utils.ToSimulationLength(celestialBodyData.Position);
    }
}

public struct CelestialBodyData : IEquatable<CelestialBodyData>
{
    public int HashCode;
    
    public double Mass;
    public double3 Force;
    public double3 Acceleration;
    public double3 Velocity;
    
    public double3 Position;

    public bool Equals(CelestialBodyData other)
    {
        return HashCode == other.HashCode;
    }

    public override int GetHashCode()
    {
        return HashCode;
    }
}