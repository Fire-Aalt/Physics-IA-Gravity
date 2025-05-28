using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private Button _simulationInfoFoldoutButton;
    [SerializeField] private GameObject _simulationInfoFoldout;
    [SerializeField] private RectTransform _simulationInfoFoldoutArrow;
    
    [SerializeField] private TMP_Dropdown _followDropdown;
    [SerializeField] private TimeRangeUI _timeScaleInput;
    [SerializeField] private TMP_InputField _gravityConstant;
    [SerializeField] private Button _restartButton;
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    private SimulationManager _sim;
    private bool _isSimulationInfoClosed;
    
    private void Start()
    {
        _sim = SimulationManager.Instance;
        
        _followDropdown.ClearOptions();
        
        var list = new List<string>(_sim.Bodies.Length);
        foreach (var body in _sim.Bodies)
        {
            list.Add(body.gameObject.name);
        }
        
        _simulationInfoFoldoutButton.onClick.AddListener(ChangeFoldState);
        
        _followDropdown.AddOptions(list);
        _followDropdown.onValueChanged.AddListener(ChangeFollowTarget);
        
        _restartButton.onClick.AddListener(_sim.InitSimulation);
        
        _timeScaleInput.Initialize(_sim.timeUnit);
        
        _gravityConstant.text = (_sim.gravityConstantMultiplier * 100f) + "%";
        _gravityConstant.onEndEdit.AddListener(EndGravityConstant);
    }
    
    private void ChangeFoldState()
    {
        _isSimulationInfoClosed = !_isSimulationInfoClosed;
        _simulationInfoFoldout.SetActive(!_isSimulationInfoClosed);

        _simulationInfoFoldoutArrow.rotation = Quaternion.Euler(0f, 0f, _isSimulationInfoClosed ? 90f : 0f);
    }
    
    private void ChangeFollowTarget(int index)
    {
        _cinemachineCamera.Follow = _sim.Bodies[index].transform;
    }
    
    private void EndGravityConstant(string input)
    {
        var prevValue = _sim.gravityConstantMultiplier;
        float timeScale;
        try
        {
            input = input.Replace("%", "");
            timeScale = float.Parse(input);
        }
        catch (Exception)
        {
            _gravityConstant.text = (prevValue * 100f) + "%";
            return;
        }
        
        _sim.gravityConstantMultiplier = timeScale / 100f;
        _sim.ValidateValues();
        _gravityConstant.text = (_sim.gravityConstantMultiplier * 100f) + "%";
    }
}
