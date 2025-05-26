using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static float LengthUnit { get; private set; }
    public static float MassUnit { get; private set; }
    public static float TimeUnit { get; private set; }
    public static float SimulationGravityConstant { get; private set; }
    
    [Header("Controller")]
    public float timeScale = 1;
    public CelestialBody relativeBody;

    [Header("Units")]
    public float timeUnit = 60 * 60;
    [SerializeField, ReadOnly] private float _lengthUnit;
    [SerializeField, ReadOnly] private float _massUnit;
    
    [Header("Constants")]
    public float gravityConstant;
    public float gravityConstantMultiplier;
    [SerializeField, ReadOnly] private float _simulationGravityConstant;
    
    [Header("Celestial Bodies Configs")]
    public List<CelestialBodyConfig> configs = new();
    
    private readonly List<CelestialBody> _bodies = new();
    
    private void OnValidate()
    {
        CalculateGravityConstant();
        
        if (Application.isPlaying) return;

        InitSimulation();
    }

    [Button]
    private void InitSimulation()
    {
        LengthUnit = relativeBody.GetRealRadius();
        _lengthUnit = LengthUnit;
        MassUnit = relativeBody.GetRealMass();
        _massUnit = MassUnit;
        CalculateGravityConstant();

        _bodies.Clear();
        foreach (var config in configs)
        {
            if (config.setInitialPosition)
            {
                Vector3 relativePos = default;
                if (config.relativeBody != null)
                {
                    relativePos = Utils.ToRealLength(config.relativeBody.transform.position);
                }
                config.celestialBody.ResetSimulationValues(relativePos + config.relativePosition);
            }
            else
            {
                config.celestialBody.ResetSimulationValues();
            }
            _bodies.Add(config.celestialBody);
        }
        
        foreach (var bodyA in _bodies)
        {
            foreach (var bodyB in _bodies)
            {
                if (bodyA == bodyB) continue;
                
                var m2 = bodyB.SimulationMass;
                var r = Vector3.Distance(bodyA.SimulationPosition, bodyB.SimulationPosition);
                bodyA.transform.LookAt(bodyB.transform);
                bodyA.SimulationVelocity += bodyA.transform.right * Mathf.Sqrt(Mathf.Abs(m2 * SimulationGravityConstant / r));
            }
        }
    }

    private void CalculateGravityConstant()
    {
        // Calculate relative to relative units
        SimulationGravityConstant = gravityConstant * (LengthUnit * LengthUnit * LengthUnit / (MassUnit * timeUnit * timeUnit));
        // Scale by a multiplier
        SimulationGravityConstant *= gravityConstantMultiplier;
        _simulationGravityConstant = SimulationGravityConstant;
    }

    private void Start()
    {
        InitSimulation();
    }

    private void FixedUpdate()
    {
        var n = _bodies.Count;
        var dt = Time.fixedDeltaTime * timeUnit * timeScale;
        
        // 1) compute pairwise gravitational forces
        for (int i = 0; i < n; i++)
        {
            var bodyA = _bodies[i];
            for (int j = i + 1; j < n; j++)
            {
                var bodyB = _bodies[j];

                var delta = bodyA.SimulationPosition - bodyB.SimulationPosition;
                var distSqr = math.lengthsq(delta);

                if (distSqr < 1e-5f) continue; // avoid div by 0
                
                var dir = math.normalize(delta);

                var gravityForceMagnitude = SimulationGravityConstant
                                 * bodyA.SimulationMass
                                 * bodyB.SimulationMass
                                 / distSqr;

                var force = dir * gravityForceMagnitude;

                // accumulate forces (Newton’s 3rd law)
                bodyA.force += (Vector3)force;
                bodyB.force -= (Vector3)force;
            }
        }

        // 2) integrate velocities and positions
        foreach (var body in _bodies)
        {
            // a = F / m
            var acceleration = body.force / body.SimulationMass;
            // v += a * dt
            body.SimulationVelocity += acceleration * dt;
            
            // pos += v * dt
            var position = body.SimulationPosition + body.SimulationVelocity * dt;
            
            // update position using a RigidBody to also account for collisions
            body.rb.MovePosition(position);
            
            // reset force
            body.force = default;
        }
    }
}