# Gera os sprites do Cão Templário (PNG + .meta), o asset TemplarAnimationSet e a prévia em SeteVidasDocs.
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/EnemySprites/gerar.ps1
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here '..\..')
Add-Type -ReferencedAssemblies System.Drawing -Path (Join-Path $here 'TemplarSpriteGen.cs')
$meta = Join-Path $root 'Tools\LionSprites\sprite_meta_template.txt'
[TemplarSpriteGen]::Run($root.Path, $meta, (Join-Path $root '..\SeteVidasDocs\Referencias\templario_animacoes.png'))
