# F4c: arte definitiva do bestiário da Floresta (Vespas da Raiz, Galho Seco/Broto, Planta Carnívora, Serpente e a toca).
# Grava em Assets/_Game/Art/Environments/Forest/Placeholder (mesmos nomes que o jogo carrega, .meta preservado) e apaga
# o "_infectado" de cada um: rode depois o menu 21 do Unity, que refaz a variante infectada a partir da arte nova.
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/EnemySprites/gerar_floresta.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $root 'Tools\CharacterSprites\PixelKit.cs'), (Join-Path $here 'ForestBestiaryGen.cs'))
$meta = Join-Path $root 'Tools\LionSprites\sprite_meta_template.txt'
$n = [ForestBestiaryGen]::Run($root, $meta, (Join-Path $root '..\SeteVidasDocs\Referencias\floresta_bestiario_f4c.png'))
Write-Host "bestiário da floresta (F4c): $n (Forest/Placeholder); prévia em SeteVidasDocs/Referencias/floresta_bestiario_f4c.png"
