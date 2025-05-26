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
    
    public Rigidbody rb;
    
    [field: SerializeField, ReadOnly] public float SimulationMass { get; set; }
    [field: SerializeField, ReadOnly] public float SimulationRadius { get; set; }
    [field: SerializeField, ReadOnly] public Vector3 SimulationVelocity { get; set; }
    
    public Vector3 SimulationPosition
    {
        get => transform.position;
        private set => transform.position = value;
    }
    [HideInInspector] public Vector3 force;
    
    public float GetRealMass() => realMass;
    public float GetRealRadius() => realRadius;
    
    public void ResetSimulationValues(Vector3 realPosition)
    {
        SimulationMass = Utils.ToSimulationMass(realMass);
        SimulationRadius = Utils.ToSimulationLength(realRadius);
        SimulationPosition = Utils.ToSimulationLength(realPosition);
        SimulationVelocity = default;
        
        rb.mass = SimulationMass;
        transform.localScale = SimulationRadius * Vector3.one;
    }
    
    public void ResetSimulationValues()
    {
        SimulationMass = Utils.ToSimulationMass(realMass);
        SimulationRadius = Utils.ToSimulationLength(realRadius);
        SimulationVelocity = default;

        rb.mass = SimulationMass;
        transform.localScale = SimulationRadius * Vector3.one;
    }
}