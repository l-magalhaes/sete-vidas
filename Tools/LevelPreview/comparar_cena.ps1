# Compara a cena salva da Fortaleza com o que o código gera (FortressLayout e FortressDressing): lista o que foi
# movido, apagado ou acrescentado à mão no Unity, para passar para o código (o menu 13 recria a cena a partir dele).
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/comparar_cena.ps1
# Saída: SeteVidasDocs/Referencias/comparar_cena.txt. Os enfeites ficam um pouco acima do ponto do código (pivô no meio
# do desenho): o "dy" já desconta o deslocamento normal de cada desenho. Fileiras aparecem numeradas na cena (ex.:
# "Tapete do salão 1..12") e santuários/gatilhos/dicas com outro nome; não são mudanças.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$ed = Join-Path $root 'Assets\_Game\Editor'
$rt = Join-Path $root 'Assets\_Game\Scripts\World\Scenery'
Add-Type -ReferencedAssemblies System.Drawing, System.Web.Extensions -Path @(
    (Join-Path $ed 'FortressLayout.cs'), (Join-Path $ed 'Scenery\FortressDressing.cs'), (Join-Path $ed 'Scenery\FortressTerrain.cs'), (Join-Path $ed 'Scenery\FortressBackgrounds.cs'),
    (Join-Path $rt 'TerrainAutotile.cs'), (Join-Path $rt 'DressingRules.cs'), (Join-Path $rt 'SceneryLayers.cs'),
    (Join-Path $here 'SceneDiff.cs'))
$out = Join-Path $root '..\SeteVidasDocs\Referencias\comparar_cena.txt'
[SceneDiff]::Run((Join-Path $root 'Assets\_Game\Scenes\World\Fortress_MVP.unity')) | Out-File -Encoding utf8 $out
Write-Host "comparação: $out"
