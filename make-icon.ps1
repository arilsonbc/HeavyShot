$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$assetFolder=Join-Path $PSScriptRoot 'assets'
New-Item -ItemType Directory -Force -Path $assetFolder | Out-Null
$iconBitmap=New-Object System.Drawing.Bitmap 64,64
$iconGraphics=[System.Drawing.Graphics]::FromImage($iconBitmap)
$iconGraphics.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$iconGraphics.Clear([System.Drawing.Color]::FromArgb(19,22,29))
$iconPen=New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(126,157,255)),5
$iconPen.StartCap=[System.Drawing.Drawing2D.LineCap]::Round
$iconPen.EndCap=[System.Drawing.Drawing2D.LineCap]::Round
$iconGraphics.DrawLines($iconPen,[System.Drawing.Point[]]@((New-Object System.Drawing.Point 13,25),(New-Object System.Drawing.Point 13,13),(New-Object System.Drawing.Point 25,13)))
$iconGraphics.DrawLines($iconPen,[System.Drawing.Point[]]@((New-Object System.Drawing.Point 39,51),(New-Object System.Drawing.Point 51,51),(New-Object System.Drawing.Point 51,39)))
$iconPen.Color=[System.Drawing.Color]::White
$iconGraphics.DrawLine($iconPen,24,23,24,43)
$iconGraphics.DrawLine($iconPen,40,21,40,41)
$iconGraphics.DrawLine($iconPen,24,33,40,31)
$iconHandle=$iconBitmap.GetHicon()
$icon=[System.Drawing.Icon]::FromHandle($iconHandle)
$iconStream=[IO.File]::Create((Join-Path $assetFolder 'HeavyShot.ico'))
try { $icon.Save($iconStream) } finally { $iconStream.Dispose(); $icon.Dispose(); $iconPen.Dispose(); $iconGraphics.Dispose(); $iconBitmap.Dispose() }
