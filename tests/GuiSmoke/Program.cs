using RtfToHtml;
internal static class Program
{
 [STAThread]
 static void Main(string[] args)
 {
  ApplicationConfiguration.Initialize();
  var root=Path.GetFullPath(args[0]);
  var dest=Path.Combine(root,"artifacts","gui-test-"+Guid.NewGuid().ToString("N")+".html");
  using var form=new MainForm();
  form.Show();
  Application.DoEvents();
  var input=(TextBox)form.Controls.Find("InputPath",true).Single();
  var output=(TextBox)form.Controls.Find("OutputPath",true).Single();
  var convert=(Button)form.Controls.Find("Convert",true).Single();
  var open=(Button)form.Controls.Find("OpenHtml",true).Single();
  var status=(Label)form.Controls.Find("Status",true).Single();
  convert.PerformClick();
  if(!status.Text.Contains("Select") || open.Enabled) throw new Exception("Empty validation failed");
  input.Text=Path.Combine(root,"samples","Sobótka_1.rtf"); output.Text=dest;
  convert.PerformClick();
  var until=DateTime.UtcNow.AddSeconds(30);
  while(!convert.Enabled && DateTime.UtcNow<until) { Application.DoEvents(); Thread.Sleep(10); }
  if(!File.Exists(dest)||!open.Enabled||!status.Text.Contains("successfully")) throw new Exception(status.Text);
  Application.DoEvents();
  using(var bitmap=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height)); bitmap.Save(Path.Combine(root,"artifacts","gui-preview.png")); }
  output.Text=Path.ChangeExtension(dest,".txt");
  if(open.Enabled) throw new Exception("Stale output link enabled");
  convert.PerformClick();
  if(!status.Text.Contains("extension")) throw new Exception("Extension validation failed");
  input.Text=Path.Combine(root,"README.md"); output.Text=dest+".html";
  convert.PerformClick();
  until=DateTime.UtcNow.AddSeconds(30);
  while(!convert.Enabled && DateTime.UtcNow<until) { Application.DoEvents(); Thread.Sleep(10); }
  if(File.Exists(output.Text)||open.Enabled||!status.Text.Contains("valid RTF")) throw new Exception("Invalid file validation failed");
  var preferenceDir=Path.Combine(root,"artifacts","preference-test-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(preferenceDir);
  var settingsFile=Path.Combine(root,"artifacts","settings-test-"+Guid.NewGuid().ToString("N"),"settings.json");
  var firstRun=new OutputLocationPreferences(settingsFile);
  if(firstRun.SuggestOutputPath(Path.Combine(root,"samples","first.rtf"))!=Path.Combine(root,"samples","first.html")) throw new Exception("Initial destination fallback failed");
  firstRun.RememberOutputPath(Path.Combine(preferenceDir,"first.html"));
  var secondRun=new OutputLocationPreferences(settingsFile);
  if(secondRun.LastOutputDirectory!=preferenceDir) throw new Exception("Output folder did not persist between app runs");
  if(secondRun.SuggestOutputPath(Path.Combine(root,"samples","second.rtf"))!=Path.Combine(preferenceDir,"second.html")) throw new Exception("Next RTF did not reuse the remembered output folder");
  Directory.Delete(preferenceDir);
  if(secondRun.LastOutputDirectory!=null||secondRun.SuggestOutputPath(Path.Combine(root,"samples","third.rtf"))!=Path.Combine(root,"samples","third.html")) throw new Exception("Unavailable saved folder fallback failed");
  Console.WriteLine("PASS: output folder preference persists across instances and falls back when unavailable");  Console.WriteLine("PASS: GUI empty fields, conversion, result activation, stale result reset, extension validation, invalid RTF");
  form.Close();
 }
}
