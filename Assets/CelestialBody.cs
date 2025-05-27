using System;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class CelestialBody : MonoBehaviour
{
    [FormerlySerializedAs("mass")]
    [SerializeField] private float realMass;
    [FormerlySerializedAs("radius")]
    [SerializeField] private float realRadius;
    
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public TrailRenderer trailRenderer;
    
    [field: SerializeField, ReadOnly] public float SimulationMass { get; set; }
    [field: SerializeField, ReadOnly] public float SimulationRadius { get; set; }
    [field: SerializeField, ReadOnly] public Vector3 SimulationVelocity { get; set; }
    
    [Header("Debug")]
    [SerializeField] private bool doNotScaleTrail;
    
    public Vector3 SimulationPosition
    {
        get => transform.position;
        private set => transform.position = value;
    }
    public Vector3 Force { get; set; }
    public bool DoNotScaleTrail => doNotScaleTrail;

    private void Validate()
    {
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponentInChildren<TrailRenderer>();
    }

    public float GetRealMass() => realMass;
    public float GetRealRadius() => realRadius;
    
    public void ResetSimulationValues(Vector3 realPosition)
    {
        Validate();
        SimulationMass = Utils.ToSimulationMass(realMass);
        SimulationRadius = Utils.ToSimulationLength(realRadius);
        SimulationPosition = Utils.ToSimulationLength(realPosition);
        rb.position = SimulationPosition;
        SimulationVelocity = default;
        Force = default;

        rb.mass = SimulationMass;
        transform.localScale = SimulationRadius * Vector3.one;
        trailRenderer.Clear();
        trailRenderer.widthMultiplier = SimulationRadius;
    }
}