$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$folder=Join-Path $PSScriptRoot 'assets'
New-Item -ItemType Directory -Force -Path $folder,(Join-Path $PSScriptRoot 'brand') | Out-Null
$stream=[IO.File]::Create((Join-Path $folder 'HeavyShot.ico'))
$writer=New-Object IO.BinaryWriter($stream)
$images=@()
foreach($size in @(16,24,32,48,64,128,256)){
 $bitmap=New-Object Drawing.Bitmap $size,$size
 $g=[Drawing.Graphics]::FromImage($bitmap);$g.Clear([Drawing.Color]::Transparent)
 $g.ScaleTransform(($size/64.0),($size/64.0));$g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
 $p=New-Object Drawing.Drawing2D.GraphicsPath
 $p.AddArc(0,0,24,24,180,90);$p.AddArc(40,0,24,24,270,90);$p.AddArc(40,40,24,24,0,90);$p.AddArc(0,40,24,24,90,90);$p.CloseFigure()
 $g.FillPath([Drawing.Brushes]::Black,$p)
 foreach($r in @(@(8,8,16,6),@(8,8,6,16),@(40,8,16,6),@(50,8,6,16),@(8,50,16,6),@(8,40,6,16),@(40,50,16,6),@(50,40,6,16),@(22,20,6,24),@(36,20,6,24),@(28,29,8,6))){$g.FillRectangle([Drawing.Brushes]::White,[int]$r[0],[int]$r[1],[int]$r[2],[int]$r[3])}
 $memory=New-Object IO.MemoryStream;$bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
 $images+=,@{size=$size;data=$memory.ToArray()}
 if($size -eq 256){$bitmap.Save((Join-Path $PSScriptRoot 'brand/HeavyShot-icon.png'),[Drawing.Imaging.ImageFormat]::Png)}
 $memory.Dispose();$p.Dispose();$g.Dispose();$bitmap.Dispose()
}
try {
 $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$images.Count);$offset=6+16*$images.Count
 foreach($im in $images){$dimension=if($im.size -eq 256){0}else{$im.size};$writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$im.data.Length);$writer.Write([uint32]$offset);$offset+=$im.data.Length}
 foreach($im in $images){$writer.Write([byte[]]$im.data)}
} finally {$writer.Dispose();$stream.Dispose()}
