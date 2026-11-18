# Só o botão das armadilhas (botao, botao_apertado): a placa de pressão de pedra com a pata acesa. Grava em
# Assets/_Game/Art/Environments/Forest/Placeholder mantendo o .meta (as cenas pegam o desenho novo sem regenerar).
# Uso (na raiz do projeto): powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/gerar_botao.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$meta = Join-Path $root 'Tools\LionSprites\sprite_meta_template.txt'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $root 'Tools\CharacterSprites\PixelKit.cs'), (Join-Path $here 'TrapButtonGen.cs'))
[TrapButtonGen]::Run($root, $meta)
Write-Host "botão das armadilhas: botao.png e botao_apertado.png (Forest/Placeholder)"
