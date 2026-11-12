using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Sintetizador dos efeitos sonoros provisórios (Docs/Audio/plano-sfx.md). C# 5 (roda pelo Add-Type do PowerShell).
// Blocos: osciladores, ruído, filtros que variam no tempo, envelopes, reverb, e os "instrumentos" de foley
// (baque, clique, sopro, metal, sino FM, corda dedilhada). As vozes sem palavras usam um sintetizador de formantes
// (Speak): sílabas inventadas, pulso glótico, 3 formantes, consoantes de ruído e entonação por frase.
// Saída: WAV 16 bits mono 44,1 kHz.

public delegate double Curve(double t);

public static class Sx
{
    public const int SR = 44100;

    public static int N(double sec) { return Math.Max(1, (int)Math.Round(sec * SR)); }
    public static double C01(double x) { return x < 0 ? 0 : x > 1 ? 1 : x; }

    // ------------------------------------------------------------ curvas (t em segundos)

    public static Curve K(double v) { return delegate(double t) { return v; }; }
    public static Curve Lin(double a, double b, double dur) { return delegate(double t) { return a + (b - a) * C01(t / dur); }; }
    /// <summary>De a até b em progressão geométrica (bom para frequência).</summary>
    public static Curve Geo(double a, double b, double dur) { return delegate(double t) { return a * Math.Pow(b / a, C01(t / dur)); }; }
    /// <summary>Ataque linear e queda exponencial (constante de tempo <paramref name="decay"/>).</summary>
    public static Curve Perc(double attack, double decay)
    {
        return delegate(double t) { return t < attack ? t / Math.Max(1e-5, attack) : Math.Exp(-(t - attack) / decay); };
    }
    /// <summary>Ataque, plateau e soltura lineares.</summary>
    public static Curve AHR(double a, double hold, double release)
    {
        return delegate(double t)
        {
            if (t < a) return t / Math.Max(1e-5, a);
            if (t < a + hold) return 1;
            return 1 - C01((t - a - hold) / Math.Max(1e-5, release));
        };
    }
    /// <summary>Sobe devagar até o fim (crescendo) e corta rápido.</summary>
    public static Curve Swell(double dur, double power)
    {
        return delegate(double t) { double k = C01(t / dur); return t > dur ? Math.Max(0, 1 - (t - dur) / 0.02) : Math.Pow(k, power); };
    }

    // ------------------------------------------------------------ fontes

    public enum W { Sine, Square, Tri, Saw, Pulse25 }

    public static float[] Osc(W w, double dur, Curve freq, Curve amp)
    {
        var y = new float[N(dur)];
        double ph = 0;
        for (int i = 0; i < y.Length; i++)
        {
            double t = (double)i / SR;
            ph += freq(t) / SR;
            ph -= Math.Floor(ph);
            double s;
            switch (w)
            {
                case W.Square: s = ph < 0.5 ? 0.6 : -0.6; break;
                case W.Pulse25: s = ph < 0.25 ? 0.6 : -0.6; break;
                case W.Tri: s = ph < 0.5 ? 4 * ph - 1 : 3 - 4 * ph; break;
                case W.Saw: s = 2 * ph - 1; break;
                default: s = Math.Sin(2 * Math.PI * ph); break;
            }
            y[i] = (float)(s * amp(t));
        }
        return y;
    }

    public static float[] Noise(double dur, Curve amp, int seed)
    {
        var r = new Random(seed);
        var y = new float[N(dur)];
        for (int i = 0; i < y.Length; i++) y[i] = (float)((r.NextDouble() * 2 - 1) * amp((double)i / SR));
        return y;
    }

    /// <summary>Ruído marrom (grave, para estrondo e vento).</summary>
    public static float[] Brown(double dur, Curve amp, int seed)
    {
        var r = new Random(seed);
        var y = new float[N(dur)];
        double v = 0;
        for (int i = 0; i < y.Length; i++)
        {
            v = (v + (r.NextDouble() * 2 - 1) * 0.08) * 0.995;
            y[i] = (float)(v * 3.5 * amp((double)i / SR));
        }
        return y;
    }

    /// <summary>Impulsos aleatórios (estalos, cascalho, unhas, folhas). <paramref name="rate"/> = estalos por segundo.</summary>
    public static float[] Crackle(double dur, Curve rate, Curve amp, int seed)
    {
        var r = new Random(seed);
        var y = new float[N(dur)];
        for (int i = 0; i < y.Length; i++)
        {
            double t = (double)i / SR;
            if (r.NextDouble() < rate(t) / SR)
            {
                double a = (0.4 + 0.6 * r.NextDouble()) * amp(t) * (r.Next(2) == 0 ? 1 : -1);
                int len = 8 + r.Next(40);
                for (int k = 0; k < len && i + k < y.Length; k++)
                    y[i + k] += (float)(a * Math.Exp(-k / (len * 0.3)) * (r.NextDouble() * 2 - 1));
            }
        }
        return y;
    }

    // ------------------------------------------------------------ filtros

    /// <summary>Biquad (RBJ) com corte variando no tempo. tipo 0 = passa-baixa, 1 = passa-alta, 2 = passa-banda.</summary>
    static float[] Biquad(float[] x, int type, Curve fc, double q)
    {
        var y = new float[x.Length];
        double x1 = 0, x2 = 0, y1 = 0, y2 = 0, b0 = 0, b1 = 0, b2 = 0, a1 = 0, a2 = 0;
        for (int i = 0; i < x.Length; i++)
        {
            if ((i & 7) == 0)
            {
                double f = Math.Max(20, Math.Min(SR * 0.45, fc((double)i / SR)));
                double w0 = 2 * Math.PI * f / SR, cs = Math.Cos(w0), al = Math.Sin(w0) / (2 * q);
                double a0;
                if (type == 0) { b0 = (1 - cs) / 2; b1 = 1 - cs; b2 = b0; }
                else if (type == 1) { b0 = (1 + cs) / 2; b1 = -(1 + cs); b2 = b0; }
                else { b0 = al; b1 = 0; b2 = -al; }
                a0 = 1 + al; a1 = -2 * cs; a2 = 1 - al;
                b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0;
            }
            double v = b0 * x[i] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x[i]; y2 = y1; y1 = v;
            y[i] = (float)v;
        }
        return y;
    }

    public static float[] LP(float[] x, Curve fc, double q) { return Biquad(x, 0, fc, q); }
    public static float[] LP(float[] x, double fc) { return Biquad(x, 0, K(fc), 0.707); }
    public static float[] HP(float[] x, Curve fc, double q) { return Biquad(x, 1, fc, q); }
    public static float[] HP(float[] x, double fc) { return Biquad(x, 1, K(fc), 0.707); }
    public static float[] BP(float[] x, Curve fc, double q) { return Biquad(x, 2, fc, q); }
    public static float[] BP(float[] x, double fc, double q) { return Biquad(x, 2, K(fc), q); }

    // ------------------------------------------------------------ montagem

    public static float[] Mix(params float[][] parts)
    {
        int n = 0;
        foreach (var p in parts) if (p != null) n = Math.Max(n, p.Length);
        var y = new float[n];
        foreach (var p in parts) if (p != null) for (int i = 0; i < p.Length; i++) y[i] += p[i];
        return y;
    }

    /// <summary>Atrasa o som <paramref name="at"/> segundos (silêncio antes).</summary>
    public static float[] At(double at, float[] x)
    {
        int off = N(at);
        var y = new float[x.Length + off];
        Array.Copy(x, 0, y, off, x.Length);
        return y;
    }

    public static float[] G(float[] x, double g)
    {
        var y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = (float)(x[i] * g);
        return y;
    }

    public static float[] Amp(float[] x, Curve a)
    {
        var y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = (float)(x[i] * a((double)i / SR));
        return y;
    }

    public static float[] Drive(float[] x, double k)
    {
        var y = new float[x.Length];
        double n = Math.Tanh(k);
        for (int i = 0; i < x.Length; i++) y[i] = (float)(Math.Tanh(x[i] * k) / n);
        return y;
    }

    /// <summary>Modulação em anel (parte do timbre da praga).</summary>
    public static float[] Ring(float[] x, double hz, double depth)
    {
        var y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = (float)(x[i] * (1 - depth + depth * Math.Sin(2 * Math.PI * hz * i / SR)));
        return y;
    }

    public static float[] Reverse(float[] x)
    {
        var y = (float[])x.Clone();
        Array.Reverse(y);
        return y;
    }

    /// <summary>Muda a altura reamostrando (muda a duração junto).</summary>
    public static float[] Pitch(float[] x, double ratio)
    {
        int n = (int)(x.Length / ratio);
        var y = new float[Math.Max(1, n)];
        for (int i = 0; i < y.Length; i++)
        {
            double p = i * ratio;
            int a = (int)p;
            double f = p - a;
            float s0 = a < x.Length ? x[a] : 0, s1 = a + 1 < x.Length ? x[a + 1] : 0;
            y[i] = (float)(s0 + (s1 - s0) * f);
        }
        return y;
    }

    public static float[] Crush(float[] x, int bits, double rate)
    {
        var y = new float[x.Length];
        double step = rate / SR, ph = 1, levels = Math.Pow(2, bits - 1);
        float hold = 0;
        for (int i = 0; i < x.Length; i++)
        {
            ph += step;
            if (ph >= 1) { ph -= 1; hold = (float)(Math.Round(x[i] * levels) / levels); }
            y[i] = hold;
        }
        return y;
    }

    /// <summary>Reverb de sala (Schroeder: 4 pentes + 2 passa-tudo). Aumenta o som em <paramref name="tail"/> s.</summary>
    public static float[] Reverb(float[] x, double wet, double room, double tail)
    {
        int n = x.Length + N(tail);
        var dry = new float[n];
        Array.Copy(x, dry, x.Length);
        int[] combs = { 1557, 1617, 1491, 1422 };
        var acc = new double[n];
        foreach (int c0 in combs)
        {
            int d = Math.Max(50, (int)(c0 * room));
            var buf = new double[d];
            int idx = 0;
            double fb = 0.78 + 0.06 * Math.Min(1, room), lp = 0;
            for (int i = 0; i < n; i++)
            {
                double o = buf[idx];
                lp = o * 0.6 + lp * 0.4;
                buf[idx] = dry[i] + lp * fb;
                idx = (idx + 1) % d;
                acc[i] += o * 0.25;
            }
        }
        foreach (int a0 in new[] { 225, 556 })
        {
            var buf = new double[a0];
            int idx = 0;
            for (int i = 0; i < n; i++)
            {
                double b = buf[idx], v = acc[i];
                buf[idx] = v + b * 0.5;
                acc[i] = b - v;
                idx = (idx + 1) % a0;
            }
        }
        var y = new float[n];
        for (int i = 0; i < n; i++) y[i] = (float)(dry[i] + acc[i] * wet);
        return y;
    }

    public static float[] Echo(float[] x, double delay, double fb, double wet, double tail)
    {
        int n = x.Length + N(tail), d = N(delay);
        var y = new float[n];
        Array.Copy(x, y, x.Length);
        var e = new float[n];
        for (int i = d; i < n; i++) e[i] = (float)((i - d < x.Length ? x[i - d] : 0) + e[i - d] * fb);
        for (int i = 0; i < n; i++) y[i] += (float)(e[i] * wet);
        return y;
    }

    public static float[] Fade(float[] x, double inSec, double outSec)
    {
        var y = (float[])x.Clone();
        int a = N(inSec), b = N(outSec);
        for (int i = 0; i < a && i < y.Length; i++) y[i] *= (float)i / a;
        for (int i = 0; i < b && i < y.Length; i++) y[y.Length - 1 - i] *= (float)i / b;
        return y;
    }

    /// <summary>Corta o silêncio do fim (abaixo de −60 dB do pico) e põe um fade curto.</summary>
    public static float[] Trim(float[] x)
    {
        double peak = 1e-9;
        foreach (var v in x) peak = Math.Max(peak, Math.Abs(v));
        int end = x.Length;
        while (end > 1 && Math.Abs(x[end - 1]) < peak * 0.001) end--;
        var y = new float[Math.Min(x.Length, end + N(0.01))];
        Array.Copy(x, y, y.Length);
        return Fade(y, 0.0005, 0.008);
    }

    public static float[] Normalize(float[] x, double peakDb)
    {
        double peak = 1e-9;
        foreach (var v in x) peak = Math.Max(peak, Math.Abs(v));
        return G(x, Math.Pow(10, peakDb / 20) / peak);
    }

    /// <summary>
    /// Normaliza pela intensidade percebida: o trecho de 50 ms mais forte fica em <paramref name="targetDb"/> (RMS),
    /// sem o pico passar de <paramref name="ceilDb"/>. Assim todos os sons saem no mesmo volume e a mixagem do jogo
    /// (SfxCatalog) é que decide a hierarquia.
    /// </summary>
    public static float[] Loudness(float[] x, double targetDb, double ceilDb)
    {
        int w = Math.Max(1, Math.Min(N(0.05), x.Length));
        double acc = 0, best = 1e-9, peak = 1e-9;
        for (int i = 0; i < x.Length; i++)
        {
            acc += (double)x[i] * x[i];
            if (i >= w) acc -= (double)x[i - w] * x[i - w];
            if (i >= w - 1) best = Math.Max(best, Math.Sqrt(Math.Max(0, acc) / w));
            peak = Math.Max(peak, Math.Abs(x[i]));
        }
        double g = Math.Pow(10, targetDb / 20) / best;
        g = Math.Min(g, Math.Pow(10, ceilDb / 20) / peak);
        return G(x, g);
    }

    /// <summary>Loop sem emenda: o fim cruza com o começo em <paramref name="xfade"/> s.</summary>
    public static float[] Loopify(float[] x, double xfade)
    {
        int f = Math.Min(N(xfade), x.Length / 3);
        int n = x.Length - f;
        var y = new float[n];
        Array.Copy(x, y, n);
        for (int i = 0; i < f; i++)
        {
            double k = (double)i / f;
            y[i] = (float)(x[i] * Math.Sqrt(k) + x[n + i] * Math.Sqrt(1 - k));
        }
        return y;
    }

    // ------------------------------------------------------------ instrumentos de foley

    /// <summary>Baque: senoide que cai de f0 para f1 (corpo de impacto).</summary>
    public static float[] Thump(double f0, double f1, double dur, double decay)
    {
        return Osc(W.Sine, dur, Geo(f0, f1, dur * 0.6), Perc(0.002, decay));
    }

    /// <summary>Clique curto (transiente).</summary>
    public static float[] Click(double hp, int seed)
    {
        return HP(Noise(0.012, Perc(0.0003, 0.002), seed), hp);
    }

    /// <summary>Sopro: ruído em banda que varre de f0 para f1, com o volume em sino.</summary>
    public static float[] Whoosh(double dur, double f0, double f1, double q, int seed)
    {
        var bell = (Curve)delegate(double t) { double k = C01(t / dur); return Math.Pow(Math.Sin(Math.PI * k), 1.6); };
        return BP(Noise(dur, bell, seed), Geo(f0, f1, dur), q);
    }

    /// <summary>Metal (armadura, lâmina): parciais inarmônicas que somem rápido.</summary>
    public static float[] Metal(double baseHz, double dur, double decay, int seed)
    {
        var r = new Random(seed);
        double[] ratios = { 1, 1.47, 2.09, 2.56, 3.39, 4.12, 5.3 };
        var parts = new List<float[]>();
        foreach (double k in ratios)
        {
            double f = baseHz * k * (1 + (r.NextDouble() - 0.5) * 0.03);
            parts.Add(Osc(W.Sine, dur, K(f), Perc(0.001, decay / Math.Sqrt(k)) ));
        }
        var m = Mix(parts.ToArray());
        return Mix(G(m, 0.3), G(Click(3000, seed), 0.5));
    }

    /// <summary>Tilintar de cota de malha / chaveiro: muitos metais pequenos.</summary>
    public static float[] Jingle(double dur, double density, double hz, int seed)
    {
        var r = new Random(seed);
        var y = new float[N(dur + 0.2)];
        int count = (int)(density * dur);
        for (int c = 0; c < count; c++)
        {
            double at = r.NextDouble() * dur;
            var m = Metal(hz * (0.8 + r.NextDouble() * 0.8), 0.12, 0.03, seed + c * 7);
            var p = At(at, G(m, 0.3 + 0.7 * r.NextDouble()));
            for (int i = 0; i < p.Length && i < y.Length; i++) y[i] += p[i];
        }
        return y;
    }

    /// <summary>Sino FM.</summary>
    public static float[] Bell(double f, double dur, double ratio, double index, double decay)
    {
        var y = new float[N(dur)];
        double pc = 0, pm = 0;
        for (int i = 0; i < y.Length; i++)
        {
            double t = (double)i / SR, env = Math.Exp(-t / decay), att = Math.Min(1, t / 0.002);
            pm += f * ratio / SR;
            pc += f / SR;
            double mod = index * env * Math.Sin(2 * Math.PI * pm);
            y[i] = (float)(Math.Sin(2 * Math.PI * pc + mod) * env * att);
        }
        return y;
    }

    /// <summary>Corda dedilhada (Karplus-Strong).</summary>
    public static float[] Pluck(double f, double dur, double damp, int seed)
    {
        var r = new Random(seed);
        int p = Math.Max(2, (int)(SR / f));
        var buf = new double[p];
        for (int i = 0; i < p; i++) buf[i] = r.NextDouble() * 2 - 1;
        var y = new float[N(dur)];
        int idx = 0;
        for (int i = 0; i < y.Length; i++)
        {
            int nx = (idx + 1) % p;
            double v = buf[idx];
            buf[idx] = (buf[idx] + buf[nx]) * 0.5 * damp;
            idx = nx;
            y[i] = (float)v;
        }
        return Fade(y, 0, 0.02);
    }

    /// <summary>Respingo d'água: bolhas (senoides que sobem rápido).</summary>
    public static float[] Droplets(double dur, int count, double lo, double hi, int seed)
    {
        var r = new Random(seed);
        var y = new float[N(dur + 0.1)];
        for (int c = 0; c < count; c++)
        {
            double at = Math.Pow(r.NextDouble(), 1.6) * dur, f = lo + r.NextDouble() * (hi - lo);
            var b = Osc(W.Sine, 0.05, Geo(f, f * 1.8, 0.04), Perc(0.001, 0.012));
            var p = At(at, G(b, (0.3 + 0.7 * r.NextDouble()) * (1 - at / dur * 0.7)));
            for (int i = 0; i < p.Length && i < y.Length; i++) y[i] += p[i];
        }
        return y;
    }

    public static double Hz(string note)
    {
        int s;
        switch (note[0]) { case 'C': s = 0; break; case 'D': s = 2; break; case 'E': s = 4; break; case 'F': s = 5; break; case 'G': s = 7; break; case 'A': s = 9; break; default: s = 11; break; }
        int i = 1;
        if (note[i] == '#') { s++; i++; } else if (note[i] == 'b') { s--; i++; }
        int oct = int.Parse(note.Substring(i));
        return 440.0 * Math.Pow(2, (12 * (oct + 1) + s - 69) / 12.0);
    }

    // ------------------------------------------------------------ voz sem palavras (formantes)

    public class Voice
    {
        public double F0 = 200, Formant = 1, Rate = 1, Breath = 0.1, Growl = 0, Jitter = 0.015, Muffle = 0, Bright = 1;
        /// <summary>Quanto a altura varia entre sílabas (0,06 = ±6%).</summary>
        public double Melody = 0.06;
        /// <summary>Queda de altura ao longo da frase.</summary>
        public double Decline = 0.12;
    }

    class Seg
    {
        public double Dur, F1, F2, F3, Voiced, Aspir, Pitch = 1, Loud = 1;
        public int Fric = -1;      // 0 s, 1 sh, 2 f, 3 burst agudo (t), 4 burst médio (k), 5 burst grave (p)
        public bool Trill;
        public int Syl;
    }

    // s, sh, f, t, k, p: equilibrados com as vogais (o "t" cru tinha 10x o pico de uma vogal)
    static readonly double[] FricGain = { 0.2, 0.3, 0.35, 0.12, 0.3, 0.45 };

    static readonly Dictionary<char, double[]> Vowels = new Dictionary<char, double[]>
    {
        { 'a', new double[] { 730, 1090, 2440 } }, { 'e', new double[] { 530, 1840, 2480 } },
        { 'i', new double[] { 300, 2250, 3000 } }, { 'o', new double[] { 570, 840, 2410 } },
        { 'u', new double[] { 320, 870, 2240 } }, { 'y', new double[] { 300, 2100, 2900 } },
    };

    static string Plain(string s)
    {
        var d = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in d)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Replace("…", "...");
    }

    /// <summary>
    /// Fala sem palavras: sílabas inventadas como "mrah-ne, tsi-tah." ou "hrr?". Espaço e hífen separam sílabas;
    /// "," e "." fazem pausa (o "." cai no fim); "..." pausa longa; "?" sobe no fim; "!" dá ênfase.
    /// </summary>
    public static float[] Speak(Voice v, string text, int seed)
    {
        var r = new Random(seed);
        string s = Plain(text);
        var segs = new List<Seg>();
        double[] cur = Vowels['a'];
        int syl = 0;
        bool question = false, exclaim = false;
        double rate = v.Rate;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            char nx = i + 1 < s.Length ? s[i + 1] : ' ';
            double[] vf = null;
            // próxima vogal (as consoantes já vão em direção a ela)
            for (int j = i; j < s.Length; j++) if (Vowels.ContainsKey(s[j])) { vf = Vowels[s[j]]; break; }
            if (vf == null) vf = cur;
            var seg = new Seg { Syl = syl, F1 = vf[0], F2 = vf[1], F3 = vf[2] };

            if (Vowels.ContainsKey(c))
            {
                cur = Vowels[c];
                seg.F1 = cur[0]; seg.F2 = cur[1]; seg.F3 = cur[2];
                seg.Voiced = 1; seg.Dur = 0.1 * rate;
                if (segs.Count > 0 && segs[segs.Count - 1].Voiced == 1 && segs[segs.Count - 1].F1 == cur[0] && i > 0 && s[i - 1] == c)
                { segs[segs.Count - 1].Dur += 0.09 * rate; continue; }
                segs.Add(seg);
                continue;
            }
            switch (c)
            {
                case 'm': case 'n':
                    if (segs.Count > 0 && i > 0 && s[i - 1] == c) { segs[segs.Count - 1].Dur += 0.07 * rate; continue; }
                    seg.F1 = 260; seg.F2 = c == 'm' ? 900 : 1300; seg.F3 = 2300; seg.Voiced = 0.45; seg.Dur = 0.065 * rate; break;
                case 'r':
                    if (segs.Count > 0 && segs[segs.Count - 1].Trill && i > 0 && s[i - 1] == 'r') { segs[segs.Count - 1].Dur += 0.05 * rate; continue; }
                    seg.F1 = 480; seg.F2 = 1350; seg.F3 = 2400; seg.Voiced = 0.65; seg.Trill = true; seg.Dur = 0.055 * rate; break;
                case 'l': seg.F1 = 380; seg.F2 = 1100; seg.F3 = 2600; seg.Voiced = 0.6; seg.Dur = 0.05 * rate; break;
                case 'w': seg.F1 = 300; seg.F2 = 700; seg.F3 = 2200; seg.Voiced = 0.7; seg.Dur = 0.05 * rate; break;
                case 'j': seg.F1 = 300; seg.F2 = 2100; seg.F3 = 2900; seg.Voiced = 0.7; seg.Dur = 0.045 * rate; break;
                case 'v': seg.F1 = 300; seg.F2 = 1200; seg.F3 = 2300; seg.Voiced = 0.4; seg.Fric = 2; seg.Dur = 0.055 * rate; break;
                case 'z': seg.Voiced = 0.35; seg.Fric = 0; seg.Dur = 0.07 * rate; break;
                case 'h':
                    if (segs.Count > 0 && segs[segs.Count - 1].Aspir > 0 && i > 0 && s[i - 1] == 'h') { segs[segs.Count - 1].Dur += 0.06 * rate; continue; }
                    seg.Aspir = 0.9; seg.Dur = 0.065 * rate; break;
                case 's':
                    if (nx == 'h') { seg.Fric = 1; seg.Dur = 0.1 * rate; i++; while (i + 1 < s.Length && (s[i + 1] == 'h' || s[i + 1] == 's')) { seg.Dur += 0.05 * rate; i++; } }
                    else { seg.Fric = 0; seg.Dur = 0.08 * rate; while (i + 1 < s.Length && s[i + 1] == 's') { seg.Dur += 0.05 * rate; i++; } }
                    break;
                case 'f': seg.Fric = 2; seg.Dur = 0.06 * rate; while (i + 1 < s.Length && s[i + 1] == 'f') { seg.Dur += 0.04 * rate; i++; } break;
                case 't': case 'd':
                    segs.Add(new Seg { Syl = syl, Dur = 0.02 * rate, F1 = vf[0], F2 = vf[1], F3 = vf[2], Voiced = c == 'd' ? 0.15 : 0 });
                    seg.Fric = 3; seg.Dur = 0.018; seg.Voiced = c == 'd' ? 0.3 : 0; break;
                case 'k': case 'c': case 'q': case 'g':
                    segs.Add(new Seg { Syl = syl, Dur = 0.025 * rate, F1 = vf[0], F2 = vf[1], F3 = vf[2], Voiced = c == 'g' ? 0.15 : 0 });
                    seg.Fric = 4; seg.Dur = 0.025; seg.Voiced = c == 'g' ? 0.3 : 0; break;
                case 'p': case 'b':
                    segs.Add(new Seg { Syl = syl, Dur = 0.02 * rate, F1 = vf[0], F2 = vf[1], F3 = vf[2], Voiced = c == 'b' ? 0.15 : 0 });
                    seg.Fric = 5; seg.Dur = 0.02; seg.Voiced = c == 'b' ? 0.3 : 0; break;
                case ' ': case '-':
                    syl++; seg.Syl = syl; seg.Dur = c == ' ' ? 0.035 * rate : 0.012; break;
                case ',': syl++; seg.Dur = 0.13 * rate; break;
                case '.':
                    if (i + 2 < s.Length && s[i + 1] == '.' && s[i + 2] == '.') { seg.Dur = 0.32 * rate; i += 2; }
                    else seg.Dur = 0.16 * rate;
                    syl++; break;
                case '?': question = true; continue;
                case '!': exclaim = true; continue;
                default: continue;
            }
            segs.Add(seg);
        }
        if (segs.Count == 0) return new float[1];

        // entonação: cada sílaba com uma altura própria, a frase cai devagar; "?" sobe no fim, "!" começa alto
        int lastSyl = 0;
        foreach (var g in segs) if (g.Voiced > 0 || g.Fric >= 0 || g.Aspir > 0) lastSyl = Math.Max(lastSyl, g.Syl);
        var sylPitch = new double[syl + 2];
        for (int k = 0; k < sylPitch.Length; k++) sylPitch[k] = 1 + (r.NextDouble() * 2 - 1) * v.Melody;
        double total = 0;
        foreach (var g in segs) total += g.Dur;
        double tAcc = 0;
        foreach (var g in segs)
        {
            double k = total > 0 ? tAcc / total : 0;
            g.Pitch = sylPitch[Math.Min(g.Syl, sylPitch.Length - 1)] * (1 - v.Decline * k);
            if (exclaim) { g.Pitch *= 1.15 - 0.15 * k; g.Loud = 1.15; }
            if (question && g.Syl == lastSyl) g.Pitch *= 1.35;
            tAcc += g.Dur;
        }

        // ruídos filtrados das consoantes (uma faixa por tipo)
        int n = N(total + 0.15);
        var white = Noise(total + 0.15, K(1), seed + 101);
        var fr = new float[6][];
        fr[0] = HP(BP(white, 6200, 1.2), 3500);
        fr[1] = BP(white, 2900, 1.8);
        fr[2] = BP(white, 1700, 0.6);
        fr[3] = HP(white, 3200);
        fr[4] = BP(white, 1900, 1.5);
        fr[5] = LP(white, 900);

        var y = new float[n];
        double ph = 0, prevU = 0, f1 = segs[0].F1, f2 = segs[0].F2, f3 = segs[0].F3, va = 0, aa = 0, pitch = segs[0].Pitch;
        var fa = new double[6];
        double[] z1 = new double[3], z2 = new double[3], w1 = new double[3], w2 = new double[3];
        double[] b0 = new double[3], b2 = new double[3], a1 = new double[3], a2 = new double[3];
        int cycle = 0, si = 0;
        double segStart = 0, jit = 1, shim = 1;
        double sm = 1 - Math.Exp(-1.0 / (0.012 * SR)), smA = 1 - Math.Exp(-1.0 / (0.006 * SR));
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / SR;
            while (si < segs.Count && t >= segStart + segs[si].Dur) { segStart += segs[si].Dur; si++; }
            Seg g = si < segs.Count ? segs[si] : null;
            double tv = g != null ? g.Voiced * g.Loud : 0, ta = g != null ? g.Aspir * g.Loud : 0;
            if (g != null && g.Trill) tv *= 0.55 + 0.45 * Math.Cos(2 * Math.PI * 26 * (t - segStart));
            va += (tv - va) * smA;
            aa += (ta - aa) * smA;
            if (g != null)
            {
                f1 += (g.F1 * v.Formant - f1) * sm; f2 += (g.F2 * v.Formant - f2) * sm; f3 += (g.F3 * v.Formant - f3) * sm;
                pitch += (g.Pitch - pitch) * sm * 0.6;
            }
            for (int k = 0; k < 6; k++)
            {
                double target = g != null && g.Fric == k ? g.Loud : 0;
                fa[k] += (target - fa[k]) * (k >= 3 ? 0.05 : smA);
            }

            // fonte: pulso glótico com tremor, e o "rosnado" (ciclos alternados mais fracos + aspereza)
            double f0 = v.F0 * pitch * jit * (1 + 0.012 * Math.Sin(2 * Math.PI * 5.5 * t));
            ph += f0 / SR;
            if (ph >= 1)
            {
                ph -= 1; cycle++;
                jit = 1 + (r.NextDouble() * 2 - 1) * v.Jitter;
                shim = 1 - r.NextDouble() * 0.1;
            }
            double u = ph < 0.4 ? 0.5 * (1 - Math.Cos(Math.PI * ph / 0.4)) : ph < 0.56 ? Math.Cos(Math.PI * (ph - 0.4) / 0.32) : 0;
            double exc = (u - prevU) * SR / Math.Max(40, f0) * 0.05;
            prevU = u;
            double growl = (cycle & 1) == 1 ? 1 - v.Growl * 0.75 : 1;
            exc *= growl * shim * 2 * Math.Sqrt(200 / Math.Max(40, f0));
            double noise = white[Math.Min(i, white.Length - 1)];
            double src = exc * va + noise * (va * v.Breath * 0.35 + aa * 0.5) * (1 + v.Growl * Math.Sin(2 * Math.PI * 33 * t));

            if ((i & 31) == 0)
            {
                double[] F = { f1, f2, f3 };
                double[] B = { 90, 120, 170 };
                for (int k = 0; k < 3; k++)
                {
                    double fq = Math.Min(SR * 0.45, F[k]), q = fq / (B[k] * Math.Max(0.7, v.Formant));
                    double w0 = 2 * Math.PI * fq / SR, al = Math.Sin(w0) / (2 * q), a0 = 1 + al;
                    b0[k] = al / a0; b2[k] = -al / a0; a1[k] = -2 * Math.Cos(w0) / a0; a2[k] = (1 - al) / a0;
                }
            }
            double outv = 0;
            double[] gains = { 1.0, 0.65 * v.Bright, 0.35 * v.Bright };
            for (int k = 0; k < 3; k++)
            {
                double o = b0[k] * src + b2[k] * z2[k] - a1[k] * w1[k] - a2[k] * w2[k];
                z2[k] = z1[k]; z1[k] = src; w2[k] = w1[k]; w1[k] = o;
                outv += o * gains[k];
            }
            for (int k = 0; k < 6; k++) outv += fr[k][i] * fa[k] * FricGain[k];
            y[i] = (float)outv;
        }
        var res = HP(y, 90);
        if (v.Muffle > 0) res = LP(res, v.Muffle);
        return res;
    }

    // ------------------------------------------------------------ arquivo

    public static void WriteWav(string path, float[] mono)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int bytes = mono.Length * 2;
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(SR); w.Write(SR * 2); w.Write((short)2); w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (var v in mono) w.Write((short)Math.Max(-32767, Math.Min(32767, (int)Math.Round(v * 32767))));
        }
    }
}
