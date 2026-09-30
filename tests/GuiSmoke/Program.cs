using RtfToHtml;
internal static class Program
{
 [STAThread]
 static void Main(string[] args)
 {
  ApplicationConfiguration.Initialize();
  var root=Path.GetFullPath(args[0]);
  var destFolder=Path.Combine(root,"artifacts","gui-test-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(destFolder);
  var dest=Path.Combine(destFolder,"Sobótka_1.html");
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
  if(!output.ReadOnly) throw new Exception("Output location must be a folder selector, not an editable file name");
  input.Text=Path.Combine(root,"samples","Sobótka_1.rtf"); output.Text=destFolder;
  convert.PerformClick();
  var until=DateTime.UtcNow.AddSeconds(30);
  while(!convert.Enabled && DateTime.UtcNow<until) { Application.DoEvents(); Thread.Sleep(10); }
  if(!File.Exists(dest)||Directory.GetFiles(destFolder).Length!=1||!open.Enabled||!status.Text.Contains("successfully")) throw new Exception(status.Text);
  Application.DoEvents();
  using(var bitmap=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height)); bitmap.Save(Path.Combine(root,"artifacts","gui-preview.png")); }
  output.Text=Path.Combine(root,"artifacts","does-not-exist");
  convert.PerformClick();
  if(open.Enabled||!status.Text.Contains("does not exist")) throw new Exception("Invalid destination folder validation failed");
  var invalidFolder=Path.Combine(root,"artifacts","invalid-rtf-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(invalidFolder);
  input.Text=Path.Combine(root,"README.md"); output.Text=invalidFolder;
  convert.PerformClick();
  until=DateTime.UtcNow.AddSeconds(30);
  while(!convert.Enabled && DateTime.UtcNow<until) { Application.DoEvents(); Thread.Sleep(10); }
  if(File.Exists(Path.Combine(invalidFolder,"README.html"))||open.Enabled||!status.Text.Contains("valid RTF")) throw new Exception("Invalid file validation failed");
  var preferenceDir=Path.Combine(root,"artifacts","preference-test-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(preferenceDir);
  var settingsFile=Path.Combine(root,"artifacts","settings-test-"+Guid.NewGuid().ToString("N"),"settings.json");
  var firstRun=new OutputLocationPreferences(settingsFile);
  if(firstRun.SuggestOutputDirectory(Path.Combine(root,"samples","first.rtf"))!=Path.Combine(root,"samples")) throw new Exception("Initial destination fallback failed");
  firstRun.RememberOutputDirectory(preferenceDir);
  var secondRun=new OutputLocationPreferences(settingsFile);
  if(secondRun.LastOutputDirectory!=preferenceDir) throw new Exception("Output folder did not persist between app runs");
  if(secondRun.SuggestOutputDirectory(Path.Combine(root,"samples","second.rtf"))!=preferenceDir) throw new Exception("Next RTF did not reuse the remembered output folder");
  Directory.Delete(preferenceDir);
  if(secondRun.LastOutputDirectory!=null||secondRun.SuggestOutputDirectory(Path.Combine(root,"samples","third.rtf"))!=Path.Combine(root,"samples")) throw new Exception("Unavailable saved folder fallback failed");
  Console.WriteLine("PASS: GUI selects an output folder and creates input-basename HTML");
  Console.WriteLine("PASS: invalid destination and RTF validation");
  Console.WriteLine("PASS: output folder preference persists across instances and falls back when unavailable");
  form.Close();
 }
}
