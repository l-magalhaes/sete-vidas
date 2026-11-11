# Catálogo da biblioteca de cenário (Etapa 9 do plano de assets): lê todos os manifest.json de
# Assets/_Game/Art/Environments e gera Docs/Arte/catalogo-cenario.md com miniatura, id, nome, função, camada,
# variações, animação e a regra de uso ("leitura") de cada asset, agrupados por pasta.
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/gerar_catalogo.ps1
# Rode de novo sempre que um manifesto mudar: o catálogo nunca é editado à mão.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$art = Join-Path $root 'Assets\_Game\Art\Environments'
$out = Join-Path $root 'Docs\Arte\catalogo-cenario.md'

function Rel([string]$full) {
    # caminho relativo a Docs/Arte, com barras normais (o preview de Markdown mostra a imagem)
    $p = $full.Substring($root.Length + 1).Replace('\', '/')
    return '../../' + ($p -replace ' ', '%20')
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Catálogo da biblioteca de cenário')
$lines.Add('')
$lines.Add('> Gerado por `Tools/EnvironmentSprites/gerar_catalogo.ps1` a partir dos `manifest.json`. **Não edite à mão**: mude o manifesto e rode o script de novo. Como usar a biblioteca: `guia-de-assets.md`.')
$lines.Add('')
$total = 0
$sections = New-Object System.Collections.Generic.List[string]
foreach ($m in Get-ChildItem $art -Recurse -Filter manifest.json | Sort-Object FullName) {
    $json = Get-Content $m.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    $folder = $m.DirectoryName.Substring($art.Length + 1).Replace('\', '/')
    $sections.Add("## $folder")
    $sections.Add('')
    $sections.Add('| | id | nome | função | camada | variações | regra de uso |')
    $sections.Add('|---|---|---|---|---|---|---|')
    foreach ($a in $json.assets) {
        $total++
        $sprites = @($a.sprites | Where-Object { $_ })
        $prefix = $a.sprites_prefixo
        $count = $sprites.Count
        if ($prefix) {
            $pfull = Join-Path $m.DirectoryName $prefix
            $found = @(Get-ChildItem (Split-Path $pfull -Parent) -Filter ((Split-Path $pfull -Leaf) + '*.png') -ErrorAction SilentlyContinue)
            $count += $found.Count
            if ($sprites.Count -eq 0 -and $found.Count -gt 0) { $sprites = @($found[0].FullName.Substring($m.DirectoryName.Length + 1)) }
        }
        $thumb = ''
        if ($sprites.Count -gt 0) {
            $img = Join-Path $m.DirectoryName $sprites[0]
            if (Test-Path $img) { $thumb = '![](' + (Rel $img) + ')' }
        }
        $var = "$count"
        if ($a.animacao -and $a.animacao.quadros) { $var += ' · anim ' + @($a.animacao.quadros).Count + 'q' }
        if ($a.animacao -and $a.animacao.quadros_por_variacao) { $var += ' · anim ' + $a.animacao.quadros_por_variacao + 'q/var' }
        if ($a.categoria -eq 'plx' -and $a.fundo) { $var += ' · paralaxe ' + (@($a.fundo.paralaxe) -join '/') }
        $camada = $a.camada; if ($a.sorting) { $camada += ' (' + $a.sorting + ')' }
        $rule = ($a.leitura -replace '\|', '/' -replace "`r?`n", ' ')
        if ($a.posicionamento -and $a.posicionamento.apoio) { $rule = '**apoio ' + $a.posicionamento.apoio + '.** ' + $rule }
        $sections.Add("| $thumb | ``$($a.id)`` | $($a.nome) | $($a.funcao) | $camada | $var | $rule |")
    }
    $sections.Add('')
}
$lines.Add("**$total assets** em " + @(Get-ChildItem $art -Recurse -Filter manifest.json).Count + ' manifestos.')
$lines.Add('')
foreach ($s in $sections) { $lines.Add($s) }
[System.IO.File]::WriteAllLines($out, $lines, (New-Object System.Text.UTF8Encoding $false))
Write-Host "catálogo: $out ($total assets)"
