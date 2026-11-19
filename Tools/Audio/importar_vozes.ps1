# A voz da Vulpis (pedido do usuário, 19/11/2026: "na pasta EfeitosSonoros tem o efeito Audio-vulpis, utilize para as
# falas de Vulpis"). Não precisa do Unity aberto. Uso (na raiz do projeto):
#   powershell -ExecutionPolicy Bypass -File Tools/Audio/importar_vozes.ps1
# Lê ..\EfeitosSonoros\Audio-vulpis.wav (2,6 s, quatro frases sem palavras), corta nas pausas e grava os murmúrios
# vox_vulpis_<emoção>_NN.wav em Assets/_Game/Resources/Audio/SFX/Voz. O diálogo escolhe pela expressão do retrato
# (DialogueRunner.VoiceClipId): serio, ironico, triste, sorriso, surpreso; sem expressão, neutro.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$src = Join-Path (Split-Path -Parent $root) 'EfeitosSonoros\Audio-vulpis.wav'
$out = Join-Path $root 'Assets\_Game\Resources\Audio\SFX\Voz'
Add-Type -Path @((Join-Path $here 'SfxSynth.cs'), (Join-Path $here 'FoleyImport.cs'))
$sr = [Sx]::SR

$y = [FoleyImport]::Load($src)
Write-Host ("Audio-vulpis.wav: {0:0.00} s" -f ($y.Length / $sr))

# as frases: trechos com som separados por pausas de pelo menos 0,12 s abaixo de 1% do pico
$win = [int]($sr * 0.01); $quiet = 0; $start = -1; $phrases = @()
for ($i = 0; $i -lt $y.Length; $i += $win) {
    $p = 0.0
    for ($j = $i; $j -lt [Math]::Min($y.Length, $i + $win); $j++) { $a = [Math]::Abs($y[$j]); if ($a -gt $p) { $p = $a } }
    if ($p -gt 0.01) { if ($start -lt 0) { $start = $i }; $quiet = 0 }
    elseif ($start -ge 0) { $quiet++; if ($quiet * 0.01 -ge 0.12) { $phrases += ,@($start, ($i - ($quiet - 1) * $win)); $start = -1; $quiet = 0 } }
}
if ($start -ge 0) { $phrases += ,@($start, $y.Length) }
Write-Host ("{0} frases" -f $phrases.Count)

function Cut($a, $z) {
    $a = [Math]::Max(0, $a - [int]($sr * 0.015)); $z = [Math]::Min($y.Length, $z + [int]($sr * 0.03))
    $r = New-Object float[] ($z - $a); [Array]::Copy($y, $a, $r, 0, $r.Length)
    return [Sx]::Fade($r, 0.01, 0.04)
}
$clips = @()
foreach ($ph in $phrases) { $clips += ,(Cut $ph[0] $ph[1]) }
# a 3ª e a 4ª são curtas: juntas viram uma frase só
if ($clips.Count -ge 4) { $clips = @($clips[0], $clips[1], [Sx]::Mix(@($clips[2], [Sx]::At(($clips[2].Length / $sr) + 0.06, $clips[3])))) }

# as emoções: altura (o tom da frase) e brilho; a voz fica baixa e com um eco de mata bem curto
$moods = [ordered]@{
    'neutro'   = @{ pitch = 1.00; lp = 0 }
    'serio'    = @{ pitch = 0.95; lp = 0 }
    'ironico'  = @{ pitch = 1.04; lp = 0 }
    'triste'   = @{ pitch = 0.90; lp = 3200 }
    'sorriso'  = @{ pitch = 1.08; lp = 0 }
    'surpreso' = @{ pitch = 1.12; lp = 0 }
}
foreach ($m in $moods.Keys) {
    for ($v = 0; $v -lt $clips.Count; $v++) {
        $c = [Sx]::Pitch($clips[$v], $moods[$m].pitch)
        if ($moods[$m].lp -gt 0) { $c = [Sx]::LP($c, $moods[$m].lp) }
        $c = [Sx]::Reverb($c, 0.08, 0.5, 0.25)
        $c = [Sx]::Normalize([Sx]::Trim($c), -3)   # o nível das outras vozes (Db -3, pelo pico)
        $path = Join-Path $out ("vox_vulpis_{0}_{1:00}.wav" -f $m, ($v + 1))
        [Sx]::WriteWav($path, $c)
        Write-Host ("  vox_vulpis_{0}_{1:00}  {2:0.00} s" -f $m, ($v + 1), ($c.Length / $sr))
    }
}
