using System.Diagnostics;
namespace RtfToHtml;
public sealed class MainForm : Form
{
    private readonly OutputLocationPreferences preferences = new();
    private readonly TextBox input = new() { Name = "InputPath", Dock = DockStyle.Fill, AccessibleName = "RTF file" };
    private readonly TextBox output = new() { Name = "OutputPath", Dock = DockStyle.Fill, AccessibleName = "Output folder", ReadOnly = true, TabStop = false };
    private readonly Button browse = new() { Text = "Browse...", AutoSize = true };
    private readonly Button save = new() { Text = "Choose folder...", AutoSize = true };
    private readonly Button convert = new() { Name = "Convert", Text = "Convert to HTML", AutoSize = true, Padding = new Padding(18, 8, 18, 8) };
    private readonly Button open = new() { Name = "OpenHtml", Text = "Open HTML", AutoSize = true, Enabled = false, Padding = new Padding(12, 8, 12, 8) };
    private readonly Label status = new() { Name = "Status", Text = "Select a document to get started.", AutoSize = true, MaximumSize = new Size(660, 0), Dock = DockStyle.Fill };
    private readonly ProgressBar progress = new() { Dock = DockStyle.Fill, Height = 5, Style = ProgressBarStyle.Marquee, Visible = false };
    private bool busy;
    private string? generatedFile;
    public MainForm()
    {
        Text = "RTF to HTML";
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
        layout.Controls.Add(new Label { Text = "RTF to HTML", Font = new Font("Segoe UI", 24, FontStyle.Bold), AutoSize = true }, 0, 0);
        layout.Controls.Add(new Label { Text = "Choose your document and where to save the result.", AutoSize = true, Margin = new Padding(3, 4, 3, 24) }, 0, 1);
        layout.Controls.Add(new Label { Text = "1   RTF document", AutoSize = true }, 0, 2);
        layout.Controls.Add(PathRow(input, browse), 0, 3);
        layout.Controls.Add(new Label { Text = "2   Destination folder", AutoSize = true, Margin = new Padding(3, 18, 3, 3) }, 0, 4);
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
        using var dialog = new OpenFileDialog { Title = "Select an RTF document", Filter = "RTF document (*.rtf)|*.rtf", CheckFileExists = true, Multiselect = false };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        input.Text = dialog.FileName;
        output.Text = preferences.SuggestOutputDirectory(dialog.FileName);
    }
    private void SelectOutput()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose the folder where the HTML file will be saved.", UseDescriptionForTitle = true, ShowNewFolderButton = true };
        try
        {
            var startingDirectory = preferences.LastOutputDirectory;
            if (startingDirectory is null && !string.IsNullOrWhiteSpace(output.Text) && Directory.Exists(output.Text))
                startingDirectory = output.Text;
            if (startingDirectory is not null) dialog.SelectedPath = startingDirectory;
        }
        catch (ArgumentException) { }
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            output.Text = dialog.SelectedPath;
            preferences.RememberOutputDirectory(dialog.SelectedPath);
        }
    }
    private void ResetResult()
    {
        generatedFile = null;
        open.Enabled = false;
        status.ForeColor = ForeColor;
        status.Text = "Click Convert to generate the HTML file.";
    }
    private async Task ConvertAsync()
    {
        if (busy) return;
        try
        {
            if (string.IsNullOrWhiteSpace(input.Text) || string.IsNullOrWhiteSpace(output.Text))
                throw new ArgumentException("Select the RTF document and the destination folder.");
            var source = Path.GetFullPath(input.Text.Trim());
            var destinationFolder = Path.GetFullPath(output.Text.Trim());
            if (!Directory.Exists(destinationFolder))
                throw new DirectoryNotFoundException("The destination folder does not exist. Choose an existing folder.");
            var destination = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(source) + ".html");
            if (File.Exists(destination) && MessageBox.Show(this, "The HTML file already exists. Do you want to replace it?", "Confirm replacement", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            ResetResult();
            SetBusy(true);
            status.Text = "Converting document…";
            generatedFile = await Task.Run(() => FileConversion.Convert(source, destinationFolder));
            preferences.RememberOutputDirectory(destinationFolder);
            open.Enabled = true;
            status.ForeColor = Color.FromArgb(21, 128, 61);
            status.Text = "HTML created successfully. Click Open HTML to view it.";
        }
        catch (Exception ex)
        {
            generatedFile = null;
            open.Enabled = false;
            status.ForeColor = Color.FromArgb(185, 28, 28);
            status.Text = "Conversion failed: " + ex.Message;
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
            status.Text = "The HTML was saved, but could not be opened: " + ex.Message;
        }
    }
}
