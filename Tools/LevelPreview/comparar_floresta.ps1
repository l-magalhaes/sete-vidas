# Lista o que o usuário mexeu à mão nas cenas da Floresta, para passar para o Assets/_Game/Editor/ForestLayout.cs
# (FX = sala.X + 1 + x local; FY = sala.Y + 1 + y local). Depois, rode checar_floresta.ps1.
# Uso (na raiz do projeto):
#   powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/comparar_floresta.ps1           # cena salva × ForestLayout
#   powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/comparar_floresta.ps1 -Git HEAD # cena salva × a do commit
# O modo padrão compara com o que o gerador monta HOJE (vale mesmo com as cenas fora do git): SUMIU, MOVEU, DESLIGOU,
# DESENHO (só o filho arrastado) e NOVO. Câmeras e filhos que andam junto com o pai não aparecem.
# Saída: SeteVidasDocs/Referencias/floresta/comparar_floresta.txt
param([string]$Git = '')
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$ed = Join-Path $root 'Assets\_Game\Editor'
Add-Type -ReferencedAssemblies System.Core -Path @((Join-Path $ed 'ForestLayout.cs'), (Join-Path $here 'SceneCmp.cs'), (Join-Path $here 'LayoutCmp.cs'))
$outDir = Join-Path $root '..\SeteVidasDocs\Referencias\floresta'
New-Item -ItemType Directory -Force $outDir | Out-Null
$out = Join-Path $outDir 'comparar_floresta.txt'
$tmp = Join-Path $env:TEMP 'sv_cena_antes.unity'
$sb = New-Object System.Text.StringBuilder
$names = @('Forest_Road','Forest_A_Roots','Forest_B_Thorns','Forest_C_Shadows','Forest_D_Refuge','Forest_E_Nest','Forest_F_Heart')
if ($Git) {
    foreach ($n in $names) {
        $rel = "Assets/_Game/Scenes/World/Forest/$n.unity"
        [void]$sb.AppendLine("===== $n =====")
        git -C $root show "${Git}:$rel" | Out-File -Encoding utf8 $tmp
        [void]$sb.AppendLine([SceneCmp]::Run($tmp, (Join-Path $root $rel)))
    }
} else {
    $paths = [string[]]($names | ForEach-Object { Join-Path $root "Assets/_Game/Scenes/World/Forest/$_.unity" })
    [void]$sb.Append([LayoutCmp]::Run($paths, [string[]]$names))
}
Remove-Item $tmp -ErrorAction SilentlyContinue
$sb.ToString() | Out-File -Encoding utf8 $out
Write-Host "comparação: $out"
