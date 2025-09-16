using System;
using Unity.Mathematics;
using UnityEngine;

public class CelestialBody : MonoBehaviour
{
    [Header("Debug")]
    public float trailSize;
    
    [HideInInspector] public TrailRenderer trailRenderer;

    public LineRenderer gravityForce;
    public LineRenderer velocity;
    
    public double RealRadius { get; private set; }

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
    
    public CelestialBodyData Initialize(CelestialBodyConfig config, int i)
    {
        RealRadius = config.realDiameterKm * 1000.0 / 2.0;
        var data = AsData(config);
        transform.localScale = Utils.ToSimulationLength(RealRadius) * Vector3.one;
        ApplyPresentationValues(data, i);
        trailRenderer?.Clear();

        return data;
    }

    public void ApplyPresentationValues(CelestialBodyData celestialBodyData, int i)
    {
        transform.position = Utils.ToSimulationLength(celestialBodyData.Position);

        var other = i switch
        {
            0 => SimulationManager.Instance.BodiesData[1],
            1 => SimulationManager.Instance.BodiesData[0],
            2 => SimulationManager.Instance.BodiesData[1],
            _ => default
        };

        var gravForce = Utils.CalculateGravitationalForce(celestialBodyData, other, SimulationManager.Instance.FinalGravityConstant).AsVector3();
        
        gravityForce.SetPosition(1, gravForce * math.pow(10f, -20f) / transform.lossyScale.x);
        velocity.SetPosition(1, celestialBodyData.Velocity.AsVector3() * 0.03f / transform.lossyScale.x);
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