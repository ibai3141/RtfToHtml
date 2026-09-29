using System.Text;
namespace RtfToHtml;
public static class FileConversion
{
    public static void Convert(string input, string output)
    {
        var inputPath = Path.GetFullPath(input);
        var outputPath = Path.GetFullPath(output);
        if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El archivo de entrada y el de salida deben ser diferentes.");
        string html;
        using (var source = File.OpenRead(inputPath))
        {
            Span<byte> signature = stackalloc byte[5];
            if (source.Read(signature) != 5 || !signature.SequenceEqual("{\\rtf"u8))
                throw new InvalidDataException("El archivo seleccionado no es un documento RTF válido.");
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
