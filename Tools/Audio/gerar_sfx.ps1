# SFX e vozes provisórios (Docs/Audio/plano-sfx.md), sintetizados por código. Não precisa do Unity aberto.
# Uso (na raiz do projeto):
#   powershell -ExecutionPolicy Bypass -File Tools/Audio/gerar_sfx.ps1              (todos)
#   powershell -ExecutionPolicy Bypass -File Tools/Audio/gerar_sfx.ps1 guarda        (só os que têm "guarda" no nome)
# Saída: Assets/_Game/Resources/Audio/SFX/<pasta>/<nome>_NN.wav. O jogo carrega tudo dessa pasta (Sfx.Play("id")).
param([string]$only = '')
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$out = Join-Path $root 'Assets\_Game\Resources\Audio\SFX'
Add-Type -Path @((Join-Path $here 'SfxSynth.cs'), (Join-Path $here 'SfxBank.cs'))

$sw = [Diagnostics.Stopwatch]::StartNew()
$files = 0
foreach ($e in [SfxBank]::All()) {
    if ($only -ne '' -and -not $e.Name.Contains($only)) { continue }
    for ($v = 0; $v -lt $e.Count; $v++) {
        $y = $e.Make.Invoke($v)
        if (-not $e.Loop) { $y = [Sx]::Trim($y) }
        if ($e.Loudness) { $y = [Sx]::Loudness($y, -18, -1) } else { $y = [Sx]::Normalize($y, $e.Db) }
        $path = Join-Path $out ("{0}\{1}_{2:00}.wav" -f $e.Folder, $e.Name, ($v + 1))
        [Sx]::WriteWav($path, $y)
        $files++
    }
}
Write-Host ("{0} arquivos em {1:n1} s -> {2}" -f $files, $sw.Elapsed.TotalSeconds, $out)
