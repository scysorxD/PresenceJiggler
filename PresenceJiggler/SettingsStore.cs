using System.Text.Json;

namespace PresenceJiggler;

internal static class SettingsStore
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PresenceJiggler");

    private static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath))
                ?? new AppSettings();
            settings.Validate();
            return settings;
        }
        catch (Exception)
        {
            // Si el archivo fue dañado, usar valores seguros sin impedir que arranque la app.
            return new AppSettings { Enabled = false };
        }
    }

    public static void Save(AppSettings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(DirectoryPath);
        string tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(settings,
            new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tempPath, FilePath, overwrite: true);
    }
}
