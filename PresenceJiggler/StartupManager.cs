using Microsoft.Win32;

namespace PresenceJiggler;

internal static class StartupManager
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PresenceJiggler";

    public static bool IsEnabled()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false);
        return runKey?.GetValue(ValueName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            using var runKey = Registry.CurrentUser.CreateSubKey(RegistryPath);
            string executable = Environment.ProcessPath
                ?? throw new InvalidOperationException("No se pudo determinar la ruta del ejecutable.");
            runKey.SetValue(ValueName, $"\"{executable}\"", RegistryValueKind.String);
        }
        else
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
            runKey?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
