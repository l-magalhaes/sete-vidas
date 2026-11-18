# Prévia da Isyathy do Ninho: cabeça (dormindo, acordada, mordendo), cratera e o corpo em duas poses.
# O desenho é feito em código (Assets/_Game/Scripts/World/Forest/IsyathyArt.cs e SerpentRaster.cs): não há PNG para
# gerar; isto só mostra como ficou. Uso (na raiz do projeto):
#   powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/previa_isyathy.ps1 [saida.png]
param([string]$out = '')
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
if ($out -eq '') { $out = Join-Path $root '..\SeteVidasDocs\Referencias\isyathy_previa.png' }
$forest = Join-Path $root 'Assets\_Game\Scripts\World\Forest'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $forest 'IsyathyArt.cs'), (Join-Path $forest 'SerpentRaster.cs'), (Join-Path $here 'IsyathyPreview.cs'))
New-Item -ItemType Directory -Force (Split-Path -Parent $out) | Out-Null
[IsyathyPreview]::Run($out)
Write-Host "prévia: $out"
