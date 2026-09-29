using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Aspose.Words;
using Aspose.Words.Saving;
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var root = Path.GetFullPath(args.Length > 0 ? args[0] : @"F:\RtfToHTML");
if (args.Contains("--browser")) { await BrowserCheck.Run(root); return; }
var results = new List<object>();
foreach (var file in Directory.GetFiles(root, "*.rtf").Order())
{
 foreach (var engine in new[] { "RtfPipe", "Aspose" })
 {
  try {
   var dir = Path.Combine(root,"evaluation","output",engine); Directory.CreateDirectory(dir);
   var output = Path.Combine(dir,Path.GetFileNameWithoutExtension(file)+".html");
   var watch = System.Diagnostics.Stopwatch.StartNew();
   if(engine == "RtfPipe") {
    using var input = File.OpenRead(file);
    var html = RtfPipe.Rtf.ToHtml(input);
    File.WriteAllText(output,"<!doctype html><html><head><meta charset=\"utf-8\"></head><body>"+html+"</body></html>",new UTF8Encoding(false));
   } else {
    var doc = new Document(file);
    doc.Save(output,new HtmlSaveOptions { ExportImagesAsBase64 = true, ExportRoundtripInformation = false });
    File.WriteAllText(Path.ChangeExtension(output,".source.txt"),doc.ToString(SaveFormat.Text));
   }
   var s = File.ReadAllText(output);
   var text = System.Net.WebUtility.HtmlDecode(Regex.Replace(s,"<[^>]+>",""));
   File.WriteAllText(Path.ChangeExtension(output,".text.txt"),text);
   var row = new { File=Path.GetFileName(file), Engine=engine, Milliseconds=watch.ElapsedMilliseconds, Bytes=new FileInfo(output).Length,
    Tables=Regex.Matches(s,@"<table\b").Count, Rows=Regex.Matches(s,@"<tr\b").Count, Images=Regex.Matches(s,@"<img\b").Count,
    ImageTypes=Regex.Matches(s,@"data:([^;,]+)").Select(m=>m.Groups[1].Value).Distinct().ToArray(),
    Fields=Regex.Matches(text,@"\[[A-Z]{2}-[^\]]+\]").Select(m=>m.Value).ToArray(),
    PolishCharacters=Regex.Matches(text,"[ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]").Count, ReplacementCharacters=text.Count(c=>c=='\ufffd') };
   results.Add(row); Console.WriteLine(JsonSerializer.Serialize(row));
  } catch(Exception ex) { results.Add(new { File=Path.GetFileName(file),Engine=engine,Error=ex.ToString() }); Console.WriteLine(engine+" "+Path.GetFileName(file)+": "+ex.Message); }
 }
}
File.WriteAllText(Path.Combine(root,"evaluation","results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
