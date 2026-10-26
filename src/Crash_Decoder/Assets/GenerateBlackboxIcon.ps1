# MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
# Reproducibly draws the app's blue tile and white four-point blackbox mark.
param([Parameter(Mandatory = $true)][string]$OutputPath)

Add-Type -AssemblyName System.Drawing

$sizes = @(16, 32, 48, 256)
$images = [System.Collections.Generic.List[byte[]]]::new()
foreach ($size in $sizes) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $tile = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $radius = [Math]::Max(2, [int]($size * 0.25))
    $extent = $size - 1
    $diameter = $radius * 2
    $tile.AddArc(0, 0, $diameter, $diameter, 180, 90)
    $tile.AddArc($extent - $diameter, 0, $diameter, $diameter, 270, 90)
    $tile.AddArc($extent - $diameter, $extent - $diameter, $diameter, $diameter, 0, 90)
    $tile.AddArc(0, $extent - $diameter, $diameter, $diameter, 90, 90)
    $tile.CloseFigure()
    $blue = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(26, 97, 158))
    $graphics.FillPath($blue, $tile)

    $center = $size / 2.0
    $outer = $size * 0.34
    $inner = $size * 0.075
    $points = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($center, $center - $outer),
        [System.Drawing.PointF]::new($center + $inner, $center - $inner),
        [System.Drawing.PointF]::new($center + $outer, $center),
        [System.Drawing.PointF]::new($center + $inner, $center + $inner),
        [System.Drawing.PointF]::new($center, $center + $outer),
        [System.Drawing.PointF]::new($center - $inner, $center + $inner),
        [System.Drawing.PointF]::new($center - $outer, $center),
        [System.Drawing.PointF]::new($center - $inner, $center - $inner)
    )
    $white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $graphics.FillPolygon($white, $points)

    $memory = [System.IO.MemoryStream]::new()
    $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
    $images.Add($memory.ToArray())
    $memory.Dispose()
    $white.Dispose()
    $blue.Dispose()
    $tile.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

$directory = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($OutputPath))
[System.IO.Directory]::CreateDirectory($directory) | Out-Null
$file = [System.IO.File]::Create($OutputPath)
$writer = [System.IO.BinaryWriter]::new($file)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
    $writer.Write([byte]$dimension)
    $writer.Write([byte]$dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$images[$i].Length)
    $writer.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($image in $images) { $writer.Write($image) }
$writer.Dispose()
