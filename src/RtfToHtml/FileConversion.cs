using System.Text;
namespace RtfToHtml;
public static class FileConversion
{
    public static string Convert(string input, string outputDirectory)
    {
        var inputPath = Path.GetFullPath(input);
        var outputFolder = Path.GetFullPath(outputDirectory);
        if (!Directory.Exists(outputFolder))
            throw new DirectoryNotFoundException("The output folder does not exist.");
        var outputPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(inputPath) + ".html");
        string html;
        using (var source = File.OpenRead(inputPath))
        {
            Span<byte> signature = stackalloc byte[5];
            if (source.Read(signature) != 5 || !signature.SequenceEqual("{\\rtf"u8))
                throw new InvalidDataException("The selected file is not a valid RTF document.");
            source.Position = 0;
            html = HtmlConverter.Convert(source);
        }
        var temporary = Path.Combine(Path.GetDirectoryName(outputPath)!, ".rtftohtml-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporary, html, new UTF8Encoding(false));
            File.Move(temporary, outputPath, overwrite: true);
            return outputPath;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
