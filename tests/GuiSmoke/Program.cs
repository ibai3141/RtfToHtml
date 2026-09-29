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
  Console.WriteLine("PASS: GUI empty fields, conversion, result activation, stale result reset, extension validation, invalid RTF");
  form.Close();
 }
}
