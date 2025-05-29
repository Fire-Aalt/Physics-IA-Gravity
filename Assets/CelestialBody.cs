using System;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class CelestialBody : MonoBehaviour
{
    [FormerlySerializedAs("mass")]
    [SerializeField] private double realMass;
    [FormerlySerializedAs("radius")]
    [SerializeField] private double realRadius;
    
    [Header("Debug")]
    [SerializeField] private bool doNotScaleTrail;
    
    [HideInInspector] public TrailRenderer trailRenderer;
    
    public double RealMass => realMass;
    public double RealRadius => realRadius;
    
    public bool DoNotScaleTrail => doNotScaleTrail;

    private void Awake()
    {
        trailRenderer = GetComponentInChildren<TrailRenderer>();
    }

    private CelestialBodyData AsData(double realPositionOffset)
    {
        return new CelestialBodyData
        {
            HashCode = GetHashCode(),
            Mass = realMass,
            Radius = realRadius,
            Position = new double3(realPositionOffset, 0, 0)
        };
    }
    
    public CelestialBodyData Initialize(double realPositionOffset)
    {
        var data = AsData(realPositionOffset);
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
    public double Radius;
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