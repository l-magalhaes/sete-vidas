# Desenha o mapa da Fortaleza (FortressLayout.cs) em SeteVidasDocs/Referencias/fortaleza_mapa.png, sem abrir o Unity.
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/gerar_mapa.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here '..\..')
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $root 'Assets\_Game\Editor\FortressLayout.cs'), (Join-Path $root 'Assets\_Game\Editor\Scenery\FortressDressing.cs'), (Join-Path $here 'FortressPreview.cs'))
$out = if ($args.Count -gt 0) { $args[0] } else { Join-Path $root '..\SeteVidasDocs\Referencias\fortaleza_mapa.png' }
[FortressPreview]::Render($out, 6)
