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
    
    [field: SerializeField, ReadOnly] public double3 RealVelocity { get; set; }
    [field: SerializeField, ReadOnly] public double3 RealPosition { get; set; }
    
    [Header("Debug")]
    [SerializeField] private bool doNotScaleTrail;
    
    [HideInInspector] public TrailRenderer trailRenderer;
    
    public double RealMass => realMass;
    public double RealRadius => realRadius;
    
    public double3 Force { get; set; }
    public bool DoNotScaleTrail => doNotScaleTrail;

    private void Awake()
    {
        trailRenderer = GetComponentInChildren<TrailRenderer>();
    }
    
    public void ResetSimulationValues(double3 realPosition)
    {
        RealPosition = realPosition;
        RealVelocity = default;
        Force = default;

        transform.localScale = Utils.ToSimulationLength(RealRadius) * Vector3.one;
        trailRenderer?.Clear();
    }

    public void ApplyPresentationValues()
    {
        transform.position = Utils.ToSimulationLength(RealPosition);
    }
}