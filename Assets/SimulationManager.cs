using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }
    public static double LengthUnit { get; private set; }
    
    [Header("Controller")]
    public double timeScale = 1;
    public CelestialBody relativeBody;

    [Header("Units")]
    [SerializeField] private double _timeUnit;
    [SerializeField, ReadOnly] private double _lengthUnit;
    
    [Header("Constants")]
    [SerializeField, ReadOnly] private double _gravityConstant = 6.67430e-11;
    public double gravityConstantMultiplier;
    [SerializeField, ReadOnly] private double _finalGravityConstant;
    
    [Header("Step Solver")]
    [SerializeField] private bool _enableStepSolver;
    [SerializeField, ShowIf("_enableStepSolver")] private double _stepDuration = 1;
    
    [Header("Celestial Bodies Configs")]
    public List<CelestialBodyConfig> configs = new();
    
    public double FinalGravityConstant { get; private set; }
    
    public readonly List<CelestialBody> Bodies = new();
    private double _lastStepTime;
    private double _realTime;
    
    private void OnValidate()
    {
        CalculateGravityConstant();
        timeScale = math.max(0.0001f, timeScale);
        _timeUnit = math.max(1f, _timeUnit);
        
        if (Application.isPlaying) return;

        InitSimulation();
        
        var dict = new Dictionary<CelestialBody, double3>(8);
        
        // Calculate real positions
        foreach (var config in configs)
        {
            if (config.setInitialPosition)
            {
                double3 realPos = default;
                if (config.relativeBody != null)
                {
                    if (!dict.TryGetValue(config.relativeBody, out realPos))
                    {
                        throw new Exception("Relative body has incorrect order in the list");
                    }
                }
                config.realPosition = realPos + config.relativePosition;
            }
            else
            {
                var realPos = Utils.ToRealLength(config.celestialBody.transform.position);
                config.realPosition = realPos;
            }
            dict.Add(config.celestialBody, config.realPosition);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    [Button]
    private void InitSimulation()
    {
        LengthUnit = relativeBody.RealRadius;
        _lengthUnit = LengthUnit;
        CalculateGravityConstant();

        Bodies.Clear();
        foreach (var config in configs)
        {
            config.celestialBody.ResetSimulationValues(config.realPosition);
            Bodies.Add(config.celestialBody);
        }
        
        foreach (var bodyA in Bodies)
        {
            foreach (var bodyB in Bodies)
            {
                if (bodyA == bodyB) continue;
                
                bodyA.transform.LookAt(bodyB.transform);
                
                var m2 = bodyB.RealMass;
                var r = math.distance(bodyA.RealPosition, bodyB.RealPosition);
                bodyA.RealVelocity += bodyA.transform.right.AsDouble3()
                                      * math.sqrt(m2 * FinalGravityConstant / r);
            }
        }
    }

    private void CalculateGravityConstant()
    {
        // Scale by a multiplier
        FinalGravityConstant = _gravityConstant * gravityConstantMultiplier;
        _finalGravityConstant = FinalGravityConstant;
    }

    private void Start()
    {
        InitSimulation();
    }

    private void Update()
    {
        foreach (var body in Bodies)
        {
            body.ApplyPresentationValues();
        }
    }
    
    private void FixedUpdate()
    {
        var deltaTime = Time.fixedDeltaTime * _timeUnit * timeScale;
        _realTime += deltaTime;

        if (_enableStepSolver)
        {
            while (_realTime > _lastStepTime + _stepDuration)
            {
                StepSimulation(_stepDuration);
                _lastStepTime += _stepDuration;
            }
        }
        else
        {
            StepSimulation(deltaTime);
        }
    }

    private void StepSimulation(double deltaTime)
    {
        // 1) compute pairwise gravitational forces
        foreach (var bodyA in Bodies)
        {
            foreach (var bodyB in Bodies)
            {
                if (bodyA == bodyB) continue;

                var delta = bodyB.RealPosition - bodyA.RealPosition;
                var distSqr = math.lengthsq(delta);

                if (distSqr < 1e-5f) continue; // avoid div by 0
                
                var dir = math.normalize(delta);

                var gravityForceMagnitude = FinalGravityConstant * bodyA.RealMass * bodyB.RealMass / distSqr;

                var force = dir * gravityForceMagnitude;

                // accumulate forces (Newton’s 3rd law)
                bodyA.Force += force;
            }
        }

        // 2) integrate velocities and positions
        foreach (var body in Bodies)
        {
            // a = F / m
            var acceleration = body.Force / body.RealMass;
            // v += a * dt
            body.RealVelocity += acceleration * deltaTime;
            // pos += v * dt
            body.RealPosition += body.RealVelocity * deltaTime;
            
            // reset force
            body.Force = default;
        }
    }
}