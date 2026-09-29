# RtfToHTML

A Windows desktop app for converting RTF documents to standalone HTML. The app can be opened with a double click for file selection, or called from another application with two file paths.

## For customers

1. Extract the ZIP to a folder.
2. Open `RtfToHTML.exe`.
3. Click **Browse…** and select an `.rtf` document.
4. Click **Save as…** and choose where to save the `.html` file.
5. Click **Convert to HTML**. When it finishes, click **Open HTML** to review the result.

The original RTF is kept. Images are embedded in the HTML. If the output file already exists, the app asks before replacing it. Use your browser's print preview to check page breaks and margins.

The app remembers the last output folder in `%LOCALAPPDATA%\RtfToHTML\settings.json`. When you select another RTF, it suggests an HTML file with the same name in that folder. The **Save as…** dialog also opens there. If the saved folder is no longer available, the app falls back to the RTF's folder.

## Command-line integration

```text
RtfToHTML.exe "C:\\Documents\\input.rtf" "C:\\Documents\\output.html"
```

With no arguments, the app opens its window. With two paths, it converts without showing a window and replaces an existing output file. The calling application should wait for it to exit. Exit codes: `0` success, `1` conversion or file error, `2` invalid arguments. Errors are written to stderr.

## Build and publish

On Windows with the .NET 9 SDK:

```powershell
dotnet build src/RtfToHtml
dotnet publish src/RtfToHtml -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish
```

The published app includes the .NET runtime. It does not require Word or LibreOffice. Keep and distribute `RtfPipe.LICENSE.txt` with the executable. GDI+ on Windows rasterizes vector images.

## Tests

With Microsoft Edge installed:

```powershell
dotnet run --project tests/Regression -- .
dotnet run --project tests/GuiSmoke -- .
```

The regression tests check left, centered and right table alignment, including nested tables, and verify template fields, table counts and images for the six sample documents. The GUI smoke test exercises validation, conversion and saved output-folder preferences. Edge and Playwright are test-only dependencies; the converter does not use them.

## Conversion notes

The local RtfPipe changes carry the table alignment tokens `trqc` and `trqr` into CSS, using the first row's alignment. See `vendor/RtfPipe/LOCAL_CHANGES.md` for the upstream revision and patch notes.

The conversion has not been certified for identical pagination to WordPad. Page margins, fonts, vertical spacing and other positioning can vary with the source document and print environment. The samples are copies of the six supplied RTF files. `artifacts` contains generated output and is excluded from Git.
