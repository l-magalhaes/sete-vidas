# Gera só a Vulpis (sprites, animações, VulpisAnimationSet.asset e retratos portrait_vulpis*). Ver VulpisSpriteGen.cs.
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/CharacterSprites/gerar_vulpis.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here '..\..')
$meta = Join-Path $root 'Tools\LionSprites\sprite_meta_template.txt'
$refs = Join-Path $root '..\SeteVidasDocs\Referencias'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $here 'PixelKit.cs'), (Join-Path $here 'VulpisSpriteGen.cs'))
[VulpisSpriteGen]::Run($root.Path, $meta, $refs)
