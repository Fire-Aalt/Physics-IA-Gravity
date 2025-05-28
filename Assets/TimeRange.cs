using Unity.Mathematics;

[System.Serializable]
public class TimeRange
{
    public TimeUnit unit = TimeUnit.Days;
    public int time;

    public double Get()
    {
        time = math.max(1, time);
        switch (unit)
        {
            case TimeUnit.Milliseconds:
                return time / 1000.0;
            case TimeUnit.Seconds:
                return time;
            case TimeUnit.Hours:
                return time * 60 * 60;
            case TimeUnit.Days:
                return time * 60 * 60 * 24;
            case TimeUnit.Months:
                return time * 60 * 60 * 24 * 30;
            case TimeUnit.Years:
                return time * 60 * 60 * 24 * 365;
            default:
                throw new System.NotImplementedException();
        }
    }
}

public enum TimeUnit
{
    Milliseconds,
    Seconds,
    Hours,
    Days,
    Months,
    Years,
}