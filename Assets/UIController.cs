using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _followDropdown;
    [SerializeField] private TMP_InputField _timeScaleInput;
    [SerializeField] private Button _restartButton;
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    private SimulationManager _sim;
    
    private void Start()
    {
        _sim = SimulationManager.Instance;
        
        _followDropdown.ClearOptions();
        
        var list = new List<string>(_sim.Bodies.Length);
        foreach (var body in _sim.Bodies)
        {
            list.Add(body.gameObject.name);
        }
        
        _followDropdown.AddOptions(list);
        _followDropdown.onValueChanged.AddListener(ChangeFollowTarget);
        _restartButton.onClick.AddListener(SimulationManager.Instance.InitSimulation);
        
        _timeScaleInput.text = _sim.timeScale.ToString();
        _timeScaleInput.onEndEdit.AddListener(EndEditTimeScale);
    }

    private void ChangeFollowTarget(int index)
    {
        _cinemachineCamera.Follow = _sim.Bodies[index].transform;
    }

    private void EndEditTimeScale(string input)
    {
        var prevValue = _sim.timeScale;
        float timeScale;
        try
        {
            timeScale = float.Parse(input);
        }
        catch (Exception)
        {
            _timeScaleInput.text = prevValue.ToString();
            return;
        }
        
        timeScale = math.max(0.0001f, timeScale);
        _sim.timeScale = timeScale;
        _timeScaleInput.text = timeScale.ToString();
    }
}
