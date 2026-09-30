using System.Diagnostics;
namespace RtfToHtml;
public sealed class MainForm : Form
{
    private readonly OutputLocationPreferences preferences = new();
    private readonly TextBox input = new() { Name = "InputPath", Dock = DockStyle.Fill, AccessibleName = "RTF file" };
    private readonly TextBox output = new() { Name = "OutputPath", Dock = DockStyle.Fill, AccessibleName = "Output folder", ReadOnly = true, TabStop = false };
    private readonly RadioButton singleMode = new() { Name = "SingleMode", Text = "Single RTF file", AutoSize = true, Checked = true };
    private readonly RadioButton folderMode = new() { Name = "FolderMode", Text = "Folder (top level only)", AutoSize = true };
    private readonly Label sourceLabel = new() { Name = "SourceLabel", Text = "1   RTF document", AutoSize = true };
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
        ClientSize = new Size(760, 600);
        MinimumSize = new Size(720, 640);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(245, 247, 251);
        ForeColor = Color.FromArgb(30, 41, 59);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30), ColumnCount = 1, RowCount = 10 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 10; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles[9] = new RowStyle(SizeType.Percent, 100);
        layout.Controls.Add(new Label { Text = "RTF to HTML", Font = new Font("Segoe UI", 24, FontStyle.Bold), AutoSize = true }, 0, 0);
        layout.Controls.Add(new Label { Text = "Convert one document or all RTF files in a folder.", AutoSize = true, Margin = new Padding(3, 4, 3, 16) }, 0, 1);
        var modes = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
        modes.Controls.AddRange([singleMode, folderMode]);
        layout.Controls.Add(modes, 0, 2);
        layout.Controls.Add(sourceLabel, 0, 3);
        layout.Controls.Add(PathRow(input, browse), 0, 4);
        layout.Controls.Add(new Label { Text = "2   Destination folder", AutoSize = true, Margin = new Padding(3, 18, 3, 3) }, 0, 5);
        layout.Controls.Add(PathRow(output, save), 0, 6);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 22, 0, 12) };
        convert.BackColor = Color.FromArgb(37, 99, 235);
        convert.ForeColor = Color.White;
        convert.FlatStyle = FlatStyle.Flat;
        convert.FlatAppearance.BorderSize = 0;
        actions.Controls.AddRange([convert, open]);
        layout.Controls.Add(actions, 0, 7);
        layout.Controls.Add(progress, 0, 8);
        layout.Controls.Add(status, 0, 9);
        Controls.Add(layout);
        AcceptButton = convert;
        browse.Click += (_, _) => SelectInput();
        save.Click += (_, _) => SelectOutput();
        convert.Click += async (_, _) => await ConvertAsync();
        open.Click += (_, _) => OpenResult();
        input.TextChanged += (_, _) => ResetResult();
        output.TextChanged += (_, _) => ResetResult();
        singleMode.CheckedChanged += (_, _) => UpdateMode();
        folderMode.CheckedChanged += (_, _) => UpdateMode();
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
        if (folderMode.Checked)
        {
            using var dialog = new FolderBrowserDialog { Description = "Select a source folder. Only RTF files directly inside it will be converted.", UseDescriptionForTitle = true };
            var initialDirectory = preferences.SuggestInputDirectory();
            if (Directory.Exists(initialDirectory)) dialog.SelectedPath = initialDirectory;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            input.Text = dialog.SelectedPath;
            preferences.RememberInputDirectory(dialog.SelectedPath);
            output.Text = preferences.SuggestOutputDirectory(Path.Combine(dialog.SelectedPath, "document.rtf"));
        }
        else
        {
            using var dialog = new OpenFileDialog { Title = "Select an RTF document", Filter = "RTF document (*.rtf)|*.rtf", CheckFileExists = true, Multiselect = false, InitialDirectory = preferences.SuggestInputDirectory() };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            input.Text = dialog.FileName;
            preferences.RememberInputFile(dialog.FileName);
            output.Text = preferences.SuggestOutputDirectory(dialog.FileName);
        }
    }

    private void UpdateMode()
    {
        if (!IsHandleCreated) return;
        input.Clear();
        sourceLabel.Text = folderMode.Checked ? "1   Source folder (RTF files at top level only)" : "1   RTF document";
        browse.Text = folderMode.Checked ? "Choose source folder..." : "Browse...";
        convert.Text = folderMode.Checked ? "Convert folder" : "Convert to HTML";
        status.Text = folderMode.Checked ? "Subfolders and non-RTF files will be skipped." : "Click Convert to generate the HTML file.";
        generatedFile = null;
        open.Enabled = false;
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
            if (folderMode.Checked)
            {
                if (!Directory.Exists(source)) throw new DirectoryNotFoundException("The source folder does not exist.");
                var files = FolderConversion.FindRtfFiles(source);
                if (files.Count == 0)
                {
                    status.Text = "No RTF files were found at the top level of the selected folder.";
                    return;
                }
                if (string.Equals(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                  destinationFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                  StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Choose a destination folder different from the source folder.");
                var existingCount = files.Count(file => File.Exists(Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(file) + ".html")));
                if (existingCount > 0 && MessageBox.Show(this,
                    $"{existingCount} HTML file(s) already exist in the destination. Replace them?",
                    "Confirm replacement", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    return;
                ResetResult();
                SetBusy(true);
                status.Text = $"Converting {files.Count} RTF file(s)...";
                var result = await Task.Run(() => FolderConversion.ConvertTopLevel(source, destinationFolder));
                preferences.RememberOutputDirectory(destinationFolder);
                status.ForeColor = result.Failures.Count == 0 ? Color.FromArgb(21, 128, 61) : Color.FromArgb(185, 28, 28);
                status.Text = $"Converted {result.Converted} of {result.Found} RTF file(s).";
                if (result.Failures.Count > 0)
                {
                    var details = string.Join(Environment.NewLine, result.Failures.Take(10).Select(f => $"{f.FileName}: {f.Error}"));
                    if (result.Failures.Count > 10) details += Environment.NewLine + $"...and {result.Failures.Count - 10} more.";
                    MessageBox.Show(this, details, "Some files could not be converted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                generatedFile = null;
                open.Enabled = false;
                return;
            }
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
        input.Enabled = output.Enabled = browse.Enabled = save.Enabled = convert.Enabled = singleMode.Enabled = folderMode.Enabled = !value;
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
