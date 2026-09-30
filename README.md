# RtfToHTML

A Windows desktop app for converting RTF documents to standalone HTML. Open it with a double click to select a file, or call it from another application with an input RTF and an output folder.

## For customers

1. Extract the ZIP to a folder.
2. Open `RtfToHTML.exe`.
3. Choose **Single RTF file** to convert one document, or **Folder (top level only)** to convert all `.rtf` files directly inside a selected folder.
4. Select the source and destination folders as prompted.
5. Click **Convert to HTML**. Each output uses its RTF file's name with the `.html` extension. Click **Open HTML** to review a single-file result.

The output name cannot be edited. For `Contract.rtf`, the app creates `Contract.html` in the selected folder. Folder mode processes only the selected folder's direct child `.rtf` files (extension matching is case-insensitive); it skips subfolders and all other files. The original RTF files are kept and images are embedded in the HTML. The app asks before replacing existing HTML files. Use your browser's print preview to check page breaks and margins.

The app remembers the last output folder in `%LOCALAPPDATA%\\RtfToHTML\\settings.json`. The RTF picker remembers its last folder, and the destination picker remembers its last folder independently. Both preferences persist after closing the app. If the saved folder is no longer available, the app falls back to the RTF's folder.

## Command-line integration

```text
RtfToHTML.exe "C:\\Documents\\Contract.rtf" "C:\\Documents\\HTML"
RtfToHTML.exe "C:\\Documents\\RTF" "C:\\Documents\\HTML"
```

The first argument can be an input RTF file or a source folder. The second is an existing destination folder. A source folder converts only its direct child `.rtf` files. Each HTML uses the corresponding RTF name. With no arguments, the app opens its window. With two arguments, it converts without showing a window and replaces existing HTML files without prompting. The calling application should wait for it to exit. Exit codes: `0` success, `1` conversion or file error, `2` invalid arguments. Errors are written to stderr.

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

The regression tests check left, centered and right table alignment, including nested tables, and verify template fields, table counts and images for the six sample documents. The GUI smoke test checks single-file and top-level folder conversion, preferences, and error handling. Edge and Playwright are test-only dependencies; the converter does not use them.

## Conversion notes

The local RtfPipe changes carry the table alignment tokens `trqc` and `trqr` into CSS, using the first row's alignment. See `vendor/RtfPipe/LOCAL_CHANGES.md` for the upstream revision and patch notes.

The conversion has not been certified for identical pagination to WordPad. Page margins, fonts, vertical spacing and other positioning can vary with the source document and print environment. The samples are copies of the six supplied RTF files. `artifacts` contains generated output and is excluded from Git.
