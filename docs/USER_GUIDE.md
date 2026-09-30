# RtfToHTML User Guide

RtfToHTML converts `.rtf` documents to `.html` on Windows. It can run as a desktop app or from a command-line terminal. Original RTF files are preserved.

## Desktop app

1. Extract `rtftohtml.zip` to a folder.
2. Run `win-x86\RtfToHTML.exe` on 32-bit Windows, or `win-x64\RtfToHTML.exe` on 64-bit Windows.
3. Choose **Single RTF file** to convert one document, or **Folder (top level only)** to convert RTF files directly inside a folder.
4. Select the source and destination, then click **Convert to HTML** or **Convert folder**.

The output name is generated from the RTF filename: `Contract.rtf` becomes `Contract.html`. Folder mode skips subfolders and non-RTF files; `.rtf` matching is case-insensitive. The destination folder must already exist. The app asks before replacing existing HTML files; command-line mode replaces them without asking. To print, open the HTML in a browser and check print preview because pagination and margins may differ from WordPad.

The app independently remembers the last source and destination folders in `%LOCALAPPDATA%\RtfToHTML\settings.json`.

## Command-line usage

```text
RtfToHTML.exe <input.rtf or source_folder> <destination_folder>
```

The first argument is one RTF file or a source folder. Folder mode processes only its direct files, not subfolders. The second argument must be an existing destination folder.

The examples assume the terminal is in `C:\Users\Ibai\RtfToHtml` and the published executable is in `artifacts\publish\win-x64`. Use `artifacts\publish\win-x86` on 32-bit Windows. From the customer ZIP, use its matching `win-x64` or `win-x86` folder.

### Git Bash

Git Bash uses Unix-style paths; Windows `C:` is `/c/`:

```bash
cd /c/Users/Ibai/RtfToHtml

# Convert top-level RTF files in a folder
./artifacts/publish/win-x64/RtfToHTML.exe "/c/Users/Ibai/Documents/RTF" "/c/Users/Ibai/Documents/HTML"

# Convert one RTF file
./artifacts/publish/win-x64/RtfToHTML.exe "/c/Users/Ibai/Documents/Contract.rtf" "/c/Users/Ibai/Documents/HTML"
```

Replace the example paths with yours. Keep quotes around paths containing spaces.

### Command Prompt (CMD)

```bat
cd /d C:\Users\Ibai\RtfToHtml

REM Convert top-level RTF files in a folder
artifacts\publish\win-x64\RtfToHTML.exe "C:\Users\Ibai\Documents\RTF" "C:\Users\Ibai\Documents\HTML"

REM Convert one RTF file
artifacts\publish\win-x64\RtfToHTML.exe "C:\Users\Ibai\Documents\Contract.rtf" "C:\Users\Ibai\Documents\HTML"

REM Show the previous command's exit code
echo %ERRORLEVEL%
```

`cd /d` changes both the current directory and drive if required.

### PowerShell

```powershell
Set-Location 'C:\Users\Ibai\RtfToHtml'

# Convert top-level RTF files in a folder
& '.\artifacts\publish\win-x64\RtfToHTML.exe' 'C:\Users\Ibai\Documents\RTF' 'C:\Users\Ibai\Documents\HTML'

# Convert one RTF file
& '.\artifacts\publish\win-x64\RtfToHTML.exe' 'C:\Users\Ibai\Documents\Contract.rtf' 'C:\Users\Ibai\Documents\HTML'

# Show the last native command's exit code
$LASTEXITCODE
```

The `&` call operator runs a program whose path is quoted.

Exit codes: `0` means success (including a folder with no RTF files), `1` means a conversion/path error, and `2` means the argument count is incorrect. Batch mode may convert some files and still return `1` if others fail. Errors are written to `stderr`.

The app does not print a success message or the exit code automatically. Query the code using the syntax for the shell you are actually running, immediately after the converter finishes:

- **Git Bash:** `echo $?`
- **CMD:** `echo %ERRORLEVEL%`
- **PowerShell:** `$LASTEXITCODE`

`$LASTEXITCODE` is a PowerShell variable. It is empty in Git Bash, so use `echo $?` there. In PowerShell, invoke the executable directly with `&`; if you start it with `Start-Process`, capture and inspect the process instead:

```powershell
$process = Start-Process '.\artifacts\publish\win-x64\RtfToHTML.exe' -ArgumentList '"C:\Docs\Contract.rtf"', '"C:\Docs\HTML"' -Wait -PassThru
$process.ExitCode
```

## Troubleshooting

- Create the destination folder before conversion.
- In folder mode, put RTF files directly in the selected folder; subfolders are not searched.
- Check the executable path. Git Bash uses `/c/...`; CMD and PowerShell use Windows paths.
- If print output differs, review fonts, margins, and page breaks in the browser's print preview.
