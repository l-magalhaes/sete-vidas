using System;
using System.Collections.Generic;

// As receitas dos SFX provisórios (Docs/Audio/plano-sfx.md). Cada entrada vira um ou mais WAVs
// (variações com semente diferente) em Assets/_Game/Resources/Audio/SFX/<pasta>/. O nome do arquivo sem o
// "_NN" final (e sem o "sfx_") é o id que o jogo usa em Sfx.Play.
public class SfxEntry
{
    public string Name;      // ex.: sfx_gato_passo
    public string Folder;    // ex.: Gato
    public int Count = 1;
    public Func<int, float[]> Make;
    public double Db = -1.5; // pico depois de normalizar
    public bool Loop;
    /// <summary>Normaliza pela intensidade (padrão). As vozes usam o pico (Db), no nível aprovado.</summary>
    public bool Loudness = true;
}

public static class SfxBank
{
    /// <summary>Liga de novo as receitas sintéticas dos sons que hoje vêm de gravação (passos e água).</summary>
    static readonly bool UseSynthFoley = false;

    // ------------------------------------------------------------ vozes [PROPOSTA]

    public static readonly Sx.Voice Lion = new Sx.Voice { F0 = 235, Formant = 1.22, Rate = 0.82, Breath = 0.14, Jitter = 0.012, Melody = 0.09 };
    public static readonly Sx.Voice Koda = new Sx.Voice { F0 = 92, Formant = 0.8, Rate = 1.25, Breath = 0.08, Growl = 0.12, Melody = 0.05, Decline = 0.08 };
    public static readonly Sx.Voice Capivara = new Sx.Voice { F0 = 118, Formant = 0.92, Rate = 1.45, Breath = 0.32, Melody = 0.07, Decline = 0.2 };
    public static readonly Sx.Voice Guarda = new Sx.Voice { F0 = 150, Formant = 0.86, Rate = 0.9, Breath = 0.22, Growl = 0.45, Muffle = 2600, Melody = 0.05 };

    static double J(int seed, double amount)
    {
        var r = new Random(seed * 7919 + 13);
        return 1 + (r.NextDouble() * 2 - 1) * amount;
    }

    static float[] Room(float[] x, double wet) { return Sx.Reverb(x, wet, 0.7, 0.6); }
    static float[] Hall(float[] x, double wet) { return Sx.Reverb(x, wet, 1.3, 1.6); }

    /// <summary>O timbre exclusivo da praga: desafina devagar, anel e reverb invertido por baixo.</summary>
    static float[] Plague(float[] x, int seed)
    {
        var detuned = Sx.Mix(x, Sx.G(Sx.Pitch(x, 0.985), 0.6));
        var ring = Sx.Ring(detuned, 37, 0.35);
        var swell = Sx.Reverse(Sx.Reverb(Sx.Reverse(x), 0.9, 1.2, 1.0));
        return Sx.Mix(ring, Sx.G(Sx.LP(swell, 2500), 0.5));
    }

    static float[] Say(Sx.Voice v, string text, int seed) { return Sx.Speak(v, text, seed); }

    static float[] Sniff(int seed, int puffs)
    {
        var parts = new List<float[]>();
        for (int p = 0; p < puffs; p++)
            parts.Add(Sx.At(p * 0.13 * J(seed + p, 0.15), Sx.BP(Sx.Noise(0.07, Sx.Perc(0.01, 0.025), seed + p), Sx.Geo(1400, 2600, 0.07), 2)));
        return Sx.Mix(parts.ToArray());
    }

    static float[] Creak(double dur, double hz, int seed)
    {
        // couro/madeira rangendo: estalos densos num ressonador estreito
        var c = Sx.Crackle(dur, Sx.Lin(200, 900, dur), Sx.K(1), seed);
        return Sx.Mix(Sx.BP(c, Sx.Lin(hz, hz * 1.4, dur), 6), Sx.G(Sx.BP(c, hz * 2.7, 8), 0.4));
    }

    static float[] Rocks(double dur, int count, int seed)
    {
        var r = new Random(seed);
        var parts = new List<float[]>();
        for (int i = 0; i < count; i++)
        {
            double at = Math.Pow(r.NextDouble(), 1.5) * dur;
            double f = 180 + r.NextDouble() * 500;
            parts.Add(Sx.At(at, Sx.G(Sx.Mix(Sx.Thump(f, f * 0.6, 0.08, 0.025), Sx.G(Sx.BP(Sx.Noise(0.05, Sx.Perc(0.001, 0.015), seed + i), f * 3, 1.5), 0.6)),
                (0.4 + 0.6 * r.NextDouble()) * (1 - at / dur * 0.6))));
        }
        return Sx.Mix(parts.ToArray());
    }

    static float[] Chord(string[] notes, double gap, double dur, double ratio, double index, double decay)
    {
        var parts = new List<float[]>();
        for (int i = 0; i < notes.Length; i++)
            parts.Add(Sx.At(i * gap, Sx.Bell(Sx.Hz(notes[i]), dur, ratio, index, decay)));
        return Sx.Mix(parts.ToArray());
    }

    static float[] Harp(string[] notes, double gap, double dur, int seed)
    {
        var parts = new List<float[]>();
        for (int i = 0; i < notes.Length; i++)
            parts.Add(Sx.At(i * gap, Sx.LP(Sx.Pluck(Sx.Hz(notes[i]), dur, 0.996, seed + i), 4000)));
        return Sx.Mix(parts.ToArray());
    }

    static float[] Pad(string[] notes, double dur, double attack, double cutoff, int seed)
    {
        var parts = new List<float[]>();
        foreach (var n in notes)
        {
            double f = Sx.Hz(n);
            parts.Add(Sx.Osc(Sx.W.Saw, dur, Sx.K(f * 0.997), Sx.AHR(attack, dur - attack - 0.15, 0.15)));
            parts.Add(Sx.Osc(Sx.W.Saw, dur, Sx.K(f * 1.004), Sx.AHR(attack, dur - attack - 0.15, 0.15)));
        }
        return Sx.LP(Sx.Mix(parts.ToArray()), cutoff);
    }

    static float[] Thunder(double dur, double cutoff, int seed)
    {
        var rumble = Sx.Brown(dur, Sx.Perc(0.03, dur * 0.35), seed);
        var crack = Sx.Crackle(dur * 0.6, Sx.Lin(400, 20, dur * 0.6), Sx.Perc(0.001, dur * 0.15), seed + 1);
        return Sx.Mix(Sx.LP(rumble, cutoff), Sx.G(Sx.LP(crack, cutoff * 2.5), 0.5));
    }

    static float[] Chip(string[] notes, double each, Sx.W w)
    {
        var parts = new List<float[]>();
        for (int i = 0; i < notes.Length; i++)
            parts.Add(Sx.At(i * each, Sx.Osc(w, each * 1.2, Sx.K(Sx.Hz(notes[i])), Sx.AHR(0.002, each * 0.8, each * 0.3))));
        return Sx.Mix(parts.ToArray());
    }

    static float[] Paper(double dur, int seed)
    {
        return Sx.BP(Sx.Crackle(dur, Sx.K(500), Sx.AHR(0.02, dur * 0.5, dur * 0.4), seed), Sx.Lin(2500, 4500, dur), 0.9);
    }

    // ------------------------------------------------------------ a lista

    static void Add(List<SfxEntry> l, string folder, string name, int count, Func<int, float[]> make)
    {
        l.Add(new SfxEntry { Folder = folder, Name = name, Count = count, Make = make });
    }

    static void Loop(List<SfxEntry> l, string folder, string name, Func<int, float[]> make)
    {
        l.Add(new SfxEntry { Folder = folder, Name = name, Count = 1, Make = make, Loop = true, Db = -3 });
    }

    public static List<SfxEntry> All()
    {
        var l = new List<SfxEntry>();

        // ======================================================== GATO: locomoção
        // sfx_gato_passo, sfx_gato_passo_lento, sfx_gato_splash e sfx_gato_splash_grande vêm de gravação (Tools/Audio/importar_foley.ps1).
        // A receita sintética do passo fica aqui só como referência (desligada):
        if (UseSynthFoley) Add(l, "Gato", "sfx_gato_passo", 5, s => Sx.Mix(
            Sx.LP(Sx.Thump(170 * J(s, 0.1), 80, 0.07, 0.018), 1500),
            Sx.G(Sx.BP(Sx.Noise(0.04, Sx.Perc(0.001, 0.01), s), 2600 * J(s + 1, 0.2), 1.2), 0.5),
            Sx.G(Sx.Click(4000, s + 2), 0.2)));
        Add(l, "Gato", "sfx_gato_passo_agachado", 3, s => Sx.LP(Sx.Mix(
            Sx.Thump(140 * J(s, 0.1), 70, 0.06, 0.015),
            Sx.G(Sx.BP(Sx.Noise(0.08, Sx.AHR(0.02, 0.02, 0.04), s), 2200, 1.0), 0.35)), 1300));
        Add(l, "Gato", "sfx_gato_pulo", 3, s => Sx.Mix(
            Sx.Whoosh(0.13, 700 * J(s, 0.1), 2600, 1.2, s),
            Sx.G(Sx.HP(Sx.Noise(0.06, Sx.Perc(0.005, 0.02), s + 1), 3500), 0.3)));
        Add(l, "Gato", "sfx_gato_aterrissa", 3, s => Sx.Mix(
            Sx.LP(Sx.Thump(150 * J(s, 0.1), 65, 0.1, 0.03), 1200),
            Sx.G(Sx.LP(Sx.Noise(0.1, Sx.Perc(0.002, 0.03), s), 1800), 0.4)));
        Add(l, "Gato", "sfx_gato_aterrissa_pesada", 3, s => Sx.Mix(
            Sx.Drive(Sx.Thump(115 * J(s, 0.1), 45, 0.2, 0.06), 1.5),
            Sx.G(Sx.LP(Sx.Noise(0.3, Sx.Perc(0.003, 0.08), s), 900), 0.7),
            Sx.G(Rocks(0.25, 6, s + 3), 0.35)));
        Add(l, "Gato", "sfx_gato_borda", 3, s => Sx.Mix(
            Sx.BP(Sx.Crackle(0.12, Sx.K(1500), Sx.Perc(0.003, 0.05), s), 3600 * J(s, 0.15), 2),
            Sx.G(Sx.LP(Sx.Thump(200, 120, 0.06, 0.02), 1000), 0.6)));
        Add(l, "Gato", "sfx_gato_sobe_borda", 2, s => Sx.Mix(
            Sx.Whoosh(0.16, 500, 1800, 1.0, s),
            Sx.At(0.13, Sx.G(Sx.LP(Sx.Thump(160, 80, 0.07, 0.02), 1200), 0.8))));
        Add(l, "Gato", "sfx_gato_hera", 3, s => Sx.Mix(
            Sx.BP(Sx.Crackle(0.16, Sx.K(900), Sx.Perc(0.004, 0.06), s), Sx.Lin(2800, 4500, 0.16), 1.2),
            Sx.G(Sx.LP(Sx.Thump(180, 110, 0.05, 0.015), 900), 0.5)));
        Add(l, "Gato", "sfx_gato_escalada", 4, s => Sx.Mix(
            Sx.BP(Sx.Crackle(0.08, Sx.K(700), Sx.Perc(0.003, 0.03), s), 3500 * J(s, 0.2), 1.3),
            Sx.G(Sx.Click(3000, s), 0.3)));
        Add(l, "Gato", "sfx_gato_pulo_parede", 3, s => Sx.Mix(
            Sx.BP(Sx.Crackle(0.08, Sx.K(1600), Sx.Perc(0.002, 0.03), s), 3400, 2),
            Sx.At(0.02, Sx.Whoosh(0.16, 900, 3200, 1.3, s + 1))));
        if (UseSynthFoley) Add(l, "Gato", "sfx_gato_splash_grande", 1, s => Room(Sx.Mix(
            Sx.LP(Sx.Noise(0.7, Sx.Perc(0.004, 0.18), s), Sx.Geo(7000, 700, 0.6), 0.9),
            Sx.G(Sx.Drive(Sx.Thump(90, 38, 0.4, 0.12), 1.3), 0.9),
            Sx.G(Sx.Droplets(1.4, 40, 900, 3200, s + 2), 0.35)), 0.2));
        if (UseSynthFoley) Add(l, "Gato", "sfx_gato_splash", 2, s => Sx.Mix(
            Sx.LP(Sx.Noise(0.3, Sx.Perc(0.003, 0.07), s), Sx.Geo(5000, 900, 0.3), 0.9),
            Sx.G(Sx.Droplets(0.5, 10, 1200, 3500, s + 2), 0.4)));
        Add(l, "Gato", "sfx_gato_esconder", 3, s => Sx.LP(Sx.BP(
            Sx.Crackle(0.3, Sx.K(600), Sx.AHR(0.04, 0.12, 0.14), s), Sx.Lin(2000, 3500, 0.3), 0.9), 4500));

        // ======================================================== GATO: adagas
        Add(l, "Gato", "sfx_adaga_golpe", 4, s => Sx.Mix(
            Sx.Whoosh(0.09, 2400 * J(s, 0.1), 5200, 2.5, s),
            Sx.G(Sx.Bell(5800 * J(s + 1, 0.05), 0.08, 1.41, 1, 0.02), 0.05)));
        Add(l, "Gato", "sfx_adaga_golpe2", 3, s => Sx.Mix(
            Sx.Whoosh(0.13, 1700 * J(s, 0.1), 4300, 2.2, s),
            Sx.G(Sx.Bell(4400 * J(s + 1, 0.05), 0.15, 1.41, 1.5, 0.04), 0.08)));
        Add(l, "Gato", "sfx_acerto", 4, s => Sx.Mix(
            Sx.Click(3000, s),
            Sx.LP(Sx.Thump(320 * J(s, 0.1), 120, 0.07, 0.02), 2000),
            Sx.G(Sx.BP(Sx.Noise(0.06, Sx.Perc(0.001, 0.02), s + 1), 900, 1.4), 0.7),
            Sx.G(Sx.LP(Sx.Noise(0.12, Sx.Perc(0.005, 0.04), s + 2), 2500), 0.2)));
        Add(l, "Gato", "sfx_acerto_costas", 3, s => Sx.Mix(
            Sx.Click(3000, s),
            Sx.LP(Sx.Thump(260 * J(s, 0.1), 95, 0.09, 0.03), 2000),
            Sx.G(Sx.BP(Sx.Noise(0.07, Sx.Perc(0.001, 0.025), s + 1), 800, 1.4), 0.7),
            Sx.G(Sx.Metal(880 * J(s + 2, 0.06), 0.18, 0.06, s + 3), 0.6)));
        Add(l, "Gato", "sfx_acerto_furtivo", 2, s => Sx.Mix(
            Sx.G(Sx.Click(2500, s), 1.2),
            Sx.Drive(Sx.LP(Sx.Thump(210, 65, 0.16, 0.05), 1500), 1.6),
            Sx.G(Sx.BP(Sx.Noise(0.09, Sx.Perc(0.001, 0.03), s + 1), 700, 1.2), 0.8),
            Sx.G(Sx.Bell(Sx.Hz("D7") * J(s, 0.01), 0.7, 3.5, 2, 0.18), 0.45),
            Sx.At(0.08, Sx.G(Sx.LP(Sx.Noise(0.3, Sx.Perc(0.01, 0.09), s + 2), 2000), 0.3))));
        Add(l, "Gato", "sfx_adaga_tink", 2, s => Sx.Mix(
            Sx.G(Sx.Metal(2500 * J(s, 0.05), 0.12, 0.025, s), 0.9), Sx.Click(4000, s + 1)));
        Add(l, "Gato", "sfx_estocada", 2, s => Sx.Whoosh(0.13, 4200 * J(s, 0.1), 1300, 2, s));
        Add(l, "Gato", "sfx_quique", 2, s => Sx.Mix(
            Sx.Click(3000, s),
            Sx.LP(Sx.Thump(300, 140, 0.05, 0.015), 1800),
            Sx.G(Sx.Osc(Sx.W.Tri, 0.14, Sx.Geo(320 * J(s, 0.05), 640, 0.1), Sx.Perc(0.003, 0.05)), 0.25)));
        Add(l, "Gato", "sfx_boneco", 3, s => Sx.Mix(
            Sx.LP(Sx.Crackle(0.12, Sx.K(2500), Sx.Perc(0.002, 0.04), s), 3500),
            Sx.G(Sx.BP(Sx.Thump(420 * J(s, 0.1), 260, 0.05, 0.015), 800, 1), 1.5),
            Sx.G(Sx.Click(2500, s + 1), 0.5)));

        // ======================================================== GATO: marreta, parede, classes e cura
        Add(l, "Gato", "sfx_marreta_ergue", 2, s => Sx.Mix(
            Sx.G(Creak(0.32, 520 * J(s, 0.1), s), 0.7),
            Sx.G(Sx.Osc(Sx.W.Sine, 0.32, Sx.Geo(180, 330, 0.32), Sx.Swell(0.32, 2)), 0.25),
            Sx.G(Sx.Metal(620, 0.3, 0.05, s + 1), 0.15)));
        Add(l, "Gato", "sfx_marreta_golpe", 2, s => Sx.Whoosh(0.26, 280 * J(s, 0.1), 750, 1.4, s));
        Add(l, "Gato", "sfx_marreta_acerto", 3, s => Sx.Mix(
            Sx.Click(2000, s),
            Sx.Drive(Sx.Thump(125 * J(s, 0.08), 40, 0.3, 0.09), 1.8),
            Sx.G(Sx.Metal(470 * J(s + 1, 0.06), 0.4, 0.1, s + 2), 0.6),
            Sx.G(Sx.LP(Sx.Noise(0.3, Sx.Perc(0.002, 0.08), s + 3), 1200), 0.5)));
        Add(l, "Gato", "sfx_marreta_chao", 2, s => Room(Sx.Mix(
            Sx.Drive(Sx.Thump(95 * J(s, 0.08), 28, 0.6, 0.18), 2.2),
            Sx.G(Rocks(0.6, 12, s), 0.6),
            Sx.G(Sx.LP(Sx.Brown(0.9, Sx.Perc(0.01, 0.3), s + 1), 400), 0.9)), 0.2));
        Add(l, "Mundo", "sfx_parede_racha", 1, s => Sx.Mix(
            Sx.G(Sx.Click(1800, s), 1.3),
            Sx.HP(Sx.Crackle(0.18, Sx.K(3000), Sx.Perc(0.001, 0.05), s), 1500),
            Sx.G(Sx.LP(Sx.Thump(160, 70, 0.15, 0.05), 900), 0.9),
            Sx.G(Rocks(0.5, 7, s + 2), 0.35)));
        Add(l, "Mundo", "sfx_parede_quebra", 2, s => Room(Sx.Mix(
            Sx.G(Sx.Click(1500, s), 1.2),
            Sx.Drive(Sx.Thump(110, 35, 0.4, 0.15), 1.6),
            Sx.G(Rocks(1.2, 30, s + 1), 0.7),
            Sx.G(Sx.LP(Sx.Brown(1.5, Sx.Perc(0.02, 0.45), s + 2), 500), 1.0),
            Sx.At(1.0, Sx.G(Sx.Thump(75, 50, 0.4, 0.15), 0.6))), 0.25));

        Add(l, "Gato", "sfx_classe_ladino", 1, s => Sx.Mix(
            Harp(new[] { "D5", "A5" }, 0.06, 0.6, s),
            Sx.G(Sx.Whoosh(0.15, 1500, 3500, 1, s + 3), 0.2)));
        Add(l, "Gato", "sfx_classe_paladino", 1, s => Room(Sx.Mix(
            Sx.G(Chord(new[] { "D4", "A4" }, 0.05, 1.0, 1.4, 1.6, 0.35), 0.7),
            Sx.G(Sx.Metal(330, 0.4, 0.1, s), 0.3),
            Sx.G(Sx.LP(Sx.Thump(110, 60, 0.15, 0.05), 600), 0.6)), 0.25));
        Add(l, "Gato", "sfx_classe_desbloqueada", 1, s => Room(Sx.Mix(
            Chord(new[] { "D5", "F#5", "A5", "D6" }, 0.08, 1.4, 2.0, 1.2, 0.4),
            Sx.G(Harp(new[] { "D4", "A4" }, 0.0, 1.2, s), 0.5)), 0.35));
        Add(l, "Gato", "sfx_cura_concentra", 1, s => Room(Sx.Mix(
            Sx.G(Pad(new[] { "D4", "A4" }, 0.5, 0.35, 1400, s), 0.5),
            Sx.G(Chord(new[] { "A5", "D6", "F#6", "A6" }, 0.1, 0.4, 3, 0.8, 0.12), 0.3)), 0.3));
        Add(l, "Gato", "sfx_cura_efeito", 1, s => Room(Sx.Mix(
            Chord(new[] { "D4", "F#4", "A4", "D5" }, 0.015, 1.2, 2.0, 1.0, 0.45),
            Sx.G(Sx.Whoosh(0.4, 1500, 6000, 0.8, s), 0.35)), 0.35));
        Add(l, "Gato", "sfx_cura_interrompida", 1, s => Sx.Mix(
            Sx.Pitch(Pad(new[] { "D4", "A4" }, 0.35, 0.01, 1200, s), 0.8),
            Sx.G(Sx.HP(Sx.Noise(0.18, Sx.Perc(0.003, 0.06), s), Sx.Geo(5000, 1500, 0.18), 0.8), 0.5)));
        Add(l, "Gato", "sfx_cura_negada", 1, s => Sx.LP(Sx.Thump(220, 160, 0.06, 0.02), 900));

        // ======================================================== GATO: vida, dano e morte
        Add(l, "Gato", "sfx_gato_dano", 3, s => Sx.Mix(
            Sx.LP(Sx.Thump(260 * J(s, 0.08), 100, 0.1, 0.03), 1600),
            Sx.G(Sx.Click(2500, s), 0.8),
            Sx.G(Say(Lion, new[] { "nngh!", "hnk!", "ngah!" }[s % 3], s), 0.9),
            Sx.G(Sx.Osc(Sx.W.Square, 0.14, Sx.Geo(900, 280, 0.12), Sx.Perc(0.002, 0.05)), 0.12)));
        Add(l, "Gato", "sfx_gato_morte", 1, s => Room(Sx.Mix(
            Say(Lion, "mh... ah...", s),
            Sx.At(0.25, Sx.G(Sx.LP(Sx.Thump(130, 60, 0.15, 0.05), 900), 0.8)),
            Sx.At(0.35, Sx.G(Chip(new[] { "D5", "C5", "A4" }, 0.22, Sx.W.Tri), 0.3))), 0.3));
        Add(l, "Gato", "sfx_gato_renasce", 1, s => Room(Sx.Mix(
            Sx.G(Sx.Bell(Sx.Hz("D5"), 2.0, 2.0, 1.2, 0.6), 0.6),
            Sx.At(0.2, Sx.G(Sx.LP(Sx.Noise(0.6, Sx.AHR(0.25, 0.1, 0.25), s), 1200), 0.25))), 0.4));
        Loop(l, "Gato", "sfx_gato_batimento_loop", s =>
        {
            double period = 60.0 / 70.0;
            var beat = (Func<double, float[]>)(f => Sx.LP(Sx.Thump(f, f * 0.7, 0.12, 0.04), 300));
            var one = Sx.Mix(beat(62), Sx.At(0.26, Sx.G(beat(52), 0.7)));
            var y = new float[Sx.N(period * 2)];
            for (int k = 0; k < 2; k++) { var p = Sx.At(k * period, one); for (int i = 0; i < p.Length && i < y.Length; i++) y[i] += p[i]; }
            return y;
        });
        Add(l, "Gato", "sfx_espinhos", 2, s => Sx.Mix(
            Sx.G(Sx.Click(3500, s), 1.0),
            Sx.BP(Sx.Noise(0.09, Sx.Perc(0.002, 0.03), s + 1), Sx.Geo(2500, 1200, 0.09), 2),
            Sx.G(Sx.Metal(1600, 0.1, 0.02, s + 2), 0.3)));
        Add(l, "Gato", "sfx_chao_seguro", 1, s => Sx.Reverse(Sx.Whoosh(0.22, 800, 3000, 1.2, s)));
        Add(l, "Mundo", "sfx_coracao_cai", 1, s => Sx.Mix(
            Sx.Osc(Sx.W.Sine, 0.12, Sx.Geo(1800, 2600, 0.08), Sx.Perc(0.002, 0.04)),
            Sx.At(0.18, Sx.G(Sx.Osc(Sx.W.Sine, 0.06, Sx.K(2200), Sx.Perc(0.001, 0.015)), 0.5))));
        Add(l, "Mundo", "sfx_coracao_pega", 1, s => Room(Sx.Mix(
            Harp(new[] { "A5", "D6" }, 0.05, 0.7, s), Sx.G(Sx.Bell(Sx.Hz("F#6"), 0.6, 3, 0.8, 0.2), 0.25)), 0.2));

        // ======================================================== VOZES (murmúrios sem palavras)
        AddVoice(l, "vox_lion_neutro", Lion, "mrah-ne, tsi-tah.", "nya-mi to.", "mre-ta, nah.");
        AddVoice(l, "vox_lion_ironico", Lion, "hmf. nyah-mi... tsk.", "heh. mra-ne-tah, tsk.", "hmm. tsi-nya, mah.");
        AddVoice(l, "vox_lion_pergunta", Lion, "mrre-nah?", "nya-ti?", "mah-re-mi?");
        AddVoice(l, "vox_lion_surpresa", Lion, "mrrp?!", "nya?!", "hrrp!");
        AddVoice(l, "vox_lion_serio", Lion, "hrrnn... ka-tah.", "nnh. mra-tak.", "tah-ne... krah.");
        AddVoice(l, "vox_lion_ferido", Lion, "nnh... mh-ah...", "hhah... nyh...", "mmh... ah...");
        AddVoice(l, "vox_koda_neutro", Koda, "hrrm. do-ra, va-hum.", "ho-ra-dum. vah.", "hrm. da-vo-ru.");
        AddVoice(l, "vox_koda_serio", Koda, "vaa... dor-um, ha-ra-don.", "do-ra-vah... hum.", "ha-ru-dom. vor.");
        AddVoice(l, "vox_koda_selo", Koda, "hraa... do-raa!", "vaa-dor!", "hrrah... vo-dum!");
        AddVoice(l, "vox_koda_sorriso", Koda, "hoh-hoh-hm.", "ho-ho. da-rum.", "hm-hm. vo-rah.");
        AddVoice(l, "vox_koda_ferido", Koda, "hhm... ra-dah...", "hhoh... vu-rah.", "hhn... do...");
        AddVoice(l, "vox_capivara_neutro", Capivara, "ehhh... mm-hmm. shu-la-me.", "mmm... sha-lo-ve.", "ehh. shi-mu-la.");
        AddVoice(l, "vox_capivara_deboche", Capivara, "heh-heh... ssshh-la-la.", "hm-hm... sha-le-mu.", "heh. shh-la.");
        AddVoice(l, "vox_capivara_revelacao", Capivara, "mmmh... va-shu... nah.", "shhh... ve-la-mu-ra.", "mmh. sha-vo... la.");

        // ======================================================== TEMPLÁRIOS (cães de armadura)
        Add(l, "Inimigos", "sfx_guarda_passo", 5, s => Sx.Mix(
            Sx.LP(Sx.Thump(120 * J(s, 0.1), 65, 0.09, 0.03), 900),
            Sx.G(Sx.Jingle(0.1, 60, 2200, s), 0.25),
            Sx.G(Sx.LP(Sx.Noise(0.05, Sx.Perc(0.001, 0.015), s + 1), 1500), 0.4)));
        Add(l, "Inimigos", "sfx_guarda_ocioso", 4, s =>
        {
            switch (s % 4)
            {
                case 0: return Say(Guarda, "hrmf.", s);
                case 1: return Say(Guarda, "haa-ahh...", s);
                case 2: return Sniff(s, 3);
                default: return Say(Guarda, "hmm-hm.", s);
            }
        });
        Add(l, "Inimigos", "sfx_guarda_suspeito", 4, s => Sx.Mix(
            Say(Guarda, new[] { "hrr?", "hn?", "hrm?", "wuh?" }[s % 4], s),
            Sx.At(0.18, Sx.G(Chip(new[] { "E5", "A5" }, 0.06, Sx.W.Square), 0.12))));
        Add(l, "Inimigos", "sfx_guarda_investiga", 3, s => Sx.Mix(Sniff(s, 3), Sx.At(0.45, Sx.G(Say(Guarda, "hrm...", s), 0.7))));
        Add(l, "Inimigos", "sfx_guarda_alerta", 4, s => Sx.Mix(
            Sx.G(Say(new Sx.Voice { F0 = 175, Formant = 0.9, Rate = 0.6, Breath = 0.3, Growl = 0.55, Muffle = 3200, Melody = 0.02 },
                new[] { "wuf!", "hrah!", "wrah!", "rruf!" }[s % 4], s), 1.3),
            Sx.At(0.05, Sx.G(Sx.Metal(700 * J(s, 0.05), 0.25, 0.06, s), 0.45)),
            Sx.G(Chip(new[] { "A5", "E6" }, 0.05, Sx.W.Square), 0.14)));
        Add(l, "Inimigos", "sfx_guarda_caca", 3, s => Sx.Mix(
            Say(new Sx.Voice { F0 = 85, Formant = 0.85, Rate = 1, Breath = 0.35, Growl = 0.8, Muffle = 2000, Melody = 0.02, Decline = 0 }, "rrrrrrrr", s),
            Sx.At(0.5, Sniff(s + 1, 2))));
        Add(l, "Inimigos", "sfx_guarda_desiste", 3, s => Sx.Mix(
            Say(Guarda, new[] { "hmph.", "hmf.", "phff." }[s % 3], s),
            Sx.At(0.15, Sx.G(Sx.Jingle(0.15, 40, 1800, s), 0.2))));
        Add(l, "Inimigos", "sfx_lanca_prepara", 3, s => Sx.Mix(
            Sx.G(Creak(0.45, 450 * J(s, 0.1), s), 0.6),
            Sx.G(Sx.BP(Sx.Noise(0.45, Sx.Swell(0.45, 1.5), s + 1), Sx.Lin(900, 1600, 0.45), 2), 0.5),
            Sx.G(Sx.HP(Sx.Crackle(0.45, Sx.Lin(100, 900, 0.45), Sx.Swell(0.45, 2), s + 2), 2500), 0.35)));
        Add(l, "Inimigos", "sfx_lanca_golpe", 3, s => Sx.Mix(
            Sx.Whoosh(0.16, 600 * J(s, 0.1), 1700, 1.5, s),
            Sx.G(Say(Guarda, "hah!", s), 0.6)));
        Add(l, "Inimigos", "sfx_guarda_dano", 4, s => Sx.Mix(
            Say(new Sx.Voice { F0 = 260, Formant = 0.95, Rate = 0.6, Breath = 0.25, Growl = 0.3, Muffle = 3000 },
                new[] { "yip!", "hnk!", "ack!", "yarp!" }[s % 4], s),
            Sx.G(Sx.Metal(600 * J(s, 0.08), 0.15, 0.04, s), 0.35)));
        Add(l, "Inimigos", "sfx_guarda_morte", 3, s => Sx.Mix(
            Say(new Sx.Voice { F0 = 220, Formant = 0.92, Rate = 1.1, Breath = 0.25, Growl = 0.3, Muffle = 2800, Decline = 0.45 }, "aaooo...", s),
            Sx.At(0.25, Sx.G(Sx.Mix(Sx.Metal(380, 0.6, 0.12, s), Sx.LP(Sx.Thump(110, 50, 0.3, 0.1), 700), Sx.Jingle(0.3, 60, 1500, s)), 0.8)),
            Sx.At(0.55, Sx.G(Sx.Metal(900, 0.3, 0.07, s + 9), 0.4))));

        // ======================================================== TEMPLÁRIO ZUMBI (depois do tomo) e VENENO
        // Os mortos da praga: a voz passa sempre pelo timbre exclusivo da praga (Plague), o resto da armadura soa
        // enferrujado e abafado. Médios-graves (inimigo); o toque e o veneno ficam nos médios-agudos (são do gato).
        var zumbi = new Sx.Voice { F0 = 68, Formant = 0.78, Rate = 1.5, Breath = 0.45, Growl = 0.85, Muffle = 1600, Melody = 0.03, Decline = 0.15 };
        Add(l, "Inimigos", "sfx_zumbi_passo", 5, s => Sx.Mix(
            // o pé arrastado na pedra: raspão longo de ruído que sobe e cai, sem o tinido do guarda
            Sx.G(Sx.BP(Sx.Noise(0.28, Sx.AHR(0.04, 0.12, 0.12), s), Sx.Lin(500 * J(s, 0.15), 900, 0.28), 2.5), 0.8),
            Sx.LP(Sx.Thump(95 * J(s, 0.1), 50, 0.08, 0.03), 700),
            Sx.At(0.04, Sx.G(Sx.LP(Sx.Jingle(0.12, 30, 1300, s), 1800), 0.12))));
        Add(l, "Inimigos", "sfx_zumbi_ocioso", 4, s =>
            Plague(Sx.LP(Say(zumbi, new[] { "hhhrrrr...", "uuurrhh...", "hhh-hrr...", "grrrhh..." }[s % 4], s), 1800), s));
        // sentiu o gato (o "!" do zumbi): gemido que sobe, sem o chip do alerta dos templários (não é um soldado)
        Add(l, "Inimigos", "sfx_zumbi_sente", 3, s => Sx.Mix(
            Plague(Sx.G(Say(new Sx.Voice { F0 = 82, Formant = 0.82, Rate = 0.9, Breath = 0.4, Growl = 0.9, Muffle = 2200, Melody = 0.12, Decline = -0.35 },
                new[] { "hrraaagh!", "uuurraah!", "grraah!" }[s % 3], s), 1.3), s),
            Sx.At(0.05, Sx.G(Sx.Metal(420 * J(s, 0.05), 0.3, 0.08, s), 0.3))));
        // perseguição: respiração rasgada em rajadas curtas
        Add(l, "Inimigos", "sfx_zumbi_caca", 3, s => Sx.Mix(
            Plague(Sx.G(Say(zumbi, "hah-hah-hahh", s), 0.9), s),
            Sx.G(Sx.BP(Sx.Noise(0.5, Sx.Swell(0.5, 1.5), s + 3), 1100, 1.5), 0.3)));
        Add(l, "Inimigos", "sfx_zumbi_dano", 4, s => Sx.Mix(
            Sx.LP(Sx.Thump(140 * J(s, 0.1), 60, 0.12, 0.04), 1200),                          // pancada mole
            Sx.G(Sx.BP(Sx.Crackle(0.15, Sx.Lin(300, 60, 0.15), Sx.Perc(0.002, 0.06), s), 900, 2), 0.6), // carne seca
            Sx.G(Sx.Metal(330 * J(s, 0.08), 0.18, 0.05, s), 0.25),                            // aço enferrujado
            Sx.At(0.04, Plague(Sx.G(Say(zumbi, new[] { "hk!", "ghh!", "urk!", "hngh!" }[s % 4], s), 0.8), s))));
        // morte: o gemido cai e o corpo se desfaz em fiapos (chiado que sobe e some, como a animação)
        Add(l, "Inimigos", "sfx_zumbi_morte", 3, s => Sx.Mix(
            Plague(Say(new Sx.Voice { F0 = 75, Formant = 0.8, Rate = 1.6, Breath = 0.5, Growl = 0.7, Muffle = 1500, Decline = 0.5 }, "uuuoohhh...", s), s),
            Sx.At(0.3, Sx.G(Sx.LP(Sx.Thump(90, 40, 0.3, 0.1), 600), 0.8)),
            Sx.At(0.3, Sx.G(Sx.LP(Sx.Jingle(0.25, 40, 1200, s), 1500), 0.3)),
            Sx.At(0.55, Sx.G(Sx.HP(Sx.Crackle(0.9, Sx.Lin(900, 100, 0.9), Sx.AHR(0.15, 0.3, 0.45), s + 5), 1800), 0.5)),
            Sx.At(0.55, Sx.G(Sx.Reverse(Sx.Reverb(Sx.Noise(0.06, Sx.Perc(0.001, 0.03), s + 7), 0.9, 1.0, 0.9)), 0.4))));
        // o toque que envenena: tapa úmido + chiado ácido (avisa que a regra disparou, mesmo sem olhar)
        Add(l, "Gato", "sfx_zumbi_toque", 3, s => Sx.Mix(
            Sx.G(Sx.BP(Sx.Noise(0.06, Sx.Perc(0.001, 0.03), s), 1800 * J(s, 0.1), 1.2), 0.9),
            Sx.LP(Sx.Thump(220 * J(s, 0.08), 110, 0.07, 0.02), 1600),
            Sx.At(0.03, Sx.G(Sx.HP(Sx.Crackle(0.35, Sx.Lin(1500, 400, 0.35), Sx.Perc(0.01, 0.15), s + 1), 2500), 0.55)),
            Sx.At(0.03, Sx.G(Plague(Sx.Osc(Sx.W.Tri, 0.3, Sx.Geo(Sx.Hz("E5"), Sx.Hz("Bb4"), 0.3), Sx.Perc(0.005, 0.12)), s), 0.35))));
        // cada tique do veneno: uma bolha que estoura e uma nota curta que cai (o gato perdendo vida)
        Add(l, "Gato", "sfx_veneno_tique", 3, s => Sx.Mix(
            Sx.G(Sx.Osc(Sx.W.Sine, 0.08, Sx.Geo(500 * J(s, 0.1), 1400, 0.08), Sx.Perc(0.002, 0.05)), 0.6),   // bolha
            Sx.At(0.07, Sx.G(Sx.BP(Sx.Noise(0.03, Sx.Perc(0.001, 0.012), s), 3000, 2), 0.6)),            // estouro
            Sx.At(0.05, Sx.G(Sx.Osc(Sx.W.Square, 0.18, Sx.Geo(Sx.Hz("A4"), Sx.Hz("E4"), 0.18), Sx.Perc(0.003, 0.09)), 0.18)),
            Sx.At(0.05, Sx.G(Sx.HP(Sx.Crackle(0.2, Sx.Lin(800, 200, 0.2), Sx.Perc(0.01, 0.1), s + 2), 2200), 0.35))));
        // o veneno passou: um suspiro curto e claro (o perigo acabou)
        Add(l, "Gato", "sfx_veneno_passa", 1, s => Sx.Mix(
            Sx.G(Sx.BP(Sx.Noise(0.3, Sx.AHR(0.03, 0.1, 0.17), s), Sx.Lin(1800, 1000, 0.3), 1.5), 0.5),
            Sx.At(0.08, Sx.G(Chip(new[] { "E5", "A5" }, 0.07, Sx.W.Tri), 0.2))));

        // ======================================================== ATIRADOR E SETEIRAS
        Add(l, "Inimigos", "sfx_besta_mira", 2, s => Sx.Mix(
            Sx.G(Sx.BP(Sx.Crackle(0.9, Sx.Lin(30, 220, 0.9), Sx.Swell(0.9, 1.2), s), 1500, 4), 0.8),
            Sx.G(Sx.Osc(Sx.W.Sine, 0.9, Sx.Geo(380, 950, 0.9), Sx.Swell(0.9, 1.5)), 0.12)));
        Add(l, "Inimigos", "sfx_besta_trava", 1, s => Sx.Mix(Sx.G(Sx.Metal(2100, 0.08, 0.02, s), 1.2), Sx.G(Sx.Click(2500, s), 1.0)));
        Add(l, "Inimigos", "sfx_besta_disparo", 3, s => Sx.Mix(
            Sx.Click(2000, s),
            Sx.G(Sx.LP(Sx.Pluck(105 * J(s, 0.05), 0.25, 0.97, s), 1500), 0.8),
            Sx.G(Sx.LP(Sx.Thump(260, 140, 0.06, 0.02), 1200), 0.6),
            Sx.G(Sx.Whoosh(0.12, 1500, 4000, 2, s + 1), 0.4)));
        Add(l, "Inimigos", "sfx_virote_crava", 3, s => Sx.Mix(
            Sx.BP(Sx.Thump(260 * J(s, 0.1), 150, 0.08, 0.025), 700, 1),
            Sx.G(Sx.Click(2000, s), 0.6),
            Sx.At(0.02, Sx.G(Sx.Amp(Sx.LP(Sx.Pluck(190 * J(s, 0.08), 0.3, 0.993, s), 1200), Sx.Perc(0.001, 0.08)), 0.5))));
        Add(l, "Inimigos", "sfx_besta_recarga", 2, s =>
        {
            var parts = new List<float[]>();
            for (int k = 0; k < 11; k++) parts.Add(Sx.At(0.1 + k * 0.1, Sx.G(Sx.BP(Sx.Click(800, s + k), 1700, 3), 1.6)));
            parts.Add(Sx.G(Creak(1.2, 380, s), 0.35));
            parts.Add(Sx.At(1.35, Sx.Mix(Sx.G(Sx.Metal(1200, 0.12, 0.03, s), 0.8), Sx.Click(1500, s))));
            return Sx.Mix(parts.ToArray());
        });
        Add(l, "Armadilhas", "sfx_seteira_disparo", 3, s => Sx.Mix(
            Sx.G(Sx.Click(2500, s), 1.2), Sx.G(Sx.Metal(1400 * J(s, 0.05), 0.08, 0.02, s), 0.4),
            Sx.G(Sx.Whoosh(0.1, 1800, 4200, 2.2, s + 1), 0.5)));

        // ======================================================== CHEFE: KODA
        Add(l, "Chefe", "sfx_koda_passo", 4, s => Sx.Mix(
            Sx.Drive(Sx.LP(Sx.Thump(90 * J(s, 0.1), 45, 0.15, 0.05), 600), 1.3),
            Sx.G(Sx.Metal(380 * J(s, 0.06), 0.2, 0.05, s), 0.35)));
        Add(l, "Chefe", "sfx_koda_ergue", 3, s => Sx.Mix(
            Sx.G(Sx.Metal(300, 0.5, 0.15, s), 0.3),
            Sx.G(Sx.BP(Sx.Noise(0.5, Sx.Swell(0.5, 1.4), s), Sx.Lin(700, 1300, 0.5), 2), 0.4),
            Sx.G(Say(Koda, new[] { "hrrm", "hrrah", "hhm" }[s % 3], s), 0.8)));
        Add(l, "Chefe", "sfx_koda_impacto", 3, s => Room(Sx.Mix(
            Sx.Whoosh(0.08, 300, 800, 1.3, s),
            Sx.At(0.05, Sx.Mix(Sx.Click(1800, s), Sx.Drive(Sx.Thump(100 * J(s, 0.06), 32, 0.45, 0.13), 2.0),
                Sx.G(Rocks(0.4, 8, s + 1), 0.5), Sx.G(Sx.Metal(260, 0.4, 0.12, s + 2), 0.35)))), 0.2));
        Add(l, "Chefe", "sfx_koda_salto_agacha", 1, s => Sx.Mix(
            Sx.G(Creak(0.5, 300, s), 0.6), Sx.G(Say(Koda, "hraah-", s), 1.0)));
        Add(l, "Chefe", "sfx_koda_salto_ar", 1, s => Sx.Whoosh(0.55, 250, 1200, 0.9, s));
        Add(l, "Chefe", "sfx_koda_salto_queda", 2, s => Hall(Sx.Mix(
            Sx.G(Sx.Click(1200, s), 1.5),
            Sx.Drive(Sx.Thump(80 * J(s, 0.05), 24, 0.8, 0.22), 2.5),
            Sx.G(Rocks(0.8, 20, s + 1), 0.7),
            Sx.G(Sx.LP(Sx.Brown(1.2, Sx.Perc(0.01, 0.4), s + 2), 350), 1.0)), 0.25));
        Loop(l, "Chefe", "sfx_koda_cura_loop", s => Sx.Loopify(Sx.Mix(
            Sx.G(Pad(new[] { "D3", "A3", "D4" }, 2.4, 0.3, 800, s), 0.6),
            Sx.G(Sx.Bell(Sx.Hz("A5"), 1.0, 2, 1, 0.3), 0.25),
            Sx.At(1.0, Sx.G(Sx.Bell(Sx.Hz("A5"), 1.0, 2, 1, 0.3), 0.25))), 0.4));
        Add(l, "Chefe", "sfx_koda_cura_quebra", 1, s => Sx.Mix(
            Sx.HP(Sx.Crackle(0.25, Sx.K(4000), Sx.Perc(0.001, 0.07), s), 2500),
            Sx.G(Sx.Pitch(Chord(new[] { "D5", "A5", "D6" }, 0, 0.6, 2.76, 3, 0.2), 0.7), 0.5),
            Sx.G(Sx.Click(2500, s), 1.0)));
        Add(l, "Chefe", "sfx_koda_atordoado", 1, s =>
        {
            var r = new Random(s);
            var parts = new List<float[]>();
            for (int k = 0; k < 8; k++) parts.Add(Sx.At(k * 0.18, Sx.G(Sx.Bell(2200 + r.NextDouble() * 1500, 0.4, 3.1, 1.2, 0.08), 0.3)));
            parts.Add(Sx.G(Say(Koda, "hhah... hhah...", s), 0.6));
            return Sx.Mix(parts.ToArray());
        });
        Add(l, "Chefe", "sfx_koda_raio_ergue", 1, s => Room(Sx.Mix(
            Sx.G(Thunder(1.4, 260, s), 0.7),
            Sx.G(Sx.BP(Sx.Osc(Sx.W.Saw, 0.8, Sx.Geo(70, 380, 0.8), Sx.Swell(0.8, 1.5)), Sx.Geo(400, 2500, 0.8), 2), 0.5),
            Sx.G(Say(Koda, "vaa-dor!", s), 0.9)), 0.3));
        Add(l, "Chefe", "sfx_raio_aviso", 1, s => Sx.Mix(
            Sx.BP(Sx.Ring(Sx.Osc(Sx.W.Square, 1.25, Sx.K(110), Sx.Swell(1.2, 1.3)), 220, 0.7), Sx.Geo(500, 3000, 1.2), 1.5),
            Sx.G(Sx.HP(Sx.Crackle(1.25, Sx.Lin(50, 600, 1.2), Sx.Swell(1.2, 1.5), s), 3000), 0.4)));
        Add(l, "Chefe", "sfx_raio", 4, s => Room(Sx.Mix(
            Sx.G(Sx.HP(Sx.Noise(0.02, Sx.Perc(0.0005, 0.004), s), 2000), 1.5),
            Sx.G(Sx.HP(Sx.Crackle(0.35, Sx.K(3000), Sx.Perc(0.001, 0.1), s + 1), 1500), 0.8),
            Sx.G(Thunder(1.1 * J(s, 0.1), 1500, s + 2), 1.0)), 0.25));
        Add(l, "Chefe", "sfx_escudo_liga", 1, s => Room(Chord(new[] { "D5", "A5", "D6" }, 0.05, 0.8, 2, 1.2, 0.3), 0.3));
        Add(l, "Chefe", "sfx_escudo_dong", 3, s => Sx.LP(Sx.Mix(
            Sx.Bell(220 * J(s, 0.04), 1.0, 2.76, 2.5, 0.35), Sx.G(Sx.Click(1200, s), 0.4)), 1600));
        Add(l, "Chefe", "sfx_escudo_desliga", 1, s => Room(Chord(new[] { "D6", "A5", "D5" }, 0.06, 0.7, 2, 1, 0.25), 0.3));
        Add(l, "Chefe", "sfx_koda_furioso", 1, s => Hall(Sx.Mix(
            Sx.G(Say(new Sx.Voice { F0 = 85, Formant = 0.78, Rate = 1.3, Breath = 0.3, Growl = 0.7, Melody = 0.03, Decline = 0.3 }, "grraaahh!", s), 1.2),
            Sx.G(Sx.Jingle(0.6, 70, 900, s), 0.35),
            Sx.At(0.3, Sx.G(Pad(new[] { "D3", "A3", "D4", "F4" }, 1.2, 0.05, 1800, s), 0.4))), 0.3));
        Add(l, "Chefe", "sfx_koda_dano", 4, s => Sx.Mix(
            Sx.G(Sx.Metal(240 * J(s, 0.08), 0.35, 0.09, s), 0.7),
            Sx.LP(Sx.Thump(180, 90, 0.1, 0.03), 1200),
            Sx.G(Say(Koda, new[] { "hnf!", "hmf!", "hrk!", "nngh!" }[s % 4], s), 0.6)));
        Add(l, "Chefe", "sfx_koda_derrota", 1, s => Hall(Sx.Mix(
            Sx.G(Sx.Metal(200, 0.8, 0.2, s), 0.6),
            Sx.Drive(Sx.LP(Sx.Thump(100, 40, 0.4, 0.12), 700), 1.5),
            Sx.At(0.4, Sx.G(Sx.Mix(Sx.Metal(330, 0.4, 0.1, s + 1), Sx.LP(Sx.Thump(140, 70, 0.2, 0.06), 800)), 0.6)),
            Sx.At(0.8, Sx.G(Say(Koda, "hhaah... hm...", s), 0.7))), 0.25));
        Add(l, "Mundo", "sfx_grade_fecha", 1, s => Hall(Sx.Mix(
            Sx.G(Sx.Jingle(0.5, 80, 900, s), 0.5),
            Sx.At(0.45, Sx.Mix(Sx.Drive(Sx.Thump(95, 40, 0.4, 0.12), 1.8), Sx.G(Sx.Metal(180, 0.7, 0.2, s), 0.6)))), 0.3));
        Add(l, "Mundo", "sfx_grade_abre", 1, s => Room(Sx.Mix(
            Sx.G(Sx.Jingle(1.0, 60, 1000, s), 0.45), Sx.G(Creak(1.0, 260, s), 0.6)), 0.25));
        Add(l, "Chefe", "sfx_selo_golpe", 1, s => Hall(Sx.Mix(
            Sx.Drive(Sx.Thump(85, 26, 0.9, 0.25), 2.4),
            Sx.G(Rocks(0.6, 14, s), 0.5),
            Sx.G(Sx.Bell(Sx.Hz("D3"), 3.0, 2.0, 3, 1.0), 0.7),
            Sx.At(0.05, Sx.G(Chord(new[] { "D5", "F#5", "A5", "D6" }, 0.01, 2.0, 2.0, 1, 0.6), 0.35))), 0.35));

        // ======================================================== O TOMO, A MALDIÇÃO E O CONTRATANTE (praga)
        Loop(l, "Praga", "sfx_tomo_zumbido_loop", s => Sx.Loopify(Plague(Sx.Mix(
            Sx.G(Sx.Osc(Sx.W.Saw, 4.6, Sx.K(Sx.Hz("D2")), Sx.K(1)), 0.25),
            Sx.G(Sx.Osc(Sx.W.Sine, 4.6, Sx.K(Sx.Hz("D2") * 1.007), Sx.K(1)), 0.5),
            Sx.G(Sx.Osc(Sx.W.Sine, 4.6, Sx.K(Sx.Hz("Eb3")), Sx.K(1)), 0.12),
            Sx.G(Sx.BP(Sx.Noise(4.6, Sx.K(1), s), (Curve)(t => 1600 + 900 * Math.Sin(t * 1.3)), 3), 0.25)), s), 0.5));
        Add(l, "Praga", "sfx_tomo_pega", 1, s => Sx.Mix(
            Sx.G(Sx.Click(1500, s), 1.2),
            Sx.Reverse(Sx.Reverb(Sx.Mix(Sx.Noise(0.05, Sx.Perc(0.001, 0.02), s), Sx.Thump(200, 90, 0.1, 0.03)), 1.0, 1.2, 0.9)),
            Sx.At(0.95, Plague(Sx.Drive(Sx.Thump(70, 30, 0.6, 0.2), 1.6), s))));
        Loop(l, "Praga", "sfx_tomo_paginas_loop", s =>
        {
            var r = new Random(s);
            var parts = new List<float[]>();
            for (int k = 0; k < 10; k++) parts.Add(Sx.At(k * 0.2 + r.NextDouble() * 0.05, Sx.G(Paper(0.09, s + k), 0.5 + 0.5 * r.NextDouble())));
            parts.Add(Sx.G(Sx.LP(Sx.Noise(2.2, Sx.K(1), s + 99), (Curve)(t => 500 + 300 * Math.Sin(t * 3)), 0.707), 0.25));
            return Sx.Loopify(Sx.Mix(parts.ToArray()), 0.2);
        });
        // cada coração arrancado toca uma nota do motivo das sete vidas, desafinando (o Si vira Si bemol)
        string[] motif = { "D5", "A5", "C6", "Bb5", "A5", "G5" };
        // (as 6 variações tocam em ordem: o jogo pede a variação pelo número do coração)
        Add(l, "Praga", "sfx_coracao_arrancado", motif.Length, v => Sx.Mix(
            Sx.G(Sx.Reverse(Sx.Whoosh(0.3, 600, 3000, 1.2, v)), 0.5),
            Sx.At(0.25, Plague(Sx.Mix(
                Sx.Osc(Sx.W.Tri, 0.9, Sx.Geo(Sx.Hz(motif[v]), Sx.Hz(motif[v]) * 0.94, 0.9), Sx.Perc(0.005, 0.3)),
                Sx.G(Sx.Bell(Sx.Hz(motif[v]), 0.9, 2, 1, 0.3), 0.4)), v))));
        Add(l, "Praga", "sfx_tomo_fecha", 1, s => Sx.Mix(
            Sx.G(Sx.LP(Sx.Noise(0.08, Sx.Perc(0.001, 0.02), s), 2500), 1.0),
            Sx.LP(Sx.Thump(180, 80, 0.15, 0.04), 1000),
            Sx.G(Sx.Reverse(Plague(Sx.Osc(Sx.W.Saw, 0.4, Sx.K(Sx.Hz("D2")), Sx.Perc(0.01, 0.15)), s)), 0.3)));
        Add(l, "Praga", "sfx_capivara_surge", 1, s => Sx.Mix(
            Sx.Reverse(Sx.Reverb(Sx.Mix(Sx.Noise(0.08, Sx.Perc(0.001, 0.03), s), Sx.Thump(150, 60, 0.15, 0.05)), 1.0, 1.3, 1.0)),
            Sx.At(1.05, Plague(Say(Capivara, "heh...", s), s))));
        Add(l, "Praga", "sfx_capivara_some", 1, s => Sx.Mix(
            Sx.Reverse(Sx.Reverb(Sx.Noise(0.06, Sx.Perc(0.001, 0.02), s), 1.0, 1.3, 0.8)),
            Sx.At(0.7, Sx.G(Sx.Click(1000, s), 0.4))));
        Add(l, "Praga", "sfx_moeda_pesada", 1, s => Room(Sx.Mix(
            Sx.Bell(1250, 0.6, 2.4, 2, 0.15),
            Sx.Amp(Sx.Bell(1180, 1.4, 2.4, 1.5, 0.5), (Curve)(t => 0.35 * (0.5 + 0.5 * Math.Sin(2 * Math.PI * (22 - 12 * Sx.C01(t / 1.3)) * t)))),
            Sx.At(1.35, Sx.G(Sx.Bell(1150, 0.3, 2.4, 1, 0.06), 0.4)),
            Sx.At(1.45, Sx.G(Sx.Bell(1150, 0.3, 2.4, 1, 0.04), 0.25))), 0.4));

        // ======================================================== MUNDO: objetos barulhentos e interativos
        Add(l, "Mundo", "sfx_cacos", 4, s =>
        {
            var r = new Random(s);
            var parts = new List<float[]> { Sx.HP(Sx.Crackle(0.16, Sx.K(2500), Sx.Perc(0.002, 0.05), s), 2500) };
            for (int k = 0; k < 5; k++) parts.Add(Sx.At(r.NextDouble() * 0.12, Sx.G(Sx.Bell(4000 + r.NextDouble() * 4000, 0.15, 2.7, 1, 0.03), 0.25)));
            return Sx.Mix(parts.ToArray());
        });
        Add(l, "Mundo", "sfx_folhas", 4, s => Sx.BP(Sx.Crackle(0.17, Sx.K(1400), Sx.AHR(0.01, 0.06, 0.1), s), 2600 * J(s, 0.15), 0.9));
        Add(l, "Mundo", "sfx_sininhos", 3, s => Room(Sx.Mix(
            Sx.Bell(Sx.Hz("E6") * J(s, 0.02), 1.3, 2.4, 1.6, 0.35),
            Sx.At(0.05, Sx.G(Sx.Bell(Sx.Hz("G#6") * J(s + 1, 0.02), 1.2, 2.4, 1.6, 0.3), 0.8)),
            Sx.At(0.11, Sx.G(Sx.Bell(Sx.Hz("B6") * J(s + 2, 0.02), 1.1, 2.4, 1.6, 0.28), 0.7)),
            Sx.At(0.2, Sx.G(Sx.Bell(Sx.Hz("E6") * J(s + 3, 0.02), 0.8, 2.4, 1.2, 0.2), 0.4))), 0.2));
        Add(l, "Mundo", "sfx_balde", 2, s => Sx.Mix(
            Sx.G(Sx.Metal(620 * J(s, 0.06), 0.4, 0.09, s), 0.8),
            Sx.LP(Sx.Thump(200, 100, 0.08, 0.025), 1200),
            Sx.At(0.18, Sx.G(Sx.Metal(700, 0.3, 0.06, s + 1), 0.5)),
            Sx.At(0.3, Sx.G(Sx.Amp(Sx.Jingle(0.6, 40, 900, s + 2), Sx.Lin(1, 0, 0.6)), 0.4))));
        Add(l, "Mundo", "sfx_moeda", 3, s => Sx.Mix(
            Sx.G(Sx.Bell(Sx.Hz("A6") * J(s, 0.01), 0.3, 2.4, 1, 0.08), 0.7),
            Sx.At(0.06, Sx.Bell(Sx.Hz("D7") * J(s, 0.01), 0.4, 2.4, 1, 0.12))));
        Add(l, "Mundo", "sfx_moeda5", 2, s => Sx.Mix(
            Sx.G(Sx.Bell(Sx.Hz("A6"), 0.3, 2.4, 1, 0.08), 0.7),
            Sx.At(0.06, Sx.Bell(Sx.Hz("D7"), 0.4, 2.4, 1, 0.12)),
            Sx.At(0.04, Sx.G(Sx.Jingle(0.25, 50, 2600, s), 0.5))));
        Add(l, "Mundo", "sfx_moeda100", 1, s => Room(Sx.Mix(
            Sx.Bell(1250, 1.0, 2.4, 2, 0.35),
            Sx.At(0.08, Sx.G(Sx.Bell(Sx.Hz("D7"), 0.8, 2.4, 1, 0.25), 0.6)),
            Sx.At(0.1, Sx.G(Plague(Sx.Osc(Sx.W.Sine, 0.8, Sx.K(Sx.Hz("D3")), Sx.Perc(0.05, 0.3)), s), 0.25))), 0.3));
        Add(l, "Mundo", "sfx_placa", 2, s => Sx.Mix(
            Sx.G(Sx.BP(Sx.Brown(0.3, Sx.AHR(0.03, 0.15, 0.12), s), 450, 1.2), 1.0),
            Sx.G(Sx.HP(Sx.Crackle(0.3, Sx.K(300), Sx.AHR(0.03, 0.15, 0.12), s + 1), 1500), 0.3)));
        Add(l, "Mundo", "sfx_santuario_descansa", 1, s => Hall(Sx.Mix(
            Sx.G(Sx.HP(Sx.Noise(0.08, Sx.Perc(0.001, 0.03), s), 2500), 0.5),
            Sx.At(0.04, Sx.G(Sx.Whoosh(0.35, 400, 1400, 0.9, s + 1), 0.4)),
            Sx.At(0.25, Sx.G(Sx.Bell(Sx.Hz("D5"), 2.5, 2.0, 1.2, 0.8), 0.5)),
            Sx.At(0.5, Harp(new[] { "D5", "A5", "C6" }, 0.28, 1.6, s + 2))), 0.3));

        // ======================================================== ARMADILHAS
        Add(l, "Armadilhas", "sfx_fio", 2, s => Sx.Mix(
            Sx.G(Sx.Amp(Sx.Pluck(175 * J(s, 0.05), 0.5, 0.995, s), Sx.Perc(0.001, 0.15)), 0.8),
            Sx.G(Sx.Amp(Sx.Pluck(352 * J(s, 0.05), 0.5, 0.995, s + 1), Sx.Perc(0.001, 0.12)), 0.5),
            Sx.At(0.05, Sx.G(Sx.Bell(1500, 0.3, 2.4, 1.5, 0.08), 0.5)),
            Sx.At(0.17, Sx.G(Sx.Bell(1500, 0.3, 2.4, 1.5, 0.08), 0.45)),
            Sx.At(0.29, Sx.G(Sx.Bell(1500, 0.4, 2.4, 1.5, 0.12), 0.4))));
        Add(l, "Armadilhas", "sfx_placa_clic", 2, s => Sx.Mix(
            Sx.LP(Sx.Thump(160 * J(s, 0.06), 80, 0.1, 0.03), 900),
            Sx.At(0.04, Sx.Mix(Sx.G(Sx.Click(2000, s), 1.3), Sx.G(Sx.Metal(1800, 0.08, 0.02, s), 0.7)))));
        Add(l, "Armadilhas", "sfx_flecha_aviso", 1, s =>
        {
            var parts = new List<float[]>();
            for (int k = 0; k < 11; k++) parts.Add(Sx.At(k * 0.026, Sx.G(Sx.BP(Sx.Click(900, s + k), 2000, 3), 1.4)));
            return Sx.Mix(parts.ToArray());
        });
        Add(l, "Armadilhas", "sfx_flecha", 3, s => Sx.Mix(
            Sx.Whoosh(0.1, 1500 * J(s, 0.1), 3800, 2.2, s), Sx.G(Sx.Click(2500, s + 1), 0.4)));
        Add(l, "Armadilhas", "sfx_flecha_crava", 3, s => Sx.Mix(
            Sx.BP(Sx.Thump(330 * J(s, 0.1), 200, 0.06, 0.018), 900, 1), Sx.G(Sx.Click(2000, s), 0.5)));
        Add(l, "Armadilhas", "sfx_lanca_aviso", 1, s => Sx.Mix(
            Sx.BP(Sx.Brown(0.28, Sx.Swell(0.25, 1), s), 220, 1.2),
            Sx.G(Rocks(0.25, 6, s + 1), 0.5)));
        Add(l, "Armadilhas", "sfx_lanca_sobe", 2, s => Sx.Mix(
            Sx.BP(Sx.Noise(0.14, Sx.Perc(0.002, 0.05), s), Sx.Geo(2000, 6000, 0.12), 2),
            Sx.G(Sx.Metal(1200 * J(s, 0.05), 0.2, 0.05, s + 1), 0.5)));
        Add(l, "Armadilhas", "sfx_chama_fagulha", 2, s => Sx.Mix(
            Sx.HP(Sx.Crackle(0.6, Sx.Lin(80, 400, 0.6), Sx.Swell(0.6, 1), s), 3500),
            Sx.G(Sx.HP(Sx.Noise(0.6, Sx.Swell(0.6, 2), s + 1), 5000), 0.15)));
        Add(l, "Armadilhas", "sfx_chama", 2, s => Sx.Mix(
            Sx.LP(Sx.Noise(1.2, Sx.AHR(0.05, 0.95, 0.2), s), Sx.Geo(300, 2600, 0.12), 0.9),
            Sx.G(Sx.Amp(Sx.LP(Sx.Brown(1.2, Sx.AHR(0.05, 0.95, 0.2), s + 1), 900), (Curve)(t => 0.75 + 0.25 * Math.Sin(2 * Math.PI * 11 * t))), 1.0),
            Sx.G(Sx.HP(Sx.Crackle(1.2, Sx.K(150), Sx.AHR(0.05, 0.95, 0.2), s + 2), 2500), 0.3)));
        Loop(l, "Armadilhas", "sfx_serra_loop", s => Sx.Loopify(Sx.Mix(
            Sx.G(Sx.Amp(Sx.BP(Sx.Osc(Sx.W.Saw, 1.3, Sx.K(880), Sx.K(1)), 1800, 1.2), (Curve)(t => 0.7 + 0.3 * Math.Sin(2 * Math.PI * 30 * t))), 0.7),
            Sx.G(Sx.Osc(Sx.W.Sine, 1.3, Sx.K(1760), Sx.K(1)), 0.12),
            Sx.G(Sx.HP(Sx.Noise(1.3, Sx.K(1), s), 4000), 0.12)), 0.3));
        Add(l, "Armadilhas", "sfx_plataforma_treme", 2, s => Sx.Mix(
            Sx.G(Sx.HP(Sx.Crackle(0.5, Sx.K(250), Sx.K(0.6), s), 1200), 0.6),
            Sx.G(Rocks(0.5, 8, s + 1), 0.6),
            Sx.G(Sx.Click(1500, s), 0.6)));
        Add(l, "Armadilhas", "sfx_plataforma_cai", 1, s => Sx.Mix(
            Sx.G(Rocks(0.6, 16, s), 0.8), Sx.G(Sx.LP(Sx.Brown(0.7, Sx.Perc(0.01, 0.2), s + 1), 500), 0.8)));
        Add(l, "Armadilhas", "sfx_plataforma_volta", 1, s => Sx.LP(Sx.Thump(150, 90, 0.1, 0.03), 900));

        // ======================================================== INTERFACE
        Add(l, "UI", "ui_cursor", 2, s => Sx.Mix(
            Sx.G(Sx.BP(Sx.Noise(0.02, Sx.Perc(0.001, 0.005), s), 2200, 1.5), 0.8),
            Sx.G(Sx.Osc(Sx.W.Sine, 0.04, Sx.K(1150 * J(s, 0.03)), Sx.Perc(0.001, 0.012)), 0.4)));
        Add(l, "UI", "ui_confirma", 1, s => Harp(new[] { "D5", "A5" }, 0.07, 0.6, s));
        Add(l, "UI", "ui_volta", 1, s => Sx.G(Harp(new[] { "A4", "D4" }, 0.07, 0.5, s), 0.8));
        Add(l, "UI", "ui_aviso", 1, s => Sx.Mix(
            Sx.LP(Sx.Osc(Sx.W.Tri, 0.6, Sx.K(Sx.Hz("D3")), Sx.AHR(0.03, 0.35, 0.2)), 1000), Sx.G(Paper(0.2, s), 0.5)));
        Add(l, "UI", "ui_apaga", 1, s => Sx.Mix(
            Sx.G(Sx.HP(Sx.Crackle(0.3, Sx.K(1500), Sx.AHR(0.01, 0.2, 0.08), s), 1500), 0.6),
            Sx.At(0.2, Harp(new[] { "D4", "F4", "A4" }, 0.04, 0.9, s))));
        Add(l, "UI", "ui_negado", 1, s => Sx.LP(Sx.Thump(200, 150, 0.05, 0.015), 800));
        Add(l, "UI", "ui_centelha", 2, s => Room(Sx.G(Sx.Bell(Sx.Hz(s % 2 == 0 ? "D6" : "A6"), 1.5, 3.0, 0.8, 0.45), 0.6), 0.4));
        Add(l, "UI", "ui_inv_abre", 1, s => Sx.Mix(
            Sx.G(Creak(0.12, 400, s), 0.5), Sx.At(0.05, Paper(0.18, s + 1))));
        Add(l, "UI", "ui_inv_fecha", 1, s => Sx.Mix(Paper(0.12, s), Sx.At(0.08, Sx.G(Sx.LP(Sx.Thump(180, 120, 0.05, 0.015), 900), 0.6))));
        Add(l, "UI", "ui_aba", 2, s => Paper(0.09, s));
        Add(l, "UI", "ui_cursor_inv", 2, s => Sx.Mix(
            Sx.G(Sx.Metal(2600 * J(s, 0.04), 0.05, 0.008, s), 0.4), Sx.G(Sx.Click(3000, s), 0.3)));
        Add(l, "UI", "ui_dialogo_abre", 1, s => Sx.G(Paper(0.16, s), 0.8));
        Add(l, "UI", "ui_dialogo_fecha", 1, s => Sx.G(Sx.Reverse(Paper(0.13, s)), 0.7));
        Add(l, "UI", "ui_avanca", 2, s => Sx.G(Sx.BP(Sx.Noise(0.025, Sx.Perc(0.001, 0.006), s), 3000, 1.5), 0.6));
        Add(l, "UI", "ui_sistema", 1, s => Room(Sx.Mix(
            Harp(new[] { "D5", "F#5", "A5", "D6" }, 0.09, 1.0, s),
            Sx.At(0.27, Sx.G(Sx.Bell(Sx.Hz("D6"), 1.0, 2, 1, 0.35), 0.4))), 0.3));
        Add(l, "UI", "ui_dica", 1, s => Sx.Mix(Sx.G(Paper(0.15, s), 0.6), Sx.At(0.08, Sx.G(Sx.Bell(Sx.Hz("A5"), 0.6, 3, 0.7, 0.2), 0.4))));
        Add(l, "UI", "ui_barra_chefe", 1, s => Hall(Sx.Mix(
            Sx.Drive(Sx.Thump(95, 55, 0.5, 0.18), 1.4), Sx.G(Sx.LP(Sx.Noise(0.2, Sx.Perc(0.002, 0.05), s), 1500), 0.4),
            Sx.G(Sx.Metal(300, 0.6, 0.15, s), 0.3)), 0.25));
        Add(l, "UI", "ui_orbe_gasto", 1, s => Sx.Pitch(Sx.Bell(Sx.Hz("A5"), 0.4, 2.76, 1, 0.1), 0.85));
        Add(l, "UI", "ui_orbe_recarga", 1, s => Chord(new[] { "D5", "F#5", "A5" }, 0.07, 0.6, 2, 0.8, 0.2));

        return l;
    }

    static void AddVoice(List<SfxEntry> l, string name, Sx.Voice v, params string[] phrases)
    {
        bool koda = v == Koda;
        bool capivara = v == Capivara;
        l.Add(new SfxEntry
        {
            Folder = "Voz", Name = name, Count = phrases.Length, Db = -3, Loudness = false,
            Make = s =>
            {
                var y = Say(v, phrases[s % phrases.Length], s);
                if (capivara) y = Plague(y, s);
                return koda ? Room(y, 0.18) : Sx.Reverb(y, 0.08, 0.5, 0.25);
            }
        });
    }
}
