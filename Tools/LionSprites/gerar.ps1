# Gera os sprites do Lion (PNG + .meta), o asset LionAnimationSet, os retratos de diálogo e as prévias em SeteVidasDocs.
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/LionSprites/gerar.ps1
# A cauda e a capa não são sprites: são desenhadas em jogo por Assets/_Game/Scripts/Player/SoftBody.cs.
# Este script compila o mesmo arquivo para desenhar a prévia delas (lion_cauda_capa.png).
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here '..\..')
$soft = Join-Path $root 'Assets\_Game\Scripts\Player\SoftBody.cs'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $here 'LionSpriteGen.cs'), (Join-Path $here 'LionPortraits.cs'), $soft)
$refs = Join-Path $root '..\SeteVidasDocs\Referencias'
$meta = Join-Path $here 'sprite_meta_template.txt'
[LionSpriteGen]::Run($root.Path, $meta, (Join-Path $refs 'lion_spritesheet_preview.png'))
[LionSpriteGen]::SoftPreview((Join-Path $refs 'lion_cauda_capa.png'), 6)
[LionSpriteGen]::Portraits($root.Path, $meta, (Join-Path $refs 'lion_retratos.png'))
