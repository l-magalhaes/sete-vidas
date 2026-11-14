# Uma sala do graybox da Floresta em texto, com o alcance (para depurar): o em pé pela rota principal, + por outra
# chegada, . fora de alcance, ! softlock; # chão, - galho, ^ perigo, | vinha.
# Uso: powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/sala_ascii.ps1 Forest_A_Roots A4
param([string]$Scene, [string]$Room)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$ed = Join-Path (Resolve-Path (Join-Path $here '..\..')).Path 'Assets\_Game\Editor'
Add-Type -Path @((Join-Path $ed 'ForestLayout.cs'), (Join-Path $ed 'ForestReach.cs'))
[SeteVidas.EditorTools.ForestReach]::Ascii($Scene, $Room)
