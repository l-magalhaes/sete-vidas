using System;
using System.Collections.Generic;
using System.IO;

// Sintetizador de rascunho das músicas (Docs/Audio/plano-musicas.md). C# 5 (roda pelo Add-Type do PowerShell).
// Timbres "16 bits com limites": harpa e pizzicato por corda dedilhada (Karplus-Strong), flauta, cordas (serras
// desafinadas e filtradas), trompa, sino (FM), tímpano, caixinha de música, ondas de chip, bateria e vento. Depois:
// reverb de sala, bitcrush suave (12 bits / 32 kHz) e normalização. Saída: WAV 16 bits estéreo 44,1 kHz.
public static class MusicSynth
{
    public const int SR = 44100;

    public enum Inst { Harp, Flute, Strings, Pizz, Horn, Bell, Timpani, MusicBox, Wind, Pulse, Triangle, Kick, Snare, Hat, Wood, Organ, Choir, Heart }

    public class Note
    {
        public double Start, Dur;   // segundos
        public double Freq, Vel, Pan;
        public Inst Inst;
        public double Param;        // uso livre do timbre (ex.: duty do pulso)
    }

    public class Song
    {
        public double Bpm = 120;
        public readonly List<Note> Notes = new List<Note>();
        public double Reverb = 0.25, RoomSize = 0.8;
        public bool Crush = true;
        /// <summary>Comprimento do loop (segundos). A cauda do reverb que passa do fim volta para o começo.</summary>
        public double Length;
        public bool Loop = true;
        public double Spb { get { return 60.0 / Bpm; } }

        /// <summary>Uma nota. <paramref name="beat"/> e <paramref name="beats"/> em tempos.</summary>
        public void Add(Inst inst, string pitch, double beat, double beats, double vel, double pan)
        {
            Notes.Add(new Note { Inst = inst, Freq = Freq(pitch), Start = beat * Spb, Dur = beats * Spb, Vel = vel, Pan = pan });
        }

        public void AddMidi(Inst inst, int midi, double beat, double beats, double vel, double pan)
        {
            Notes.Add(new Note { Inst = inst, Freq = MidiFreq(midi), Start = beat * Spb, Dur = beats * Spb, Vel = vel, Pan = pan });
        }

        /// <summary>Uma linha melódica: "D4:2 A4:1 C5:1.5 r:1" (nota:tempos; r = pausa). Retorna o tempo final.</summary>
        public double Line(Inst inst, double beat, string notes, double vel, double pan)
        {
            foreach (var tok in notes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = tok.Split(':');
                double len = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                if (parts[0] != "r") Add(inst, parts[0], beat, len, vel, pan);
                beat += len;
            }
            return beat;
        }
    }

    // ------------------------------------------------------------ notas e acordes

    static readonly Dictionary<char, int> Semis = new Dictionary<char, int> { { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 }, { 'G', 7 }, { 'A', 9 }, { 'B', 11 } };

    public static int Midi(string p)
    {
        int s = Semis[p[0]], i = 1;
        if (p[i] == '#') { s++; i++; } else if (p[i] == 'b') { s--; i++; }
        int oct = int.Parse(p.Substring(i));
        return 12 * (oct + 1) + s;
    }

    public static double Freq(string p) { return MidiFreq(Midi(p)); }
    public static double MidiFreq(int m) { return 440.0 * Math.Pow(2, (m - 69) / 12.0); }

    /// <summary>Acorde ("Dm", "F", "Asus4", "A7", "Bb") como MIDI: [fundamental, terça, quinta, (sétima)], fundamental entre Lá2 e Sol#3.</summary>
    public static int[] Chord(string name)
    {
        int i = 1, s = Semis[name[0]];
        if (i < name.Length && name[i] == '#') { s++; i++; } else if (i < name.Length && name[i] == 'b') { s--; i++; }
        string q = name.Substring(i);
        int root = 48 + ((s % 12) + 12) % 12;   // C3..B3
        if (root > 56) root -= 12;               // A2..G#3
        int third = q.StartsWith("m") && !q.StartsWith("maj") ? 3 : q.StartsWith("sus4") ? 5 : q.StartsWith("sus2") ? 2 : 4;
        bool dim = q.StartsWith("dim");
        if (dim) third = 3;
        int fifth = dim ? 6 : 7;
        if (q.Contains("7")) return new[] { root, root + third, root + fifth, root + (q.Contains("maj7") ? 11 : 10) };
        return new[] { root, root + third, root + fifth };
    }

    // ------------------------------------------------------------ render

    public static float[] Render(Song song)
    {
        double end = song.Length;
        if (end <= 0) foreach (var n in song.Notes) end = Math.Max(end, n.Start + n.Dur);
        double tail = 4.0;
        int total = (int)((end + tail) * SR);
        var L = new float[total];
        var R = new float[total];
        var rng = new Random(7);
        foreach (var n in song.Notes) RenderNote(n, L, R, rng);

        Reverb(L, R, song.Reverb, song.RoomSize);

        int len = (int)(end * SR);
        if (song.Loop)
        {
            // a cauda que passa do fim do loop soma no começo: o loop fica sem emenda
            for (int i = len; i < total; i++) { L[i - len] += L[i]; R[i - len] += R[i]; }
        }
        else len = total;

        var outp = new float[len * 2];
        double peak = 1e-9;
        for (int i = 0; i < len; i++) { peak = Math.Max(peak, Math.Max(Math.Abs(L[i]), Math.Abs(R[i]))); }
        double g = 0.89 / peak;   // ~ -1 dBFS
        float holdL = 0, holdR = 0;
        double phase = 0, step = 32000.0 / SR;
        for (int i = 0; i < len; i++)
        {
            float l = (float)(L[i] * g), r = (float)(R[i] * g);
            if (song.Crush)
            {
                // sample-and-hold a 32 kHz e 12 bits: o calor do console, sem sujar demais
                phase += step;
                if (phase >= 1.0) { phase -= 1.0; holdL = Q(l); holdR = Q(r); }
                l = holdL; r = holdR;
            }
            outp[2 * i] = l; outp[2 * i + 1] = r;
        }
        return outp;
    }

    static float Q(float x) { return (float)(Math.Round(x * 2047.0) / 2047.0); }

    static double Env(double t, double dur, double a, double d, double s, double r)
    {
        double v;
        if (t < a) v = t / a;
        else if (t < a + d) v = 1 - (1 - s) * (t - a) / d;
        else v = s;
        if (t > dur) { double k = 1 - (t - dur) / r; v *= k < 0 ? 0 : k; }
        return v;
    }

    static double Release(Inst inst)
    {
        switch (inst)
        {
            case Inst.Harp: return 2.5;
            case Inst.Pizz: return 0.5;
            case Inst.Bell: return 4.0;
            case Inst.Timpani: return 1.4;
            case Inst.MusicBox: return 1.6;
            case Inst.Strings: return 0.9;
            case Inst.Choir: return 1.2;
            case Inst.Wind: return 2.5;
            case Inst.Flute: return 0.18;
            case Inst.Horn: return 0.25;
            case Inst.Organ: return 0.12;
            case Inst.Kick: case Inst.Snare: case Inst.Hat: case Inst.Wood: case Inst.Heart: return 0.4;
            default: return 0.08;
        }
    }

    static double Gain(Inst inst)
    {
        switch (inst)
        {
            case Inst.Harp: return 0.38;
            case Inst.Flute: return 0.30;
            case Inst.Strings: return 0.09;
            case Inst.Pizz: return 0.42;
            case Inst.Horn: return 0.16;
            case Inst.Bell: return 0.22;
            case Inst.Timpani: return 0.55;
            case Inst.MusicBox: return 0.26;
            case Inst.Wind: return 0.10;
            case Inst.Pulse: return 0.09;
            case Inst.Triangle: return 0.30;
            case Inst.Kick: return 0.65;
            case Inst.Snare: return 0.22;
            case Inst.Hat: return 0.08;
            case Inst.Wood: return 0.20;
            case Inst.Organ: return 0.09;
            case Inst.Choir: return 0.08;
            case Inst.Heart: return 0.7;
            default: return 0.2;
        }
    }

    static void RenderNote(Note n, float[] L, float[] R, Random rng)
    {
        double rel = Release(n.Inst);
        int start = (int)(n.Start * SR);
        int count = (int)((n.Dur + rel) * SR);
        if (start >= L.Length) return;
        if (start + count > L.Length) count = L.Length - start;
        double amp = Gain(n.Inst) * n.Vel;
        double pl = Math.Cos((n.Pan + 1) * Math.PI / 4), pr = Math.Sin((n.Pan + 1) * Math.PI / 4);
        double f = n.Freq, dur = n.Dur;

        // estado por nota
        double[] ks = null; int ksLen = 0, ksPos = 0; double ksGain = 1;
        if (n.Inst == Inst.Harp || n.Inst == Inst.Pizz)
        {
            ksLen = Math.Max(2, (int)Math.Round(SR / f));
            ks = new double[ksLen];
            double bright = n.Inst == Inst.Harp ? 0.55 : 0.3, prev = 0;
            for (int i = 0; i < ksLen; i++) { double w = rng.NextDouble() * 2 - 1; prev = prev + bright * (w - prev); ks[i] = prev; }
            double t60 = n.Inst == Inst.Harp ? 3.0 : 0.35;
            ksGain = Math.Pow(0.001, 1.0 / (t60 * f));
        }
        double lp = 0, lp2 = 0, lp3 = 0, noiseLp = 0;
        double[] det = { 0.996, 1.0, 1.004 };

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / SR, x = 0;
            switch (n.Inst)
            {
                case Inst.Harp:
                case Inst.Pizz:
                {
                    int j = ksPos, k = (ksPos + 1) % ksLen;
                    double y = ks[j];
                    ks[j] = ksGain * 0.5 * (ks[j] + ks[k]);
                    ksPos = k;
                    x = y * 2.2;
                    if (n.Inst == Inst.Pizz && t > dur) x *= Math.Max(0, 1 - (t - dur) / rel);
                    break;
                }
                case Inst.Flute:
                {
                    double vib = 1 + (t > 0.25 ? 0.004 * Math.Sin(2 * Math.PI * 5.2 * t) : 0);
                    double ph = 2 * Math.PI * f * vib * t;
                    double tone = Math.Sin(ph) + 0.18 * Math.Sin(2 * ph) + 0.06 * Math.Sin(3 * ph);
                    double w = rng.NextDouble() * 2 - 1;
                    noiseLp += 0.25 * (w - noiseLp);
                    double breath = noiseLp * (0.25 * Math.Exp(-t * 18) + 0.04);
                    x = (tone + breath) * Env(t, dur, 0.07, 0.15, 0.85, rel);
                    break;
                }
                case Inst.Strings:
                case Inst.Choir:
                {
                    double s = 0;
                    for (int v = 0; v < 3; v++)
                    {
                        double ph = f * det[v] * t + v * 0.31;
                        s += n.Inst == Inst.Strings ? 2 * (ph - Math.Floor(ph + 0.5)) : Math.Sin(2 * Math.PI * ph) + 0.3 * Math.Sin(4 * Math.PI * ph);
                    }
                    double fc = n.Inst == Inst.Strings ? 1800 : 1200;
                    double a = 1 - Math.Exp(-2 * Math.PI * fc / SR);
                    lp += a * (s - lp); lp2 += a * (lp - lp2);
                    double atk = n.Param > 0 ? n.Param : 0.5;
                    x = lp2 * Env(t, dur, atk, 0.3, 0.9, rel);
                    break;
                }
                case Inst.Horn:
                {
                    double vib = 1 + (t > 0.3 ? 0.003 * Math.Sin(2 * Math.PI * 4.8 * t) : 0);
                    double ph = f * vib * t;
                    double saw = 2 * (ph - Math.Floor(ph + 0.5));
                    double cut = 300 + 1500 * Math.Min(1, t / 0.12) * (t > dur ? 0.6 : 1);
                    double a = 1 - Math.Exp(-2 * Math.PI * cut / SR);
                    lp += a * (saw - lp); lp2 += a * (lp - lp2);
                    x = lp2 * 2.2 * Env(t, dur, 0.06, 0.2, 0.8, rel);
                    break;
                }
                case Inst.Bell:
                {
                    double idx = 3.0 * Math.Exp(-t * 2.5);
                    x = Math.Sin(2 * Math.PI * f * t + idx * Math.Sin(2 * Math.PI * f * 1.4 * t)) * Math.Exp(-t * 0.9);
                    x += 0.3 * Math.Sin(2 * Math.PI * f * 2.76 * t) * Math.Exp(-t * 2.2);
                    break;
                }
                case Inst.Timpani:
                {
                    double ff = f * (1 + 0.08 * Math.Exp(-t * 20));
                    double w = rng.NextDouble() * 2 - 1;
                    noiseLp += 0.08 * (w - noiseLp);
                    x = (Math.Sin(2 * Math.PI * ff * t) + 0.35 * Math.Sin(2 * Math.PI * ff * 1.5 * t)) * Math.Exp(-t * 2.4) + noiseLp * 2.5 * Math.Exp(-t * 30);
                    break;
                }
                case Inst.MusicBox:
                {
                    x = (Math.Sin(2 * Math.PI * f * t) + 0.25 * Math.Sin(2 * Math.PI * f * 4.02 * t) * Math.Exp(-t * 6)) * Math.Exp(-t * 2.2);
                    break;
                }
                case Inst.Wind:
                {
                    double w = rng.NextDouble() * 2 - 1;
                    double cut = 250 + 350 * (0.5 + 0.5 * Math.Sin(2 * Math.PI * 0.11 * (n.Start + t)));
                    double a = 1 - Math.Exp(-2 * Math.PI * cut / SR);
                    lp += a * (w - lp); lp2 += a * (lp - lp2);
                    double gust = 0.55 + 0.45 * Math.Sin(2 * Math.PI * 0.07 * (n.Start + t) + 1.3);
                    x = lp2 * 6 * gust * Env(t, dur, 2.0, 0.1, 1, rel);
                    break;
                }
                case Inst.Pulse:
                {
                    double duty = n.Param > 0 ? n.Param : 0.25;
                    double ph = f * t; ph -= Math.Floor(ph);
                    x = (ph < duty ? 1 : -1) * Env(t, dur, 0.005, 0.1, 0.7, rel);
                    break;
                }
                case Inst.Triangle:
                {
                    double ph = f * t; ph -= Math.Floor(ph);
                    x = (4 * Math.Abs(ph - 0.5) - 1) * Env(t, dur, 0.005, 0.05, 0.9, rel);
                    break;
                }
                case Inst.Organ:
                {
                    double ph = f * t; ph -= Math.Floor(ph);
                    double ph2 = 2 * f * t; ph2 -= Math.Floor(ph2);
                    x = ((ph < 0.5 ? 1 : -1) + 0.5 * (ph2 < 0.5 ? 1 : -1)) * Env(t, dur, 0.004, 0.05, 0.85, rel);
                    double a = 1 - Math.Exp(-2 * Math.PI * 2400 / SR);
                    lp += a * (x - lp); x = lp;
                    break;
                }
                case Inst.Kick:
                case Inst.Heart:
                {
                    double ff = (n.Inst == Inst.Heart ? 45 : 50) + 110 * Math.Exp(-t * 35);
                    lp3 += 2 * Math.PI * ff / SR;
                    x = Math.Sin(lp3) * Math.Exp(-t * (n.Inst == Inst.Heart ? 9 : 7));
                    break;
                }
                case Inst.Snare:
                {
                    double w = rng.NextDouble() * 2 - 1;
                    x = w * Math.Exp(-t * 16) + 0.5 * Math.Sin(2 * Math.PI * 190 * t) * Math.Exp(-t * 25);
                    break;
                }
                case Inst.Hat:
                {
                    double w = rng.NextDouble() * 2 - 1;
                    lp += 0.5 * (w - lp);
                    x = (w - lp) * Math.Exp(-t * 40);
                    break;
                }
                case Inst.Wood:
                {
                    x = Math.Sin(2 * Math.PI * 900 * t) * Math.Exp(-t * 45) + 0.4 * Math.Sin(2 * Math.PI * 1400 * t) * Math.Exp(-t * 60);
                    break;
                }
            }
            double outv = x * amp;
            L[start + i] += (float)(outv * pl);
            R[start + i] += (float)(outv * pr);
        }
    }

    // ------------------------------------------------------------ reverb (Schroeder/Freeverb simplificado)

    static void Reverb(float[] L, float[] R, double wet, double room)
    {
        if (wet <= 0) return;
        int[] combs = { 1116, 1188, 1277, 1356 };
        int[] alls = { 556, 441 };
        var outL = Channel(L, combs, alls, 0, room);
        var outR = Channel(R, combs, alls, 23, room);
        for (int i = 0; i < L.Length; i++) { L[i] = (float)(L[i] * (1 - wet * 0.5) + outL[i] * wet); R[i] = (float)(R[i] * (1 - wet * 0.5) + outR[i] * wet); }
    }

    static float[] Channel(float[] x, int[] combs, int[] alls, int spread, double room)
    {
        var y = new float[x.Length];
        double fb = 0.70 + 0.28 * room, damp = 0.3;
        foreach (int c0 in combs)
        {
            int c = c0 + spread;
            var buf = new double[c]; int p = 0; double store = 0;
            for (int i = 0; i < x.Length; i++)
            {
                double o = buf[p];
                store = o * (1 - damp) + store * damp;
                buf[p] = x[i] * 0.25 + store * fb;
                p = (p + 1) % c;
                y[i] += (float)o;
            }
        }
        foreach (int a0 in alls)
        {
            int a = a0 + spread;
            var buf = new double[a]; int p = 0;
            for (int i = 0; i < y.Length; i++)
            {
                double b = buf[p];
                double o = -y[i] + b;
                buf[p] = y[i] + b * 0.5;
                p = (p + 1) % a;
                y[i] = (float)o;
            }
        }
        return y;
    }

    // ------------------------------------------------------------ WAV

    public static void WriteWav(string path, float[] stereo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int frames = stereo.Length / 2, bytes = frames * 4;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes); w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)2);
            w.Write(SR); w.Write(SR * 4); w.Write((short)4); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
            foreach (var s in stereo) w.Write((short)Math.Max(-32767, Math.Min(32767, s * 32767)));
        }
    }
}
