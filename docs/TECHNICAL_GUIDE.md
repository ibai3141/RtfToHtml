# RtfToHTML Technical Guide

This guide helps developers open the repository, understand the implementation, make changes, and publish the app. See the [user guide](USER_GUIDE.md) for daily use and Git Bash, CMD, and PowerShell examples.

## Getting started

### Requirements

- 64-bit Windows 10/11 for development. The application uses Windows Forms and Windows graphics APIs. Customer builds target both x86 and x64 Windows.
- 32-bit x86 Windows is supported by the `win-x86` self-contained publish. Use the x86 executable on 32-bit Windows and `win-x64` on 64-bit Windows.
- .NET 9 SDK to build, publish, and run tests. Check it with `dotnet --info`.
- Microsoft Edge is needed only by regression tests that render HTML; the published converter does not depend on Edge.
- Git and an IDE are optional when working from an existing checkout. Open `src/RtfToHtml/RtfToHtml.csproj` directly in your IDE, or run commands from the repository root.

`vendor/RtfPipe/RtfPipe.Tests` is an old upstream test project that C# Dev Kit may report as unsupported. It is not part of this app or its maintained tests, and does not need to be converted or built.

### Restore, build, and run

Open PowerShell in the repository root (the folder containing `src`, `tests`, and `vendor`):

```powershell
Set-Location 'C:\Users\Ibai\RtfToHtml'
dotnet restore src/RtfToHtml
dotnet build src/RtfToHtml
dotnet run --project src/RtfToHtml
```

`dotnet run` builds Debug if needed and opens the UI. To convert from the development build, place `--` before the app arguments:

```powershell
dotnet run --project src/RtfToHtml -- 'C:\Docs\Contract.rtf' 'C:\Docs\HTML'
dotnet run --project src/RtfToHtml -- 'C:\Docs\RTF' 'C:\Docs\HTML'
```

The first example converts one RTF; the second converts a folder. The destination must exist.

### Tests

```powershell
dotnet run --project tests/GuiSmoke -- .
dotnet run --project tests/Regression -- .
```

The GUI smoke test automates the app. Regression tests convert documents in `samples` and check document structure, images, and visual output; they require Edge. Generated outputs go under `artifacts/`, which is excluded from Git.

## What the application does

RtfToHTML is a Windows desktop app that converts RTF to standalone HTML. It supports one-file conversion in the GUI, top-level folder conversion, and command-line invocation by another application or terminal.

Folder mode enumerates direct files only (`SearchOption.TopDirectoryOnly`), selects `.rtf` case-insensitively, and sorts names for stable processing. It skips subfolders and other file types and preserves the original RTF files. Each output is named `<rtf-base-name>.html` in an existing destination folder. The GUI asks before replacing an output; CLI mode replaces without prompting.

## Repository layout

| Path | Responsibility |
| --- | --- |
| `src/RtfToHtml/Program.cs` | Starts WinForms with no arguments; dispatches CLI conversion with two. |
| `src/RtfToHtml/MainForm.cs` | UI, selectors, overwrite confirmation, progress/status, and batch mode. |
| `src/RtfToHtml/FileConversion.cs` | Validates an RTF input, names and writes the output, replaces atomically via a temporary file. |
| `src/RtfToHtml/FolderConversion.cs` | Selects top-level RTF files and converts them while collecting per-file failures. |
| `src/RtfToHtml/HtmlConverter.cs` | Calls RtfPipe and creates the standalone HTML, including embedded images. |
| `src/RtfToHtml/OutputLocationPreferences.cs` | Remembers source and destination folders. |
| `vendor/RtfPipe/` | Vendored RtfPipe source, MIT license, and documented local changes. |
| `tests/Regression/` | Checks converted content, images, and table alignment. |
| `tests/GuiSmoke/` | Checks the UI, single-file mode, folder mode, preferences, and errors. |
| `samples/` | RTF inputs used by tests. |
| `artifacts/` | Local publish output, packages, and generated test files. |

The application namespace is `RtfToHtml`. The SDK-style main project targets `net9.0-windows`, enables Windows Forms, and references the local RtfPipe project. No separate RtfPipe DLL download is required.

## Code walkthrough

### 1. `Program.cs`: startup and mode selection

`Main` selects a mode from the argument count. No arguments initialize WinForms and open `MainForm`. Anything other than two arguments prints usage to `stderr` and returns `2`. With two arguments, the app attaches to the caller's console so errors are visible. If the first argument is an existing directory, it runs folder mode; otherwise it treats it as a file. A misspelled directory path is therefore treated as a file path and reported as an input error.

The dispatch is small; both modes use the same conversion services. This excerpt leaves out CLI error reporting:

```csharp
if (Directory.Exists(args[0]))
{
    var batch = FolderConversion.ConvertTopLevel(args[0], args[1]);
    return batch.Failures.Count > 0 ? 1 : 0;
}

FileConversion.Convert(args[0], args[1]);
return 0;
```

The dispatch is small; both modes use the same conversion services:

```csharp
if (Directory.Exists(args[0]))
{
    var batch = FolderConversion.ConvertTopLevel(args[0], args[1]);
    return batch.Failures.Count > 0 ? 1 : 0;
}

FileConversion.Convert(args[0], args[1]);
return 0;
```

### 2. `MainForm.cs`: user interface

`MainForm` constructs the WinForms controls: source-mode radio buttons, source path, read-only destination path, browse/convert/open buttons, and progress/status display. `SelectInput` chooses a file or folder picker based on the current mode. `SelectOutput` chooses the destination; the user cannot edit the generated filename.

`ConvertAsync` validates the selections and destination, asks before replacement, and runs conversion through `Task.Run` so the UI remains responsive. Batch mode displays the converted count and up to ten file errors. `OpenResult` opens the last single-file result with the Windows-associated application. Controls are disabled during conversion to prevent overlapping runs.

### 3. `OutputLocationPreferences.cs`: remembered paths

This class stores folder paths, not document contents. Its JSON settings file is `%LOCALAPPDATA%\RtfToHTML\settings.json` and has separate `LastInputDirectory` and `LastOutputDirectory` values. It reuses a remembered directory while it exists; otherwise it falls back to Documents for source selection and to the RTF/source folder for output. Corrupt settings or file access errors do not block conversion.

### 4. `FolderConversion.cs`: batch processing

`FindRtfFiles` verifies the source folder and enumerates only its direct files. It filters `.rtf` without case sensitivity and sorts by filename. `ConvertTopLevel` checks that the destination exists and differs from the source folder. It calls `FileConversion` for each file; a failed file is recorded and processing continues. `FolderConversionResult` reports counts and failures. No RTF files is an empty successful batch (CLI exit code `0`).

The top-level filter is explicit:

```csharp
Directory.EnumerateFiles(sourcePath, "*", SearchOption.TopDirectoryOnly)
    .Where(path => string.Equals(
        Path.GetExtension(path), ".rtf", StringComparison.OrdinalIgnoreCase))
```

The top-level filter is explicit:

```csharp
Directory.EnumerateFiles(sourcePath, "*", SearchOption.TopDirectoryOnly)
    .Where(path => string.Equals(
        Path.GetExtension(path), ".rtf", StringComparison.OrdinalIgnoreCase))
```

### 5. `FileConversion.cs`: one RTF document

This is the shared conversion layer. It normalizes paths, verifies the destination exists, derives `<base-name>.html`, and checks the RTF opening marker (opening brace, backslash, then `rtf`). It passes the input stream to `HtmlConverter`. The returned HTML is written as UTF-8 without a BOM to a temporary file in the destination folder and then moved over the final output. A `finally` block removes leftover temporary files after an error. The temporary write avoids leaving a partially written result.

The temporary-file replacement is the key part of the write path:

```csharp
File.WriteAllText(temporary, html, new UTF8Encoding(false));
File.Move(temporary, outputPath, overwrite: true);
```

The temporary-file replacement is the key part of the write path:

```csharp
File.WriteAllText(temporary, html, new UTF8Encoding(false));
File.Move(temporary, outputPath, overwrite: true);
```

### 6. `HtmlConverter.cs`: RTF, HTML, and images

The converter registers a code-page provider for older RTF encodings, calls `Rtf.ToHtml`, then wraps the fragment in a document with a doctype, UTF-8 charset, and body. Supported images are embedded as `data:` URIs. WMF and EMF images are rasterized through GDI+ to PNG and embedded; the HTML is self-contained, but those vector images become bitmaps.

### 7. `vendor/RtfPipe`: conversion library

RtfPipe parses RTF and produces HTML. The repository retains its source and MIT license. Local table-alignment changes are described in `vendor/RtfPipe/LOCAL_CHANGES.md`; they carry centered and right table alignment into CSS and do not alter paragraph alignment. The vendored project is built as a dependency of the main app.

### 8. Maintained tests

`tests/GuiSmoke` checks UI validation, single-file conversion, preferences, and that folder mode ignores nested folders and non-RTF files. `tests/Regression` checks sample documents for template fields, images, counts, and table alignment. The upstream `RtfPipe.Tests` project is old and is not needed for this app.

## CLI behavior

```text
RtfToHTML.exe <input.rtf or source_folder> <destination_folder>
```

Exit code `0` means success, `1` means a conversion or path error, and `2` means the argument count is wrong. A batch returns `1` if any file failed, even when others succeeded. No RTF files returns `0`. CLI mode never prompts before replacing outputs; errors are written to `stderr`.

## Build and publish

The main app is WinForms targeting `net9.0-windows`. RtfPipe is built from `vendor/RtfPipe`; neither Word nor LibreOffice is required. Both customer executables are self-contained single-file publishes, so customers do not need to install .NET. Distribute `RtfPipe.LICENSE.txt` with them.

Build and publish on Windows with .NET 9 SDK:

```powershell
dotnet build src/RtfToHtml
dotnet publish src/RtfToHtml -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish/win-x86
dotnet publish src/RtfToHtml -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish/win-x64
```

The customer archive is `artifacts/packages/rtftohtml.zip`; it contains `win-x86/RtfToHTML.exe`, `win-x64/RtfToHTML.exe`, the license, and English `LEEME.txt`. Do not include `bin`, `obj`, samples, or test output in the ZIP.

## Known limitations

Table-alignment changes are documented in `vendor/RtfPipe/LOCAL_CHANGES.md`. Center and right table alignment are carried into CSS; paragraph alignment is unchanged. Mixed alignment across rows and floating-table positioning are not added.

Printed pagination is not guaranteed to match WordPad. It depends on fonts, browser, and print settings. WMF/EMF images become PNG bitmaps.
