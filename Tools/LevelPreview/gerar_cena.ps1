# Prévia de UMA TELA da Fortaleza com a arte de verdade (fundo, terreno, rampas, tufos, enfeites, gato),
# em cores e em cinza (teste de leitura). Não precisa do Unity.
# Uso (na raiz do projeto):
#   powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/gerar_cena.ps1 [x] [y] [nome] [escala]
#   x, y = centro da câmera em tiles (padrão: alto da montanha). Saída: SeteVidasDocs/Referencias/cena_<nome>.png e _cinza.png
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$x = if ($args.Count -gt 0) { [float]$args[0] } else { 2.5 }
$y = if ($args.Count -gt 1) { [float]$args[1] } else { 64 }
$name = if ($args.Count -gt 2) { $args[2] } else { 'montanha' }
$scale = if ($args.Count -gt 3) { [int]$args[3] } else { 2 }
$ed = Join-Path $root 'Assets\_Game\Editor'
$rt = Join-Path $root 'Assets\_Game\Scripts\World\Scenery'
Add-Type -ReferencedAssemblies System.Drawing, System.Web.Extensions -Path @(
    (Join-Path $ed 'FortressLayout.cs'), (Join-Path $ed 'Scenery\FortressDressing.cs'), (Join-Path $ed 'Scenery\FortressTerrain.cs'), (Join-Path $ed 'Scenery\FortressBackgrounds.cs'),
    (Join-Path $rt 'TerrainAutotile.cs'), (Join-Path $rt 'DressingRules.cs'), (Join-Path $rt 'SceneryLayers.cs'),
    (Join-Path $here 'ScenePreview.cs'))
$out = Join-Path $root "..\SeteVidasDocs\Referencias\cena_$name.png"
[ScenePreview]::Render($root, $x, $y, $out, $scale)
Write-Host "cena: $out (e _cinza)"
