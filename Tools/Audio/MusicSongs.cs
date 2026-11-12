using System;
using I = MusicSynth.Inst;

// As partituras das músicas (Docs/Audio/plano-musicas.md). Tempos contados em semínimas.
public static class MusicSongs
{
    // o motivo das sete vidas (3/4): Ré–Lá | Dó–Si–Lá | Sol–Lá | Lá (sem resolver)
    public const string Motif = "D4:2 A4:1 C5:1.5 B4:0.5 A4:1 G4:2 A4:4";

    /// <summary>Transpõe uma linha ("D4:2 A4:1") em oitavas.</summary>
    public static string Oct(string line, int octaves)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var tok in line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var p = tok.Split(':');
            string n = p[0];
            if (n != "r")
            {
                int i = n.Length - 1;
                while (i > 0 && char.IsDigit(n[i - 1])) i--;
                n = n.Substring(0, i) + (int.Parse(n.Substring(i)) + octaves);
            }
            sb.Append(n).Append(':').Append(p[1]).Append(' ');
        }
        return sb.ToString();
    }

    /// <summary>Arpejo de harpa num compasso de 3/4: 6 colcheias (fundamental, quinta, oitava, décima, quinta acima, décima).</summary>
    static void HarpBar(MusicSynth.Song s, string chord, double beat, double vel, double pan)
    {
        var c = MusicSynth.Chord(chord);
        int r = c[0], t = c[1], f = c[2];
        int[] pat = { r, f, r + 12, t + 12, f + 12, t + 12 };
        for (int i = 0; i < 6; i++) s.AddMidi(I.Harp, pat[i], beat + i * 0.5, 0.5, vel * (i == 0 ? 1.0 : 0.8), pan + (i % 2 == 0 ? -0.15 : 0.15));
    }

    /// <summary>Cordas sustentando o acorde (voz aberta: fundamental, quinta, terça na oitava de cima), com baixo opcional.</summary>
    static void PadBar(MusicSynth.Song s, string chord, double beat, double beats, double vel, bool bass)
    {
        var c = MusicSynth.Chord(chord);
        s.AddMidi(I.Strings, c[0], beat, beats, vel, -0.3);
        s.AddMidi(I.Strings, c[2], beat, beats, vel, 0.3);
        s.AddMidi(I.Strings, c[1] + 12, beat, beats, vel * 0.9, 0.0);
        if (bass) s.AddMidi(I.Strings, c[0] - 12, beat, beats, vel * 1.1, 0.0);
    }

    static double Bar(int bar) { return (bar - 1) * 3.0; }   // compasso (1 em diante) → tempo

    /// <summary>MUS-01 — Sete Vidas (tema principal, menu). 72 BPM, 3/4, Ré dórico, 30 compassos.</summary>
    public static MusicSynth.Song TemaPrincipal()
    {
        var s = new MusicSynth.Song { Bpm = 72, Reverb = 0.32, RoomSize = 0.85 };
        s.Length = 30 * 3 * s.Spb;

        // vento do penhasco o tempo todo, bem baixo
        s.Notes.Add(new MusicSynth.Note { Inst = I.Wind, Start = 0, Dur = s.Length - 2.5, Vel = 1, Pan = 0 });

        string[] intro = { "Dm", "Dm", "Dm", "Dm" };
        string[] a = { "Dm", "F", "G", "Asus4", "Dm", "F", "G", "A" };
        string[] b = { "Bb", "C", "Dm", "A", "Bb", "C", "Dm", "A" };
        string[] a2 = { "Dm", "F", "G", "A", "Bb", "C" };
        string[] fim = { "Dm", "F", "G", "Dm" };

        // 1–4: introdução: harpa sozinha, o sino da torre longe
        s.Add(I.Bell, "D3", 0, 4, 0.9, -0.4);
        for (int i = 0; i < 4; i++) HarpBar(s, intro[i], Bar(1 + i), 0.55 + 0.1 * i, 0);

        // 5–12: A: a flauta canta o motivo inteiro (2×), cordas baixas entram
        for (int i = 0; i < 8; i++)
        {
            HarpBar(s, a[i], Bar(5 + i), 0.6, 0);
            PadBar(s, a[i], Bar(5 + i), 3, 0.55, false);
        }
        s.Line(I.Flute, Bar(5), Motif, 0.9, 0.1);
        s.Line(I.Flute, Bar(9), "D4:2 A4:1 D5:1.5 C5:0.5 B4:1 A4:2 G4:1 A4:3", 0.95, 0.1);

        // 13–20: B: pizzicato com o motivo do Lion, a flauta responde
        for (int i = 0; i < 8; i++)
        {
            HarpBar(s, b[i], Bar(13 + i), 0.45, 0);
            PadBar(s, b[i], Bar(13 + i), 3, 0.7, true);
        }
        const string lion = "D3:0.5 F3:0.5 A3:0.5 G3:0.5 F3:0.5 E3:0.5 D3:1 r:2";
        s.Line(I.Pizz, Bar(13), lion, 0.9, -0.35);
        s.Line(I.Pizz, Bar(17), lion, 0.95, -0.35);
        s.Line(I.Flute, Bar(15), "F5:1 E5:1 D5:1 C#5:2 A4:1", 0.8, 0.2);
        s.Line(I.Flute, Bar(19), "F5:1.5 E5:0.5 D5:1 E5:3", 0.85, 0.2);

        // 21–26: A': clímax: trompa e cordas no grave com o motivo, flauta uma oitava acima, tímpano
        for (int i = 0; i < 6; i++)
        {
            HarpBar(s, a2[i], Bar(21 + i), 0.55, 0);
            PadBar(s, a2[i], Bar(21 + i), 3, 1.0, true);
            var c = MusicSynth.Chord(a2[i]);
            s.AddMidi(I.Timpani, c[0] - 12, Bar(21 + i), 1, i == 0 ? 1.0 : 0.7, 0);
        }
        s.Add(I.Bell, "D3", Bar(21), 4, 0.6, -0.4);
        s.Line(I.Horn, Bar(21), Oct(Motif, -1), 1.0, -0.2);
        s.Line(I.Flute, Bar(21), Oct(Motif, 1), 0.8, 0.25);
        s.Line(I.Horn, Bar(25), "D3:3 E3:3", 0.9, -0.2);
        s.Line(I.Flute, Bar(25), "F5:2 G5:1 A5:3", 0.85, 0.25);

        // 27–30: final em aberto: a caixinha de música toca o motivo só até a 6ª nota e para
        for (int i = 0; i < 3; i++) PadBar(s, fim[i], Bar(27 + i), 3, 0.45, false);
        s.Line(I.MusicBox, Bar(27), "D5:2 A5:1 C6:1.5 B5:0.5 A5:1 G5:2", 0.9, 0.0);
        HarpBar(s, "Dm", Bar(27), 0.3, 0);
        // compasso 30: silêncio (só o vento e a cauda do reverb), e o loop volta ao começo
        return s;
    }
}
