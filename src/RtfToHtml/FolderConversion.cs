namespace RtfToHtml;

public sealed record FileConversionFailure(string FileName, string Error);

public sealed record FolderConversionResult(int Found, int Converted, IReadOnlyList<FileConversionFailure> Failures);

public static class FolderConversion
{
    public static IReadOnlyList<string> FindRtfFiles(string sourceFolder)
    {
        var sourcePath = Path.GetFullPath(sourceFolder);
        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException("The source folder does not exist.");

        return Directory.EnumerateFiles(sourcePath, "*", SearchOption.TopDirectoryOnly)
            .Where(path => string.Equals(Path.GetExtension(path), ".rtf", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static FolderConversionResult ConvertTopLevel(string sourceFolder, string outputFolder)
    {
        var sourcePath = Path.GetFullPath(sourceFolder);
        var outputPath = Path.GetFullPath(outputFolder);
        if (!Directory.Exists(outputPath))
            throw new DirectoryNotFoundException("The destination folder does not exist.");
        if (string.Equals(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                          outputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                          StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Choose a destination folder different from the source folder.");

        var files = FindRtfFiles(sourcePath);
        var failures = new List<FileConversionFailure>();
        var converted = 0;
        foreach (var file in files)
        {
            try
            {
                FileConversion.Convert(file, outputPath);
                converted++;
            }
            catch (Exception ex)
            {
                failures.Add(new FileConversionFailure(Path.GetFileName(file), ex.Message));
            }
        }

        return new FolderConversionResult(files.Count, converted, failures);
    }
}
