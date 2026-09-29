using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using RtfToHtml;

var root = Path.GetFullPath(args.Length == 0 ? "." : args[0]);
var output = Path.Combine(root, "artifacts", "alignment");
Directory.CreateDirectory(output);
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1000, Height = 1400 } });
await page.RouteAsync("**/*", r => r.Request.Url.StartsWith("http") ? r.AbortAsync() : r.ContinueAsync());
var checks = new List<string>();
void Check(bool condition, string label)
{
    if (!condition) throw new Exception("FAIL: " + label);
    checks.Add(label);
}
string ConvertRtf(string rtf)
{
    using var stream = new MemoryStream(Encoding.ASCII.GetBytes(rtf));
    return HtmlConverter.Convert(stream);
}
const string tableBounds = """
() => [...document.querySelectorAll('table')].map(t => {
 const r=t.getBoundingClientRect(), p=t.parentElement.getBoundingClientRect();
 return {left:r.left-p.left,right:p.right-r.right,width:r.width};
})
""";

foreach (var alignment in new[] { "ql", "qc", "qr" })
{
    var html = ConvertRtf(@"{\rtf1\ansi\trowd\tr" + alignment + @"\trleft720\cellx3600\pard\intbl\ql text\cell\row\pard\par}");
    await page.SetContentAsync(html);
    var bounds = (await page.EvaluateAsync<JsonElement>(tableBounds))[0];
    var left = bounds.GetProperty("left").GetDouble();
    var right = bounds.GetProperty("right").GetDouble();
    Check(alignment == "ql" ? Math.Abs(left - 48) < 2 : alignment == "qc" ? Math.Abs(left - right) < 2 : Math.Abs(right) < 2,
        "table " + alignment + " geometry (trleft does not override center/right)");
    Check(await page.Locator("td").First.EvaluateAsync<string>("e => getComputedStyle(e).textAlign") == "left", "cell text stays left: " + alignment);
}

var header = ConvertRtf(@"{\rtf1\ansi\trowd\trqc\trhdr\cellx3000\pard\intbl H\cell\row\trowd\trqc\cellx3000\pard\intbl B\cell\row\pard\par}");
await page.SetContentAsync(header);
Check(await page.Locator("thead").CountAsync() == 1, "header row retained");
var headerBounds = (await page.EvaluateAsync<JsonElement>(tableBounds))[0];
Check(Math.Abs(headerBounds.GetProperty("left").GetDouble() - headerBounds.GetProperty("right").GetDouble()) < 2, "table with header centered");

// The same alignment on parent and child must be emitted twice: margins do not inherit.
var nested = ConvertRtf(@"{\rtf1\ansi\trowd\trqr\cellx9000\pard\intbl\itap1 outer\par {\trowd\trqr\cellx3000\pard\intbl\itap2 inner\nestcell\nestrow}\pard\intbl\itap1\cell\row\pard\par}");
await page.SetContentAsync(nested);
Check(await page.Locator("table table").CountAsync() == 1, "nested table retained");
Check(await page.Locator("table").EvaluateAllAsync<bool>("ts => ts.every(t => t.style.marginLeft === 'auto' && t.style.marginRight === '0px')"), "nested right margins explicitly emitted");

foreach (var file in Directory.GetFiles(Path.Combine(root, "samples"), "*.rtf").Order())
{
    var name = Path.GetFileNameWithoutExtension(file);
    using var source = File.OpenRead(file);
    var html = HtmlConverter.Convert(source);
    var path = Path.Combine(output, name + ".html");
    File.WriteAllText(path, html, new UTF8Encoding(false));
    var baseline = Path.Combine(root, "evaluation", "output", "RtfPipePng", name + ".html");
    await page.GotoAsync(new Uri(baseline).AbsoluteUri);
    var oldText = await page.Locator("body").InnerTextAsync();
    var oldTables = await page.Locator("table").CountAsync();
    var oldRows = await page.Locator("tr").CountAsync();
    var oldImages = await page.Locator("img").CountAsync();
    await page.GotoAsync(new Uri(path).AbsoluteUri);
    Check(Regex.Replace(oldText, @"\s", "") == Regex.Replace(await page.Locator("body").InnerTextAsync(), @"\s", ""), name + ": visible text unchanged ignoring whitespace");
    Check(await page.Locator("table").CountAsync() == oldTables && await page.Locator("tr").CountAsync() == oldRows, name + ": tables and rows unchanged");
    Check(await page.Locator("img").CountAsync() == oldImages && await page.EvaluateAsync<bool>("[...document.images].every(i=>i.complete && i.naturalWidth>0 && i.src.startsWith('data:image/png'))"), name + ": images embedded and loaded");
    if (name == "Sobótka_1")
    {
        foreach (var media in new[] { Media.Screen, Media.Print })
        {
            await page.EmulateMediaAsync(new() { Media = media });
            var rects = await page.EvaluateAsync<JsonElement>(tableBounds);
            Check(Math.Abs(rects[0].GetProperty("left").GetDouble() - rects[0].GetProperty("right").GetDouble()) < 2, "Sobótka_1: header centered " + media);
            Check(Math.Abs(rects[1].GetProperty("right").GetDouble()) < 2, "Sobótka_1: client table right " + media);
            Check(Math.Abs(rects[2].GetProperty("left").GetDouble() - rects[2].GetProperty("right").GetDouble()) < 2, "Sobótka_1: third table centered " + media);
        }
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
    }
    await page.ScreenshotAsync(new() { Path = Path.ChangeExtension(path, ".png"), FullPage = true });
    await page.PdfAsync(new() { Path = Path.ChangeExtension(path, ".pdf"), Format = "A4", PrintBackground = true });
    Console.WriteLine("PASS " + name);
}
File.WriteAllText(Path.Combine(output, "checks.json"), JsonSerializer.Serialize(checks, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS: {checks.Count} checks");
