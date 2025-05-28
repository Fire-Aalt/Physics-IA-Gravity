using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Assertions;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }
    public static double LengthUnit { get; private set; }
    
    [Header("Controller")]
    public CelestialBody relativeBody;

    [Header("Units")]
    public TimeRange timeUnit;
    [SerializeField, NaughtyAttributes.ReadOnly] private double _lengthUnit;
    
    [Header("Constants")]
    [SerializeField, NaughtyAttributes.ReadOnly] private double _gravityConstant = 6.67430e-11;
    public double gravityConstantMultiplier;
    [SerializeField, NaughtyAttributes.ReadOnly] private double _finalGravityConstant;
    
    [Header("Step Solver")]
    [SerializeField] private TimeRange _stepDuration;
    
    [Header("Celestial Bodies Configs")]
    public List<CelestialBodyConfig> configs = new();

    public double FinalGravityConstant => _gravityConstant * gravityConstantMultiplier;
    public double FinalTimeScale => timeUnit.Get();
    public bool IsSimulationPaused { get; private set; }

    public CelestialBody[] Bodies { get; private set; }

    private NativeArray<CelestialBodyData> _bodiesData;
    private NativeReference<double> _lastStepTime;
    private double _realTime;
    
    private void OnValidate()
    {
        ValidateValues();

        if (Application.isPlaying) return;
        
        LengthUnit = relativeBody.RealRadius;
        _lengthUnit = LengthUnit;
        ValidatePositions();
    }

    public void ValidateValues()
    {
        _finalGravityConstant = FinalGravityConstant;
    }

    private void ValidatePositions()
    {
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
    
    private void Start()
    {
        Bodies = new CelestialBody[configs.Count];
        _bodiesData = new NativeArray<CelestialBodyData>(configs.Count, Allocator.Persistent);
        _lastStepTime = new NativeReference<double>(0, Allocator.Persistent);
        InitSimulation();
    }

    private void OnDestroy()
    {
        _bodiesData.Dispose();
        _lastStepTime.Dispose();
    }

    public void InitSimulation()
    {
        Assert.IsTrue(configs.Count == Bodies.Length && configs.Count == _bodiesData.Length);

        _realTime = 0;
        _lastStepTime.Value = 0;
        
        // Initialize bodies
        for (var i = 0; i < configs.Count; i++)
        {
            var config = configs[i];
            Bodies[i] = config.celestialBody;
            _bodiesData[i] = config.celestialBody.Initialize(config.realPosition);
        }

        // Set initial velocities
        for (var i = 0; i < _bodiesData.Length; i++)
        {
            var bodyA = Bodies[i];
            ref var bodyAData = ref _bodiesData.ElementAt(i);
            for (var j = 0; j < _bodiesData.Length; j++)
            {
                var bodyB = Bodies[j];
                ref var bodyBData = ref _bodiesData.ElementAt(j);
                if (bodyAData.Equals(bodyBData)) continue;

                bodyA.transform.LookAt(bodyB.transform);

                var m2 = bodyBData.Mass;
                var r = math.distance(bodyAData.Position, bodyBData.Position);
                bodyAData.Velocity += bodyA.transform.right.AsDouble3()
                                      * math.sqrt(m2 * FinalGravityConstant / r);
            }
        }
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime * FinalTimeScale;
        _realTime += deltaTime;

        // Run simulation
        var stepDuration = _stepDuration.Get();
        if (_realTime > _lastStepTime.Value + stepDuration)
        {
            const int maxStepsPerSecond = 10_000_000;
                        
            var remainingTime = _realTime - _lastStepTime.Value;
            var stepsNeeded = (int)(remainingTime / stepDuration);
            
            if (stepsNeeded >= Time.deltaTime * maxStepsPerSecond)
            {
                // Rollback to allow continuation of the simulation
                _realTime -= deltaTime;
                IsSimulationPaused = true;
                throw new Exception("Simulation is stuck. Please lower either TimeScale, TimeUnit or increase StepDuration");
            }
            IsSimulationPaused = false;
            
            new SimulationJob
            {
                Bodies = _bodiesData,
                LastStepTime = _lastStepTime,
                RealTime = _realTime,
                StepDuration = stepDuration,
                GravityConstant = FinalGravityConstant
            }.Schedule().Complete();
        }
        
        // Scale simulation results down and apply them
        for (var i = 0; i < Bodies.Length; i++)
        {
            var body = Bodies[i];
            var bodyData = _bodiesData[i];
            body.ApplyPresentationValues(bodyData);
        }
    }
    
    [BurstCompile]
    private struct SimulationJob : IJob
    {
        public NativeArray<CelestialBodyData> Bodies;
        public NativeReference<double> LastStepTime;
        
        public double RealTime;
        public double StepDuration;
        
        public double GravityConstant;
        
        public void Execute()
        {
            while (RealTime > LastStepTime.Value + StepDuration)
            {
                StepSimulation(StepDuration);
                LastStepTime.Value += StepDuration;
            }
        }
        
        private void StepSimulation(double deltaTime)
        {
            // 1) compute pairwise gravitational forces
            for (var i = 0; i < Bodies.Length; i++)
            {
                ref var bodyA = ref Bodies.ElementAt(i);
                for (var j = 0; j < Bodies.Length; j++)
                {
                    ref var bodyB = ref Bodies.ElementAt(j);
                    if (bodyA.Equals(bodyB)) continue;

                    var delta = bodyB.Position - bodyA.Position;
                    var distSqr = math.lengthsq(delta);

                    if (distSqr < 1e-5f) continue; // avoid div by 0

                    var dir = math.normalize(delta);

                    var gravityForceMagnitude = GravityConstant * bodyA.Mass * bodyB.Mass / distSqr;

                    var force = dir * gravityForceMagnitude;

                    // accumulate forces (Newton’s 3rd law)
                    bodyA.Force += force;
                }
            }

            // 2) integrate velocities and positions
            for (var i = 0; i < Bodies.Length; i++)
            {
                ref var body = ref Bodies.ElementAt(i);
                // a = F / m
                var acceleration = body.Force / body.Mass;
                // v += a * dt
                body.Velocity += acceleration * deltaTime;
                // pos += v * dt
                body.Position += body.Velocity * deltaTime;

                // reset force
                body.Force = default;
            }
        }
    }
}