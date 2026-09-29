using Microsoft.Playwright;
using System.Text.Json;
public static class BrowserCheck {
 public static async Task Run(string root) {
 using var pw=await Playwright.CreateAsync();
 await using var browser=await pw.Chromium.LaunchAsync(new(){Channel="msedge",Headless=true});
 var page=await browser.NewPageAsync(new(){ViewportSize=new(){Width=1000,Height=1400}});
 await page.RouteAsync("**/*", route => route.Request.Url.StartsWith("http") ? route.AbortAsync() : route.ContinueAsync());
 var results=new List<object>();
 foreach(var file in Directory.GetFiles(Path.Combine(root,"evaluation","output"),"*.html",SearchOption.AllDirectories).Append(Path.Combine(root,"Sobótka_1.html"))) {
 await page.GotoAsync(new Uri(file).AbsoluteUri);
 await page.EvaluateAsync("document.fonts.ready");
 var metrics=await page.EvaluateAsync<JsonElement>("""
 () => ({images:[...document.images].map(x=>({loaded:x.complete&&x.naturalWidth>0,width:x.naturalWidth,height:x.naturalHeight})),tables:document.querySelectorAll('table').length,bodyWidth:document.body.scrollWidth, viewport:innerWidth,text:document.body.innerText})
 """);
 results.Add(new{File=file,Metrics=metrics});
 var artifact = Path.GetDirectoryName(file) == root ? Path.Combine(root,"evaluation","reference",Path.GetFileName(file)) : file;
 Directory.CreateDirectory(Path.GetDirectoryName(artifact)!);
 await page.ScreenshotAsync(new(){Path=Path.ChangeExtension(artifact,".png"),FullPage=true});
 await page.PdfAsync(new(){Path=Path.ChangeExtension(artifact,".pdf"),Format="A4",PrintBackground=true,PreferCSSPageSize=true});
 Console.WriteLine(Path.GetFileName(Path.GetDirectoryName(file))+"/"+Path.GetFileName(file)+" images: "+metrics.GetProperty("images"));
 }
 File.WriteAllText(Path.Combine(root,"evaluation","browser-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
 }
}
