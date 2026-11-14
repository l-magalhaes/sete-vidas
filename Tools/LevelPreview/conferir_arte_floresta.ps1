# Confere a arte da F5 em todas as cenas da Floresta sem o Unity: cada tile, massa, rampa e detalhe que o ForestPainter
# vai pedir tem PNG em Art/Environments/Forest/Terrain. Rode depois de Tools/EnvironmentSprites/gerar.ps1.
# Uso: powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/conferir_arte_floresta.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$ed = Join-Path $root 'Assets\_Game\Editor'
$sc = Join-Path $root 'Assets\_Game\Scripts\World\Scenery'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $ed 'ForestLayout.cs'), (Join-Path $sc 'TerrainAutotile.cs'), (Join-Path $sc 'ForestAutotile.cs'),
    (Join-Path $ed 'Scenery\ForestTerrain.cs'), (Join-Path $ed 'Scenery\ForestBackgrounds.cs'), (Join-Path $here 'ForestArtPreview.cs'))
$r = [ForestArtPreview]::CheckAll($root)
Write-Host $r
if ($r -match 'FALTAM') { exit 1 }
