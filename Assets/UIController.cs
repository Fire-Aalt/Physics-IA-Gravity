using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _followDropdown;
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    
    private void Start()
    {
        var sim = SimulationManager.Instance;
        
        _followDropdown.ClearOptions();
        
        var list = new List<string>(sim.Bodies.Count);
        foreach (var body in sim.Bodies)
        {
            list.Add(body.gameObject.name);
        }
        
        _followDropdown.AddOptions(list);
        _followDropdown.onValueChanged.AddListener(ChangeFollowTarget);
    }

    private void ChangeFollowTarget(int index)
    {
        _cinemachineCamera.Follow = SimulationManager.Instance.Bodies[index].transform;
    }
}
