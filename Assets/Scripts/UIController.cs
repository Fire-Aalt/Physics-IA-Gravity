using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    
    [SerializeField] private Button _simulationInfoFoldoutButton;
    [SerializeField] private GameObject _simulationInfoFoldout;
    [SerializeField] private RectTransform _simulationInfoFoldoutArrow;
    
    [SerializeField] private TMP_Dropdown _followDropdown;
    [SerializeField] private TimeRangeUI _timeScaleInput;
    [SerializeField] private TMP_InputField _gravityConstant;
    [SerializeField] private Button _restartButton;
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private TextMeshProUGUI _earthOrbitalPeriodText;
    [SerializeField] private TextMeshProUGUI _moonOrbitalPeriodText;
    
    private bool _isSimulationInfoClosed;
    private SimulationManager Sim => SimulationManager.Instance;
    
    private float _earthPeriod = -1f;
    private float _moonPeriod = -1f;
    private float _days = -1f;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        _followDropdown.value = 0;
        _followDropdown.ClearOptions();
        
        var list = new List<string>(Sim.Bodies.Length);
        foreach (var body in Sim.Bodies)
        {
            list.Add(body.gameObject.name.Replace("(Clone)", ""));
        }
        
        _simulationInfoFoldoutButton.onClick.AddListener(ChangeFoldState);
        
        _followDropdown.AddOptions(list);
        _followDropdown.onValueChanged.AddListener(ChangeFollowTarget);
        
        _restartButton.onClick.AddListener(Sim.InitSimulation);
        
        _timeScaleInput.Initialize(Sim.timeUnit);
        
        _gravityConstant.text = (Sim.gravityConstantMultiplier * 100f) + "%";
        _gravityConstant.onEndEdit.AddListener(EndGravityConstant);
    }

    private void Update()
    {
        const float toDays = 60f * 60f * 24f;

        ShowOrbitalPeriod(toDays, Sim.EarthOrbitRotationEndTimes, SetEarthOrbitalPeriod);
        ShowOrbitalPeriod(toDays, Sim.MoonOrbitRotationEndTimes, SetMoonOrbitalPeriod);
        
        SetTime(Mathf.FloorToInt((float)Sim.RealTime / toDays));
    }

    private void ShowOrbitalPeriod(float toDays, NativeList<double> specificOrbitalPeriodEndTimes, Action<float> setSpecificOrbitalPeriod)
    {
        if (specificOrbitalPeriodEndTimes.Length >= 1)
        {
            const int averageCount = 100;


            var length = specificOrbitalPeriodEndTimes.Length;

            var period = 0.0;
            if (length - averageCount - 1 >= 0)
            {
                var prevTime = specificOrbitalPeriodEndTimes[length - averageCount - 1];
                for (int i = length - averageCount; i < length; i++)
                {
                    period += specificOrbitalPeriodEndTimes[i] - prevTime;
                    prevTime = specificOrbitalPeriodEndTimes[i];
                }
            }
            else
            {
                var prevTime = 0.0;
                for (int i = 0; i < length; i++)
                {
                    period += specificOrbitalPeriodEndTimes[i] - prevTime;
                    prevTime = specificOrbitalPeriodEndTimes[i];
                }
            }
            
            period /= math.min(averageCount, length);
            period = math.floor(period / toDays * 1000f) / 1000f;
            
            setSpecificOrbitalPeriod.Invoke((float)period);
        }
        else
        {
            setSpecificOrbitalPeriod.Invoke(0f);
        }
    }

    public void SetEarthOrbitalPeriod(float period)
    {
        if (_earthPeriod != period)
        {
            if (period == 0f)
            {
                _earthOrbitalPeriodText.text = "Earth Orbital Period: Nan";
            }
            else
            {
                _earthOrbitalPeriodText.text = $"Earth Orbital Period: {period} Days";
            }
            _earthPeriod = period;
        }
    }
    
    public void SetMoonOrbitalPeriod(float period)
    {
        if (_moonPeriod != period)
        {
            if (period == 0f)
            {
                _moonOrbitalPeriodText.text = "Moon Orbital Period: Nan";
            }
            else
            {
                _moonOrbitalPeriodText.text = $"Moon Orbital Period: {period} Days";
            }
            _moonPeriod = period;
        }
    }
    
    public void SetTime(float days)
    {
        if (_days != days)
        {
            _timeText.text = $"Elapsed: {days} Days";
            _days = days;
        }
    }

    public void Restart()
    {
        ChangeFollowTarget(_followDropdown.value);
    }
    
    private void ChangeFoldState()
    {
        _isSimulationInfoClosed = !_isSimulationInfoClosed;
        _simulationInfoFoldout.SetActive(!_isSimulationInfoClosed);

        _simulationInfoFoldoutArrow.rotation = Quaternion.Euler(0f, 0f, _isSimulationInfoClosed ? 90f : 0f);
    }
    
    private void ChangeFollowTarget(int index)
    {
        _cinemachineCamera.Follow = Sim.Bodies[index].transform;
    }
    
    private void EndGravityConstant(string input)
    {
        var prevValue = Sim.gravityConstantMultiplier;
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
        
        Sim.gravityConstantMultiplier = timeScale / 100f;
        Sim.ValidateValues();
        _gravityConstant.text = (Sim.gravityConstantMultiplier * 100f) + "%";
    }
}
