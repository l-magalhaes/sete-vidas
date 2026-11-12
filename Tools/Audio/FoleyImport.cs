using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Corta e trata as gravações de foley (Tools/Audio/foley/originais) para o jogo. C# 5 (Add-Type do PowerShell).
// - corrida.wav (23 s de corrida contínua) vira passos soltos: 8 de corrida e 3 de andar devagar (o trecho final,
//   mais fraco), cada um nivelado pela intensidade;
// - pulo_agua.wav vira o respingo grande (o salto de fé, inteiro) e 2 respingos pequenos (só o primeiro impacto,
//   mais curtos e um pouco mais agudos), em mono.
public static class FoleyImport
{
    const int SR = Sx.SR;

    // ------------------------------------------------------------ leitura (PCM 8/16/24/32, float, mono ou estéreo)

    public static float[] Load(string path)
    {
        var b = File.ReadAllBytes(path);
        int sr = 0, ch = 0, bits = 0, fmt = 0, pos = 12, dataPos = -1, dataLen = 0;
        while (pos + 8 <= b.Length)
        {
            string id = Encoding.ASCII.GetString(b, pos, 4);
            int len = BitConverter.ToInt32(b, pos + 4);
            if (id == "fmt ")
            {
                fmt = BitConverter.ToUInt16(b, pos + 8); ch = BitConverter.ToInt16(b, pos + 10);
                sr = BitConverter.ToInt32(b, pos + 12); bits = BitConverter.ToInt16(b, pos + 22);
                if (fmt == 0xFFFE && len >= 26) fmt = BitConverter.ToUInt16(b, pos + 32);
            }
            else if (id == "data") { dataPos = pos + 8; dataLen = Math.Min(len, b.Length - dataPos); break; }
            pos += 8 + len + (len & 1);
        }
        if (dataPos < 0 || ch <= 0 || bits <= 0) throw new Exception("WAV ilegível: " + path);
        int bps = bits / 8, frames = dataLen / (bps * ch);
        var y = new float[frames];
        for (int i = 0; i < frames; i++)
        {
            double s = 0;
            for (int c = 0; c < ch; c++)
            {
                int o = dataPos + (i * ch + c) * bps;
                double v;
                if (fmt == 3 && bits == 32) v = BitConverter.ToSingle(b, o);
                else if (bits == 16) v = BitConverter.ToInt16(b, o) / 32768.0;
                else if (bits == 24) v = (b[o] | (b[o + 1] << 8) | ((sbyte)b[o + 2] << 16)) / 8388608.0;
                else if (bits == 32) v = BitConverter.ToInt32(b, o) / 2147483648.0;
                else v = (b[o] - 128) / 128.0;
                s += v;
            }
            y[i] = (float)(s / ch);   // estéreo vira mono: a posição no jogo vem do Unity
        }
        if (sr != SR) y = Sx.Pitch(y, (double)sr / SR);
        return Sx.HP(y, 35);          // tira o grave inútil (vento, mesa)
    }

    static float[] Slice(float[] y, double from, double to)
    {
        int a = Math.Max(0, (int)(from * SR)), z = Math.Min(y.Length, (int)(to * SR));
        var r = new float[Math.Max(1, z - a)];
        Array.Copy(y, a, r, 0, r.Length);
        return r;
    }

    static double Peak(float[] y, int a, int z)
    {
        double p = 0;
        for (int i = Math.Max(0, a); i < Math.Min(y.Length, z); i++) p = Math.Max(p, Math.Abs(y[i]));
        return p;
    }

    // ------------------------------------------------------------ passos

    public class Step { public double Start, End, PeakDb, Clean; }

    /// <summary>Acha os passos: picos do envelope de 5 ms, com pelo menos 0,13 s entre eles.</summary>
    public static List<Step> FindSteps(float[] y)
    {
        int w = SR / 200;
        var env = new List<double>();
        for (int i = 0; i + w <= y.Length; i += w)
        {
            double a = 0;
            for (int k = 0; k < w; k++) a += y[i + k] * y[i + k];
            env.Add(Math.Sqrt(a / w));
        }
        double max = 0;
        foreach (var e in env) max = Math.Max(max, e);
        var hits = new List<int>();
        for (int i = 3; i < env.Count - 3; i++)
        {
            bool localMax = env[i] >= env[i - 1] && env[i] >= env[i + 1] && env[i] >= env[i - 2] && env[i] >= env[i + 2];
            if (!localMax || env[i] < max * 0.02 || env[i] < env[i - 3] * 2.0) continue;
            if (hits.Count > 0 && (i - hits[hits.Count - 1]) * 0.005 < 0.13)
            {
                if (env[i] > env[hits[hits.Count - 1]]) hits[hits.Count - 1] = i;
                continue;
            }
            hits.Add(i);
        }

        var steps = new List<Step>();
        for (int h = 0; h < hits.Count; h++)
        {
            int peakI = hits[h] * w;
            double localPeak = Peak(y, peakI - SR / 50, peakI + SR / 50);
            // começo: volta até o som cair abaixo de 10% do pico do passo
            int s = peakI;
            while (s > 0 && s > peakI - SR / 20 && Math.Abs(y[s]) > localPeak * 0.1) s--;
            int lookback = SR / 25;
            double before = Peak(y, s - lookback, s - SR / 400);
            s = Math.Max(0, s - SR / 250);  // 4 ms de folga
            // fim: 0,22 s no máximo, antes do próximo passo
            int next = h + 1 < hits.Count ? hits[h + 1] * w - SR / 30 : y.Length;
            int e = Math.Min(Math.Min(s + (int)(0.22 * SR), next), y.Length);
            steps.Add(new Step
            {
                Start = (double)s / SR, End = (double)e / SR,
                PeakDb = 20 * Math.Log10(Math.Max(localPeak, 1e-9)),
                Clean = before / Math.Max(localPeak, 1e-9)   // quanto som sobra antes do passo (menor = mais limpo)
            });
        }
        return steps;
    }

    /// <summary>Escolhe <paramref name="count"/> passos limpos e de volume parecido, espalhados no trecho (variedade).</summary>
    public static List<Step> Pick(List<Step> all, double from, double to, int count)
    {
        var pool = all.FindAll(st => st.Start >= from && st.End <= to && st.End - st.Start >= 0.09);
        if (pool.Count == 0) return pool;
        var dbs = new List<double>();
        foreach (var st in pool) dbs.Add(st.PeakDb);
        dbs.Sort();
        double med = dbs[dbs.Count / 2];
        var good = pool.FindAll(st => Math.Abs(st.PeakDb - med) <= 4 && st.Clean < 0.35);
        if (good.Count < count) good = pool.FindAll(st => st.Clean < 0.5);
        if (good.Count < count) good = pool;
        var pick = new List<Step>();
        for (int i = 0; i < count && i < good.Count; i++)
        {
            int idx = (int)Math.Round(i * (good.Count - 1) / (double)Math.Max(1, count - 1));
            if (pick.Count == 0 || pick[pick.Count - 1] != good[idx]) pick.Add(good[idx]);
        }
        return pick;
    }

    public static float[] CutStep(float[] y, Step st)
    {
        return Sx.Fade(Slice(y, st.Start, st.End), 0.002, 0.03);
    }

    // ------------------------------------------------------------ água

    /// <summary>O respingo inteiro (o salto de fé): do primeiro impacto até o fim das gotas, sem o silêncio.</summary>
    public static float[] BigSplash(float[] y)
    {
        double start = FirstSound(y);
        return Sx.Fade(Slice(y, Math.Max(0, start - 0.01), start + 1.6), 0.003, 0.35);
    }

    /// <summary>Um respingo pequeno: um trecho curto, mais agudo (corpo menor).</summary>
    public static float[] SmallSplash(float[] y, double at, double length, double pitch)
    {
        var s = Sx.Fade(Slice(y, at, at + length * pitch), 0.003, length * 0.4);
        return Sx.Pitch(s, pitch);
    }

    public static double FirstSound(float[] y)
    {
        double p = Peak(y, 0, y.Length);
        for (int i = 0; i < y.Length; i++) if (Math.Abs(y[i]) > p * 0.08) return (double)i / SR;
        return 0;
    }
}
