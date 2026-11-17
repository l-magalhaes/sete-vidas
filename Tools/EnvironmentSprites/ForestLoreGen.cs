// Floresta, F5 parte 2 (14/11/2026): objetos das Ruínas dos Primatas e a Isyathy vista de longe.
// Grava em Assets/_Game/Art/Environments/Forest/Placeholder (os nomes que o ForestGrayboxSetup carrega), preservando .meta.
//
// Ruínas dos Primatas (guia: Docs/Arte/floresta-guia-visual.md, "Ruínas dos Primatas"): linhas retas e 90°, concreto
// #3c4042/#626868, vidro #36505c com reflexo de lua, ferrugem #623e2c, estrada negra #222226; sempre com musgo ou
// trepadeira por cima; máquinas com UMA luz verde pequena. Luz da lua no alto à esquerda.
//   Placas que se leem (uma por texto P-PRIM):
//   pri_placa_estrada   24 × 44  placa de metal num poste: seta e um desenho de algo com rodas (P-PRIM-01, A2)
//   pri_mapa            36 × 44  painel de pé com o mapa de linhas coloridas e pontos (P-PRIM-02, A5)
//   pri_vitrine         56 × 40  vidro inteiro; atrás, cadeiras viradas para uma parede vazia (P-PRIM-03, C3)
//   pri_farol_0/1       16 × 28  luzinha verde no alto da torre, acesa/apagada (P-PRIM-04, B4)
//   pri_painel          20 × 40  painel do elevador: botões em duas colunas (P-PRIM-05, G2)
//   pri_carcaca         96 × 40  carroça de metal sem cavalo, enferrujada e tomada pelo mato (P-PRIM-06, C5)
//   pri_lapide          28 × 32  lápide do templo: primatas talhados olhando para cima (P-PRIM-07, D6)
//   pri_cabine_mesa     44 × 40  mesa, caneca colada pelo musgo e a lista de horários na parede (P-PRIM-08, A5 segredo)
//   Só cenário (atrás do gato, sem entradas escuras):
//   pri_poste           20 × 80  poste de luz torto (a lâmpada quebrada, sem luz), trepadeira subindo
//   pri_poste_tombado   72 × 20  poste caído no chão
//   pri_maquina_0/1     32 × 36  armário de máquina com uma luz verde piscando devagar
//   pri_janela          32 × 40  janela de concreto com cacos de vidro; atrás, terra e raízes (prédio soterrado)
//   pri_escada          64 × 64  lance de escada de concreto quebrado, com vergalhão
//   pri_relogio         24 × 40  relógio da estação pendurado do teto, parado
//   Mercado dos Primatas (térreo da C3, 15/11/2026), tomado pelas vinhas:
//   pri_prateleira      40 × 40  prateleira cheia de latas iguais com o mesmo rosto sorrindo (P-PRIM-09)
//   pri_caixa           44 × 28  balcão do caixa: esteira parada e a gaveta aberta com discos e papéis (P-PRIM-10)
//   pri_letreiro        64 × 20  letreiro de metal pendurado torto (uma corrente partiu), letras que ninguém lê
//   pri_carrinho        32 × 22  gaiola de arame com rodinhas, uma roda a menos, a trepadeira por dentro
//   pri_gondola         48 × 40  gôndola de metal com poucas latas, o resto caído; vinhas subindo
//   pri_gondola_tombada 56 × 24  gôndola caída de lado, latas espalhadas
//   pri_vinhas_teto     32 × 40  vinhas penduradas do teto (o topo no teto)
// Isyathy vista de longe (mapa §6; corpo #2e4834, ventre #52664c, mas escurecidos: fica no fundo):
//   isy_olhos_0..2      40 × 12  dois olhos amarelo-esverdeados: abertos, meio fechados, fechados (A4 e o lago da C4)
//   isy_silhueta       192 × 40  um trecho do corpo passando atrás das copas (B4)
//   isy_fundo_0..5      96 × 80  o pescoço que espia entre as árvores do fundo: olhos fechados, meio, abertos, de lado (2), piscada
// C# 5.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SeteVidas.World.Scenery;

public static class ForestLoreGen
{
    static string s_dir, s_template;

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        s_template = File.ReadAllText(metaTemplatePath);
        s_dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Placeholder");
        Directory.CreateDirectory(s_dir);
        Palette();
        Write("pri_placa_estrada", RoadSign());
        Write("pri_mapa", LineMap());
        Write("pri_vitrine", ShowWindow());
        Write("pri_farol_0", Beacon(true)); Write("pri_farol_1", Beacon(false));
        Write("pri_painel", ElevatorPanel());
        Write("pri_carcaca", Wreck());
        Write("pri_lapide", Tablet());
        Write("pri_cabine_mesa", Desk());
        Write("pri_poste", LampPost());
        Write("pri_poste_tombado", FallenPost());
        Write("pri_maquina_0", Machine(true)); Write("pri_maquina_1", Machine(false));
        Write("pri_janela", BuriedWindow());
        Write("pri_escada", BrokenStairs());
        Write("pri_relogio", Clock());
        // o Mercado dos Primatas (térreo da C3, 15/11/2026)
        Write("pri_letreiro", MarketSign()); Write("pri_caixa", Checkout()); Write("pri_carrinho", Cart());
        Write("pri_gondola", Gondola()); Write("pri_gondola_tombada", FallenGondola()); Write("pri_prateleira", CanShelf());
        Write("pri_vinhas_teto", HangingVines());
        for (int i = 0; i < 3; i++) Write("isy_olhos_" + i, Eyes(i));
        Write("isy_silhueta", Silhouette());
        // o pescoço que espia entre as árvores do fundo (pedido do usuário, 14/11/2026): fechados, meio, abertos,
        // de lado, mais de lado, piscada
        int[,] peek = { { 0, 0 }, { 0, 1 }, { 0, 2 }, { 1, 2 }, { 2, 2 }, { 1, 0 } };
        for (int i = 0; i < 6; i++) Write("isy_fundo_" + i, Peek(peek[i, 0], peek[i, 1], false));
        Write("isy_fundo_6", Peek(0, 2, true));                                                  // a língua
        // documentos das cabanas abandonadas (a gente que rezava para a Isyathy)
        Write("doc_oracao", DocPrayer()); Write("doc_caderno", DocLedger()); Write("doc_carta", DocLetter()); Write("doc_pagina", DocPage());
        // portões (pedido do usuário, 14/11/2026: "melhore os assets da porta do templo e dos portões da arena")
        Write("portao_templo_fechado", TempleDoor(false)); Write("portao_templo_aberto", TempleDoor(true));
        Write("portal_templo_pilares", TemplePillars()); Write("portal_templo_lintel", TempleLintel());
        Write("portao_barras", ArenaBars());
        Preview(Path.Combine(previewDir, "floresta_ruinas_f5.png"));
        return "8 placas dos Primatas, 6 objetos de ruína e 4 aparições da Isyathy";
    }

    /// <summary>Nomes de tudo o que este gerador grava (o teste confere que os PNG existem).</summary>
    public static readonly string[] Names =
    {
        "pri_placa_estrada", "pri_mapa", "pri_vitrine", "pri_farol_0", "pri_farol_1", "pri_painel", "pri_carcaca", "pri_lapide",
        "pri_cabine_mesa", "pri_poste", "pri_poste_tombado", "pri_maquina_0", "pri_maquina_1", "pri_janela", "pri_escada",
        "pri_relogio", "pri_letreiro", "pri_caixa", "pri_carrinho", "pri_gondola", "pri_gondola_tombada", "pri_prateleira",
        "pri_vinhas_teto", "isy_olhos_0", "isy_olhos_1", "isy_olhos_2", "isy_silhueta",
        "portao_templo_fechado", "portao_templo_aberto", "portal_templo_pilares", "portal_templo_lintel", "portao_barras",
        "isy_fundo_0", "isy_fundo_1", "isy_fundo_2", "isy_fundo_3", "isy_fundo_4", "isy_fundo_5", "isy_fundo_6",
        "doc_oracao", "doc_caderno", "doc_carta", "doc_pagina",
    };

    static void Write(string name, PixelCanvas cv)
    {
        PixelKit.WriteSprite(cv, Path.Combine(s_dir, name + ".png"), "flo_prop/" + name, s_template, false);
    }

    static uint H(int x, int y, int s) { return TerrainAutotile.Hash(x, y, s); }

    static void Palette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('a', "#16181a");                                                                   // contorno
        PixelKit.P('c', "#2a2d2f"); PixelKit.P('C', "#3c4042"); PixelKit.P('D', "#626868"); PixelKit.P('E', "#868c88"); // concreto
        PixelKit.P('v', "#1e2c33"); PixelKit.P('V', "#36505c"); PixelKit.P('W', "#5c7c88"); PixelKit.P('X', "#a8c4cc"); // vidro
        PixelKit.P('r', "#3e261c"); PixelKit.P('R', "#623e2c"); PixelKit.P('S', "#8a5a3a");          // ferrugem
        PixelKit.P('k', "#222226"); PixelKit.P('K', "#34343a");                                       // estrada negra
        PixelKit.P('g', "#24402e"); PixelKit.P('G', "#3c6440"); PixelKit.P('H', "#64905a");          // musgo
        PixelKit.P('n', "#2e3238"); PixelKit.P('m', "#4a4e56"); PixelKit.P('M', "#6e747e");          // metal
        PixelKit.P('l', "#1f5a34"); PixelKit.P('L', "#4ade6a"); PixelKit.P('Y', "#c8ffd0");          // a luz verde
        PixelKit.P('p', "#4a5a50"); PixelKit.P('P', "#6a7c6c");                                       // tinta verde gasta (placas)
        PixelKit.P('w', "#b8b4a0"); PixelKit.P('x', "#8a8676");                                       // branco gasto
        PixelKit.P('q', "#8a4a46"); PixelKit.P('u', "#4a6a8a"); PixelKit.P('o', "#9a8a4a");          // linhas do mapa (desbotadas)
        PixelKit.P('b', "#4e3a2a"); PixelKit.P('B', "#6e5238");                                       // estofado e barro
        PixelKit.P('e', "#2e2219"); PixelKit.P('f', "#43301f");                                       // terra e raiz (atrás da janela)
        // pedra do templo (a mesma do ForestPropGen.TemplePalette)
        PixelKit.P('1', "#1a1a21"); PixelKit.P('2', "#2e2f38"); PixelKit.P('3', "#43444f"); PixelKit.P('4', "#5a5c68"); PixelKit.P('5', "#747783");
        // Isyathy de longe: escamas e ventre escurecidos (fundo), olhos amarelo-esverdeados, a praga
        PixelKit.P('i', "#121a16"); PixelKit.P('j', "#1a2820"); PixelKit.P('J', "#24382c"); PixelKit.P('T', "#2e4834");
        PixelKit.P('U', "#3a4a3a"); PixelKit.P('Z', "#52664c");
        PixelKit.P('y', "#d8e85a"); PixelKit.P('z', "#8fd14f"); PixelKit.P('h', "#5b2a6e");
        // turquesa do templo (a mesma da Chave do Templo e do olho do portão)
        PixelKit.P('6', "#1f6a64"); PixelKit.P('7', "#5ce0c8"); PixelKit.P('8', "#c8fff4");
    }

    // ============================================================ documentos das cabanas (a gente que rezava para a Isyathy)

    /// <summary>Caixote de madeira (base dos documentos): tábuas com luar em cima, pregos, musgo no pé.</summary>
    static void Crate(PixelCanvas c, int x0, int y0, int x1, int y1)
    {
        Box(c, x0, y0, x1, y1, 'B', 'B', 'b');
        for (int y = y0 + 3; y < y1; y += 3) for (int x = x0 + 1; x < x1; x++) c.Force(x, y, 'b');
        c.Force(x0 + 2, y0 + 2, 'M'); c.Force(x1 - 2, y0 + 2, 'M');
        for (int x = x0; x <= x1; x++) if (H(x, y1, 301) % 3u == 0) c.Force(x, y1, 'G');
    }

    /// <summary>A oração pregada numa tábua fincada no chão, com a serpentezinha de madeira entalhada no pé (oferenda).</summary>
    static PixelCanvas DocPrayer()
    {
        var c = new PixelCanvas(22, 26);
        Rect(c, 9, 6, 11, 25, 'b'); Rect(c, 9, 6, 9, 25, 'B');                                    // a estaca
        Box(c, 3, 2, 17, 15, 'w', 'w', 'x');                                                     // o papel
        for (int r = 0; r < 4; r++) for (int x = 5; x <= 15; x++) if (H(x, r, 311) % 4u != 0) c.Force(x, 5 + r * 2, 'm');
        c.Force(10, 2, 'M'); c.Force(10, 3, 'n');                                                // o prego
        // a serpente de madeira enrolada no pé (oferenda) e um pãozinho velho
        string[] idol = { "..BB..", ".B..B.", "..BB.B", ".B...B", "B.BBB.", "BBBBBB" };
        for (int y = 0; y < idol.Length; y++) for (int x = 0; x < 6; x++) if (idol[y][x] != '.') c.Force(13 + x, 20 + y, idol[y][x]);
        Rect(c, 2, 23, 6, 25, 'S'); Rect(c, 3, 23, 5, 23, 'B');
        for (int x = 0; x < 22; x++) if (H(x, 25, 312) % 2u == 0) c.Force(x, 25, 'g');
        return c;
    }

    /// <summary>O caderno de contas aberto em cima de um caixote.</summary>
    static PixelCanvas DocLedger()
    {
        var c = new PixelCanvas(20, 18);
        Crate(c, 1, 7, 18, 17);
        Rect(c, 3, 3, 16, 7, 'w'); Rect(c, 9, 3, 10, 7, 'x'); Rect(c, 3, 7, 16, 7, 'b');          // o caderno aberto
        for (int x = 4; x <= 15; x++) if (x != 9 && x != 10 && H(x, 4, 321) % 3u != 0) { c.Force(x, 4, 'm'); if (H(x, 5, 322) % 2u == 0) c.Force(x, 6, 'm'); }
        Rect(c, 2, 2, 17, 2, 'b');
        return c;
    }

    /// <summary>A carta dobrada, nunca enviada, amarrada com barbante em cima do caixote.</summary>
    static PixelCanvas DocLetter()
    {
        var c = new PixelCanvas(18, 16);
        Crate(c, 1, 6, 16, 15);
        Rect(c, 4, 2, 13, 6, 'w'); Rect(c, 4, 6, 13, 6, 'x');
        for (int x = 4; x <= 13; x++) c.Force(x, 2 + Math.Abs(x - 8) / 3, 'x');                 // a dobra em V
        Rect(c, 8, 2, 8, 6, 'S'); c.Force(7, 1, 'S'); c.Force(9, 1, 'S');                          // o barbante
        return c;
    }

    /// <summary>A última página, rasgada, no chão, presa por uma pedra; uma mancha roxa da praga no canto.</summary>
    static PixelCanvas DocPage()
    {
        var c = new PixelCanvas(18, 12);
        for (int y = 4; y < 11; y++) for (int x = 2; x < 16; x++) if (!(x > 12 && y < 4 + (x - 12) * 2)) c.Force(x, y, (x + y) % 5 == 0 ? 'x' : 'w');
        for (int r = 0; r < 2; r++) for (int x = 4; x <= 11; x++) if (H(x, r, 331) % 3u != 0) c.Force(x, 6 + r * 2, 'm');
        Rect(c, 13, 8, 15, 10, 'h'); c.Force(12, 9, 'h');                                         // a praga
        Rect(c, 3, 2, 7, 5, 'D'); Rect(c, 3, 2, 7, 2, 'E'); Rect(c, 3, 5, 7, 5, 'c');             // a pedra
        for (int x = 0; x < 18; x++) if (H(x, 11, 332) % 2u == 0) c.Force(x, 11, 'g');
        return c;
    }

    // ============================================================ portões

    /// <summary>
    /// Portão do Templo da Serpente (16 × 64): laje de pedra do templo com a serpente entalhada em relevo subindo em S,
    /// a cabeça no alto com o olho turquesa e, no meio, a fechadura turquesa (a Chave do Templo). Aberto: a laje desceu
    /// para dentro do chão; sobra a borda de cima dela, rente ao piso.
    /// </summary>
    static PixelCanvas TempleDoor(bool open)
    {
        var c = new PixelCanvas(16, 64);
        if (open)
        {
            Rect(c, 1, 60, 14, 63, '3'); Rect(c, 1, 60, 14, 60, '5'); Rect(c, 1, 63, 14, 63, '1');
            c.Force(4, 61, '7'); c.Force(11, 62, '2');
            return c;
        }
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 16; x++)
            {
                char ch = x == 0 || x == 15 || y == 0 || y == 63 ? '1' : x == 1 || y == 1 ? '4' : x == 14 || y == 62 ? '2' : '3';
                if (ch == '3' && y % 16 == 0) ch = '2';                                       // juntas dos blocos
                c.Force(x, y, ch);
            }
        // a serpente em relevo: corpo em S (luar à esquerda, sombra à direita)
        for (int y = 14; y < 58; y++)
        {
            int sx = 8 + (int)Math.Round(3.2 * Math.Sin((y - 14) * 0.2));
            c.Force(sx - 2, y, '5'); c.Force(sx - 1, y, '4'); c.Force(sx, y, (y % 3 == 0) ? '3' : '4'); c.Force(sx + 1, y, '2'); c.Force(sx + 2, y, '1');
        }
        // a cabeça no alto (losango) com o olho turquesa e a língua
        string[] head = { "...1111...", "..154441..", ".15444441.", "1544874441", ".14444441.", "..122221..", "....11...." };
        for (int y = 0; y < head.Length; y++) for (int x = 0; x < 10; x++) if (head[y][x] != '.') c.Force(3 + x, 5 + y, head[y][x]);
        c.Force(8, 12, 'q'); c.Force(8, 13, 'q'); c.Force(7, 14, 'q'); c.Force(9, 14, 'q');
        // fechadura turquesa no meio
        Rect(c, 6, 34, 9, 39, '1'); Rect(c, 7, 35, 8, 36, '7'); c.Force(7, 37, '6'); c.Force(8, 37, '6'); c.Force(7, 38, '6'); c.Force(7, 35, '8');
        // musgo e rachaduras
        for (int x = 1; x < 15; x++) { if (H(x, 0, 211) % 2u == 0) c.Force(x, 1, 'G'); if (H(x, 1, 212) % 3u == 0) c.Force(x, 2, 'g'); }
        for (int x = 1; x < 15; x++) { int up = (int)(H(x, 2, 213) % 5u); for (int y = 62; y > 62 - up; y--) c.Force(x, y, y > 60 ? 'g' : 'G'); }
        int cx = 3; for (int y = 40; y < 52; y++) { c.Force(cx, y, '1'); if (y % 4 == 0) cx++; }
        return c;
    }

    /// <summary>
    /// Pilares do portal (48 × 64, atrás do gato): um pilar de cada lado do vão do portão (o meio fica vazio: o portão
    /// é desenhado ali), base de blocos, fuste com caneluras e musgo subindo.
    /// </summary>
    static PixelCanvas TemplePillars()
    {
        var c = new PixelCanvas(48, 64);
        foreach (int x0 in new[] { 4, 32 })
        {
            for (int y = 0; y < 64; y++)
                for (int x = x0; x < x0 + 12; x++)
                {
                    bool foot = y >= 56, cap = y < 6;
                    if (!foot && !cap && (x == x0 || x == x0 + 11)) continue;                  // o fuste é mais fino que base e capitel
                    char ch = foot || cap
                        ? (y == 56 || y == 0 ? '5' : y == 63 || y == 5 ? '1' : x == x0 ? '4' : x == x0 + 11 ? '2' : '3')
                        : (x == x0 + 1 || x == x0 + 10 ? '1' : x == x0 + 2 ? '5' : (x - x0) % 3 == 0 ? '2' : x < x0 + 6 ? '4' : '3');
                    if (!foot && !cap && y % 20 == 10 && x > x0 + 1 && x < x0 + 10) ch = '2';  // juntas dos tambores
                    c.Force(x, y, ch);
                }
            for (int x = x0; x < x0 + 12; x++)
            {
                int up = 2 + (int)(H(x, 3, 221 + x0) % 10u);
                for (int y = 63; y > 63 - up; y--) if (c.Get(x, y) != '.') c.Force(x, y, y > 61 ? 'g' : (H(x, y, 222) % 3u == 0 ? 'H' : 'G'));
            }
            Creeper(c, x0 + 9, 14, 56, 223 + x0);
        }
        return c;
    }

    /// <summary>
    /// Verga do portal (48 × 32, NA FRENTE da parede em cima do portão): blocos de pedra do templo com a cabeça da
    /// serpente entalhada de frente, os dois olhos turquesa, e a cornija com musgo e raízes caindo.
    /// </summary>
    static PixelCanvas TempleLintel()
    {
        var c = new PixelCanvas(48, 32);
        for (int y = 4; y < 32; y++)
            for (int x = 0; x < 48; x++)
            {
                char ch = y == 4 || x == 0 || x == 47 ? '1' : y == 5 ? '5' : y == 31 ? '1' : y == 30 ? '2' : x < 2 ? '4' : '3';
                if (ch == '3' && (y == 17 || ((x + (y < 17 ? 0 : 8)) % 16 == 0))) ch = '2';  // blocos em fiada
                c.Force(x, y, ch);
            }
        Rect(c, 0, 0, 47, 3, '4'); Rect(c, 0, 0, 47, 0, '5'); Rect(c, 0, 3, 47, 3, '1');               // cornija
        // a cabeça da serpente de frente (no meio, em cima do vão)
        string[] head =
        {
            "....11111111....",
            "...1555444441...",
            "..154444444441..",
            ".15448744478441.",
            ".14444444444441.",
            "..144422224441..",
            "...1442qq2441...",
            "....12q..q21....",
            ".....1....1.....",
        };
        for (int y = 0; y < head.Length; y++) for (int x = 0; x < 16; x++) if (head[y][x] != '.') c.Force(16 + x, 9 + y, head[y][x]);
        // musgo na cornija e raízes caindo
        for (int x = 0; x < 48; x++)
        {
            if (H(x, 0, 231) % 3u != 0) c.Force(x, 0, 'G');
            if (H(x, 1, 232) % 4u == 0) c.Force(x, 1, 'g');
            if (H(x, 2, 233) % 9u == 0) { int len = 3 + (int)(H(x, 3, 234) % 10u); for (int y = 4; y < 4 + len && y < 32; y++) c.Force(x, y, y % 4 == 0 ? 'G' : 'g'); }
        }
        return c;
    }

    /// <summary>
    /// Grade da arena (16 × 64, desenhada em mosaico: emenda sem corte em cima e embaixo). Duas barras de ferro grossas
    /// com luar à esquerda, travessas rebitadas, ferrugem escorrendo e pontas afiadas embaixo (é ela que desce).
    /// </summary>
    static PixelCanvas ArenaBars()
    {
        var c = new PixelCanvas(16, 64);
        foreach (int bx in new[] { 3, 10 })
            for (int y = 0; y < 64; y++)
            {
                bool tip = y >= 58;
                int half = tip ? Math.Min(1, (63 - y) / 2) : 1;                                // a ponta afina
                for (int x = bx - half; x <= bx + half + 1; x++)
                    c.Force(x, y, x == bx - half ? 'M' : x == bx + half + 1 ? 'n' : 'm');
            }
        foreach (int ty in new[] { 6, 30, 52 })
        {
            Rect(c, 0, ty, 15, ty + 2, 'm'); Rect(c, 0, ty, 15, ty, 'M'); Rect(c, 0, ty + 2, 15, ty + 2, 'n');
            c.Force(3, ty + 1, 'E'); c.Force(11, ty + 1, 'E');                                // rebites
        }
        Rust(c, 0, 0, 15, 57, 'm', 241, 7u);
        Rust(c, 0, 0, 15, 57, 'n', 242, 9u);
        c.Force(10, 33, 'g'); c.Force(11, 34, 'G'); c.Force(10, 35, 'g'); c.Force(4, 9, 'G');           // um pouco de musgo
        return c;
    }

    // ============================================================ ajudantes

    static void Rect(PixelCanvas c, int x0, int y0, int x1, int y1, char ch)
    {
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) c.Force(x, y, ch);
    }

    /// <summary>Caixa com contorno 'a', luar na borda de cima e da esquerda, sombra embaixo e à direita.</summary>
    static void Box(PixelCanvas c, int x0, int y0, int x1, int y1, char body, char light, char dark)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                char ch = x == x0 || x == x1 || y == y0 || y == y1 ? 'a'
                        : y == y0 + 1 || x == x0 + 1 ? light
                        : y == y1 - 1 || x == x1 - 1 ? dark : body;
                c.Force(x, y, ch);
            }
    }

    /// <summary>Musgo por cima de tudo o que é sólido na linha de cima de cada coluna (de x0 a x1).</summary>
    static void MossTop(PixelCanvas c, int x0, int x1, int seed)
    {
        for (int x = x0; x <= x1; x++)
        {
            int top = -1;
            for (int y = 0; y < c.H; y++) if (c.Get(x, y) != '.') { top = y; break; }
            if (top < 0) continue;
            uint h = H(x, top, seed);
            if (h % 4u == 0) continue;
            c.Force(x, top, 'G');
            if (h % 3u == 0) c.Force(x, top + 1, 'g');
            if (h % 5u == 0 && top > 0) c.Force(x, top - 1, 'H');
        }
    }

    /// <summary>Pingos de ferrugem nos pixels de corpo (body) do retângulo.</summary>
    static void Rust(PixelCanvas c, int x0, int y0, int x1, int y1, char body, int seed, uint every)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (c.Get(x, y) == body && H(x, y, seed) % every == 0)
                {
                    c.Force(x, y, 'R');
                    if (c.Get(x, y + 1) == body && H(x, y, seed + 1) % 2u == 0) c.Force(x, y + 1, 'r');   // escorre
                }
    }

    /// <summary>Trepadeira subindo em zigue-zague pela coluna x, de y1 (baixo) até y0, com folhas.</summary>
    static void Creeper(PixelCanvas c, int x, int y0, int y1, int seed)
    {
        int cx = x;
        for (int y = y1; y >= y0; y--)
        {
            c.Force(cx, y, 'g');
            if (y % 3 == 0) cx += (H(cx, y, seed) % 2u == 0) ? 1 : -1;
            cx = Math.Max(x - 2, Math.Min(x + 2, cx));
            if (y % 4 == 1) { c.Force(cx - 1, y, 'G'); c.Force(cx + 1, y - 1, 'H'); }
        }
    }

    // ============================================================ placas que se leem

    /// <summary>P-PRIM-01: placa retangular de metal verde num poste; uma seta e um desenho de algo com rodas.</summary>
    static PixelCanvas RoadSign()
    {
        var c = new PixelCanvas(24, 44);
        Rect(c, 11, 14, 12, 43, 'm'); for (int y = 14; y < 44; y++) c.Force(11, y, y % 7 == 0 ? 'R' : 'M');   // poste
        Box(c, 0, 1, 23, 15, 'p', 'P', 'n');
        // seta para a direita (em cima) e o "carro" (embaixo)
        Rect(c, 4, 5, 13, 5, 'w'); c.Force(14, 5, 'w'); c.Force(13, 4, 'w'); c.Force(13, 6, 'w'); c.Force(12, 3, 'w'); c.Force(12, 7, 'w');
        Rect(c, 5, 10, 14, 11, 'w'); Rect(c, 7, 9, 12, 9, 'w');
        c.Force(7, 12, 'w'); c.Force(12, 12, 'w');
        c.Force(18, 4, 'x'); c.Force(19, 4, 'x'); c.Force(18, 6, 'x'); c.Force(20, 6, 'x');                 // letras gastas
        c.Force(17, 9, 'x'); c.Force(19, 9, 'x'); c.Force(20, 11, 'x');
        c.Force(2, 3, 'M'); c.Force(21, 3, 'M'); c.Force(2, 13, 'M'); c.Force(21, 13, 'M');                 // parafusos
        Rust(c, 2, 2, 21, 14, 'p', 31, 9u);
        MossTop(c, 0, 23, 32);
        Rect(c, 8, 42, 15, 43, 'g'); c.Force(9, 41, 'G'); c.Force(14, 40, 'H');                             // mato no pé
        return c;
    }

    /// <summary>P-PRIM-02: painel de pé com o mapa (linhas coloridas que se cruzam, pontos) e uma palavra repetida.</summary>
    static PixelCanvas LineMap()
    {
        var c = new PixelCanvas(36, 44);
        Rect(c, 4, 28, 5, 43, 'm'); Rect(c, 30, 28, 31, 43, 'm'); c.Force(4, 30, 'R'); c.Force(31, 35, 'R');   // pés
        Box(c, 0, 0, 35, 29, 'w', 'w', 'x');
        Rect(c, 2, 2, 33, 21, 'x');                                                                          // o papel velho
        // as linhas: vermelha (reta), azul (diagonal), amarela (em L)
        for (int x = 4; x <= 31; x++) c.Force(x, 8, 'q');
        for (int i = 0; i <= 14; i++) c.Force(7 + i, 4 + i, 'u');
        for (int y = 4; y <= 19; y++) c.Force(25, y, 'o');
        for (int x = 14; x <= 25; x++) c.Force(x, 15, 'o');
        // as estações (pontos claros com contorno)
        int[,] dots = { { 4, 8 }, { 31, 8 }, { 7, 4 }, { 21, 18 }, { 25, 4 }, { 14, 15 }, { 25, 19 }, { 11, 8 }, { 25, 8 }, { 18, 15 } };
        for (int i = 0; i < dots.GetLength(0); i++)
        {
            int x = dots[i, 0], y = dots[i, 1];
            c.Force(x, y, 'a'); c.Force(x - 1, y, 'n'); c.Force(x + 1, y, 'n'); c.Force(x, y - 1, 'n'); c.Force(x, y + 1, 'n');
            c.Force(x, y, 'w');
        }
        // embaixo, a mesma palavra três vezes
        for (int k = 0; k < 3; k++) for (int j = 0; j < 7; j++) if (j != 2) c.Force(4 + k * 10 + j, 25, j % 3 == 0 ? 'n' : 'm');
        // manchas de umidade e musgo
        for (int y = 2; y <= 27; y++) for (int x = 2; x <= 33; x++) if (H(x, y, 41) % 23u == 0) c.Force(x, y, 'D');
        MossTop(c, 0, 35, 42);
        for (int y = 0; y < 12; y++) c.Force(34, y, H(34, y, 43) % 2u == 0 ? 'g' : 'G');                    // escorre pela borda
        return c;
    }

    /// <summary>P-PRIM-03: moldura de concreto com o vidro inteiro; atrás, fileiras de cadeiras viradas para a direita.</summary>
    static PixelCanvas ShowWindow()
    {
        var c = new PixelCanvas(56, 40);
        Box(c, 0, 0, 55, 39, 'C', 'D', 'c');
        Rect(c, 4, 4, 51, 31, 'v');                                                                         // a sala escura
        Rect(c, 44, 8, 49, 26, 'V');                                                                        // a parede vazia
        // cadeiras (encosto à esquerda, assento à direita), em 3 fileiras subindo para trás
        for (int row = 0; row < 3; row++)
            for (int k = 0; k < 5; k++)
            {
                int x = 8 + k * 7 - row, y = 27 - row * 6;
                Rect(c, x, y - 4, x, y, 'n'); Rect(c, x, y, x + 3, y, 'n'); c.Force(x + 3, y + 1, 'n'); c.Force(x, y + 1, 'n');
                if (H(k, row, 51) % 4u == 0) Rect(c, x + 1, y - 1, x + 3, y - 1, 'm');                     // uma cadeira tombada
            }
        // reflexo da lua no vidro (diagonais claras do alto à esquerda)
        for (int i = 0; i < 18; i++) { c.Force(8 + i, 4 + i, 'W'); c.Force(9 + i, 4 + i, 'W'); if (i < 6) c.Force(10 + i, 4 + i, 'X'); }
        for (int i = 0; i < 10; i++) c.Force(30 + i, 4 + i, 'W');
        Rect(c, 2, 32, 53, 33, 'E');                                                                        // peitoril
        MossTop(c, 0, 55, 52);
        for (int x = 1; x < 55; x++) if (H(x, 38, 53) % 3u == 0) c.Force(x, 38, 'G');                       // musgo no pé
        Creeper(c, 2, 4, 37, 54);
        return c;
    }

    /// <summary>P-PRIM-04: a luzinha verde no alto da torre (cúpula de vidro sobre uma caixa de metal num suporte).</summary>
    static PixelCanvas Beacon(bool on)
    {
        var c = new PixelCanvas(16, 28);
        Rect(c, 7, 14, 8, 27, 'm'); c.Force(7, 20, 'R'); c.Force(8, 24, 'r');
        Box(c, 3, 10, 12, 15, 'm', 'M', 'n');
        for (int y = 3; y <= 9; y++)
            for (int x = 4; x <= 11; x++)
            {
                double dx = x + 0.5 - 8, dy = y + 0.5 - 10;
                if (dx * dx / 16.0 + dy * dy / 49.0 > 1.0) continue;
                c.Force(x, y, on ? (dx < -1 && dy < -3 ? 'Y' : 'L') : (dx < -1 && dy < -3 ? 'W' : 'l'));
            }
        c.Force(7, 2, 'a'); c.Force(8, 2, 'a');                                                            // a ponta
        if (on) { c.Force(2, 6, 'l'); c.Force(13, 6, 'l'); c.Force(8, 0, 'l'); c.Force(1, 9, 'l'); c.Force(14, 9, 'l'); }   // o brilho em volta
        Creeper(c, 7, 17, 27, 61);
        return c;
    }

    /// <summary>P-PRIM-05: o painel do elevador (metal, duas colunas de botões redondos, setas em cima).</summary>
    static PixelCanvas ElevatorPanel()
    {
        var c = new PixelCanvas(20, 40);
        Box(c, 0, 0, 19, 39, 'm', 'M', 'n');
        c.Force(6, 3, 'w'); c.Force(5, 4, 'w'); c.Force(7, 4, 'w');                                         // ▲
        c.Force(13, 4, 'w'); c.Force(12, 3, 'w'); c.Force(14, 3, 'w');                                      // ▼
        for (int row = 0; row < 8; row++)
            for (int col = 0; col < 2; col++)
            {
                int x = 5 + col * 7, y = 8 + row * 4;
                c.Force(x, y, 'a'); c.Force(x + 1, y, 'a'); c.Force(x, y + 1, 'D'); c.Force(x + 1, y + 1, 'E');
                c.Force(x + 3, y, 'x');                                                                     // o número gasto
            }
        Rust(c, 2, 2, 17, 37, 'm', 71, 7u);
        MossTop(c, 0, 19, 72);
        return c;
    }

    /// <summary>P-PRIM-06: a carroça de metal sem cavalo (um carro velho), enferrujada, sem rodas, mato por cima.</summary>
    static PixelCanvas Wreck()
    {
        var c = new PixelCanvas(96, 40);
        // carroceria (embaixo) e cabine (em cima), cantos chanfrados
        for (int y = 18; y <= 33; y++)
            for (int x = 2; x <= 93; x++)
            {
                if ((y == 18 && (x < 6 || x > 89)) || (y == 33 && (x < 4 || x > 91))) continue;
                char ch = y == 18 || y == 33 || x == 2 || x == 93 ? 'a' : y == 19 ? 'S' : y > 29 ? 'r' : 'R';
                c.Force(x, y, ch);
            }
        for (int y = 6; y <= 18; y++)
        {
            int inset = (18 - y) / 2;
            for (int x = 20 + inset; x <= 74 - inset / 2; x++)
            {
                char ch = y == 6 || x == 20 + inset || x == 74 - inset / 2 ? 'a' : y == 7 ? 'S' : 'R';
                c.Force(x, y, ch);
            }
        }
        // janelas (vidro quebrado), o círculo de segurar e os bancos
        for (int y = 9; y <= 16; y++)
        {
            int inset = (18 - y) / 2;
            for (int x = 23 + inset; x <= 45; x++) c.Force(x, y, 'v');
            for (int x = 49; x <= 71 - inset / 2; x++) c.Force(x, y, 'v');
        }
        c.Force(46, 9, 'a'); for (int y = 9; y <= 17; y++) { c.Force(47, y, 'R'); c.Force(48, y, 'a'); }   // coluna do meio
        PixelKit.Ellipse(c, 32f, 14f, 3f, 3f, 'n'); PixelKit.Ellipse(c, 32f, 14f, 1.5f, 1.5f, 'v');        // o círculo de segurar
        Rect(c, 54, 12, 62, 16, 'b'); Rect(c, 54, 12, 55, 16, 'B');                                        // banco
        Rect(c, 37, 13, 42, 16, 'b');
        for (int i = 0; i < 6; i++) { c.Force(26 + i, 9 + i, 'W'); c.Force(52 + i, 9 + i, 'W'); }          // luar no vidro
        c.Force(27, 9, 'X'); c.Force(53, 9, 'X');
        // vãos das rodas (vazios) e os eixos
        foreach (int wx in new[] { 18, 78 })
            for (int y = 26; y <= 33; y++)
                for (int x = wx - 8; x <= wx + 8; x++)
                {
                    double dx = x + 0.5 - wx, dy = y + 0.5 - 34;
                    if (dx * dx + dy * dy < 64) c.Force(x, y, dx * dx + dy * dy > 49 ? 'a' : '.');
                }
        Rect(c, 10, 33, 26, 34, 'n'); Rect(c, 70, 33, 86, 34, 'n');
        // faróis apagados, detalhes, ferrugem e furos
        Rect(c, 3, 21, 5, 23, 'x'); Rect(c, 90, 21, 92, 23, 'q');
        for (int x = 8; x <= 88; x += 20) c.Force(x, 25, 'r');
        Rust(c, 3, 19, 92, 32, 'R', 81, 6u);
        for (int y = 20; y <= 31; y++) for (int x = 4; x <= 91; x++) if (H(x, y, 82) % 61u == 0) c.Force(x, y, 'a');   // furos
        // mato: capim alto embaixo, musgo no teto, trepadeira pela frente
        MossTop(c, 0, 95, 83);
        for (int x = 0; x < 96; x++)
        {
            int up = (int)(H(x, 0, 84) % 7u);
            for (int y = 39; y > 39 - up; y--) c.Force(x, y, y > 37 ? 'g' : (H(x, y, 85) % 2u == 0 ? 'G' : 'H'));
        }
        Creeper(c, 86, 18, 38, 86); Creeper(c, 22, 12, 38, 87);
        return c;
    }

    /// <summary>P-PRIM-07: lápide de pedra do templo com três primatas talhados, sentados, olhando para o alto.</summary>
    static PixelCanvas Tablet()
    {
        var c = new PixelCanvas(28, 32);
        for (int y = 2; y <= 31; y++)
            for (int x = 1; x <= 26; x++)
            {
                double dx = x + 0.5 - 14, dy = y + 0.5 - 13;
                if (y < 13 && dx * dx + dy * dy > 13.5 * 13.5) continue;                                     // topo em arco
                char ch = x == 1 || x == 26 || y == 31 ? '1' : (y < 13 && dx * dx + dy * dy > 12.5 * 12.5) ? '1' : x < 4 ? '4' : x > 23 ? '2' : '3';
                c.Force(x, y, ch);
            }
        // três primatas sentados (cabeça redonda virada para cima, ombros, joelhos)
        foreach (int px in new[] { 6, 12, 18 })
        {
            string[] fig = { ".22.", "2532", ".22.", "2332", "2332", "2222" };
            for (int y = 0; y < fig.Length; y++) for (int x = 0; x < 4; x++) if (fig[y][x] != '.') c.Force(px + x, 17 + y, fig[y][x]);
        }
        // a coisa para a qual todos olham: um círculo no alto
        PixelKit.Ellipse(c, 14f, 8f, 3f, 3f, '2'); PixelKit.Ellipse(c, 14f, 8f, 1.6f, 1.6f, '5');
        for (int x = 4; x <= 24; x++) if (H(x, 26, 91) % 2u == 0) c.Force(x, 26, '2');                   // a fenda da linha de base
        MossTop(c, 0, 27, 92);
        for (int x = 1; x < 27; x++) if (H(x, 30, 93) % 2u == 0) { c.Force(x, 30, 'G'); if (H(x, 29, 94) % 2u == 0) c.Force(x, 29, 'g'); }
        return c;
    }

    /// <summary>P-PRIM-08: a cabine escondida da estação: mesa de metal, cadeira, caneca colada pelo musgo, a lista na parede.</summary>
    static PixelCanvas Desk()
    {
        var c = new PixelCanvas(44, 40);
        // o papel na parede: lista de horários, o último riscado à mão
        Box(c, 6, 0, 21, 17, 'w', 'w', 'x');
        for (int r = 0; r < 6; r++) for (int x = 9; x <= 18; x++) if (x != 12 && H(x, r, 101) % 5u != 0) c.Force(x, 3 + r * 2, r == 5 ? 'x' : 'm');
        for (int i = 0; i < 11; i++) c.Force(8 + i, 14 - i / 4, 'q');                                      // o risco
        c.Force(13, 0, 'S'); c.Force(14, 0, 'S');                                                          // o prego
        // mesa
        Box(c, 2, 24, 31, 27, 'm', 'M', 'n');
        Rect(c, 4, 28, 5, 39, 'n'); Rect(c, 28, 28, 29, 39, 'n');
        Rust(c, 3, 25, 30, 26, 'm', 102, 6u);
        // caneca de barro (com musgo prendendo na mesa)
        Box(c, 18, 19, 22, 23, 'B', 'B', 'b'); c.Force(23, 20, 'b'); c.Force(24, 21, 'b'); c.Force(23, 22, 'b');
        c.Force(17, 23, 'G'); c.Force(23, 23, 'G'); c.Force(18, 23, 'g');
        // cadeira (à direita, encosto alto)
        Rect(c, 35, 18, 36, 39, 'n'); Rect(c, 33, 29, 41, 30, 'm'); Rect(c, 40, 31, 41, 39, 'n'); Rect(c, 35, 18, 37, 19, 'M');
        MossTop(c, 0, 43, 103);
        for (int x = 0; x < 44; x++) if (H(x, 39, 104) % 3u == 0) c.Force(x, 39, 'G');
        return c;
    }

    // ============================================================ só cenário

    /// <summary>Poste de luz torto, com a lâmpada quebrada (sem luz) e trepadeira subindo.</summary>
    static PixelCanvas LampPost()
    {
        var c = new PixelCanvas(20, 80);
        for (int y = 12; y < 80; y++)
        {
            int lean = (80 - y) / 22;                                                                       // entorta para a direita no alto
            c.Force(5 + lean, y, 'a'); c.Force(6 + lean, y, 'M'); c.Force(7 + lean, y, 'm'); c.Force(8 + lean, y, 'a');
        }
        Rect(c, 3, 74, 10, 79, 'C'); Rect(c, 3, 74, 10, 74, 'D');                                        // base de concreto
        for (int x = 8; x <= 15; x++) { c.Force(x, 12 - (x - 8) / 3, 'm'); c.Force(x, 13 - (x - 8) / 3, 'a'); }   // o braço
        Box(c, 13, 9, 18, 13, 'n', 'm', 'a'); c.Force(15, 14, 'V'); c.Force(16, 14, 'v');                 // a cúpula quebrada
        Rust(c, 0, 0, 19, 79, 'm', 111, 5u);
        Creeper(c, 7, 30, 78, 112);
        for (int x = 0; x < 20; x++) if (H(x, 79, 113) % 2u == 0) c.Force(x, 79, 'G');
        return c;
    }

    /// <summary>Poste caído, deitado no chão (a cúpula à direita), musgo por cima e capim em volta.</summary>
    static PixelCanvas FallenPost()
    {
        var c = new PixelCanvas(72, 20);
        for (int x = 2; x < 62; x++)
        {
            int y = 12 + (x < 20 ? 0 : (x - 20) / 30);
            c.Force(x, y, 'a'); c.Force(x, y + 1, 'M'); c.Force(x, y + 2, 'm'); c.Force(x, y + 3, 'a');
        }
        Rect(c, 0, 10, 5, 17, 'C'); Rect(c, 0, 10, 5, 10, 'D'); c.Force(0, 17, 'a');                      // pedaço da base
        Box(c, 62, 9, 69, 16, 'n', 'm', 'a'); Rect(c, 64, 15, 66, 15, 'v');
        Rust(c, 0, 0, 71, 19, 'm', 121, 4u);
        MossTop(c, 0, 71, 122);
        for (int x = 0; x < 72; x++)
        {
            int up = (int)(H(x, 0, 123) % 5u);
            for (int y = 19; y > 19 - up; y--) c.Force(x, y, y == 19 ? 'g' : 'G');
        }
        return c;
    }

    /// <summary>Armário de máquina: grades, uma tela morta e UMA luz verde que pisca devagar (acesa/apagada).</summary>
    static PixelCanvas Machine(bool on)
    {
        var c = new PixelCanvas(32, 36);
        Box(c, 1, 4, 30, 35, 'C', 'D', 'c');
        Rect(c, 1, 4, 30, 4, 'a');
        Box(c, 5, 8, 20, 17, 'v', 'V', 'v');                                                                // tela morta
        c.Force(7, 10, 'W'); c.Force(8, 10, 'W'); c.Force(7, 11, 'W');
        for (int y = 21; y <= 31; y += 2) Rect(c, 5, y, 20, y, 'c');                                       // grade
        Rect(c, 24, 21, 27, 31, 'n');                                                                      // a portinhola
        c.Force(25, 10, 'a'); c.Force(26, 10, 'a'); c.Force(25, 11, 'a'); c.Force(26, 11, 'a');
        c.Force(25, 10, on ? 'Y' : 'l'); c.Force(26, 10, on ? 'L' : 'l'); c.Force(25, 11, on ? 'L' : 'l'); c.Force(26, 11, on ? 'L' : 'n');
        if (on) { c.Force(24, 9, 'l'); c.Force(27, 12, 'l'); }
        Rust(c, 2, 5, 29, 34, 'C', 131, 13u);
        MossTop(c, 0, 31, 132);
        for (int x = 0; x < 32; x++) { int d = (int)(H(x, 1, 133) % 4u); for (int y = 4; y < 4 + d; y++) if (c.Get(x, y) != '.') c.Force(x, y, 'G'); }
        Creeper(c, 29, 14, 35, 134);
        return c;
    }

    /// <summary>Janela do prédio soterrado: moldura de concreto, cacos de vidro; atrás, terra e raízes (nada de vão escuro).</summary>
    static PixelCanvas BuriedWindow()
    {
        var c = new PixelCanvas(32, 40);
        Box(c, 0, 0, 31, 39, 'C', 'D', 'c');
        for (int y = 4; y <= 33; y++) for (int x = 4; x <= 27; x++) c.Force(x, y, H(x, y, 141) % 9u == 0 ? 'f' : 'e');
        // raízes atravessando
        int rx = 8; for (int y = 4; y <= 33; y++) { c.Force(rx, y, 'f'); c.Force(rx + 1, y, 'B'); if (y % 5 == 0) rx++; }
        int ry = 14; for (int x = 4; x <= 27; x++) { c.Force(x, ry, 'f'); if (x % 6 == 0) ry++; }
        // cacos de vidro nos cantos
        for (int i = 0; i < 6; i++) { c.Force(4 + i, 4, 'V'); c.Force(4, 4 + i, 'V'); if (i < 4) c.Force(5 + i, 5, 'W'); }
        for (int i = 0; i < 5; i++) { c.Force(27 - i, 33, 'V'); c.Force(27, 33 - i, 'V'); }
        c.Force(5, 5, 'X');
        Rect(c, 2, 34, 29, 35, 'E');
        MossTop(c, 0, 31, 142);
        for (int y = 36; y < 40; y++) for (int x = 1; x < 31; x++) if (H(x, y, 143) % 5u == 0) c.Force(x, y, 'G');
        return c;
    }

    /// <summary>Lance de escada de concreto que sobe para a direita e termina quebrado no ar, com vergalhão.</summary>
    static PixelCanvas BrokenStairs()
    {
        var c = new PixelCanvas(64, 64);
        for (int s = 0; s < 9; s++)
        {
            int x0 = 2 + s * 6, y0 = 58 - s * 6;
            for (int y = y0; y < 64; y++)
                for (int x = x0; x < x0 + 6; x++)
                {
                    if (y > y0 + 14 && x < 2 + s * 6 + (y - y0 - 14)) continue;                            // a laje inclinada por baixo
                    char ch = y == y0 ? 'E' : y == y0 + 1 ? 'D' : x == x0 + 5 ? 'c' : 'C';
                    c.Force(x, y, ch);
                }
        }
        // o topo quebrado: recorta em escadinha irregular e põe o vergalhão
        for (int y = 0; y < 30; y++) for (int x = 48; x < 64; x++) if (H(x, y, 151) % 3u != 0 && c.Get(x, y) != '.') c.Force(x, y, '.');
        for (int i = 0; i < 4; i++) { int x = 50 + i * 3; for (int y = 6 + i * 2; y < 16 + i; y++) c.Force(x, y, i % 2 == 0 ? 'R' : 'S'); }
        Rust(c, 0, 0, 63, 63, 'C', 152, 17u);
        MossTop(c, 0, 63, 153);
        Creeper(c, 10, 34, 63, 154);
        return c;
    }

    // ============================================================ o Mercado dos Primatas (térreo da C3)

    /// <summary>Lata de 5 × 7: aro claro em cima, rótulo colorido; <paramref name="face"/> = o rosto sorrindo (P-PRIM-09).</summary>
    static void Can(PixelCanvas c, int x, int y, char label, bool face)
    {
        Rect(c, x, y, x + 4, y, 'w');
        Rect(c, x, y + 1, x + 4, y + 6, label);
        c.Force(x, y + 1, 'x'); c.Force(x, y + 6, 'n'); c.Force(x + 4, y + 6, 'n');
        for (int yy = y + 1; yy <= y + 6; yy++) c.Force(x + 4, yy, yy == y + 1 ? label : 'n');   // sombra à direita
        if (!face) { Rect(c, x + 1, y + 3, x + 3, y + 3, 'w'); return; }
        c.Force(x + 1, y + 2, 'a'); c.Force(x + 3, y + 2, 'a');                                  // olhos
        c.Force(x + 1, y + 4, 'a'); c.Force(x + 2, y + 5, 'a'); c.Force(x + 3, y + 4, 'a');      // o sorriso
    }

    /// <summary>Uprights e prateleiras de metal: de (x0, y0) a (x1, y1), prateleiras nas linhas <paramref name="shelves"/>.</summary>
    static void ShelfFrame(PixelCanvas c, int x0, int y0, int x1, int y1, int[] shelves)
    {
        Rect(c, x0, y0, x0 + 1, y1, 'n'); c.Force(x0, y0, 'M');
        Rect(c, x1 - 1, y0, x1, y1, 'n'); c.Force(x1 - 1, y0, 'M');
        Rect(c, x0 + 2, y0 + 1, x1 - 2, y1, 'v');                                                   // o fundo escuro
        foreach (int s in shelves) { Rect(c, x0, s, x1, s, 'M'); Rect(c, x0, s + 1, x1, s + 1, 'n'); }
    }

    /// <summary>P-PRIM-09: prateleira cheia de latas iguais, viradas para a frente, todas com o mesmo rosto sorrindo.</summary>
    static PixelCanvas CanShelf()
    {
        var c = new PixelCanvas(40, 40);
        int[] shelves = { 11, 22, 33 };
        ShelfFrame(c, 0, 0, 39, 39, shelves);
        foreach (int s in shelves)
            for (int k = 0; k < 6; k++) Can(c, 3 + k * 6, s - 7, 'o', true);
        Rect(c, 0, 37, 39, 39, 'n');                                                                 // rodapé
        Rust(c, 0, 0, 39, 39, 'n', 401, 11u);
        MossTop(c, 0, 39, 402);
        Creeper(c, 1, 3, 39, 403); Creeper(c, 37, 12, 39, 404);
        for (int x = 2; x < 38; x++) if (H(x, 38, 405) % 3u == 0) c.Force(x, 38, 'G');            // mato no pé
        return c;
    }

    /// <summary>P-PRIM-10: o balcão do caixa com a esteira parada e, à direita, a máquina com a gaveta aberta.</summary>
    static PixelCanvas Checkout()
    {
        var c = new PixelCanvas(44, 28);
        Box(c, 0, 12, 43, 27, 'm', 'M', 'n');                                                       // o balcão
        Rect(c, 2, 10, 27, 12, 'k'); Rect(c, 2, 10, 27, 10, 'K');                                   // a esteira
        for (int x = 4; x <= 26; x += 4) c.Force(x, 11, 'a');
        Box(c, 30, 2, 41, 11, 'C', 'D', 'c');                                                       // a máquina
        Rect(c, 32, 4, 39, 6, 'v'); c.Force(33, 4, 'W');                                            // a telinha morta
        for (int x = 32; x <= 39; x += 2) c.Force(x, 9, 'a');                                       // teclas
        Box(c, 29, 13, 42, 17, 'n', 'm', 'a');                                                      // a gaveta aberta, para fora
        c.Force(31, 14, 'o'); c.Force(32, 14, 'o'); c.Force(34, 15, 'o'); c.Force(35, 14, 'Y');     // os discos
        Rect(c, 37, 14, 40, 15, 'P'); c.Force(38, 14, 'q');                                          // os papéis, colados
        for (int y = 19; y <= 25; y += 3) Rect(c, 3, y, 26, y, 'n');                                // painel do balcão
        Rust(c, 1, 13, 42, 26, 'm', 411, 13u);
        MossTop(c, 0, 43, 412);
        Creeper(c, 2, 14, 27, 413); Creeper(c, 25, 16, 27, 414);
        return c;
    }

    /// <summary>Letreiro de metal pendurado por duas correntes; a da direita partiu e ele pende torto. Letras gastas.</summary>
    static PixelCanvas MarketSign()
    {
        var c = new PixelCanvas(64, 20);
        for (int y = 0; y <= 3; y++) { c.Force(8, y, y % 2 == 0 ? 'M' : 'n'); }                    // corrente inteira
        c.Force(55, 0, 'M'); c.Force(55, 1, 'n');                                                    // o toco da partida
        for (int x = 2; x <= 61; x++)
        {
            int dy = Math.Max(0, (x - 10) / 12);                                                     // cai para a direita
            for (int y = 4 + dy; y <= 13 + dy; y++)
            {
                bool edge = x == 2 || x == 61 || y == 4 + dy || y == 13 + dy;
                c.Force(x, y, edge ? 'a' : y == 5 + dy ? 'P' : 'p');
            }
            // as letras: blocos claros que ninguém lê (7 letras com espaço)
            int k = (x - 6) % 8;
            if (x >= 6 && x <= 58 && k < 5 && (x - 6) / 8 != 3)
                for (int y = 7 + dy; y <= 10 + dy; y++)
                    if (!(k == 2 && y == 8 + dy) && H(x, y, 421) % 7u != 0) c.Force(x, y, (x + y) % 5 == 0 ? 'x' : 'w');
        }
        Rust(c, 3, 5, 60, 18, 'p', 422, 9u);
        MossTop(c, 2, 61, 423);
        // vinhas escorrendo por baixo
        foreach (int x in new[] { 6, 19, 33, 47, 58 })
        {
            int dy = Math.Max(0, (x - 10) / 12), len = 2 + (int)(H(x, 0, 424) % 4u);
            for (int y = 14 + dy; y < Math.Min(20, 14 + dy + len); y++) c.Force(x, y, y % 2 == 0 ? 'g' : 'G');
        }
        return c;
    }

    /// <summary>Gaiola de arame com rodinhas (o carrinho): sem a roda da frente, inclinado, com a trepadeira por dentro.</summary>
    static PixelCanvas Cart()
    {
        var c = new PixelCanvas(32, 22);
        // a cesta: trapézio (mais largo em cima), grade de arame
        for (int y = 3; y <= 13; y++)
        {
            int x0 = 5 + (y - 3) / 3, x1 = 28 - (y - 3) / 4;
            for (int x = x0; x <= x1; x++)
            {
                bool rim = y == 3 || y == 13 || x == x0 || x == x1;
                if (rim) c.Force(x, y, y == 3 ? 'M' : 'm');
                else if ((x - x0) % 3 == 0 || (y - 3) % 3 == 0) c.Force(x, y, 'n');
            }
        }
        Rect(c, 1, 2, 5, 2, 'M'); c.Force(1, 3, 'm'); c.Force(1, 4, 'm');                         // a alça
        Rect(c, 8, 14, 27, 15, 'n');                                                                 // o chassi
        Rect(c, 9, 16, 9, 18, 'm'); Rect(c, 25, 16, 25, 17, 'm');                                    // pés
        Rect(c, 8, 19, 10, 21, 'a'); c.Force(9, 20, 'm');                                            // a roda que sobrou
        Rect(c, 24, 18, 26, 18, 'R');                                                                // sem roda: só ferrugem
        Rust(c, 5, 3, 28, 15, 'n', 431, 6u);
        Creeper(c, 16, 4, 21, 432); Creeper(c, 22, 8, 21, 433);
        for (int x = 3; x < 30; x++) if (H(x, 21, 434) % 3u == 0) c.Force(x, 21, 'G');
        return c;
    }

    /// <summary>Gôndola de metal com poucas latas (o resto caiu ou sumiu); vinhas subindo pelos lados e caindo de cima.</summary>
    static PixelCanvas Gondola()
    {
        var c = new PixelCanvas(48, 40);
        int[] shelves = { 11, 22, 33 };
        ShelfFrame(c, 0, 0, 47, 39, shelves);
        char[] labels = { 'q', 'u', 'o' };
        foreach (int s in shelves)
            for (int k = 0; k < 7; k++)
            {
                uint h = H(k, s, 441);
                if (h % 3u == 0) continue;                                                           // vão: caiu ou sumiu
                if (h % 5u == 0) { Rect(c, 3 + k * 6, s - 3, 9 + k * 6, s - 1, labels[k % 3]); c.Force(3 + k * 6, s - 3, 'w'); continue; } // deitada
                Can(c, 3 + k * 6, s - 7, labels[(int)(h % 3u)], false);
            }
        Rect(c, 0, 37, 47, 39, 'n');
        Rust(c, 0, 0, 47, 39, 'n', 442, 10u);
        MossTop(c, 0, 47, 443);
        Creeper(c, 1, 2, 39, 444); Creeper(c, 46, 6, 39, 445); Creeper(c, 22, 14, 39, 446);
        for (int x = 4; x <= 44; x += 9) { int len = 3 + (int)(H(x, 1, 447) % 6u); for (int y = 1; y < len; y++) c.Force(x, y, y % 2 == 0 ? 'g' : 'G'); }
        return c;
    }

    /// <summary>Gôndola caída de lado (as prateleiras viram paredes), latas espalhadas no chão e o mato tomando tudo.</summary>
    static PixelCanvas FallenGondola()
    {
        var c = new PixelCanvas(56, 24);
        Rect(c, 2, 8, 53, 9, 'n'); Rect(c, 2, 8, 53, 8, 'M');                                      // a lateral, agora em cima
        Rect(c, 2, 19, 53, 20, 'n');                                                                 // a outra, no chão
        Rect(c, 3, 10, 52, 18, 'v');                                                                 // o fundo
        foreach (int x in new[] { 2, 18, 35, 52 }) { Rect(c, x, 10, x + 1, 18, 'M'); c.Force(x + 1, 18, 'n'); } // prateleiras de pé
        char[] labels = { 'u', 'o', 'q' };
        for (int k = 0; k < 8; k++)                                                                  // latas rolaram para fora
        {
            int x = 1 + (int)(H(k, 0, 451) % 50u), y = 21;
            Rect(c, x, y, x + 3, y + 2, labels[k % 3]); c.Force(x, y, 'w'); c.Force(x + 3, y + 2, 'n');
        }
        Rust(c, 2, 8, 53, 20, 'n', 452, 7u);
        MossTop(c, 0, 55, 453);
        Creeper(c, 10, 2, 22, 454); Creeper(c, 44, 4, 22, 455); Creeper(c, 27, 9, 22, 456);
        for (int x = 0; x < 56; x++) if (H(x, 23, 457) % 2u == 0) c.Force(x, 23, H(x, 23, 458) % 3u == 0 ? 'H' : 'G');
        return c;
    }

    /// <summary>Vinhas penduradas do teto (o topo no teto): um tufo de musgo e fios de comprimentos diferentes, com folhas.</summary>
    static PixelCanvas HangingVines()
    {
        var c = new PixelCanvas(32, 40);
        for (int x = 0; x < 32; x++) { c.Force(x, 0, 'g'); if (H(x, 0, 461) % 2u == 0) c.Force(x, 1, 'G'); }
        foreach (int x0 in new[] { 3, 9, 14, 20, 26, 30 })
        {
            int len = 10 + (int)(H(x0, 1, 462) % 28u), x = x0;
            for (int y = 1; y < len && y < 40; y++)
            {
                c.Force(x, y, y % 3 == 0 ? 'G' : 'g');
                if (y % 5 == 2) { c.Force(x - 1, y, 'G'); c.Force(x + 1, y + 1, 'H'); }                // folhas
                if (y % 7 == 0) x += H(x, y, 463) % 2u == 0 ? 1 : -1;
                x = Math.Max(0, Math.Min(31, x));
            }
        }
        return c;
    }

    /// <summary>Relógio da estação pendurado do teto por uma haste, parado (ponteiros nas 4 e 10).</summary>
    static PixelCanvas Clock()
    {
        var c = new PixelCanvas(24, 40);
        Rect(c, 11, 0, 12, 17, 'm'); c.Force(11, 0, 'a'); c.Force(12, 0, 'a'); c.Force(11, 9, 'R');
        PixelKit.Ellipse(c, 12f, 28f, 11f, 11f, 'a');
        PixelKit.Ellipse(c, 12f, 28f, 10f, 10f, 'm');
        PixelKit.Ellipse(c, 12f, 28f, 8.5f, 8.5f, 'w');
        for (int k = 0; k < 12; k++)
        {
            double a = k * Math.PI / 6;
            c.Force((int)Math.Round(12 + Math.Sin(a) * 7.5 - 0.5), (int)Math.Round(28 - Math.Cos(a) * 7.5 - 0.5), 'n');
        }
        foreach (var p in PixelKit.Line(12, 28, 7, 24)) c.Force(p.X, p.Y, 'a');                             // ponteiro curto (10)
        foreach (var p in PixelKit.Line(12, 28, 17, 31)) c.Force(p.X, p.Y, 'a');                            // ponteiro longo (4)
        c.Force(5, 23, 'W'); c.Force(6, 22, 'W'); c.Force(7, 21, 'X');                                     // luar no vidro
        for (int y = 18; y < 40; y++) for (int x = 0; x < 24; x++) if (c.Get(x, y) == 'w' && H(x, y, 161) % 13u == 0) c.Force(x, y, 'x');
        MossTop(c, 0, 23, 162);
        return c;
    }

    // ============================================================ Isyathy de longe

    /// <summary>Dois olhos amarelo-esverdeados com pupila em fenda: 0 abertos, 1 meio fechados, 2 fechados.</summary>
    static PixelCanvas Eyes(int stage)
    {
        var c = new PixelCanvas(40, 12);
        foreach (int ex in new[] { 8, 30 })
            for (int y = 0; y < 12; y++)
                for (int x = ex - 7; x <= ex + 7; x++)
                {
                    double dx = (x + 0.5 - ex) / 7.0, dy = (y + 0.5 - 6) / (stage == 0 ? 4.5 : stage == 1 ? 2.2 : 0.6);
                    if (dx * dx + dy * dy > 1.0) continue;
                    bool rim = dx * dx + dy * dy > 0.6;
                    char ch = stage == 2 ? 'j' : Math.Abs(x + 0.5 - ex) < 1.0 ? 'i' : rim ? 'z' : 'y';
                    c.Force(x, y, ch);
                }
        return c;
    }

    /// <summary>
    /// Tubo de corpo de serpente ao longo de pontos (cx, cy, raio): luar em cima, ventre embaixo, escamas em
    /// losango no meio. Pinta disco por disco (os de depois por cima).
    /// </summary>
    static void Body(PixelCanvas c, List<double[]> pts, int seed, bool belly)
    {
        // 1) preenche disco por disco guardando de qual ponto veio cada pixel (os de depois ficam por cima)
        var owner = new int[c.W, c.H];
        for (int x = 0; x < c.W; x++) for (int y = 0; y < c.H; y++) owner[x, y] = c.Get(x, y) == '.' ? -1 : int.MinValue;
        for (int k = 0; k < pts.Count; k++)
        {
            double cx = pts[k][0], cy = pts[k][1], r = pts[k][2];
            for (int y = (int)(cy - r) - 1; y <= (int)(cy + r) + 1; y++)
                for (int x = (int)(cx - r) - 1; x <= (int)(cx + r) + 1; x++)
                {
                    if (!c.In(x, y)) continue;
                    double dx = x + 0.5 - cx, dy = y + 0.5 - cy, d2 = dx * dx + dy * dy;
                    if (d2 > r * r) continue;
                    double v = dy / r;                                                                     // -1 em cima, 1 embaixo
                    char ch = v < -0.6 ? 'J'
                            : v > 0.5 ? (belly ? 'U' : 'j')
                            : ((x + 2 * y) % 6 == 0 || (x - 2 * y + 600) % 6 == 0) ? 'i' : 'j';
                    c.Force(x, y, ch);
                    owner[x, y] = k;
                }
        }
        // 2) contorno: na borda da silhueta e onde uma volta passa por cima de outra (pontos distantes no corpo)
        var edge = new List<Point>();
        for (int x = 0; x < c.W; x++)
            for (int y = 0; y < c.H; y++)
            {
                int o = owner[x, y];
                if (o < 0) continue;
                foreach (var d in new[] { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) })
                {
                    int nx = x + d.X, ny = y + d.Y;
                    int n = c.In(nx, ny) ? owner[nx, ny] : -1;
                    if (n == -1 || (n >= 0 && o - n > 40)) { edge.Add(new Point(x, y)); break; }
                }
            }
        foreach (var p in edge) c.Force(p.X, p.Y, 'i');
        // brilho de escamas (luar) espalhado na faixa de cima
        foreach (var p in pts)
            if (H((int)p[0], (int)p[1], seed) % 7u == 0) c.Set((int)p[0], (int)(p[1] - p[2] * 0.7), 'T');
    }

    /// <summary>Um trecho do corpo em S passando atrás das copas: só a silhueta escura, poucas escamas à luz.</summary>
    static PixelCanvas Silhouette()
    {
        var c = new PixelCanvas(192, 40);
        var pts = new List<double[]>();
        for (double x = 4; x <= 188; x += 0.75)
            pts.Add(new[] { x, 20 + Math.Sin(x / 192.0 * Math.PI * 2.4) * 8, 9 - Math.Abs(x - 96) / 96.0 * 3 });
        Body(c, pts, 171, false);
        return c;
    }

    /// <summary>
    /// O pescoço que espia entre as árvores do fundo (96 × 80): sobe de baixo (o resto do corpo fica escondido) e curva
    /// para a direita até a cabeça, de perfil. <paramref name="sway"/> 0..2 = o alto do pescoço inclinado para a
    /// direita; <paramref name="eyes"/> 0 fechados, 1 meio, 2 abertos.
    /// </summary>
    static PixelCanvas Peek(int sway, int eyes, bool tongue)
    {
        var c = new PixelCanvas(96, 80);
        var pts = new List<double[]>();
        double lean = sway * 4.0;
        double hx = 0, hy = 0;
        for (double t = 0; t <= 1.0; t += 0.004)
        {
            // de (26, 82) subindo e curvando: a parte de cima inclina com o "sway"
            double x = 26 + Math.Sin(t * Math.PI * 0.9) * 22 + t * t * (24 + lean);
            double y = 82 - t * 62 + t * t * 10;
            double r = 8.5 - t * 2.0;
            pts.Add(new[] { x, y, r });
            hx = x; hy = y;
        }
        Body(c, pts, 251 + sway, true);
        // a cabeça de perfil, virada para a direita (focinho comprido), por cima do fim do pescoço
        double cx = hx + 9, cy = hy - 1;
        for (int y = (int)cy - 8; y <= (int)cy + 8; y++)
            for (int x = (int)cx - 14; x <= (int)cx + 14; x++)
            {
                double dx = (x + 0.5 - cx) / 14.0, dy = (y + 0.5 - cy) / (7.0 - Math.Max(0, x + 0.5 - cx) * 0.18);
                if (dx * dx + dy * dy > 1.0) continue;
                double d = dx * dx + dy * dy;
                c.Force(x, y, d > 0.8 ? 'i' : dy < -0.35 ? 'J' : dy > 0.45 ? 'U' : 'j');
            }
        // sobrancelha de escamas e o olho (fenda): 0 fechado (risco), 1 meio, 2 aberto e aceso
        int ex = (int)cx + 3, ey = (int)cy - 2;
        c.Force(ex - 3, ey - 2, 'i'); c.Force(ex - 2, ey - 3, 'i'); c.Force(ex - 1, ey - 3, 'i'); c.Force(ex, ey - 3, 'i'); c.Force(ex + 1, ey - 2, 'i');
        if (eyes == 0) { for (int x = ex - 2; x <= ex + 1; x++) c.Force(x, ey, 'i'); }
        else
        {
            for (int x = ex - 2; x <= ex + 1; x++) { c.Force(x, ey, 'y'); if (eyes == 2) { c.Force(x, ey - 1, 'z'); c.Force(x, ey + 1, 'z'); } }
            c.Force(ex, ey, 'i'); if (eyes == 2) c.Force(ex, ey - 1, 'i');                    // a pupila em fenda
        }
        // narina e boca
        c.Force((int)cx + 12, (int)cy - 1, 'i');
        for (int x = (int)cx - 2; x <= (int)cx + 12; x++) c.Force(x, (int)cy + 3, 'i');
        if (tongue)   // a língua bifurcada saindo da ponta do focinho
        {
            int tx = (int)cx + 14, ty = (int)cy + 2;
            for (int i = 0; i < 6; i++) c.Force(tx + i, ty, 'q');
            c.Force(tx + 6, ty - 1, 'q'); c.Force(tx + 7, ty - 2, 'q'); c.Force(tx + 6, ty + 1, 'q'); c.Force(tx + 7, ty + 2, 'q');
        }
        return c;
    }

    // ============================================================ prévia

    static void Preview(string path)
    {
        const int S = 2;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var sheet = new Bitmap(760 * S, 300 * S))
        using (var g = Graphics.FromImage(sheet))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.Clear(ColorTranslator.FromHtml("#161a38"));
            int x = 4, y = 4, rowH = 0;
            foreach (var n in Names)
            {
                string p = Path.Combine(s_dir, n + ".png");
                if (!File.Exists(p)) continue;
                using (var tmp = new Bitmap(p))
                {
                    if (x + tmp.Width > 756) { x = 4; y += rowH + 6; rowH = 0; }
                    g.DrawImage(tmp, x * S, y * S, tmp.Width * S, tmp.Height * S);
                    x += tmp.Width + 6; rowH = Math.Max(rowH, tmp.Height);
                }
            }
            sheet.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
