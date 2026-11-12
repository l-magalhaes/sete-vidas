# Foley gravado (Tools/Audio/foley/originais) cortado e tratado para o jogo. Não precisa do Unity aberto.
# Uso (na raiz do projeto): powershell -ExecutionPolicy Bypass -File Tools/Audio/importar_foley.ps1
#   corrida.wav   -> sfx_gato_passo_01..08 (correndo) e sfx_gato_passo_lento_01..03 (andando devagar)
#   pulo_agua.wav -> sfx_gato_splash_grande_01 (o salto de fé) e sfx_gato_splash_01..02 (entrar na água)
# Estes sons NÃO saem do gerar_sfx.ps1 (as receitas sintéticas deles foram desligadas no SfxBank).
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$src = Join-Path $here 'foley\originais'
$out = Join-Path $root 'Assets\_Game\Resources\Audio\SFX\Gato'
Add-Type -Path @((Join-Path $here 'SfxSynth.cs'), (Join-Path $here 'FoleyImport.cs'))

function Save($name, $y) {
    $y = [Sx]::Loudness($y, -18, -1)   # o mesmo padrão dos outros SFX: a mixagem fica no SfxCatalog
    [Sx]::WriteWav((Join-Path $out "$name.wav"), $y)
    Write-Host ("  {0}  {1:0.00} s" -f $name, ($y.Length / [Sx]::SR))
}

# ---------------------------------------------------------------- passos
$run = [FoleyImport]::Load((Join-Path $src 'corrida.wav'))
$steps = [FoleyImport]::FindSteps($run)
Write-Host ("corrida.wav: {0} passos encontrados" -f $steps.Count)

$fast = [FoleyImport]::Pick($steps, 2.0, 19.2, 8)    # corrida no ritmo (0,24 s entre passos)
$i = 1
foreach ($st in $fast) {
    Write-Host ("  passo {0}: {1:0.00}-{2:0.00} s ({3:0.0} dB)" -f $i, $st.Start, $st.End, $st.PeakDb)
    Save ("sfx_gato_passo_{0:00}" -f $i) ([FoleyImport]::CutStep($run, $st)); $i++
}
$slow = [FoleyImport]::Pick($steps, 19.3, 22.0, 3)   # o fim, mais fraco: desacelerando
$i = 1
foreach ($st in $slow) {
    Write-Host ("  lento {0}: {1:0.00}-{2:0.00} s ({3:0.0} dB)" -f $i, $st.Start, $st.End, $st.PeakDb)
    Save ("sfx_gato_passo_lento_{0:00}" -f $i) ([FoleyImport]::CutStep($run, $st)); $i++
}

# ---------------------------------------------------------------- água
$water = [FoleyImport]::Load((Join-Path $src 'pulo_agua.wav'))
$first = [FoleyImport]::FirstSound($water)
Write-Host ("pulo_agua.wav: primeiro impacto em {0:0.00} s" -f $first)
Save 'sfx_gato_splash_grande_01' ([FoleyImport]::BigSplash($water))
Save 'sfx_gato_splash_01' ([FoleyImport]::SmallSplash($water, [Math]::Max(0, $first - 0.005), 0.30, 1.12))
Save 'sfx_gato_splash_02' ([FoleyImport]::SmallSplash($water, $first + 0.28, 0.30, 1.2))
