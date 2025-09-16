using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SimulationConfig", menuName = "SimulationConfig")]
public class SimulationConfigSO : ScriptableObject
{
    [Header("Celestial Bodies Configs")]
    public List<CelestialBodyConfig> configs = new();
}