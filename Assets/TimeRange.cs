[System.Serializable]
public class TimeRange
{
    public TimeUnit unit = TimeUnit.Years;
    public double time;

    public double Get()
    {
        switch (unit)
        {
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
    Seconds,
    Hours,
    Days,
    Months,
    Years,
}