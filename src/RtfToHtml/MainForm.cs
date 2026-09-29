using System.Diagnostics;
namespace RtfToHtml;
public sealed class MainForm : Form
{
    private readonly TextBox input = new() { Name = "InputPath", Dock = DockStyle.Fill, AccessibleName = "Archivo RTF" };
    private readonly TextBox output = new() { Name = "OutputPath", Dock = DockStyle.Fill, AccessibleName = "Archivo HTML de destino" };
    private readonly Button browse = new() { Text = "Seleccionar…", AutoSize = true };
    private readonly Button save = new() { Text = "Guardar como…", AutoSize = true };
    private readonly Button convert = new() { Name = "Convert", Text = "Convertir a HTML", AutoSize = true, Padding = new Padding(18, 8, 18, 8) };
    private readonly Button open = new() { Name = "OpenHtml", Text = "Abrir HTML", AutoSize = true, Enabled = false, Padding = new Padding(12, 8, 12, 8) };
    private readonly Label status = new() { Name = "Status", Text = "Selecciona un documento para empezar.", AutoSize = true, MaximumSize = new Size(660, 0), Dock = DockStyle.Fill };
    private readonly ProgressBar progress = new() { Dock = DockStyle.Fill, Height = 5, Style = ProgressBarStyle.Marquee, Visible = false };
    private bool busy;
    private string? generatedFile;
    public MainForm()
    {
        Text = "RTF a HTML";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(760, 490);
        MinimumSize = new Size(720, 530);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(245, 247, 251);
        ForeColor = Color.FromArgb(30, 41, 59);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30), ColumnCount = 1, RowCount = 9 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 9; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles[8] = new RowStyle(SizeType.Percent, 100);
        layout.Controls.Add(new Label { Text = "De RTF a HTML", Font = new Font("Segoe UI", 24, FontStyle.Bold), AutoSize = true }, 0, 0);
        layout.Controls.Add(new Label { Text = "Elige tu documento y dónde quieres guardar el resultado.", AutoSize = true, Margin = new Padding(3, 4, 3, 24) }, 0, 1);
        layout.Controls.Add(new Label { Text = "1   Documento RTF", AutoSize = true }, 0, 2);
        layout.Controls.Add(PathRow(input, browse), 0, 3);
        layout.Controls.Add(new Label { Text = "2   Archivo HTML de destino", AutoSize = true, Margin = new Padding(3, 18, 3, 3) }, 0, 4);
        layout.Controls.Add(PathRow(output, save), 0, 5);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 22, 0, 12) };
        convert.BackColor = Color.FromArgb(37, 99, 235);
        convert.ForeColor = Color.White;
        convert.FlatStyle = FlatStyle.Flat;
        convert.FlatAppearance.BorderSize = 0;
        actions.Controls.AddRange([convert, open]);
        layout.Controls.Add(actions, 0, 6);
        layout.Controls.Add(progress, 0, 7);
        layout.Controls.Add(status, 0, 8);
        Controls.Add(layout);
        AcceptButton = convert;
        browse.Click += (_, _) => SelectInput();
        save.Click += (_, _) => SelectOutput();
        convert.Click += async (_, _) => await ConvertAsync();
        open.Click += (_, _) => OpenResult();
        input.TextChanged += (_, _) => ResetResult();
        output.TextChanged += (_, _) => ResetResult();
        FormClosing += (_, e) => { if (busy) e.Cancel = true; };
    }
    private static TableLayoutPanel PathRow(TextBox field, Button button)
    {
        var row = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Fill, AutoSize = true, Margin = Padding.Empty };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        field.Margin = new Padding(3, 6, 10, 3);
        row.Controls.Add(field, 0, 0);
        row.Controls.Add(button, 1, 0);
        return row;
    }
    private void SelectInput()
    {
        using var dialog = new OpenFileDialog { Title = "Seleccionar documento RTF", Filter = "Documento RTF (*.rtf)|*.rtf", CheckFileExists = true, Multiselect = false };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        input.Text = dialog.FileName;
        output.Text = Path.ChangeExtension(dialog.FileName, ".html");
    }
    private void SelectOutput()
    {
        using var dialog = new SaveFileDialog { Title = "Guardar HTML como", Filter = "Documento HTML (*.html)|*.html", DefaultExt = "html", AddExtension = true, OverwritePrompt = false };
        try
        {
            if (!string.IsNullOrWhiteSpace(output.Text))
            {
                var path = Path.GetFullPath(output.Text.Trim());
                dialog.FileName = Path.GetFileName(path);
                if (Directory.Exists(Path.GetDirectoryName(path))) dialog.InitialDirectory = Path.GetDirectoryName(path);
            }
        }
        catch (ArgumentException) { }
        if (dialog.ShowDialog(this) == DialogResult.OK) output.Text = dialog.FileName;
    }
    private void ResetResult()
    {
        generatedFile = null;
        open.Enabled = false;
        status.ForeColor = ForeColor;
        status.Text = "Pulsa Convertir para generar el archivo HTML.";
    }
    private async Task ConvertAsync()
    {
        if (busy) return;
        try
        {
            if (string.IsNullOrWhiteSpace(input.Text) || string.IsNullOrWhiteSpace(output.Text))
                throw new ArgumentException("Selecciona el documento RTF y la ruta del archivo HTML.");
            var source = Path.GetFullPath(input.Text.Trim());
            var destination = Path.GetFullPath(output.Text.Trim());
            if (!string.Equals(Path.GetExtension(destination), ".html", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Path.GetExtension(destination), ".htm", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("El archivo de destino debe tener extensión .html o .htm.");
            if (File.Exists(destination) && MessageBox.Show(this, "Ya existe el archivo HTML. ¿Quieres reemplazarlo?", "Confirmar reemplazo", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            ResetResult();
            SetBusy(true);
            status.Text = "Convirtiendo el documento…";
            await Task.Run(() => FileConversion.Convert(source, destination));
            generatedFile = destination;
            open.Enabled = true;
            status.ForeColor = Color.FromArgb(21, 128, 61);
            status.Text = "HTML creado correctamente. Puedes abrirlo con el botón Abrir HTML.";
        }
        catch (Exception ex)
        {
            generatedFile = null;
            open.Enabled = false;
            status.ForeColor = Color.FromArgb(185, 28, 28);
            status.Text = "No se pudo convertir: " + ex.Message;
        }
        finally { SetBusy(false); }
    }
    private void SetBusy(bool value)
    {
        busy = value;
        input.Enabled = output.Enabled = browse.Enabled = save.Enabled = convert.Enabled = !value;
        progress.Visible = value;
        UseWaitCursor = value;
    }
    private void OpenResult()
    {
        try
        {
            if (generatedFile != null) Process.Start(new ProcessStartInfo(generatedFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            status.ForeColor = Color.FromArgb(185, 28, 28);
            status.Text = "El HTML se ha guardado, pero no se pudo abrir: " + ex.Message;
        }
    }
}
