using System.Text;
using RtfToHtml;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: RtfToHTML.exe <input.rtf> <output.html>");
    return 2;
}

string? temporary = null;
try
{
    var inputPath = Path.GetFullPath(args[0]);
    var outputPath = Path.GetFullPath(args[1]);
    if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Input and output paths must be different.");
    string html;
    using (var input = File.OpenRead(inputPath))
    {
        Span<byte> signature = stackalloc byte[5];
        if (input.Read(signature) != 5 || !signature.SequenceEqual("{\\rtf"u8))
            throw new InvalidDataException("The input is not an RTF document.");
        input.Position = 0;
        html = HtmlConverter.Convert(input);
    }
    temporary = Path.Combine(Path.GetDirectoryName(outputPath)!, ".rtftohtml-" + Guid.NewGuid().ToString("N") + ".tmp");
    File.WriteAllText(temporary, html, new UTF8Encoding(false));
    File.Move(temporary, outputPath, overwrite: true);
    temporary = null;
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Conversion failed: " + ex.Message);
    return 1;
}
finally
{
    if (temporary != null && File.Exists(temporary))
        File.Delete(temporary);
}
