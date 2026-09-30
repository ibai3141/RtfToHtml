using System.Runtime.InteropServices;
namespace RtfToHtml;
internal static class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint processId);
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
            return 0;
        }
        if (!Console.IsErrorRedirected) AttachConsole(unchecked((uint)-1));
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: RtfToHTML.exe <input.rtf> <output_folder>");
            return 2;
        }
        try { FileConversion.Convert(args[0], args[1]); return 0; }
        catch (Exception ex) { Console.Error.WriteLine("Conversion failed: " + ex.Message); return 1; }
    }
}
