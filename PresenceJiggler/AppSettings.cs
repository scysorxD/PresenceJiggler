namespace PresenceJiggler;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public int IdleMinutes { get; set; } = 3;
    public bool StartWithWindows { get; set; }

    public AppSettings Clone() => new()
    {
        Enabled = Enabled,
        IdleMinutes = IdleMinutes,
        StartWithWindows = StartWithWindows
    };

    public void Validate()
    {
        IdleMinutes = Math.Clamp(IdleMinutes, 1, 30);
    }
}
