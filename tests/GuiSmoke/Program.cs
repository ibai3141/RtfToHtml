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
  var folderMode=(RadioButton)form.Controls.Find("FolderMode",true).Single();
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
  var batchSource=Path.Combine(root,"artifacts","batch-source-"+Guid.NewGuid().ToString("N"));
  var batchDestination=Path.Combine(root,"artifacts","batch-destination-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(batchSource); Directory.CreateDirectory(batchDestination);
  File.Copy(Path.Combine(root,"samples","Sobótka_1.rtf"),Path.Combine(batchSource,"Batch.rtf"));
  File.Copy(Path.Combine(root,"samples","Sobótka_1.rtf"),Path.Combine(batchSource,"Uppercase.RTF"));
  File.WriteAllText(Path.Combine(batchSource,"ignored.txt"),"not RTF");
  Directory.CreateDirectory(Path.Combine(batchSource,"nested"));
  File.Copy(Path.Combine(root,"samples","Sobótka_1.rtf"),Path.Combine(batchSource,"nested","nested.rtf"));
  var batch=FolderConversion.ConvertTopLevel(batchSource,batchDestination);
  if(batch.Found!=2||batch.Converted!=2||batch.Failures.Count!=0||!File.Exists(Path.Combine(batchDestination,"Batch.html"))||!File.Exists(Path.Combine(batchDestination,"Uppercase.html"))||File.Exists(Path.Combine(batchDestination,"nested.html"))) throw new Exception("Top-level folder conversion failed");
  folderMode.Checked=true;
  var guiBatchDestination=Path.Combine(root,"artifacts","batch-gui-destination-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(guiBatchDestination);
  input.Text=batchSource; output.Text=guiBatchDestination;
  convert.PerformClick();
  until=DateTime.UtcNow.AddSeconds(30);
  while(!convert.Enabled && DateTime.UtcNow<until) { Application.DoEvents(); Thread.Sleep(10); }
  if(!status.Text.Contains("Converted 2 of 2")||open.Enabled||!File.Exists(Path.Combine(guiBatchDestination,"Batch.html"))||!File.Exists(Path.Combine(guiBatchDestination,"Uppercase.html"))) throw new Exception("GUI folder conversion failed: "+status.Text);
  Console.WriteLine("PASS: folder mode converts only top-level RTF files and ignores nested folders and other files");
  var preferenceDir=Path.Combine(root,"artifacts","preference-test-"+Guid.NewGuid().ToString("N"));
  var inputPreferenceDir=Path.Combine(root,"artifacts","input-preference-test-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(preferenceDir);
  Directory.CreateDirectory(inputPreferenceDir);
  var settingsFile=Path.Combine(root,"artifacts","settings-test-"+Guid.NewGuid().ToString("N"),"settings.json");
  var firstRun=new OutputLocationPreferences(settingsFile);
  if(firstRun.SuggestOutputDirectory(Path.Combine(root,"samples","first.rtf"))!=Path.Combine(root,"samples")) throw new Exception("Initial destination fallback failed");
  firstRun.RememberInputFile(Path.Combine(inputPreferenceDir,"previous.rtf"));
  firstRun.RememberOutputDirectory(preferenceDir);
  var secondRun=new OutputLocationPreferences(settingsFile);
  if(secondRun.LastOutputDirectory!=preferenceDir) throw new Exception("Output folder did not persist between app runs");
  if(secondRun.LastInputDirectory!=inputPreferenceDir||secondRun.SuggestInputDirectory()!=inputPreferenceDir) throw new Exception("Input folder did not persist between app runs");
  if(secondRun.SuggestOutputDirectory(Path.Combine(root,"samples","second.rtf"))!=preferenceDir) throw new Exception("Next RTF did not reuse the remembered output folder");
  Directory.Delete(preferenceDir);
  if(secondRun.LastOutputDirectory!=null||secondRun.SuggestOutputDirectory(Path.Combine(root,"samples","third.rtf"))!=Path.Combine(root,"samples")) throw new Exception("Unavailable output folder fallback failed");
  if(secondRun.LastInputDirectory!=inputPreferenceDir) throw new Exception("Saving the output folder erased the input folder preference");
  Directory.Delete(inputPreferenceDir);
  if(secondRun.LastInputDirectory!=null||secondRun.SuggestInputDirectory()!=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) throw new Exception("Unavailable input folder fallback failed");
  Console.WriteLine("PASS: input and output folder preferences persist independently and fall back when unavailable");
  form.Close();
 }
}
