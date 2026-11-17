Add-Type -AssemblyName System.Drawing
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$previewRoot = Join-Path $repoRoot 'SourceAssets/ReferenceProps/Previews'
$sources = @{
    'EightProps.png' = $previewRoot
    'UnityEightProps.png' = (Join-Path $repoRoot 'RecoveredProjects/TheGhost_School_Unity6/RecoveryReports/ReferenceProps')
}
$names = @('MetalBlank','GoldKey','SilverKey','SawBlade','SawHandle','Screws','AssembledSaw','KeyMold')
$labels = @('01  METAL BLANK','02  GOLD KEY','03  SILVER KEY','04  SAW BLADE','05  WOOD HANDLE','06  SCREWS','07  ASSEMBLED SAW','08  KEY MOLD')
foreach ($output in $sources.Keys) {
    $canvas = New-Object System.Drawing.Bitmap 1400,780
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.Clear([System.Drawing.Color]::FromArgb(22,29,26))
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $font = New-Object System.Drawing.Font 'Segoe UI',13
    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(205,214,207))
    for ($i=0; $i -lt 8; $i++) {
        $img = [System.Drawing.Image]::FromFile((Join-Path $sources[$output] ($names[$i]+'.png')))
        $x=($i%4)*350; $y=[math]::Floor($i/4)*390
        $graphics.DrawImage($img,[int]($x+15),[int]($y+15),320,320)
        $graphics.DrawString($labels[$i],$font,$brush,[single]($x+25),[single]($y+350))
        $img.Dispose()
    }
    $canvas.Save((Join-Path $previewRoot $output),[System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $canvas.Dispose(); $font.Dispose(); $brush.Dispose()
}
