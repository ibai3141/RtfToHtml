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
            var directory = ReadSettings()?.LastOutputDirectory;
            return directory is not null && Directory.Exists(directory) ? directory : null;
        }
    }

    public string? LastInputDirectory
    {
        get
        {
            var directory = ReadSettings()?.LastInputDirectory;
            return directory is not null && Directory.Exists(directory) ? directory : null;
        }
    }

    public string SuggestInputDirectory() => LastInputDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public string SuggestOutputDirectory(string inputPath)
    {
        return LastOutputDirectory ?? Path.GetDirectoryName(Path.GetFullPath(inputPath))!;
    }

    public void RememberOutputDirectory(string outputDirectory)
    {
        WriteSettings(Path.GetFullPath(outputDirectory), LastInputDirectory);
    }

    public void RememberInputFile(string inputPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(inputPath));
            if (directory is not null && Directory.Exists(directory))
                WriteSettings(LastOutputDirectory, directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Remembering a folder is a convenience; it must not block file selection.
        }
    }

    private Settings? ReadSettings()
    {
        try
        {
            return File.Exists(settingsPath)
                ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath))
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private void WriteSettings(string? outputDirectory, string? inputDirectory)
    {
        try
        {
            var settingsDirectory = Path.GetDirectoryName(settingsPath)!;
            Directory.CreateDirectory(settingsDirectory);
            var temporaryPath = settingsPath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new Settings(outputDirectory, inputDirectory)));
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

    private sealed record Settings(string? LastOutputDirectory, string? LastInputDirectory);
}
