# Só os desenhos do Ninho Adormecido (F7): a cabeça do bote, a sombra no chão, a escama e a folha.
# Grava em Assets/_Game/Art/Environments/Forest/Placeholder sem regravar o resto do ForestLoreGen (o gerar.ps1 completo
# também os grava). Não precisa do Unity aberto. Uso (na raiz do projeto):
#   powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/gerar_ninho.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$meta = Join-Path $root 'Tools\LionSprites\sprite_meta_template.txt'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $root 'Tools\CharacterSprites\PixelKit.cs'),
    (Join-Path $root 'Assets\_Game\Scripts\World\Scenery\TerrainAutotile.cs'), (Join-Path $here 'ForestLoreGen.cs'))
[ForestLoreGen]::RunNest($root, $meta)
Write-Host "ninho (F7): isy_bote_0/1, isy_aviso, isy_escama, folha_caindo (Forest/Placeholder)"
