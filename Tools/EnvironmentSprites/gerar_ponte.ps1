# Só os blocos da ponte de pedra da Estrada (Forest_Road, R4), na quantidade que o ForestLayout pede (ponte_pedra_<n>_<i>).
# Grava em Assets/_Game/Art/Environments/Forest/Placeholder sem regravar o resto (o gerar.ps1 completo também os grava).
# Não precisa do Unity aberto. Uso (na raiz do projeto):
#   powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/gerar_ponte.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$meta = Join-Path $root 'Tools\LionSprites\sprite_meta_template.txt'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $root 'Tools\CharacterSprites\PixelKit.cs'),
    (Join-Path $root 'Assets\_Game\Scripts\World\Scenery\TerrainAutotile.cs'), (Join-Path $root 'Assets\_Game\Scripts\World\Scenery\ForestAutotile.cs'),
    (Join-Path $root 'Assets\_Game\Editor\ForestLayout.cs'), (Join-Path $here 'ForestPropGen.cs'))
$n = [ForestPropGen]::RunBridges($root, $meta)
Write-Host "ponte de pedra: blocos para $n (Forest/Placeholder)"
