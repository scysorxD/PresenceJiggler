using System.Globalization;
using System.Text.Json;

namespace PresenceJiggler;

internal sealed record ActivityHistory(
    DateOnly FirstDay, DateOnly LastDay, IReadOnlyList<DateTimeOffset> Movements);

internal sealed class ActivityHistoryStore(string directoryPath)
{
    private const int RetentionDays = 30;
    private const string DayFormat = "yyyy-MM-dd";
    private const string Extension = ".log";
    private static readonly JsonSerializerOptions ExportOptions = new() { WriteIndented = true };

    public ActivityHistoryStore() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PresenceJiggler", "history"))
    {
    }

    public void Record(DateTimeOffset occurredAt)
    {
        var day = LocalDay(occurredAt);
        string path = PathFor(day);
        if (!File.Exists(path))
            Prune(day, reserveToday: true);

        Directory.CreateDirectory(directoryPath);
        File.AppendAllText(path,
            occurredAt.ToString("O", CultureInfo.InvariantCulture) + Environment.NewLine);
    }

    public void Prune(DateOnly today) => Prune(today, reserveToday: false);

    private void Prune(DateOnly today, bool reserveToday)
    {
        var firstDay = FirstRetainedDay(today);
        var files = HistoryFiles().ToArray();
        foreach (var (day, path) in files)
        {
            if (day < firstDay)
                File.Delete(path);
        }

        // The local date can move backward after midnight when the time zone changes.
        // Preserve those newer records while there is room in the 30-day store.
        int usedSlots = files.Count(file => file.Day >= firstDay && file.Day <= today);
        if (reserveToday && !files.Any(file => file.Day == today))
            usedSlots++;

        foreach (var file in files.Where(file => file.Day > today).OrderBy(file => file.Day)
                     .Skip(RetentionDays - usedSlots))
            File.Delete(file.Path);
    }

    public ActivityHistory Load(DateOnly today)
    {
        var firstDay = FirstRetainedDay(today);
        var movements = new List<DateTimeOffset>();
        foreach (var (day, path) in HistoryFiles())
        {
            if (day < firstDay || day > today)
                continue;

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                while (reader.ReadLine() is { } line)
                {
                    if (DateTimeOffset.TryParseExact(line, "O", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var occurredAt) && LocalDay(occurredAt) == day)
                    {
                        movements.Add(occurredAt);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // Another session may have removed an expired file after enumeration.
            }
        }

        movements.Sort();
        return new ActivityHistory(firstDay, today, movements);
    }

    public static void Export(string path, ActivityHistory history, DateOnly? selectedDay, DateTimeOffset exportedAt)
    {
        var data = new
        {
            schemaVersion = 1,
            exportedAt,
            firstDay = selectedDay ?? history.FirstDay,
            lastDay = selectedDay ?? history.LastDay,
            movements = history.Movements
                .Where(time => selectedDay is null || LocalDay(time) == selectedDay)
                .Select(time => new { occurredAt = time })
        };

        string temporaryPath = Path.Combine(Path.GetDirectoryName(path)!, $".{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = File.Open(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                JsonSerializer.Serialize(stream, data, ExportOptions);

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    public static DateOnly LocalDay(DateTimeOffset time) => DateOnly.FromDateTime(time.DateTime);

    private static DateOnly FirstRetainedDay(DateOnly today) => today.AddDays(1 - RetentionDays);

    private IEnumerable<(DateOnly Day, string Path)> HistoryFiles()
    {
        string[] files;
        try
        {
            files = Directory.GetFiles(directoryPath, "*" + Extension);
        }
        catch (DirectoryNotFoundException)
        {
            yield break;
        }

        foreach (string path in files)
        {
            if (DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(path), DayFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            {
                yield return (day, path);
            }
        }
    }

    private string PathFor(DateOnly day) =>
        Path.Combine(directoryPath, day.ToString(DayFormat, CultureInfo.InvariantCulture) + Extension);
}
