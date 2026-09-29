using System.Text;
namespace RtfToHtml;
public static class FileConversion
{
    public static void Convert(string input, string output)
    {
        var inputPath = Path.GetFullPath(input);
        var outputPath = Path.GetFullPath(output);
        if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The input and output files must be different.");
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
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
