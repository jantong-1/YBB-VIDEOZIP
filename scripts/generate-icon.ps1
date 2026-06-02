$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assets = Join-Path $root "assets"
$icoPath = Join-Path $assets "YBBvideozip.ico"
$pngPath = Join-Path $assets "YBBvideozip-icon.png"

New-Item -ItemType Directory -Force -Path $assets | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class NativeIconMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bmp)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $scale = $size / 512.0
    $matrix = New-Object System.Drawing.Drawing2D.Matrix
    $matrix.Scale([single]$scale, [single]$scale)
    $graphics.Transform = $matrix

    $black = [System.Drawing.Color]::FromArgb(17, 17, 17)
    $penMain = New-Object System.Drawing.Pen $black, 34
    $penMain.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penMain.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penMain.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $penThin = New-Object System.Drawing.Pen $black, 24
    $penThin.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penThin.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penThin.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $brushWhite = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $brushBlack = New-Object System.Drawing.SolidBrush $black

    $roundRect = New-Object System.Drawing.Drawing2D.GraphicsPath
    $roundRect.AddArc(70, 70, 144, 144, 180, 90)
    $roundRect.AddArc(298, 70, 144, 144, 270, 90)
    $roundRect.AddArc(298, 298, 144, 144, 0, 90)
    $roundRect.AddArc(70, 298, 144, 144, 90, 90)
    $roundRect.CloseFigure()
    $graphics.FillPath($brushWhite, $roundRect)
    $graphics.DrawPath($penMain, $roundRect)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddLines(@(
        (New-Object System.Drawing.PointF 156, 150),
        (New-Object System.Drawing.PointF 244, 256),
        (New-Object System.Drawing.PointF 244, 362)
    ))
    $graphics.DrawPath($penMain, $path)
    $path.Dispose()

    $graphics.DrawLine($penMain, 332, 150, 244, 256)
    $graphics.DrawLine($penMain, 302, 172, 302, 362)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddBezier(302, 172, 382, 172, 392, 242, 318, 256)
    $graphics.DrawPath($penMain, $path)
    $path.Dispose()

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddBezier(302, 256, 394, 256, 396, 362, 302, 362)
    $graphics.DrawPath($penMain, $path)
    $path.Dispose()

    $play = New-Object System.Drawing.Drawing2D.GraphicsPath
    $play.AddPolygon(@(
        (New-Object System.Drawing.PointF 150, 285),
        (New-Object System.Drawing.PointF 150, 344),
        (New-Object System.Drawing.PointF 204, 314)
    ))
    $graphics.FillPath($brushBlack, $play)
    $play.Dispose()

    $graphics.DrawLine($penThin, 110, 314, 66, 314)
    $graphics.DrawLine($penThin, 402, 314, 446, 314)

    $graphics.Dispose()
    $penMain.Dispose()
    $penThin.Dispose()
    $brushWhite.Dispose()
    $brushBlack.Dispose()
    $roundRect.Dispose()

    return $bmp
}

function Write-UInt32($writer, [UInt32]$value) {
    $writer.Write($value)
}

function Write-Int32($writer, [Int32]$value) {
    $writer.Write($value)
}

function Write-UInt16($writer, [UInt16]$value) {
    $writer.Write($value)
}

function Convert-BitmapToIconDibBytes($bitmap, [int]$size) {
    $memory = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($memory)
    try {
        Write-UInt32 $writer 40
        Write-Int32 $writer $size
        Write-Int32 $writer ($size * 2)
        Write-UInt16 $writer 1
        Write-UInt16 $writer 32
        Write-UInt32 $writer 0
        Write-UInt32 $writer ($size * $size * 4)
        Write-Int32 $writer 0
        Write-Int32 $writer 0
        Write-UInt32 $writer 0
        Write-UInt32 $writer 0

        for ($y = $size - 1; $y -ge 0; $y--) {
            for ($x = 0; $x -lt $size; $x++) {
                $pixel = $bitmap.GetPixel($x, $y)
                $writer.Write([byte]$pixel.B)
                $writer.Write([byte]$pixel.G)
                $writer.Write([byte]$pixel.R)
                $writer.Write([byte]$pixel.A)
            }
        }

        $maskStride = [int]([Math]::Ceiling($size / 32.0) * 4)
        $mask = New-Object byte[] ($maskStride * $size)
        $writer.Write($mask)
        $writer.Flush()
        return $memory.ToArray()
    }
    finally {
        $writer.Dispose()
        $memory.Dispose()
    }
}

$preview = New-IconBitmap 512
try {
    $preview.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $preview.Dispose()
}

$iconBitmap = New-IconBitmap 256
$hIcon = [IntPtr]::Zero
try {
    $hIcon = $iconBitmap.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)
    $stream = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    try {
        $icon.Save($stream)
    }
    finally {
        $stream.Dispose()
        $icon.Dispose()
    }
}
finally {
    if ($hIcon -ne [IntPtr]::Zero) {
        [NativeIconMethods]::DestroyIcon($hIcon) | Out-Null
    }
    $iconBitmap.Dispose()
}

Write-Host "Created: $icoPath"
Write-Host "Created: $pngPath"
