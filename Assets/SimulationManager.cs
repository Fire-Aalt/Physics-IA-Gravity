using System;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private double _moonOrbitAverageTime;
    [SerializeField] private int _moonOrbitAverageTimeCount;
    
    [Header("Units")]
    public TimeRange timeUnit;
    [SerializeField, NaughtyAttributes.ReadOnly] private double _lengthUnit;
    
    [Header("Constants")]
    [SerializeField, NaughtyAttributes.ReadOnly] private double _gravityConstant = 6.67430e-11;
    public double gravityConstantMultiplier;
    [SerializeField, NaughtyAttributes.ReadOnly] private double _finalGravityConstant;

    [Header("Step Solver")] 
    [SerializeField] private IntegrationMethod _integrationMethod;
    [SerializeField] private TimeRange _stepDuration;
    
    [Header("Celestial Bodies Configs")]
    public List<CelestialBodyConfig> configs = new();

    public double FinalGravityConstant => _gravityConstant * gravityConstantMultiplier;
    public double FinalTimeScale => timeUnit.Get();
    public bool IsSimulationPaused { get; private set; }

    public CelestialBody[] Bodies { get; private set; }

    private NativeArray<CelestialBodyData> _bodiesData;
    private NativeReference<double> _lastStepTime;
    private NativeList<double> _moonOrbitRotationEndTimes;
    private double _realTime;
    
    private void OnValidate()
    {
        ValidateValues();

        if (Application.isPlaying) return;
        
        LengthUnit = relativeBody.RealRadius;
        _lengthUnit = LengthUnit;
    }

    public void ValidateValues()
    {
        _finalGravityConstant = FinalGravityConstant;
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
        _moonOrbitRotationEndTimes = new NativeList<double>(8, Allocator.Persistent);
        InitSimulation();
    }

    private void OnDestroy()
    {
        _bodiesData.Dispose();
        _lastStepTime.Dispose();
        _moonOrbitRotationEndTimes.Dispose();
    }

    public void InitSimulation()
    {
        Assert.IsTrue(configs.Count == Bodies.Length && configs.Count == _bodiesData.Length);

        _realTime = 0;
        _lastStepTime.Value = 0;

        var found = FindObjectsByType<CelestialBody>(sortMode: FindObjectsSortMode.None);
        foreach (var body in found)
        {
            Destroy(body.gameObject);
        }
        
        // Initialize bodies
        for (var i = 0; i < configs.Count; i++)
        {
            var config = configs[i];
            
            var instance = Instantiate(config.celestialBodyPrefab);
            Bodies[i] = instance.GetComponent<CelestialBody>();
            _bodiesData[i] = Bodies[i].Initialize(config.realPositionOffset);
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

    private bool _pause;
    
    private void Update()
    {
        if (_pause) return;
        
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
                GravityConstant = FinalGravityConstant,
                IntegrationMethod = _integrationMethod,
                MoonOrbitRotationEndTimes = _moonOrbitRotationEndTimes,
                EarthBodyIndex = 0,
                MoonBodyIndex = 1
            }.Schedule().Complete();
        }
        
        // Scale simulation results down and apply them
        for (var i = 0; i < Bodies.Length; i++)
        {
            var body = Bodies[i];
            var bodyData = _bodiesData[i];
            body.ApplyPresentationValues(bodyData);
        }
        
        if (_moonOrbitRotationEndTimes.Length > 0)
        {
            var sum = 0.0;
            var prevTime = 0.0;
            foreach (var endTime in _moonOrbitRotationEndTimes)
            {
                sum += endTime - prevTime;
                prevTime = endTime;
            }

            _moonOrbitAverageTime = sum / _moonOrbitRotationEndTimes.Length;
            _moonOrbitAverageTimeCount = _moonOrbitRotationEndTimes.Length;
        }



        // if (_realTime > 60 * 60 * 24 * 365)
        // {
        //     _pause = true;
        // }
    }
    
    [BurstCompile]
    private struct SimulationJob : IJob
    {
        public NativeArray<CelestialBodyData> Bodies;
        public NativeReference<double> LastStepTime;
        
        public double RealTime;
        public double StepDuration;
        
        public double GravityConstant;
        public IntegrationMethod IntegrationMethod;
        
        
        public NativeList<double> MoonOrbitRotationEndTimes;
        public int EarthBodyIndex;
        public int MoonBodyIndex;

        private double3 _previousMoonRelativePosition;
        
        public void Execute()
        {
            _previousMoonRelativePosition = GetMoonRelativePosition();
            while (RealTime > LastStepTime.Value + StepDuration)
            {
                StepSimulation(StepDuration);
                LastStepTime.Value += StepDuration;
            }
        }

        private void StepSimulation(double deltaTime)
        {
            if (IntegrationMethod == IntegrationMethod.VelocityVerlet)
            {
                for (int i = 0; i < Bodies.Length; i++)
                {
                    ref var body = ref Bodies.ElementAt(i);
                    // x_{n+1} = x_n + v_n*dt + 0.5*a_n*dt^2
                    body.Position += body.Velocity * deltaTime + 0.5f * body.Acceleration * deltaTime * deltaTime;
                }
            }

            for (int i = 0; i < Bodies.Length; i++)
            {
                ref var bodyA = ref Bodies.ElementAt(i);
                for (int j = i + 1; j < Bodies.Length; j++)
                {
                    ref var bodyB = ref Bodies.ElementAt(j);
                    
                    var delta = bodyB.Position - bodyA.Position;
                    var distSqr = math.lengthsq(delta) + 1e-9; // avoid zero
                    var forceMagnitude = GravityConstant * (bodyA.Mass * bodyB.Mass) / distSqr;
                    var force = math.normalize(delta) * forceMagnitude;
                    
                    bodyA.Force += force;
                    bodyB.Force -= force;
                }
            }

            for (int i = 0; i < Bodies.Length; i++)
            {
                ref var body = ref Bodies.ElementAt(i);
                // a = F / m
                var newAcceleration = body.Force / body.Mass;
                
                if (IntegrationMethod == IntegrationMethod.VelocityVerlet)
                {
                    // v_{n+1} = v_n + 0.5*(a_n + a_{n+1})*dt
                    body.Velocity += 0.5f * (body.Acceleration + newAcceleration) * deltaTime;

                    body.Acceleration = newAcceleration;
                }
                else if (IntegrationMethod == IntegrationMethod.Euler)
                {
                    // v += a * dt
                    body.Velocity += newAcceleration * deltaTime;
                    // pos += v * dt
                    body.Position += body.Velocity * deltaTime;
                }

                body.Force = default;
            }

            var moonRelativePosition = GetMoonRelativePosition();
            if (_previousMoonRelativePosition.z < 0.0 && moonRelativePosition.z > 0.0)
            {
                MoonOrbitRotationEndTimes.Add(LastStepTime.Value + deltaTime);
            }
            _previousMoonRelativePosition = moonRelativePosition;
        }
        
        private double3 GetMoonRelativePosition()
        {
            return Bodies[MoonBodyIndex].Position - Bodies[EarthBodyIndex].Position;
        }
    }
    
    public enum IntegrationMethod
    {
        Euler,
        [InspectorName("Velocity Verlet (a.k.a. leapfrog)")]
        VelocityVerlet
    }
}