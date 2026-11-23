namespace PresenceJiggler;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public int IdleMinutes { get; set; } = 3;
    public bool StartWithWindows { get; set; }
    public bool AllDay { get; set; } = true;
    public TimeOnly ActiveStart { get; set; } = new(9, 0);
    public TimeOnly ActiveEnd { get; set; } = new(18, 0);

    public AppSettings Clone() => new()
    {
        Enabled = Enabled,
        IdleMinutes = IdleMinutes,
        StartWithWindows = StartWithWindows,
        AllDay = AllDay,
        ActiveStart = ActiveStart,
        ActiveEnd = ActiveEnd
    };

    public bool IsActiveAt(TimeOnly localTime)
    {
        if (!Enabled)
            return false;

        if (AllDay)
            return true;

        return ActiveStart <= ActiveEnd
            ? localTime >= ActiveStart && localTime < ActiveEnd
            : localTime >= ActiveStart || localTime < ActiveEnd;
    }

    public void Validate()
    {
        IdleMinutes = Math.Clamp(IdleMinutes, 1, 30);
    }
}
