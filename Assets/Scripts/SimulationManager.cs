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
    public static double LengthUnit => Instance._lengthUnit;
    
    [Header("Units")]
    public TimeRange timeUnit;
    [SerializeField] private double _lengthUnit;
    
    [Header("Constants")]
    [SerializeField, NaughtyAttributes.ReadOnly] private double _gravityConstant = 6.674e-11;
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
    public NativeArray<CelestialBodyData> BodiesData;
    public NativeList<double> EarthOrbitRotationEndTimes { get; private set; }
    public NativeList<double> MoonOrbitRotationEndTimes { get; private set; }
    public double RealTime { get; private set; }

    private NativeReference<double> _lastStepTime;
    private bool _pause;
    
    private void OnValidate()
    {
        ValidateValues();
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
        BodiesData = new NativeArray<CelestialBodyData>(settings.configs.Count, Allocator.Persistent);
        _lastStepTime = new NativeReference<double>(0, Allocator.Persistent);
        EarthOrbitRotationEndTimes = new NativeList<double>(8, Allocator.Persistent);
        MoonOrbitRotationEndTimes = new NativeList<double>(8, Allocator.Persistent);
        
        InitSimulation();
    }

    private void OnDestroy()
    {
        BodiesData.Dispose();
        _lastStepTime.Dispose();
        EarthOrbitRotationEndTimes.Dispose();
        MoonOrbitRotationEndTimes.Dispose();
    }

    public void InitSimulation()
    {
        Assert.IsTrue(settings.configs.Count == Bodies.Length && settings.configs.Count == BodiesData.Length);

        RealTime = 0;
        _lastStepTime.Value = 0;
        EarthOrbitRotationEndTimes.Clear();
        MoonOrbitRotationEndTimes.Clear();

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
        }
        
        for (var i = 0; i < settings.configs.Count; i++)
        {
            var config = settings.configs[i];
            
            BodiesData[i] = Bodies[i].Initialize(config, i);
        }

        // Set initial velocities
        ApplyInitialVelocity(BodiesData[0], ref BodiesData.ElementAt(1));
        ApplyInitialVelocity(BodiesData[1], ref BodiesData.ElementAt(2));
        
        UIController.Instance.Restart();
    }

    private void ApplyInitialVelocity(in CelestialBodyData greaterBody, ref CelestialBodyData smallerBody)
    {
        var delta = smallerBody.Position - greaterBody.Position;
        
        var cross = math.cross(delta, math.up());
        var direction = math.normalize(cross);
        
        //var m2 = greaterBody.Mass;
        var m2 = greaterBody.Mass + smallerBody.Mass;
        var r = math.distance(greaterBody.Position, smallerBody.Position);
        smallerBody.Velocity += greaterBody.Velocity + direction * math.sqrt(m2 * FinalGravityConstant / r);
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
                const int maxStepsPerSecond = 100_000_000;
                
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
                    Bodies = BodiesData,
                    LastStepTime = _lastStepTime,
                    RealTime = RealTime,
                    StepDuration = stepDuration,
                    GravityConstant = FinalGravityConstant,
                    IntegrationMethod = _integrationMethod,
                    EarthOrbitalPeriods = EarthOrbitRotationEndTimes,
                    MoonOrbitalPeriods = MoonOrbitRotationEndTimes
                }.Schedule().Complete();
            }
            
            // Scale simulation results down and apply them
            for (var i = 0; i < Bodies.Length; i++)
            {
                var body = Bodies[i];
                var bodyData = BodiesData[i];
                body.ApplyPresentationValues(bodyData, i);
            }
        }
        
        for (int i = 0; i < BodiesData.Length; i++)
        {
            ref var bodyAData = ref BodiesData.ElementAt(i);
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
        public NativeList<double> EarthOrbitalPeriods;
        public NativeList<double> MoonOrbitalPeriods;
        
        public double RealTime;
        public double StepDuration;
        
        public double GravityConstant;
        public IntegrationMethod IntegrationMethod;
        
        private double3 _lastSunPosition;
        private double3 _lastEarthPosition;
        private double3 _lastMoonPosition;
        
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
            _lastMoonPosition = Bodies[2].Position;
            
            // Gravitational force
            for (int i = 0; i < Bodies.Length; i++)
            {
                ref var bodyA = ref Bodies.ElementAt(i);
                
                for (int j = i + 1; j < Bodies.Length; j++)
                {
                    ref var bodyB = ref Bodies.ElementAt(j);
                    
                    var force = Utils.CalculateGravitationalForce(bodyA, bodyB, GravityConstant);
                    
                    bodyA.Force += force;
                    bodyB.Force -= force;
                }
            }

            // Integration
            for (int i = 0; i < Bodies.Length; i++)
            {
                ref var body = ref Bodies.ElementAt(i);
                var newAcceleration = body.Force / body.Mass;

                if (IntegrationMethod == IntegrationMethod.VelocityVerlet)
                {
                    body.Position += body.Velocity * deltaTime + body.Acceleration * (deltaTime * deltaTime * 0.5f);
                    body.Acceleration = newAcceleration;
                    body.Velocity += (body.Acceleration + newAcceleration) * (deltaTime * 0.5f);
                }
                else if (IntegrationMethod == IntegrationMethod.Euler)
                {
                    body.Velocity += newAcceleration * deltaTime;
                    body.Position += body.Velocity * deltaTime;
                }
                
                body.Force = default;
            }

            // Orbital Period 360 rotation check
            if (_lastEarthPosition.z < _lastSunPosition.z && Bodies[1].Position.z > Bodies[0].Position.z)
            {
                EarthOrbitalPeriods.Add(LastStepTime.Value + deltaTime);
            }
            if (_lastMoonPosition.z < _lastEarthPosition.z && Bodies[2].Position.z > Bodies[1].Position.z)
            {
                MoonOrbitalPeriods.Add(LastStepTime.Value + deltaTime);
            }
        }
    }

    private enum IntegrationMethod
    {
        Euler,
        [InspectorName("Velocity Verlet (a.k.a. leapfrog)")]
        VelocityVerlet
    }
}