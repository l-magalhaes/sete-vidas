using System;
using System.Collections.Generic;

// Os sons da Floresta Silenciosa (F3 a F9; Docs/Audio/floresta-audio.md), pedido do usuário em 19/11/2026: "todo o som
// da Floresta". Mesmo padrão do SfxBank (C# 5, roda pelo Add-Type do gerar_sfx.ps1). Pastas novas em
// Resources/Audio/SFX: Floresta (mundo e gato), Bestiario (inimigos), Isyathy, Vulpis, Ponte, Tobi, Ambiencia.
// Regras do plano: o telégrafo tem a duração do aviso visual; infectados usam o timbre da praga (como o zumbi);
// quanto mais perto do Coração, menos som (o silêncio é o pilar da fase).
public static partial class SfxBank
{
    // ------------------------------------------------------------ vozes da floresta [PROPOSTA]

    /// <summary>A Tobi: toupeira engenheira, aguda e rápida, um pouco anasalada (a Vulpis usa a gravação Audio-vulpis.wav).</summary>
    public static readonly Sx.Voice Tobi = new Sx.Voice { F0 = 300, Formant = 1.3, Rate = 0.68, Breath = 0.1, Jitter = 0.02, Melody = 0.16, Decline = 0.04, Bright = 1.2 };
    /// <summary>O espírito da Isyathy: grave, devagar, sussurrado (quase só sopro), com eco suave.</summary>
    public static readonly Sx.Voice Spirit = new Sx.Voice { F0 = 96, Formant = 0.86, Rate = 1.6, Breath = 0.75, Jitter = 0.006, Melody = 0.05, Decline = 0.1 };

    /// <summary>Variante infectada de um som de inimigo: mais grave, rasgada, com o timbre da praga (como o zumbi).</summary>
    static float[] Inf(float[] x, int seed)
    {
        var low = Sx.Drive(Sx.Pitch(x, 0.84), 1.6);
        var hiss = Sx.G(Sx.BP(Sx.Crackle(Math.Max(0.1, x.Length / (double)Sx.SR), Sx.K(900), Sx.Perc(0.01, 0.15), seed + 91), 3200, 1.2), 0.25);
        return Plague(Sx.Mix(low, hiss), seed);
    }

    /// <summary>Um som de inimigo e a variante infectada (id + "_inf").</summary>
    static void Enemy(List<SfxEntry> l, string name, int count, Func<int, float[]> make)
    {
        Add(l, "Bestiario", name, count, make);
        Add(l, "Bestiario", name + "_inf", count, s => Inf(make(s), s));
    }

    /// <summary>Terra: baque grave e torrões caindo.</summary>
    static float[] Dirt(double dur, int seed)
    {
        return Sx.Mix(
            Sx.LP(Sx.Brown(dur, Sx.Perc(0.01, dur * 0.3), seed), 700),
            Sx.G(Rocks(dur * 0.8, 10, seed + 1), 0.45),
            Sx.G(Sx.LP(Sx.Crackle(dur, Sx.Lin(600, 80, dur), Sx.Perc(0.005, dur * 0.25), seed + 2), 2500), 0.4));
    }

    /// <summary>Madeira: estalo seco (transiente) + corpo oco.</summary>
    static float[] WoodKnock(double f, int seed)
    {
        return Sx.Mix(Sx.Click(2500, seed), Sx.LP(Sx.Thump(f, f * 0.55, 0.12, 0.035), 1800),
            Sx.G(Sx.BP(Sx.Noise(0.08, Sx.Perc(0.001, 0.02), seed + 1), f * 3.2, 3), 0.6));
    }

    /// <summary>Zumbido de inseto (asa): pulso com vibrato rápido.</summary>
    static float[] Buzz(double dur, double hz, double vib, Curve amp, int seed)
    {
        Curve f = delegate(double t) { return hz * (1 + 0.04 * Math.Sin(t * vib * 6.283) + 0.02 * Math.Sin(t * 3.1 + seed)); };
        return Sx.BP(Sx.Osc(Sx.W.Pulse25, dur, f, amp), hz * 2.2, 0.9);
    }

    /// <summary>Sibilo de serpente: ruído agudo em banda.</summary>
    static float[] Hiss(double dur, Curve amp, int seed) { return Sx.BP(Sx.HP(Sx.Noise(dur, amp, seed), 2500), Sx.Lin(5200, 6800, dur), 1.1); }

    /// <summary>A respiração colossal: inspiração e expiração graves (60–120 Hz), com o chiado do ar.</summary>
    static float[] Breath(double cycle, double gain, int seed)
    {
        double half = cycle * 0.5;
        var inhale = Sx.Mix(Sx.LP(Sx.Brown(half, Sx.Swell(half, 1.6), seed), 160), Sx.G(Sx.BP(Sx.Noise(half, Sx.Swell(half, 2), seed + 1), 700, 0.7), 0.18));
        var exhale = Sx.Mix(Sx.LP(Sx.Brown(half, Sx.Perc(0.08, half * 0.45), seed + 2), 120), Sx.G(Sx.BP(Sx.Noise(half, Sx.Perc(0.05, half * 0.35), seed + 3), 500, 0.6), 0.2));
        return Sx.G(Sx.Mix(inhale, Sx.At(half, exhale)), gain);
    }

    /// <summary>Uma ambiência em loop: as camadas somadas e o rabo emendado no começo.</summary>
    static void Amb(List<SfxEntry> l, string name, Func<int, float[]> make)
    {
        l.Add(new SfxEntry { Folder = "Ambiencia", Name = name, Count = 1, Loop = true, Db = -3, Make = s => Sx.Loopify(Sx.Crush(make(s), 12, 32000), 1.5) });
    }

    /// <summary>Eventos espalhados no tempo (pássaro, gota, estalo) a partir de uma receita.</summary>
    static float[] Scatter(double dur, int count, int seed, Func<int, float[]> one)
    {
        var r = new Random(seed);
        var parts = new List<float[]>();
        for (int i = 0; i < count; i++) parts.Add(Sx.At(r.NextDouble() * (dur - 1.5), one(seed * 31 + i)));
        parts.Add(new float[Sx.N(dur)]);
        return Sx.Mix(parts.ToArray());
    }

    static void Forest(List<SfxEntry> l)
    {
        // ======================================================== MUNDO DA FLORESTA (F1–F5)
        // tronco rachado: golpe grave e seco (×4), a quebra, e o "tunk" da adaga (não passa)
        Add(l, "Floresta", "sfx_tronco_golpe", 4, s => Room(Sx.Mix(WoodKnock(150 * J(s, 0.08), s), Sx.G(Sx.Crackle(0.2, Sx.K(400), Sx.Perc(0.002, 0.05), s + 5), 0.4)), 0.12));
        Add(l, "Floresta", "sfx_tronco_quebra", 2, s => Room(Sx.Mix(
            Sx.Drive(Sx.Thump(110, 45, 0.4, 0.12), 1.4),
            Sx.G(Sx.BP(Sx.Crackle(0.7, Sx.Lin(2500, 300, 0.7), Sx.Perc(0.003, 0.25), s), 1400, 0.8), 0.9),
            Sx.At(0.12, Sx.G(Rocks(0.6, 12, s + 2), 0.5))), 0.2));
        Add(l, "Floresta", "sfx_tronco_adaga", 2, s => Sx.Mix(Sx.LP(Sx.Thump(230 * J(s, 0.05), 160, 0.06, 0.02), 1500), Sx.G(Sx.Click(3500, s), 0.5)));
        // baú e madeira de reconstrução (jingle curto de madeira)
        Add(l, "Floresta", "sfx_bau_abre", 1, s => Room(Sx.Mix(Creak(0.45, 320, s), Sx.At(0.38, WoodKnock(180, s + 1))), 0.15));
        Add(l, "Floresta", "sfx_madeira_pega", 1, s => Room(Sx.Mix(WoodKnock(260, s), Sx.At(0.06, Sx.G(Chord(new[] { "D5", "A5", "D6" }, 0.07, 0.7, 2, 0.7, 0.25), 0.6))), 0.25));
        // galhos: o que treme e o que quebra; raiz que cresce de volta
        Add(l, "Floresta", "sfx_galho_treme", 2, s => Sx.G(Creak(0.4, 520 * J(s, 0.1), s), 0.8));
        Add(l, "Floresta", "sfx_galho_quebra", 2, s => Sx.Mix(Sx.BP(Sx.Crackle(0.25, Sx.Lin(2500, 500, 0.25), Sx.Perc(0.001, 0.07), s), 1900, 1), Sx.G(WoodKnock(200, s + 1), 0.7)));
        Add(l, "Floresta", "sfx_raiz_refaz", 1, s => Sx.Mix(Sx.G(Creak(0.7, 260, s), 0.6), Sx.G(Sx.LP(Sx.Brown(0.7, Sx.AHR(0.2, 0.3, 0.2), s + 1), 400), 0.6)));
        // atalhos: cipó que se solta, corda descendo, alavanca e porta de metal dos Primatas
        Add(l, "Floresta", "sfx_cipo_solta", 1, s => Sx.Mix(Sx.BP(Sx.Crackle(0.3, Sx.K(1200), Sx.Perc(0.002, 0.08), s), 2600, 1.2), Sx.At(0.1, Sx.Whoosh(0.5, 400, 1600, 1, s + 1)), Sx.At(0.55, Sx.G(WoodKnock(130, s + 2), 0.7))));
        Add(l, "Floresta", "sfx_corda_desce", 2, s => Sx.Mix(Sx.BP(Sx.Noise(0.5, Sx.AHR(0.05, 0.3, 0.15), s), Sx.Lin(1800, 1200, 0.5), 2), Sx.G(Creak(0.5, 700, s + 1), 0.4)));
        Add(l, "Floresta", "sfx_alavanca_primatas", 1, s => Room(Sx.Mix(Sx.Metal(520, 0.5, 0.12, s), Sx.At(0.18, Sx.G(Sx.Click(1500, s), 0.8)), Sx.At(0.2, Sx.G(Sx.Osc(Sx.W.Square, 0.3, Sx.K(880), Sx.Perc(0.002, 0.05)), 0.12))), 0.3));
        Add(l, "Floresta", "sfx_porta_metal", 1, s => Hall(Sx.Mix(Sx.Metal(160, 1.2, 0.4, s), Sx.G(Sx.LP(Sx.Brown(1.0, Sx.AHR(0.05, 0.6, 0.3), s + 1), 300), 0.7)), 0.3));
        // colmeia do Broto soltando a vespa; a leva da arena (aviso e chegada); mel no bolso
        Add(l, "Floresta", "sfx_colmeia_solta", 2, s => Sx.Mix(Sx.G(Sx.BP(Sx.Crackle(0.2, Sx.K(1500), Sx.Perc(0.002, 0.05), s), 2200, 1), 0.6), Buzz(0.35, 190 * J(s, 0.08), 30, Sx.AHR(0.03, 0.15, 0.15), s)));
        Add(l, "Floresta", "sfx_arena_leva_aviso", 1, s => Hall(Sx.Mix(Sx.G(Sx.LP(Sx.Brown(0.9, Sx.Swell(0.8, 1.5), s), 220), 1), Sx.G(Sx.BP(Sx.Crackle(0.9, Sx.Lin(200, 1500, 0.9), Sx.Swell(0.9, 2), s + 1), 1100, 1), 0.5)), 0.3));
        Add(l, "Floresta", "sfx_arena_leva_chega", 3, s => Sx.Mix(Sx.LP(Sx.Thump(130 * J(s, 0.1), 60, 0.18, 0.05), 900), Sx.G(Sx.LP(Sx.Noise(0.2, Sx.Perc(0.002, 0.06), s), 1600), 0.6)));
        Add(l, "Floresta", "sfx_mel_pega", 1, s => Sx.Mix(Sx.G(Sx.Droplets(0.25, 3, 600, 1100, s), 0.7), Sx.At(0.05, Sx.G(Sx.Bell(Sx.Hz("E6"), 0.5, 2, 0.6, 0.15), 0.4))));
        // o espírito da Isyathy pela floresta: aparece (sino grave e coro de sopro) e fala
        Add(l, "Floresta", "sfx_espirito_surge", 1, s => Hall(Sx.Mix(
            Sx.G(Pad(new[] { "D3", "A3", "E4", "F#4" }, 2.4, 0.9, 1400, s), 0.5),
            Sx.G(Sx.Bell(Sx.Hz("A5"), 2.2, 3.01, 0.6, 0.8), 0.25)), 0.6));
        Add(l, "Floresta", "sfx_espirito_fala", 1, s => Hall(Sx.G(Harp(new[] { "D4", "A4", "E5" }, 0.12, 1.4, s), 0.5), 0.5));

        // ======================================================== GATO NA FLORESTA
        Add(l, "Floresta", "sfx_pogo_espinho", 3, s => Room(Sx.Mix(Sx.Bell(Sx.Hz("E6") * J(s, 0.02), 0.5, 2.0, 1.2, 0.12), Sx.G(Sx.Click(4000, s), 0.4)), 0.2));   // cristalino, turquesa
        Add(l, "Floresta", "sfx_pogo_cacto", 2, s => Sx.Mix(Sx.LP(Sx.Osc(Sx.W.Sine, 0.25, Sx.Geo(180, 420, 0.12), Sx.Perc(0.003, 0.08)), 1500), Sx.G(Sx.LP(Sx.Thump(160, 90, 0.1, 0.03), 800), 0.6)));   // "boing" abafado
        Add(l, "Floresta", "sfx_pogo_carne", 3, s => Sx.Mix(Sx.LP(Sx.Thump(140 * J(s, 0.1), 70, 0.12, 0.04), 1000), Sx.G(Sx.BP(Sx.Noise(0.1, Sx.Perc(0.002, 0.03), s), 700, 1.2), 0.7)));
        Add(l, "Floresta", "sfx_gato_queda_galhos", 1, s => Sx.Mix(
            Scatter(2.2, 9, s, k => Sx.G(Sx.Mix(Sx.BP(Sx.Crackle(0.2, Sx.K(1500), Sx.Perc(0.001, 0.05), k), 2000, 1), Sx.G(WoodKnock(220, k + 1), 0.6)), 0.8)),
            Sx.G(Sx.Whoosh(2.2, 300, 1200, 0.8, s + 4), 0.6)));

        // ======================================================== BESTIÁRIO (F4): normal e infectado
        // Vespa da Raiz: acorda (zumbido subindo), voa (loop), investida (telégrafo 0,3 s + rasante), dano, morte
        Enemy(l, "sfx_vespa_acorda", 2, s => Buzz(0.6, 170 * J(s, 0.06), 28, Sx.Swell(0.55, 1.2), s));
        l.Add(new SfxEntry { Folder = "Bestiario", Name = "sfx_vespa_voo_loop", Count = 1, Loop = true, Db = -3, Make = s => Sx.Loopify(Buzz(2.5, 210, 32, Sx.K(1), s), 0.3) });
        Enemy(l, "sfx_vespa_investida", 2, s => Sx.Mix(Buzz(0.45, 260 * J(s, 0.05), 40, Sx.AHR(0.02, 0.3, 0.1), s), Sx.At(0.25, Sx.G(Sx.Whoosh(0.2, 1500, 4000, 2, s + 1), 0.6))));
        Enemy(l, "sfx_vespa_dano", 2, s => Sx.Mix(Sx.G(Sx.Click(3000, s), 0.6), Buzz(0.15, 320 * J(s, 0.05), 50, Sx.Perc(0.002, 0.05), s)));
        Enemy(l, "sfx_vespa_morte", 2, s => Sx.Mix(Buzz(0.5, 260, 20, Sx.Perc(0.002, 0.15), s), Sx.G(Sx.Pitch(Buzz(0.5, 260, 20, Sx.Perc(0.002, 0.2), s + 1), 0.6), 0.5), Sx.At(0.35, Sx.G(Sx.Click(2000, s), 0.5))));
        // Galho Seco: o estalo de madeira (acordar e o braço erguendo: telégrafo 0,6 s), a pancada, dano e a queda
        Enemy(l, "sfx_galho_estalo", 3, s => Room(Sx.Mix(Creak(0.5, 300 * J(s, 0.1), s), Sx.At(0.42, WoodKnock(170, s + 1))), 0.12));
        Enemy(l, "sfx_galho_pancada", 2, s => Room(Sx.Mix(Sx.Drive(Sx.Thump(95, 45, 0.3, 0.09), 1.4), Sx.G(Rocks(0.35, 8, s), 0.5), Sx.G(WoodKnock(140, s + 2), 0.6)), 0.15));
        Enemy(l, "sfx_galho_dano", 3, s => Sx.Mix(WoodKnock(190 * J(s, 0.08), s), Sx.G(Sx.BP(Sx.Crackle(0.15, Sx.K(1800), Sx.Perc(0.002, 0.05), s + 3), 2200, 1), 0.5)));
        Enemy(l, "sfx_galho_tomba", 1, s => Room(Sx.Mix(Sx.G(Creak(0.9, 220, s), 0.8), Sx.At(0.85, Sx.Drive(Sx.Thump(90, 40, 0.5, 0.15), 1.5)), Sx.At(0.88, Sx.G(Rocks(0.6, 14, s + 1), 0.6))), 0.2));
        // Planta Carnívora: o aviso (folhas abrindo, chiado úmido, 0,45 s), a mordida, dano, morte, esporos
        Enemy(l, "sfx_planta_aviso", 2, s => Sx.Mix(Sx.BP(Sx.Crackle(0.45, Sx.K(700), Sx.Swell(0.42, 1.5), s), 1500, 0.8), Sx.G(Sx.BP(Sx.Noise(0.45, Sx.Swell(0.42, 2), s + 1), 900, 1.5), 0.5)));
        Enemy(l, "sfx_planta_mordida", 3, s => Sx.Mix(Sx.LP(Sx.Thump(220 * J(s, 0.08), 110, 0.08, 0.025), 1600), Sx.G(Sx.BP(Sx.Noise(0.15, Sx.Perc(0.001, 0.04), s), 1200, 1.2), 0.8), Sx.At(0.04, Sx.G(Sx.Droplets(0.2, 4, 500, 1200, s + 2), 0.4))));
        Enemy(l, "sfx_planta_dano", 2, s => Sx.Mix(Sx.G(Sx.BP(Sx.Noise(0.12, Sx.Perc(0.001, 0.04), s), 800, 1), 0.8), Sx.LP(Sx.Thump(180, 100, 0.08, 0.025), 1200)));
        Enemy(l, "sfx_planta_morte", 1, s => Sx.Mix(Sx.G(Sx.BP(Sx.Crackle(0.6, Sx.Lin(900, 150, 0.6), Sx.Perc(0.005, 0.25), s), 900, 0.8), 0.8), Sx.G(Sx.LP(Sx.Brown(0.6, Sx.Perc(0.01, 0.2), s + 1), 400), 0.7)));
        Add(l, "Bestiario", "sfx_planta_esporos", 2, s => Sx.Mix(Sx.G(Sx.HP(Sx.Noise(0.6, Sx.AHR(0.02, 0.1, 0.45), s), 3000), 0.5), Sx.G(Sx.BP(Sx.Noise(0.25, Sx.Perc(0.002, 0.06), s + 1), 600, 1), 0.6)));
        // Serpente da Floresta: sibilo (o aviso), o bote, dano e morte
        Enemy(l, "sfx_serpente_aviso", 2, s => Hiss(0.5, Sx.AHR(0.04, 0.3, 0.15), s));
        Enemy(l, "sfx_serpente_bote", 2, s => Sx.Mix(Sx.Whoosh(0.18, 600, 2500, 1.5, s), Sx.At(0.12, Sx.LP(Sx.Thump(240, 120, 0.06, 0.02), 1500)), Sx.G(Hiss(0.15, Sx.Perc(0.002, 0.05), s + 1), 0.5)));
        Enemy(l, "sfx_serpente_dano", 2, s => Sx.Mix(Sx.G(Hiss(0.2, Sx.Perc(0.002, 0.07), s), 0.8), Sx.LP(Sx.Thump(200, 110, 0.07, 0.02), 1200)));
        Enemy(l, "sfx_serpente_morte", 1, s => Sx.Mix(Hiss(0.9, Sx.Perc(0.01, 0.35), s), Sx.At(0.2, Sx.G(Sx.LP(Sx.Thump(160, 70, 0.2, 0.06), 900), 0.7))));
        // a nuvem tóxica do Galho infectado (o timbre da praga: ela é a praga)
        Add(l, "Bestiario", "sfx_nuvem_toxica", 1, s => Plague(Sx.Mix(Sx.G(Sx.LP(Sx.Brown(1.2, Sx.AHR(0.05, 0.5, 0.6), s), 900), 0.8), Sx.G(Sx.BP(Sx.Crackle(1.2, Sx.K(300), Sx.AHR(0.05, 0.5, 0.6), s + 1), 1800, 1), 0.4)), s));

        // ======================================================== ISYATHY (F7 e F8)
        // a respiração em 4 níveis (variações 01–04: 4 s, 2,5 s, 1,5 s + escamas, 1,2 s subindo), toca com PlayVariant
        l.Add(new SfxEntry { Folder = "Isyathy", Name = "sfx_isyathy_respira", Count = 4, Db = -1.5, Make = s =>
        {
            double[] cycles = { 4.0, 2.5, 1.5, 1.2 };
            var b = Breath(cycles[s % 4], 1, s);
            if (s % 4 >= 2) b = Sx.Mix(b, Sx.G(Sx.BP(Sx.Crackle(cycles[s % 4], Sx.K(600), Sx.K(0.5), s + 7), 2600, 1.5), 0.25));   // escamas raspando
            if (s % 4 == 3) b = Sx.Mix(b, Sx.G(Sx.LP(Sx.Osc(Sx.W.Saw, 1.2, Sx.Geo(45, 80, 1.2), Sx.Swell(1.15, 1.4)), 300), 0.6));   // o rugido grave subindo
            return Hall(b, 0.35);
        } });
        Add(l, "Isyathy", "sfx_isyathy_tremor", 2, s => Sx.Mix(Sx.LP(Sx.Brown(1.1, Sx.AHR(0.15, 0.7, 0.25), s), 150), Sx.G(Rocks(1.1, 14, s + 1), 0.4), Sx.G(Sx.BP(Sx.Crackle(1.1, Sx.Lin(80, 600, 1.1), Sx.Swell(1.05, 1.5), s + 2), 900, 0.8), 0.5)));   // o aviso do bote (1,1 s, o tempo da tela tremendo)
        Add(l, "Isyathy", "sfx_isyathy_emerge", 3, s => Room(Sx.Mix(Dirt(0.8, s), Sx.G(Sx.Whoosh(0.5, 200, 900, 0.8, s + 3), 0.5)), 0.2));
        Add(l, "Isyathy", "sfx_isyathy_mergulha", 2, s => Sx.Mix(Sx.G(Dirt(0.9, s), 0.8), Sx.G(Sx.Whoosh(0.6, 900, 200, 0.8, s + 3), 0.4)));
        Add(l, "Isyathy", "sfx_isyathy_bote", 3, s => Sx.Mix(Sx.Whoosh(0.3, 300, 1500, 1.2, s), Sx.G(Sx.LP(Sx.Brown(0.3, Sx.Perc(0.01, 0.1), s + 1), 300), 0.7)));
        Add(l, "Isyathy", "sfx_isyathy_mordida", 3, s => Room(Sx.Mix(Sx.Drive(Sx.Thump(170 * J(s, 0.06), 70, 0.2, 0.05), 1.6), Sx.G(Sx.Click(1800, s), 0.8), Sx.G(Sx.BP(Sx.Noise(0.2, Sx.Perc(0.001, 0.05), s + 1), 900, 1), 0.6)), 0.15));
        Add(l, "Isyathy", "sfx_isyathy_impacto", 2, s => Room(Sx.Mix(Sx.Drive(Sx.Thump(80, 35, 0.6, 0.18), 1.6), Sx.G(Dirt(1.0, s), 0.8)), 0.25));
        Add(l, "Isyathy", "sfx_isyathy_rugido", 2, s => Hall(Sx.Mix(
            Sx.G(Say(new Sx.Voice { F0 = 58, Formant = 0.62, Rate = 1.6, Breath = 0.5, Growl = 0.9, Melody = 0.04, Decline = 0.2, Muffle = 1800 }, new[] { "hrraaaaahhh", "ghaaaaarrhh" }[s % 2], s), 1.3),
            Sx.G(Sx.LP(Sx.Brown(1.6, Sx.AHR(0.1, 0.9, 0.6), s + 1), 180), 0.8),
            Sx.G(Hiss(1.4, Sx.AHR(0.2, 0.6, 0.6), s + 2), 0.25)), 0.4));
        // fase II em diante: o rugido com o timbre da praga (o plano: a Isyathy usa o timbre a partir da fase II)
        Add(l, "Isyathy", "sfx_isyathy_rugido_praga", 2, s => Plague(Hall(Sx.G(Say(new Sx.Voice { F0 = 54, Formant = 0.6, Rate = 1.6, Breath = 0.5, Growl = 1.0, Melody = 0.06, Decline = 0.25, Muffle = 1700 }, new[] { "hraaaaagghh", "ghrraaaahh" }[s % 2], s), 1.3), 0.4), s));
        Add(l, "Isyathy", "sfx_isyathy_cuspe", 2, s => Sx.Mix(Sx.G(Sx.BP(Sx.Noise(0.35, Sx.AHR(0.1, 0.05, 0.15), s), Sx.Lin(400, 1400, 0.3), 1), 0.8), Sx.At(0.25, Sx.G(Sx.Droplets(0.3, 5, 300, 800, s + 1), 0.7)), Sx.At(0.22, Sx.LP(Sx.Thump(160, 80, 0.1, 0.03), 900))));
        Add(l, "Isyathy", "sfx_isyathy_poca", 2, s => Plague(Sx.Mix(Sx.G(Sx.Droplets(0.6, 8, 250, 700, s), 0.8), Sx.G(Sx.LP(Sx.Noise(0.5, Sx.Perc(0.01, 0.2), s + 1), 700), 0.5)), s));
        Add(l, "Isyathy", "sfx_isyathy_dano", 3, s => Sx.Mix(Sx.LP(Sx.Thump(140 * J(s, 0.08), 70, 0.15, 0.04), 1000), Sx.G(Say(new Sx.Voice { F0 = 70, Formant = 0.66, Rate = 1.1, Breath = 0.6, Growl = 0.8, Muffle = 1500 }, "hhrrgh", s), 0.7)));
        Add(l, "Isyathy", "sfx_isyathy_resvala", 2, s => Sx.Mix(Sx.Metal(900 * J(s, 0.08), 0.2, 0.05, s), Sx.G(Sx.BP(Sx.Noise(0.1, Sx.Perc(0.001, 0.03), s), 3000, 1), 0.5)));   // escamas / a casca
        Add(l, "Isyathy", "sfx_isyathy_casca", 1, s => Plague(Hall(Sx.Mix(Sx.G(Pad(new[] { "C3", "F#3", "B3", "F4" }, 1.6, 0.6, 1600, s), 0.6), Sx.At(1.45, Sx.Mix(Sx.Drive(Sx.Thump(120, 50, 0.4, 0.12), 1.5), Sx.G(Sx.Metal(300, 0.6, 0.2, s), 0.5)))), 0.4), s));
        Add(l, "Isyathy", "sfx_isyathy_casca_quebra", 1, s => Hall(Sx.Mix(Sx.G(Sx.Jingle(0.8, 90, 2800, s), 0.7), Sx.Drive(Sx.Thump(150, 60, 0.4, 0.1), 1.3), Sx.At(0.05, Sx.G(Chord(new[] { "D5", "A5", "E6" }, 0.03, 1.2, 2, 0.8, 0.4), 0.4))), 0.35));
        Add(l, "Isyathy", "sfx_isyathy_corrompe", 1, s => Plague(Sx.Mix(Sx.G(Sx.LP(Sx.Brown(1.2, Sx.Swell(1.1, 1.5), s), 400), 0.8), Sx.G(Sx.BP(Sx.Crackle(1.2, Sx.Lin(100, 900, 1.2), Sx.Swell(1.1, 1.2), s + 1), 1400, 1), 0.5)), s));
        l.Add(new SfxEntry { Folder = "Isyathy", Name = "sfx_isyathy_arrasto_loop", Count = 1, Loop = true, Db = -3, Make = s => Sx.Loopify(Sx.Mix(Sx.LP(Sx.Brown(3.0, Sx.K(0.8), s), 200), Sx.G(Sx.BP(Sx.Crackle(3.0, Sx.K(120), Sx.K(0.6), s + 1), 700, 0.8), 0.5)), 0.4) });
        Add(l, "Isyathy", "sfx_isyathy_derrota", 1, s => Hall(Sx.Mix(
            Sx.G(Say(new Sx.Voice { F0 = 60, Formant = 0.62, Rate = 2.2, Breath = 0.7, Growl = 0.6, Melody = 0.03, Decline = 0.5, Muffle = 1500 }, "hhaaaaaaahhh...", s), 1.2),
            Sx.At(1.2, Sx.G(Sx.LP(Sx.Brown(2.0, Sx.Perc(0.05, 0.8), s + 1), 200), 0.7))), 0.5));
        // a voz do espírito (sem palavras): sussurro grave com eco suave
        l.Add(new SfxEntry { Folder = "Voz", Name = "vox_isyathy_neutro", Count = 3, Db = -3, Loudness = false, Make = s =>
            Sx.Echo(Sx.Reverb(Sx.LP(Say(Spirit, new[] { "hhaa-so... meh-raa.", "shhaa... o-rra-na.", "hhmm... sa-ve-rah." }[s % 3], s), 2400), 0.35, 1.2, 1.2), 0.32, 0.35, 0.3, 1.0) });

        // ======================================================== VULPIS (F6 e F8)
        Add(l, "Vulpis", "sfx_vulpis_arco_tensiona", 2, s => Sx.Mix(Creak(0.5, 600 * J(s, 0.05), s), Sx.G(Sx.Osc(Sx.W.Tri, 0.5, Sx.Geo(300, 600, 0.5), Sx.Swell(0.48, 1)), 0.15)));   // 0,5 s: o aviso
        Add(l, "Vulpis", "sfx_vulpis_flecha_voa", 2, s => Sx.Mix(Sx.G(Sx.Click(3000, s), 0.6), Sx.Whoosh(0.3, 2500, 6000, 3, s + 1)));
        Add(l, "Vulpis", "sfx_vulpis_flecha_crava", 2, s => Sx.Mix(WoodKnock(320 * J(s, 0.05), s), Sx.At(0.02, Sx.G(Sx.Osc(Sx.W.Sine, 0.25, Sx.K(180), Sx.Perc(0.002, 0.08)), 0.4))));
        // a flecha forte de magia verde: junta (sino subindo) e estoura (MUS-F10: o motivo de 3 notas da flauta dela)
        Add(l, "Vulpis", "sfx_vulpis_magia", 1, s => Hall(Sx.Mix(Sx.G(Sx.Osc(Sx.W.Sine, 1.2, Sx.Geo(600, 1500, 1.2), Sx.Swell(1.15, 1.3)), 0.3), Sx.G(Sx.Jingle(1.2, 30, 3500, s), 0.3)), 0.4));
        Add(l, "Vulpis", "sfx_vulpis_motivo", 1, s => Room(Sx.Mix(
            Sx.At(0.0, Sx.LP(Sx.Osc(Sx.W.Tri, 0.25, Sx.K(Sx.Hz("A5")), Sx.AHR(0.01, 0.16, 0.08)), 4000)),
            Sx.At(0.18, Sx.LP(Sx.Osc(Sx.W.Tri, 0.25, Sx.K(Sx.Hz("E6")), Sx.AHR(0.01, 0.16, 0.08)), 4000)),
            Sx.At(0.36, Sx.LP(Sx.Osc(Sx.W.Tri, 0.7, Sx.K(Sx.Hz("D6")), Sx.AHR(0.01, 0.4, 0.3)), 4000))), 0.35));

        // ======================================================== PONTE (F3 e F9) e a horda
        Add(l, "Ponte", "sfx_ponte_pedra_racha", 2, s => Room(Sx.Mix(Sx.BP(Sx.Crackle(0.6, Sx.Lin(300, 2500, 0.6), Sx.AHR(0.05, 0.35, 0.2), s), 1800, 0.8), Sx.G(Sx.LP(Sx.Brown(0.6, Sx.AHR(0.1, 0.3, 0.2), s + 1), 250), 0.7)), 0.2));
        Add(l, "Ponte", "sfx_ponte_pedra_cai", 3, s => Room(Sx.Mix(Sx.G(Rocks(0.7, 10, s), 0.9), Sx.Drive(Sx.Thump(100 * J(s, 0.1), 45, 0.35, 0.1), 1.3)), 0.25));
        Add(l, "Ponte", "sfx_ponte_desaba", 1, s => Hall(Sx.Mix(Sx.G(Rocks(2.5, 60, s), 0.9), Sx.Drive(Sx.Thump(70, 30, 1.2, 0.4), 1.6), Sx.G(Thunder(2.5, 600, s + 3), 0.8)), 0.4));
        Add(l, "Ponte", "sfx_ponte_madeira_range", 3, s => Sx.Mix(Creak(0.9 * J(s, 0.15), 240 * J(s + 1, 0.1), s), Sx.G(Creak(0.6, 420, s + 2), 0.4)));
        Add(l, "Ponte", "sfx_horda_geme", 3, s => Plague(Hall(Sx.Mix(
            Sx.G(Say(new Sx.Voice { F0 = 88, Formant = 0.84, Rate = 1.3, Breath = 0.5, Growl = 0.7, Muffle = 1800, Melody = 0.1 }, new[] { "uuuhhh... aaahh", "hhooouuhh", "aaarrhh... uhh" }[s % 3], s), 0.8),
            Sx.At(0.3, Sx.G(Sx.Pitch(Say(new Sx.Voice { F0 = 76, Formant = 0.8, Rate = 1.4, Breath = 0.5, Growl = 0.8, Muffle = 1600 }, "ooouuhhh", s + 4), 0.95), 0.6))), 0.4), s));

        // ======================================================== TOBI (F9): cômicos e exagerados (+3 dB na cena)
        Add(l, "Tobi", "sfx_tobi_chave_gigante", 1, s => Room(Sx.Mix(Sx.Metal(220, 1.0, 0.35, s), Sx.G(Sx.Bell(Sx.Hz("G3"), 1.0, 2.76, 2, 0.3), 0.5), Sx.G(Sx.Click(2000, s), 0.8)), 0.25));   // "clank"
        Add(l, "Tobi", "sfx_tobi_cambaleia", 1, s => Sx.Mix(Scatter(1.2, 6, s, k => Sx.G(WoodKnock(250 + (k % 5) * 40, k), 0.6)), Sx.G(Sx.Osc(Sx.W.Sine, 1.2, delegate(double t) { return 500 + 120 * Math.Sin(t * 9); }, Sx.AHR(0.05, 0.9, 0.2)), 0.12)));
        Add(l, "Tobi", "sfx_tobi_cava", 2, s => Sx.Mix(Scatter(1.0, 10, s, k => Sx.G(Sx.LP(Sx.Noise(0.06, Sx.Perc(0.002, 0.02), k), 1200), 0.9)), Sx.G(Sx.LP(Sx.Brown(1.0, Sx.K(0.5), s + 1), 300), 0.5)));
        Add(l, "Tobi", "sfx_tobi_emerge", 1, s => Sx.Mix(Sx.Osc(Sx.W.Sine, 0.15, Sx.Geo(300, 900, 0.08), Sx.Perc(0.002, 0.05)), Sx.G(Dirt(0.4, s), 0.6)));   // "pop"
        Add(l, "Tobi", "sfx_tobi_martelo_rapido", 1, s => Scatter(1.2, 9, s, k => Sx.Mix(Sx.G(Sx.Metal(700 + (k % 3) * 120, 0.12, 0.03, k), 0.6), Sx.G(WoodKnock(220, k + 1), 0.6))));
        Add(l, "Tobi", "sfx_tobi_serrote", 1, s => Sx.BP(Sx.Noise(1.0, delegate(double t) { return 0.4 + 0.6 * Math.Abs(Math.Sin(t * 14)); }, s), Sx.K(2400), 2));
        Add(l, "Tobi", "sfx_tobi_broca_gira", 1, s => Sx.Mix(Sx.LP(Sx.Osc(Sx.W.Saw, 1.2, Sx.Geo(200, 900, 1.0), Sx.AHR(0.05, 0.9, 0.2)), 2500), Sx.G(Sx.BP(Sx.Noise(1.2, Sx.K(0.5), s), 3000, 2), 0.3)));
        Add(l, "Tobi", "sfx_tobi_plink", 4, s => Room(Sx.Bell(Sx.Hz(new[] { "C6", "E6", "G6", "C7" }[s % 4]), 0.4, 3.5, 0.8, 0.12), 0.2));
        Add(l, "Tobi", "sfx_tobi_bolso", 1, s => Sx.Mix(Sx.G(Paper(0.15, s), 0.6), Sx.At(0.12, Sx.G(Sx.Bell(Sx.Hz("A5"), 0.3, 2, 0.5, 0.08), 0.4))));
        Add(l, "Tobi", "sfx_tobi_madeira_voa", 3, s => Sx.Mix(Sx.Whoosh(0.25, 800, 2200, 1.4, s), Sx.At(0.2, Sx.G(WoodKnock(280 * J(s, 0.1), s + 1), 0.6))));
        // MUS-F12: o jingle da ponte reconstruída (madeira e sinos, 5 s, resolve em Ré como o tema principal)
        Add(l, "Tobi", "sfx_tobi_jingle", 1, s => Hall(Sx.Mix(
            Chip(new[] { "D5", "F#5", "A5", "D6" }, 0.16, Sx.W.Tri),
            Sx.At(0.0, Sx.G(Harp(new[] { "D4", "A4", "D5" }, 0.16, 1.2, s), 0.6)),
            Sx.At(0.7, Sx.G(Chord(new[] { "D5", "F#5", "A5", "D6" }, 0.02, 3.0, 2, 0.7, 0.9), 0.5)),
            Sx.At(0.7, Sx.G(WoodKnock(200, s + 4), 0.6))), 0.35));
        AddVoice(l, "vox_tobi_neutro", Tobi, "tik-ra, be-ka-tok!", "tri-ki, tra-ka.", "vum-ti, ke-ra!");
        AddVoice(l, "vox_tobi_animada", Tobi, "ti-ki! tra-ka-tok!", "be-ka! vum-vum!", "tri-ri-ki, tok!");
        AddVoice(l, "vox_tobi_ofendida", Tobi, "hmpf! tik-ra.", "tsk. be-ka-tok?!", "hm! tra-ka.");
        AddVoice(l, "vox_tobi_gananciosa", Tobi, "heh-heh. tik-ka-ching.", "hmm-hm... tra-ki.", "ke-ke! be-ka.");
        AddVoice(l, "vox_tobi_orgulhosa", Tobi, "ha! vum-ti-ra!", "tra-ka-tok! ha-ha!", "be-ka-ra, hm-hm!");

        // ======================================================== AMBIÊNCIAS (loops de 18 s, bitcrush leve)
        // floresta viva (A, B, D): vento nas folhas, grilos, coruja longe, gotas, rangidos
        Amb(l, "amb_floresta_viva", s => Sx.Mix(
            Sx.G(Sx.BP(Sx.Noise(18, delegate(double t) { return 0.5 + 0.3 * Math.Sin(t * 0.35) + 0.2 * Math.Sin(t * 0.9 + 1); }, s), 900, 0.5), 0.35),
            Sx.G(Scatter(18, 40, s + 1, k => Sx.BP(Sx.Osc(Sx.W.Square, 0.05, Sx.K(4200 + (k % 4) * 250), Sx.Perc(0.002, 0.015)), 4500, 3)), 0.12),
            Sx.G(Scatter(18, 2, s + 2, k => Hall(Sx.LP(Sx.Mix(Sx.Osc(Sx.W.Sine, 0.4, Sx.Geo(420, 380, 0.4), Sx.AHR(0.04, 0.2, 0.15)), Sx.At(0.55, Sx.Osc(Sx.W.Sine, 0.6, Sx.Geo(400, 360, 0.6), Sx.AHR(0.04, 0.35, 0.2)))), 1200), 0.5)), 0.25),
            Sx.G(Scatter(18, 6, s + 3, k => Sx.Droplets(0.4, 1, 900, 1800, k)), 0.18),
            Sx.G(Scatter(18, 3, s + 4, k => Creak(0.6, 260 + k % 5 * 30, k)), 0.12)));
        // ruínas dos Primatas: goteiras em metal, ressonância, zumbido elétrico raro
        Amb(l, "amb_floresta_ruinas", s => Sx.Mix(
            Sx.G(Sx.LP(Sx.Brown(18, Sx.K(0.6), s), 220), 0.5),
            Sx.G(Scatter(18, 10, s + 1, k => Room(Sx.Metal(1400 + (k % 5) * 260, 0.4, 0.12, k), 0.5)), 0.2),
            Sx.G(Scatter(18, 2, s + 2, k => Sx.BP(Sx.Osc(Sx.W.Saw, 1.5, Sx.K(120), Sx.AHR(0.2, 1.0, 0.3)), 240, 2)), 0.15),
            Sx.G(Sx.BP(Sx.Noise(18, Sx.K(0.4), s + 3), 600, 0.6), 0.15)));
        // corrompida (C7, partes de C e E): ar rarefeito, estalos orgânicos, bolhas, nenhum animal
        Amb(l, "amb_floresta_corrompida", s => Plague(Sx.Mix(
            Sx.G(Sx.LP(Sx.Noise(18, delegate(double t) { return 0.4 + 0.2 * Math.Sin(t * 0.22); }, s), 500), 0.4),
            Sx.G(Scatter(18, 14, s + 1, k => Sx.BP(Sx.Crackle(0.25, Sx.K(900), Sx.Perc(0.003, 0.06), k), 1500, 1.2)), 0.2),
            Sx.G(Scatter(18, 8, s + 2, k => Sx.Droplets(0.5, 3, 200, 500, k)), 0.25)), s));
        // o ninho: quase nada (vento muito baixo e eco); a respiração dela é a camada principal
        Amb(l, "amb_ninho", s => Sx.Mix(
            Sx.G(Sx.LP(Sx.Noise(18, delegate(double t) { return 0.3 + 0.15 * Math.Sin(t * 0.18); }, s), 350), 0.35),
            Sx.G(Scatter(18, 3, s + 1, k => Hall(Sx.G(Rocks(0.3, 3, k), 0.5), 0.6)), 0.15)));
        // a estrada de Soluki: vento aberto, tochas crepitando, gemidos distantes
        Amb(l, "amb_estrada", s => Sx.Mix(
            Sx.G(Sx.BP(Sx.Noise(18, delegate(double t) { return 0.5 + 0.35 * Math.Sin(t * 0.3) * Math.Sin(t * 0.07 + 2); }, s), 600, 0.4), 0.45),
            Sx.G(Sx.HP(Sx.Crackle(18, Sx.K(25), Sx.K(0.5), s + 1), 1500), 0.12),
            Sx.G(Scatter(18, 2, s + 2, k => Hall(Plague(Sx.G(Say(new Sx.Voice { F0 = 80, Formant = 0.8, Rate = 1.6, Breath = 0.6, Growl = 0.6, Muffle = 1200 }, "uuuhh", k), 0.5), k), 0.6)), 0.25)));
        // pontuais (AmbientSound): coruja, galho caindo longe, sapo do lago, a máquina dos Primatas
        Add(l, "Ambiencia", "amb_coruja", 2, s => Hall(Sx.LP(Sx.Mix(Sx.Osc(Sx.W.Sine, 0.35, Sx.Geo(440, 400, 0.35), Sx.AHR(0.03, 0.2, 0.1)), Sx.At(0.5, Sx.Osc(Sx.W.Sine, 0.7, Sx.Geo(420, 370, 0.7), Sx.AHR(0.03, 0.4, 0.25)))), 1300), 0.5));
        Add(l, "Ambiencia", "amb_galho_cai", 2, s => Room(Sx.Mix(Sx.G(Sx.BP(Sx.Crackle(0.3, Sx.K(1000), Sx.Perc(0.002, 0.08), s), 1500, 1), 0.7), Sx.At(0.5, Sx.G(WoodKnock(160, s + 1), 0.5))), 0.5));
        Add(l, "Ambiencia", "amb_sapo", 3, s => Sx.BP(Sx.Osc(Sx.W.Square, 0.35, delegate(double t) { return 160 * J(s, 0.1) * (1 + 0.3 * Math.Sin(t * 60)); }, Sx.AHR(0.02, 0.2, 0.1)), 500, 1.5));
        Add(l, "Ambiencia", "amb_maquina_bip", 1, s => Room(Sx.G(Sx.Osc(Sx.W.Square, 0.12, Sx.K(1320), Sx.Perc(0.003, 0.05)), 0.4), 0.4));
    }
}
