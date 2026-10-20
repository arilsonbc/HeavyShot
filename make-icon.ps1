$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$assetFolder=Join-Path $PSScriptRoot 'assets'
New-Item -ItemType Directory -Force -Path $assetFolder | Out-Null
$iconBitmap=New-Object System.Drawing.Bitmap 64,64
$iconGraphics=[System.Drawing.Graphics]::FromImage($iconBitmap)
$iconGraphics.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$iconGraphics.Clear([System.Drawing.Color]::Transparent)
$iconPath=New-Object System.Drawing.Drawing2D.GraphicsPath
$iconPath.AddArc(2,2,28,28,180,90)
$iconPath.AddArc(34,2,28,28,270,90)
$iconPath.AddArc(34,34,28,28,0,90)
$iconPath.AddArc(2,34,28,28,90,90)
$iconPath.CloseFigure()
$iconFill=New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(66,104,235))
$iconGraphics.FillPath($iconFill,$iconPath)
$iconPen=New-Object System.Drawing.Pen ([System.Drawing.Color]::White),4
$iconPen.StartCap=[System.Drawing.Drawing2D.LineCap]::Round
$iconPen.EndCap=[System.Drawing.Drawing2D.LineCap]::Round
$iconGraphics.DrawLines($iconPen,[System.Drawing.Point[]]@((New-Object System.Drawing.Point 20,14),(New-Object System.Drawing.Point 20,44),(New-Object System.Drawing.Point 50,44)))
$iconGraphics.DrawLines($iconPen,[System.Drawing.Point[]]@((New-Object System.Drawing.Point 14,20),(New-Object System.Drawing.Point 44,20),(New-Object System.Drawing.Point 44,50)))
$iconHandle=$iconBitmap.GetHicon()
$icon=[System.Drawing.Icon]::FromHandle($iconHandle)
$iconStream=[IO.File]::Create((Join-Path $assetFolder 'HeavyShot.ico'))
try { $icon.Save($iconStream) } finally { $iconStream.Dispose(); $icon.Dispose(); $iconPen.Dispose(); $iconFill.Dispose(); $iconPath.Dispose(); $iconGraphics.Dispose(); $iconBitmap.Dispose() }
