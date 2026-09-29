param([string]$Root = (Split-Path $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$out=Join-Path $Root 'evaluation\output\RtfPipePng'
New-Item -ItemType Directory -Force $out | Out-Null
Get-ChildItem (Join-Path $Root 'evaluation\output\RtfPipe\*.html') | ForEach-Object {
$s=[IO.File]::ReadAllText($_.FullName)
$s=[regex]::Replace($s,'data:windows/metafile;base64,([^"'']+)',[Text.RegularExpressions.MatchEvaluator]{param($m)
$ms=[IO.MemoryStream]::new([Convert]::FromBase64String($m.Groups[1].Value))
$im=[Drawing.Image]::FromStream($ms)
$bmp=[Drawing.Bitmap]::new($im.Width,$im.Height)
$g=[Drawing.Graphics]::FromImage($bmp)
$g.Clear([Drawing.Color]::White)
$g.DrawImage($im,0,0,$im.Width,$im.Height)
$png=[IO.MemoryStream]::new()
$bmp.Save($png,[Drawing.Imaging.ImageFormat]::Png)
$value='data:image/png;base64,'+[Convert]::ToBase64String($png.ToArray())
$g.Dispose(); $bmp.Dispose(); $im.Dispose(); $ms.Dispose(); $png.Dispose()
return $value
})
[IO.File]::WriteAllText((Join-Path $out $_.Name),$s,[Text.UTF8Encoding]::new($false))
}
