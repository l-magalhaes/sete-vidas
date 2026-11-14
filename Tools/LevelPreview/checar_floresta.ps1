# Confere o graybox da Floresta (Assets/_Game/Editor/ForestLayout.cs) sem abrir o Unity: baús e saídas alcançáveis,
# softlock, e desenha um PNG por cena com o alcance por cima (verde = alcançável, cinza = fora, vermelho = softlock).
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/checar_floresta.ps1 [-v]
# Saída: SeteVidasDocs/Referencias/floresta/floresta_<cena>.png e o relatório no console.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$ed = Join-Path $root 'Assets\_Game\Editor'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $ed 'ForestLayout.cs'), (Join-Path $ed 'ForestReach.cs'), (Join-Path $here 'ForestPreview.cs'))
$out = Join-Path $root '..\SeteVidasDocs\Referencias\floresta'
$verbose = $args -contains '-v'
[ForestPreview]::Run($out, 6, $verbose)
Write-Host "PNGs: $out"
