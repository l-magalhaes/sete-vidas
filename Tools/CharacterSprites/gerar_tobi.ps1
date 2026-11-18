# Gera só a Tobi (sprites, animações, TobiAnimationSet.asset e retratos portrait_tobi*, ponte_madeira_*). Ver TobiSpriteGen.cs.
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/CharacterSprites/gerar_tobi.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here '..\..')
$meta = Join-Path $root 'Tools\LionSprites\sprite_meta_template.txt'
$refs = Join-Path $root '..\SeteVidasDocs\Referencias'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $here 'PixelKit.cs'), (Join-Path $here 'TobiSpriteGen.cs'))
[TobiSpriteGen]::Run($root.Path, $meta, $refs)
