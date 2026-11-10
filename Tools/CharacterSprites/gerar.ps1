# Gera os sprites, animações e retratos de Koda (chefe/companheiro) e do Contratante (Capivara Necromante).
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/CharacterSprites/gerar.ps1
# Saída: Assets/_Game/Art/Characters/{Koda,Contractor}, ScriptableObjects/Characters/*AnimationSet.asset,
#        Art/UI/Portraits/portrait_{golden,koda_*,contractor*}.png e prévias em SeteVidasDocs/Referencias.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here '..\..')
$meta = Join-Path $root 'Tools\LionSprites\sprite_meta_template.txt'
$refs = Join-Path $root '..\SeteVidasDocs\Referencias'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $here 'PixelKit.cs'), (Join-Path $here 'KodaSpriteGen.cs'), (Join-Path $here 'ContractorSpriteGen.cs'))
[KodaSpriteGen]::Run($root.Path, $meta, $refs)
[ContractorSpriteGen]::Run($root.Path, $meta, $refs)
