using System.Text.Json;

namespace RtfToHtml;

public sealed class OutputLocationPreferences
{
    private readonly string settingsPath;

    public OutputLocationPreferences(string? settingsPath = null)
    {
        this.settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RtfToHTML", "settings.json");
    }

    public string? LastOutputDirectory
    {
        get
        {
            try
            {
                if (!File.Exists(settingsPath)) return null;
                var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath));
                return settings?.LastOutputDirectory is { } directory && Directory.Exists(directory)
                    ? directory
                    : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                return null;
            }
        }
    }

    public string SuggestOutputDirectory(string inputPath)
    {
        return LastOutputDirectory ?? Path.GetDirectoryName(Path.GetFullPath(inputPath))!;
    }

    public void RememberOutputDirectory(string outputDirectory)
    {
        try
        {
            var directory = Path.GetFullPath(outputDirectory);
            if (!Directory.Exists(directory)) return;
            var settingsDirectory = Path.GetDirectoryName(settingsPath)!;
            Directory.CreateDirectory(settingsDirectory);
            var temporaryPath = settingsPath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new Settings(directory)));
                File.Move(temporaryPath, settingsPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Remembering a folder is a convenience; it must not block conversion.
        }
    }

    private sealed record Settings(string LastOutputDirectory);
}
