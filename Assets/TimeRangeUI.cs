using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TimeRangeUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _timeValueInput;
    [SerializeField] private TMP_Dropdown _unitDropdown;

    private TimeRange _boundTimeRange;
    private string[] _unitNames;
    
    public void Initialize(TimeRange boundTimeRange)
    {
        _boundTimeRange = boundTimeRange;
        
        _timeValueInput.text = _boundTimeRange.time.ToString();
        _timeValueInput.onEndEdit.AddListener(EndEdit);

        _unitNames = Enum.GetNames(typeof(TimeUnit));
        _unitDropdown.ClearOptions();
        _unitDropdown.AddOptions(_unitNames.ToList());
        _unitDropdown.onValueChanged.AddListener(ChangeUnit);
        _unitDropdown.value = (int)_boundTimeRange.unit;
    }

    private void ChangeUnit(int index)
    {
        _boundTimeRange.unit = Enum.Parse<TimeUnit>(_unitNames[index]);
    }

    private void EndEdit(string input)
    {
        var prevValue = _boundTimeRange.time;
        int timeScale;
        try
        {
            timeScale = int.Parse(input);
        }
        catch (Exception)
        {
            _timeValueInput.text = prevValue.ToString();
            return;
        }
        
        _boundTimeRange.time = timeScale;
        SimulationManager.Instance.ValidateValues();
        _timeValueInput.text = _boundTimeRange.time.ToString();
    }
}
