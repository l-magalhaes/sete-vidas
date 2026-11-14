# Uma tela da Floresta com a arte da F5 (fundo, massa, chão, vinha, rampas, detalhes), sem abrir o Unity.
# Uso: powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/ver_floresta.ps1 <cena> <sala> [dx] [dy]
#   ex.: ... ver_floresta.ps1 Forest_A_Roots A3 -8 4   (câmera no meio da A3, 8 tiles à esquerda e 4 acima do chão)
# Saída: SeteVidasDocs/Referencias/floresta/arte_<cena>_<sala>.png (2×). Rode antes Tools/EnvironmentSprites/gerar.ps1.
param([string]$Scene, [string]$Room, [float]$Dx = 0, [float]$Dy = 0)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$ed = Join-Path $root 'Assets\_Game\Editor'
$sc = Join-Path $root 'Assets\_Game\Scripts\World\Scenery'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $ed 'ForestLayout.cs'), (Join-Path $sc 'TerrainAutotile.cs'), (Join-Path $sc 'ForestAutotile.cs'),
    (Join-Path $ed 'Scenery\ForestTerrain.cs'), (Join-Path $ed 'Scenery\ForestBackgrounds.cs'), (Join-Path $here 'ForestArtPreview.cs'))
$c = [ForestArtPreview]::RoomCenter($Scene, $Room, $Dx, $Dy)
$out = Join-Path $root "..\SeteVidasDocs\Referencias\floresta\arte_$($Scene)_$Room.png"
$miss = [ForestArtPreview]::Render($root, $Scene, $c.X, $c.Y, $out, 2)
Write-Host "tela: $out (camera $($c.X), $($c.Y)); sem arte: $miss"
