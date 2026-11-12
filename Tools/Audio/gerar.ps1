# Rascunhos das músicas (Docs/Audio/plano-musicas.md), sintetizados por código.
# Não precisa do Unity aberto. Uso (na raiz do projeto):
#   powershell -ExecutionPolicy Bypass -File Tools/Audio/gerar.ps1            (todas)
#   powershell -ExecutionPolicy Bypass -File Tools/Audio/gerar.ps1 tema       (só uma)
param([string]$only = '')
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$out = Join-Path $here 'rascunhos'   # fora do Assets: as músicas do jogo são as do Treblo
Add-Type -Path @((Join-Path $here 'MusicSynth.cs'), (Join-Path $here 'MusicSongs.cs'))

$songs = [ordered]@{
    'tema' = @{ file = 'rascunho_tema_principal.wav'; make = { [MusicSongs]::TemaPrincipal() } }
}
foreach ($k in $songs.Keys) {
    if ($only -ne '' -and $only -ne $k) { continue }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $song = & $songs[$k].make
    $pcm = [MusicSynth]::Render($song)
    $path = Join-Path $out $songs[$k].file
    [MusicSynth]::WriteWav($path, $pcm)
    $secs = [Math]::Round($pcm.Length / 2 / [MusicSynth]::SR, 1)
    Write-Host ("{0}: {1} s ({2} notas) em {3:n1} s" -f $songs[$k].file, $secs, $song.Notes.Count, $sw.Elapsed.TotalSeconds)
}
