# Biblioteca de cenário (plano de assets e cenários da Fortaleza, Etapa 1):
#  1. exporta a paleta mestre (EnvPalette.cs) para Docs/Arte/paleta.md, .gpl, .hex e a prévia em SeteVidasDocs/Referencias;
#  2. garante as pastas da biblioteca em Assets/_Game/Art/Environments;
#  3. valida todos os manifest.json: ids únicos, campos válidos, imagens existentes e cores reservadas nos enfeites.
# Não precisa do Unity aberto. Uso (na raiz do projeto):
#   powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/gerar.ps1
# Sai com código 1 se houver erro no manifesto (avisos de cor não bloqueiam).
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$refs = Join-Path $root '..\SeteVidasDocs\Referencias'
$meta = Join-Path $root 'Tools\LionSprites\sprite_meta_template.txt'
Add-Type -ReferencedAssemblies System.Drawing -Path @((Join-Path $here 'EnvPalette.cs'), (Join-Path $root 'Tools\CharacterSprites\PixelKit.cs'), (Join-Path $here 'PropSpriteGen.cs'),
    (Join-Path $root 'Assets\_Game\Scripts\World\Scenery\TerrainAutotile.cs'), (Join-Path $here 'TerrainSpriteGen.cs'), (Join-Path $here 'WaterSpriteGen.cs'), (Join-Path $here 'ArchitectureSpriteGen.cs'))

# 1) paleta
[EnvPalette]::Export($root, $refs)
Write-Host "paleta: Docs/Arte/paleta.md, paleta_fortaleza.gpl, paleta_fortaleza.hex"

# 1b) objetos de cenário desenhados por código (prévias em SeteVidasDocs/Referencias)
[PropSpriteGen]::Run($root, $meta, $refs)
Write-Host "objetos: tocha de parede (Common/env_com_obj_wall_torch_0..3)"
[TerrainSpriteGen]::Run($root, $meta, $refs)
Write-Host "terreno: tilesets ext_solo e ext_rocha, rampas, tufos, seixos, rochas, fosseis (Exterior), placas (Primates)"
[WaterSpriteGen]::Run($root, $meta, $refs)
Write-Host "agua: cachoeira, nascente, espuma e lagoa (Exterior/Water)"
[ArchitectureSpriteGen]::Run($root, $meta, $refs)
Write-Host "arquitetura: fundacao, soluki, reforma, abandono, madeira, terra batida, gargulas (Citadel/Architecture)"

# 2) pastas da biblioteca (plano, 3.1 e 7.9). Unity cria os .meta das pastas ao abrir.
$art = Join-Path $root 'Assets\_Game\Art\Environments'
$folders = 'Common', 'Exterior', 'Soluki', 'Citadel', 'Primates', 'Necro',
           'Backgrounds\Sky', 'Backgrounds\Ext', 'Backgrounds\Cid', 'Backgrounds\Mia', 'Backgrounds\Interior'
foreach ($f in $folders) {
    $dir = Join-Path $art $f
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }
    $keep = Join-Path $dir '.gitkeep'
    if (-not (Test-Path $keep) -and -not (Get-ChildItem $dir -File -Filter *.png -ErrorAction SilentlyContinue)) { New-Item -ItemType File $keep | Out-Null }
}

# 3) validação dos manifestos
$layers = 'ceu', 'bg_longe', 'bg_medio', 'bg_perto', 'parede_fundo', 'decor_atras', 'jogo', 'terreno_detalhe', 'decor_frente', 'fg'
$functions = 'decorativo', 'colisor', 'plataforma', 'interativo', 'gate', 'perigo', 'esconderijo', 'luz', 'narrativo'
$supports = 'chao', 'teto', 'parede', 'borda', 'livre'
$checkColors = 'decorativo', 'narrativo', 'luz'   # gates, perigos e esconderijos podem usar as cores reservadas
$errors = 0; $warnings = 0; $count = 0
$ids = @{}
foreach ($m in Get-ChildItem $art -Recurse -Filter manifest.json) {
    $rel = $m.FullName.Substring($root.Length + 1)
    try { $json = Get-Content $m.FullName -Raw -Encoding UTF8 | ConvertFrom-Json }
    catch { Write-Host "ERRO $rel : JSON inválido ($($_.Exception.Message))" -ForegroundColor Red; $errors++; continue }
    foreach ($a in $json.assets) {
        $count++
        $id = $a.id
        $where = "$id ($rel)"
        if (-not $id) { Write-Host "ERRO $rel : entrada sem id" -ForegroundColor Red; $errors++; continue }
        if ($ids.ContainsKey($id)) { Write-Host "ERRO id repetido $id em $rel e $($ids[$id])" -ForegroundColor Red; $errors++ } else { $ids[$id] = $rel }
        $layer = if ($a.camada) { $a.camada } else { 'decor_atras' }
        $func = if ($a.funcao) { $a.funcao } else { 'decorativo' }
        $support = if ($a.posicionamento -and $a.posicionamento.apoio) { $a.posicionamento.apoio } else { 'livre' }
        if ($layers -notcontains $layer) { Write-Host "ERRO $where : camada '$layer'" -ForegroundColor Red; $errors++ }
        if ($functions -notcontains $func) { Write-Host "ERRO $where : função '$func'" -ForegroundColor Red; $errors++ }
        if ($supports -notcontains $support) { Write-Host "ERRO $where : apoio '$support'" -ForegroundColor Red; $errors++ }
        if ($a.categoria -eq 'plx' -and (-not $a.fundo -or @($a.fundo.paralaxe).Count -lt 2)) { Write-Host "ERRO $where : fundo sem paralaxe [x, y]" -ForegroundColor Red; $errors++ }
        $prefixed = @()
        if ($a.sprites_prefixo) {
            $full = Join-Path $m.DirectoryName $a.sprites_prefixo
            $folder = Split-Path -Parent $full; $leaf = Split-Path -Leaf $full
            $prefixed = @(Get-ChildItem $folder -Filter "$leaf*.png" -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName.Substring($m.DirectoryName.Length + 1) })
            if ($prefixed.Count -eq 0) { Write-Host "ERRO $where : nenhuma imagem com o prefixo $($a.sprites_prefixo)" -ForegroundColor Red; $errors++ }
        }
        $images = @($a.sprites) + $prefixed + @($a.animacao.quadros) | Where-Object { $_ } | Select-Object -Unique
        if (@($a.sprites | Where-Object { $_ }).Count -eq 0 -and $prefixed.Count -eq 0) { Write-Host "ERRO $where : sem sprites" -ForegroundColor Red; $errors++ }
        foreach ($img in $images) {
            $path = Join-Path $m.DirectoryName $img
            if (-not (Test-Path $path)) {
                # sprites desenhados pelo editor (CitadelDecorSetup) podem ainda não existir num clone novo
                Write-Host "AVISO $where : imagem ainda não existe $img (é criada ao gerar a cena?)" -ForegroundColor Yellow; $warnings++; continue
            }
            if ($checkColors -contains $func) {
                foreach ($hit in [EnvValidator]::FindReserved($path, $a.categoria -eq 'plx')) {
                    Write-Host "AVISO $where : $img usa cor reservada $hit" -ForegroundColor Yellow; $warnings++
                }
            }
        }
    }
}
Write-Host ("biblioteca: {0} assets, {1} erro(s), {2} aviso(s)" -f $count, $errors, $warnings)
if ($errors -gt 0) { exit 1 }
