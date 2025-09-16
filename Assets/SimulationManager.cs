using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Vector3 = UnityEngine.Vector3;

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
    [SerializeField] private IntegrationMethod _integrationMethod;
    [SerializeField] private TimeRange _stepDuration;
    
    [Header("Celestial Bodies Configs")]
    public SimulationConfigSO settings;

    public double FinalGravityConstant => _gravityConstant * gravityConstantMultiplier;
    public double FinalTimeScale => timeUnit.Get();
    public bool IsSimulationPaused { get; private set; }

    public CelestialBody[] Bodies { get; private set; }
    public NativeList<double> EarthOrbitRotationEndTimes { get; private set; }
    public double RealTime { get; private set; }

    private NativeArray<CelestialBodyData> _bodiesData;
    private NativeReference<double> _lastStepTime;
    private bool _pause;
    
    private void OnValidate()
    {
        ValidateValues();

        if (Application.isPlaying || settings ==null) return;
        
        LengthUnit = settings.configs.First(c => c.celestialBodyPrefab == relativeBody.gameObject).realDiameterKm * 1000.0 / 2.0;
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
        Bodies = new CelestialBody[settings.configs.Count];
        _bodiesData = new NativeArray<CelestialBodyData>(settings.configs.Count, Allocator.Persistent);
        _lastStepTime = new NativeReference<double>(0, Allocator.Persistent);
        EarthOrbitRotationEndTimes = new NativeList<double>(8, Allocator.Persistent);
        
        InitSimulation();
    }

    private void OnDestroy()
    {
        _bodiesData.Dispose();
        _lastStepTime.Dispose();
        EarthOrbitRotationEndTimes.Dispose();
    }

    public void InitSimulation()
    {
        Assert.IsTrue(settings.configs.Count == Bodies.Length && settings.configs.Count == _bodiesData.Length);

        RealTime = 0;
        _lastStepTime.Value = 0;
        EarthOrbitRotationEndTimes.Clear();

        var found = FindObjectsByType<CelestialBody>(sortMode: FindObjectsSortMode.None);
        foreach (var body in found)
        {
            Destroy(body.gameObject);
        }
        
        // Initialize bodies
        for (var i = 0; i < settings.configs.Count; i++)
        {
            var config = settings.configs[i];
            
            var instance = Instantiate(config.celestialBodyPrefab);
            Bodies[i] = instance.GetComponent<CelestialBody>();
            _bodiesData[i] = Bodies[i].Initialize(config);
        }

        // Set initial velocities
        for (var i = 0; i < _bodiesData.Length; i++)
        {
            ref var bodyAData = ref _bodiesData.ElementAt(i);
            for (var j = 0; j < _bodiesData.Length; j++)
            {
                ref var bodyBData = ref _bodiesData.ElementAt(j);
                if (bodyAData.Equals(bodyBData)) continue;
        
                var delta = bodyAData.Position - bodyBData.Position;
                
                var cross = math.cross(delta, math.up());
                var direction = math.normalize(cross);
                
                var m2 = bodyBData.Mass;
                var r = math.distance(bodyAData.Position, bodyBData.Position);
                bodyAData.Velocity += direction * math.sqrt(m2 * FinalGravityConstant / r);
            }
        }
         
        UIController.Instance.Restart();
    }
    
    private void Update()
    {
        if (!_pause)
        {
            var deltaTime = Time.deltaTime * FinalTimeScale;
            RealTime += deltaTime;
            
            // Run simulation
            var stepDuration = _stepDuration.Get();
            if (RealTime > _lastStepTime.Value + stepDuration)
            {
                const int maxStepsPerSecond = 10_000_000;
                
                var remainingTime = RealTime - _lastStepTime.Value;
                var stepsNeeded = (int)(remainingTime / stepDuration);
                
                if (stepsNeeded >= Time.deltaTime * maxStepsPerSecond)
                {
                    // Rollback to allow continuation of the simulation
                    RealTime -= deltaTime;
                    IsSimulationPaused = true;
                    throw new Exception("Simulation is stuck. Please lower either TimeScale, TimeUnit or increase StepDuration");
                }
                IsSimulationPaused = false;
                
                new SimulationJob
                {
                    Bodies = _bodiesData,
                    LastStepTime = _lastStepTime,
                    RealTime = RealTime,
                    StepDuration = stepDuration,
                    GravityConstant = FinalGravityConstant,
                    IntegrationMethod = _integrationMethod,
                    OrbitalPeriods = EarthOrbitRotationEndTimes
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
        
        for (int i = 0; i < _bodiesData.Length; i++)
        {
            ref var bodyAData = ref _bodiesData.ElementAt(i);
            Debug.DrawRay(Utils.ToSimulationLength(bodyAData.Position), bodyAData.Velocity.AsVector3(), Color.green);

            if (i == 0)
            {
                Debug.DrawRay(Utils.ToSimulationLength(bodyAData.Position), Vector3.right * 100000f, Color.red);
            }
        }
    }
    
    [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
    private struct SimulationJob : IJob
    {
        public NativeArray<CelestialBodyData> Bodies;
        public NativeReference<double> LastStepTime;
        
        public double RealTime;
        public double StepDuration;
        
        public double GravityConstant;
        public IntegrationMethod IntegrationMethod;
        
        public NativeList<double> OrbitalPeriods;
        private double3 _lastSunPosition;
        private double3 _lastEarthPosition;
        
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
            _lastSunPosition = Bodies[0].Position;
            _lastEarthPosition = Bodies[1].Position;
            
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
                    var distSqr = math.lengthsq(delta);
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

            if (_lastEarthPosition.z < _lastSunPosition.z && Bodies[1].Position.z > Bodies[0].Position.z)
            {
                OrbitalPeriods.Add(LastStepTime.Value + deltaTime);
            }
        }
    }
    
    public enum IntegrationMethod
    {
        Euler,
        [InspectorName("Velocity Verlet (a.k.a. leapfrog)")]
        VelocityVerlet
    }
}