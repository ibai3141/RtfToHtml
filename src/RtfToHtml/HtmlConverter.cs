using System.Drawing.Imaging;
using System.Text;
using RtfPipe;
using RtfPipe.Tokens;

namespace RtfToHtml;

public static class HtmlConverter
{
    public static string Convert(Stream input)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var fragment = Rtf.ToHtml(input, new RtfHtmlSettings { ImageUriGetter = ImageDataUri });
        return "<!doctype html><html><head><meta charset=\"utf-8\"></head><body>"
            + fragment + "</body></html>";
    }

    private static string ImageDataUri(Picture picture)
    {
        if (picture.Type is not WmMetafile && picture.Type is not EmfBlip)
            return "data:" + picture.MimeType() + ";base64," + System.Convert.ToBase64String(picture.Bytes);

        using var source = new MemoryStream(picture.Bytes);
        using var image = Image.FromStream(source);
        // Preserve the vector canvas; the HTML retains the RTF display size.
        using var bitmap = new Bitmap(image.Width, image.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
            graphics.DrawImage(image, 0, 0, bitmap.Width, bitmap.Height);
        }
        using var output = new MemoryStream();
        bitmap.Save(output, ImageFormat.Png);
        return "data:image/png;base64," + System.Convert.ToBase64String(output.ToArray());
    }
}
