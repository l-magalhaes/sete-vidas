# Mapa da Fortaleza INTEIRA com a arte de verdade (terreno, enfeites, fundo de cada trecho, janelas), costurando telas
# da prévia de cena (ScenePreview). Serve para o teste da Etapa 8: ver a transição natureza → ordem → corrupção.
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/gerar_mapa_arte.ps1 [escala_final]
#   Saída: SeteVidasDocs/Referencias/fortaleza_mapa_arte.png (escala 0,5 por padrão: 1 tile = 8 px)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$final = if ($args.Count -gt 0) { [float]$args[0] } else { 0.5 }
$ed = Join-Path $root 'Assets\_Game\Editor'
$rt = Join-Path $root 'Assets\_Game\Scripts\World\Scenery'
Add-Type -ReferencedAssemblies System.Drawing, System.Web.Extensions -Path @(
    (Join-Path $ed 'FortressLayout.cs'), (Join-Path $ed 'Scenery\FortressDressing.cs'), (Join-Path $ed 'Scenery\FortressTerrain.cs'), (Join-Path $ed 'Scenery\FortressBackgrounds.cs'),
    (Join-Path $rt 'TerrainAutotile.cs'), (Join-Path $rt 'DressingRules.cs'), (Join-Path $rt 'SceneryLayers.cs'),
    (Join-Path $here 'ScenePreview.cs'))
Add-Type -AssemblyName System.Drawing
$tmp = Join-Path $env:TEMP 'sete_vidas_mapa'
New-Item -ItemType Directory -Force $tmp | Out-Null
$x0 = -18.0; $x1 = 254.0; $y0 = -6.0; $y1 = 120.0
$sw = 30.0; $sh = 270.0 / 16.0
$cols = [math]::Ceiling(($x1 - $x0) / $sw); $rows = [math]::Ceiling(($y1 - $y0) / $sh)
$W = [int]($cols * 480 * $final); $H = [int]($rows * 270 * $final)
$map = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($map)
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
for ($r = 0; $r -lt $rows; $r++) {
    for ($c = 0; $c -lt $cols; $c++) {
        $cx = $x0 + ($c + 0.5) * $sw
        $cy = $y1 - ($r + 0.5) * $sh
        $out = Join-Path $tmp ("t_{0}_{1}.png" -f $r, $c)
        [ScenePreview]::Render($root, $cx, $cy, $out, 1)
        $im = [System.Drawing.Image]::FromFile($out)
        $g.DrawImage($im, [int]($c * 480 * $final), [int]($r * 270 * $final), [int](480 * $final), [int](270 * $final))
        $im.Dispose()
    }
    Write-Host ("linha {0}/{1}" -f ($r + 1), $rows)
}
$dest = Join-Path $root '..\SeteVidasDocs\Referencias\fortaleza_mapa_arte.png'
$map.Save($dest)
Write-Host "mapa: $dest ($W x $H)"
