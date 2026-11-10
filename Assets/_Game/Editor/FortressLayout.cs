using System.Collections.Generic;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// A Fortaleza (Cidadela do Sol Eterno) como DADOS: retângulos de tiles e objetos com posição em tiles.
    /// O <c>FortressSetup</c> monta a cena a partir daqui, e <c>Tools/LevelPreview</c> desenha o mapa inteiro num PNG
    /// para conferir pulos e caminhos sem abrir o Unity. Fichas das salas e checagem anti-softlock:
    /// Docs/Level/fortaleza-mvp.md.
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell para a prévia).
    /// Convenção: "chão com topo em Y" = tiles sólidos até Y-1; objetos ficam em y = Y.
    /// Métricas (Docs/Level/metricas.md): pulo 4 (desenhar degraus ≤ 3), vão ≤ 4, agarrar borda 5,
    /// chaminé de pulo de parede = 3 tiles de largura, passagem baixa = 1 tile.
    /// </summary>
    public static class FortressLayout
    {
        /// <summary>Suba quando mudar o mapa: o Unity pergunta se quer gerar a cena de novo (FortressSetup).</summary>
        public const int Version = 8;

        public enum Map { Ground, Climbable, Background }

        public class TileRect
        {
            public Map Map; public bool Clear; public int X0, Y0, X1, Y1; public string Note;
        }

        /// <summary>
        /// Rampa (triângulo de colisão). Base em Y0, de X0 a X0+W; o lado alto (Y0+H) fica à direita se RisesRight.
        /// Andáveis: 1:1 e 1:2 (até 46°). Encoste o lado alto num bloco de tiles com topo em Y0+H.
        /// </summary>
        public class SlopeRect
        {
            public int X0, Y0, W, H; public bool RisesRight; public string Note;
        }

        public class Obj
        {
            public string Kind;      // ver FortressSetup.Place
            public string Name;
            public float X, Y;
            public float W, H;       // tamanho (sombras, gatilhos), largura em tiles (espinhos)
            public float A, B;       // patrulha, trilho da serra, atraso, intervalo
            public int Facing;       // 1 = direita, -1 = esquerda
            public int Value;        // moedas
            public string Id;        // id de save (santuário, parede, moeda), chave de conversa
            public string Text;      // placa, nome do santuário, tipo de enfeite
            public string Link;      // nome do objeto que o gatilho dispara
        }

        // ------------------------------------------------------------ constantes

        public const float SpawnX = 2.5f, SpawnY = 60f;      // alto da montanha
        public const int CliffTop = 60;                       // topo do penhasco do início
        public const int WallWalk = 43, BellHall = 72, SealFloor = 101; // andares do castelo

        // Câmara do Selo (BossSetup.PlaceArena)
        public const float ArenaMinX = 146f, ArenaMaxX = 177f, ArenaFloor = SealFloor, ArenaCeiling = 9f;
        public const float ArenaEntryGateX = 145.5f, ArenaExitGateX = 177.5f, BossX = 166f, TomeX = 181.5f;
        public const float SealSanctuaryX = 137.5f;

        /// <summary>Limites do mapa (tiles) para a prévia e para o fundo.</summary>
        public const int MinX = -18, MaxX = 254, MinY = -12, MaxY = 116;

        // ----------------------------------------------------------------- tiles

        public static List<TileRect> Tiles()
        {
            var t = new List<TileRect>();
            // chão base de todo o mapa (topo em 0) e paredes de limite
            G(t, -18, -12, 254, -1, "chão base");
            G(t, -18, -12, -15, 90, "rocha à esquerda");
            G(t, 251, -12, 254, 40, "rocha à direita");

            // 1) Montanha: penhasco com topo em 60 (x -14..12); parede de rocha atrás do início
            G(t, -14, 0, 12, CliffTop - 1, "penhasco");
            G(t, -14, CliffTop, -11, 90, "rocha atrás do início");
            // Morrinho de 1 tile atrás do início (relevo natural; a corrida até o salto, x 4..12, continua plana).
            // A rampa 1:2 de x -7 a -5 sobe até ele (ver Slopes).
            G(t, -10, CliffTop, -8, CliffTop, "morrinho atrás do início (topo 61)");
            // Lagoa: bacia de 1 tile de fundo no pé do penhasco, onde a cachoeira cai e o gato pousa.
            // A parede esquerda é o próprio penhasco; a margem direita é uma rampa 1:1 (x 22, ver Slopes): sai-se andando.
            C(t, 13, -1, 22, -1, "bacia da lagoa (fundo em -1)");

            // 2) Trilha: pedras de 2 e 3, fosso de 4 com espinhos no fundo (2 tiles abaixo)
            G(t, 26, 0, 28, 1, "pedra de 2");
            G(t, 31, 0, 33, 2, "pedra de 3");
            C(t, 37, -2, 40, -1, "fosso de 4");

            // 3) Pátio de treino: arquibancada (passagem baixa de 1 tile por baixo), torre de vigia
            G(t, 50, 1, 55, 6, "arquibancada (topo 7; passagem baixa embaixo)");
            V(t, 55, 1, 55, 6, "vinha atrás da arquibancada (sobe para pegar o atirador de costas)");
            // Torre de escalada dos escudeiros: duas colunas com vinhas por dentro (vão de 3 = pulo de parede);
            // entra-se pela porta baixa da coluna esquerda, sai-se pelo topo da direita (cai atrás do guarda)
            G(t, 88, 2, 89, 14, "coluna esquerda (porta de 2 embaixo)");
            V(t, 89, 2, 89, 14, "vinha da coluna esquerda");
            G(t, 93, 0, 94, 18, "coluna direita (topo 19)");
            // Fundos sem colisão para nada flutuar no céu: as tábuas atrás/embaixo da arquibancada e a parede
            // de trás da torre de escalada (entre as colunas; a porta de baixo da coluna esquerda vira porta de verdade)
            B(t, 50, 0, 55, 6, "fundo de tábuas da arquibancada");
            B(t, 88, 0, 94, 18, "parede de trás da torre de escalada");
            B(t, 112, 0, 118, 3, "fundo do estábulo (atrás da porta rachada não pode aparecer o céu)");
            V(t, 93, 6, 93, 18, "vinha da coluna direita");

            // 4) Escalada: três muralhas em degrau com vinhas na face esquerda
            G(t, 112, 0, 119, 14, "muralha 1 (topo 15)");
            G(t, 120, 0, 127, 28, "muralha 2 (topo 29)");
            G(t, 128, 0, 135, WallWalk - 1, "muralha 3 (topo 43)");
            C(t, 112, 0, 118, 3, "estábulo (porta rachada em x 112)");
            V(t, 112, 4, 112, 14, "vinha 1");
            V(t, 120, 16, 120, 28, "vinha 2");
            V(t, 128, 30, 128, 42, "vinha 3");

            // 5) O castelo: maciço com as salas escavadas
            B(t, 136, -2, 234, 112, "fundo do interior");
            G(t, 136, 0, 234, 112, "maciço do castelo");
            C(t, 136, WallWalk, 210, 51, "galeria da muralha (andar 43, teto 52)");
            G(t, 154, 47, 166, 47, "passarela (topo 48)");
            G(t, 181, 47, 184, 47, "prateleira do atirador (topo 48, agarrar a borda)");
            V(t, 153, 45, 153, 47, "hera da passarela");

            // 6) Torre do Sino: chaminé de 3 (x 208..210) de 43 até o salão
            C(t, 208, 52, 210, BellHall - 1, "chaminé do sino");
            V(t, 211, 44, 211, 54, "vinha do sino (direita)");
            V(t, 207, 52, 207, 62, "vinha do sino (esquerda)");
            V(t, 211, 60, 211, BellHall - 1, "vinha do sino (direita, topo)");

            // 7) Salão dos Votos (à direita) e Galeria das Armadilhas (à esquerda), andar 72, teto 82
            C(t, 140, BellHall, 216, 81, "salão e galeria");
            G(t, 181, 74, 184, 74, "plataforma do salão 1 (topo 75)");
            G(t, 190, 74, 193, 74, "plataforma do salão 2 (topo 75)");
            G(t, 197, 77, 202, 77, "sacada do atirador (topo 78)");
            G(t, 161, 74, 162, 74, "plataforma do fogo 1 (topo 75)");
            G(t, 165, 74, 166, 74, "plataforma do fogo 2");
            G(t, 169, 74, 170, 74, "plataforma do fogo 3");
            G(t, 173, 74, 174, 74, "plataforma do fogo 4");
            G(t, 153, 74, 156, 74, "plataforma sobre a serra");
            C(t, 145, BellHall - 2, 150, BellHall - 1, "fosso de espinhos da galeria");

            // 8) Torre do Selo: chaminé de 3 (x 140..142) de 72 até a câmara
            C(t, 140, 82, 142, SealFloor - 1, "chaminé do selo");
            V(t, 139, BellHall + 1, 139, 86, "vinha do selo (esquerda)");
            V(t, 143, 84, 143, 94, "vinha do selo (direita)");
            V(t, 139, 92, 139, SealFloor - 1, "vinha do selo (esquerda, topo)");

            // 9) Câmara do Selo (arena 146..177, teto em 110), sala do tomo, e a passagem dos fundos
            C(t, 136, SealFloor, 222, 109, "andar da câmara");
            G(t, 144, 105, 145, 109, "parede acima da grade de entrada");
            G(t, 177, 105, 178, 109, "parede acima da grade do tomo");
            G(t, 179, 107, 184, 109, "teto da sala do tomo");
            G(t, 185, 105, 185, 109, "parede acima da porta rachada do tomo");
            G(t, 186, 106, 222, 109, "teto da passagem dos fundos");
            G(t, 200, 105, 200, 105, "acima da parede rachada do cofre do guardião");

            // 10) Poço dos fundos (x 218..222, 5 de largura) com patamares; os buracos alternam de lado para cada queda
            //     parar no patamar seguinte, e o lado da direita (x 221..222) é sempre chão: dali se quebra a porta do cofre
            C(t, 218, 0, 222, SealFloor - 1, "poço dos fundos");
            G(t, 218, SealFloor - 1, 219, SealFloor - 1, "chão da passagem sobre o poço");
            G(t, 221, SealFloor - 1, 222, SealFloor - 1, "chão da passagem sobre o poço (buraco em 220)");
            G(t, 219, BellHall - 1, 222, BellHall - 1, "patamar 72 (buraco em 218)");
            G(t, 218, WallWalk - 1, 219, WallWalk - 1, "patamar 43");
            G(t, 221, WallWalk - 1, 222, WallWalk - 1, "patamar 43 (buraco em 220)");
            G(t, 219, 14, 222, 14, "patamar 15 (buraco em 218)");
            C(t, 223, BellHall, 230, BellHall + 3, "cofre do patamar 72");
            C(t, 223, WallWalk, 230, WallWalk + 3, "cofre do patamar 43");
            C(t, 223, 15, 230, 18, "cofre do patamar 15");
            C(t, 223, 0, 250, 12, "portão dos fundos e estrada");
            G(t, 223, 4, 223, 12, "acima do portão dos fundos: só quebrando se passa");
            return t;
        }

        // ---------------------------------------------------------------- rampas

        /// <summary>Rampas do mapa (relevo natural; Etapa 2 do plano de cenário). Andáveis: 1:1 e 1:2.</summary>
        public static List<SlopeRect> Slopes()
        {
            var s = new List<SlopeRect>();
            // 1) Montanha: sobe do chão do penhasco (60) para o morrinho (61), lado alto à esquerda, encostado nele
            S(s, -7, CliffTop, 2, 1, false, "do morrinho");
            // 2) Trilha: margem da lagoa, do fundo da bacia (-1) até o chão (0), lado alto à direita
            S(s, 22, -1, 1, 1, true, "da margem da lagoa");
            return s;
        }

        static void S(List<SlopeRect> s, int x0, int y0, int w, int h, bool risesRight, string note)
        {
            s.Add(new SlopeRect { X0 = x0, Y0 = y0, W = w, H = h, RisesRight = risesRight, Note = note });
        }

        static void G(List<TileRect> t, int x0, int y0, int x1, int y1, string note) { t.Add(new TileRect { Map = Map.Ground, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Note = note }); }
        static void C(List<TileRect> t, int x0, int y0, int x1, int y1, string note) { t.Add(new TileRect { Map = Map.Ground, Clear = true, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Note = note }); }
        static void B(List<TileRect> t, int x0, int y0, int x1, int y1, string note) { t.Add(new TileRect { Map = Map.Background, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Note = note }); }
        /// <summary>Parede escalável: tira o chão do lugar e põe a vinha.</summary>
        static void V(List<TileRect> t, int x0, int y0, int x1, int y1, string note)
        {
            C(t, x0, y0, x1, y1, note);
            t.Add(new TileRect { Map = Map.Climbable, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Note = note });
        }

        // --------------------------------------------------------------- objetos

        public static List<Obj> Objects()
        {
            var o = new List<Obj>();

            // ===== 1) Montanha: alto do penhasco, cachoeira, salto de fé até a lagoa
            Add(o, "trigger", "Chegada", 4f, CliffTop + 1.5f, w: 4f, h: 3f, id: "D-FORT-01", link: "intro_fortaleza");
            Add(o, "sign", "Placa do salto", 9.5f, CliffTop + 2.6f, text: "salto de fé: corra e pule →");
            // Cachoeira: nasce de uma fenda na face do penhasco (topo da água em 55, longe da corrida do salto) e cai
            // colada na face (x 13,0..14,5) até a superfície da lagoa (-0,125). Y = superfície, H = altura até o topo.
            Add(o, "waterfall", "Cachoeira", 13.75f, -0.125f, h: 55.125f);
            Add(o, "pool", "Lagoa", 18f, -1f, w: 10f); // bacia x 13..23, fundo em -1
            Add(o, "trigger", "Lagoa", 18.5f, 1.5f, w: 10f, h: 3f, id: "D-FORT-02", link: "lagoa_fortaleza");

            // ===== 2) Trilha
            Add(o, "lore", "Marco da estrada", 23f, 0f, id: "D-PLACA-10");
            Add(o, "sign", "Placa do pulo", 25f, 2.6f, text: "Espaço: pular");
            Add(o, "coin", "Moeda (pedra alta)", 32f, 3.7f, id: "fort_pedra", value: 1);
            Add(o, "spikes", "Espinhos do fosso", 39f, -2f, w: 4f);
            Add(o, "coin", "Moeda (sobre o fosso)", 39f, 3.2f, id: "fort_fosso", value: 1);

            // ===== 3) Pátio de treino dos guardas
            Add(o, "tripwire", "Fio do pátio", 46.5f, 0f, w: 1.6f, link: "Lançador do pátio");
            Add(o, "launcher", "Lançador do pátio", 49.6f, 1.2f, facing: -1, a: 0f);
            Add(o, "sign", "Placa da arquibancada", 46.5f, 3f, text: "S: agachar (passe por baixo)");
            Add(o, "coin", "Moeda (sob a arquibancada)", 53f, 0.5f, id: "fort_passagem", value: 1);
            Add(o, "archer", "Atirador da arquibancada", 50.6f, 7f, facing: -1);
            Add(o, "lore", "Placa do pátio", 58f, 0f, id: "D-PLACA-01");
            Add(o, "dummy", "Boneco de treino 1", 60.5f, 0f, facing: 1);
            Add(o, "dummy", "Boneco de treino 2", 64.5f, 0f, facing: -1);
            Add(o, "sign", "Placa dos bonecos", 62.5f, 3.4f, text: "bonecos: pelas costas dói mais");
            Add(o, "sentinel", "Instrutor do pátio", 76f, 0f, facing: -1, a: 72f, b: 80f);
            Add(o, "shadow", "Sombra da tenda", 85f, 2.5f, w: 6f, h: 5f);
            Add(o, "bush", "Fardo de feno", 85f, 0f, text: "feno");        // esconderijo: o desenho é um fardo de feno de verdade
            Add(o, "sign", "Placa da torre de escalada", 91f, 3.6f, text: "↑ pule de parede em parede");
            Add(o, "launcher", "Seteira da torre de escalada", 93.4f, 10.2f, facing: -1, a: 1.8f);
            Add(o, "coin", "Moeda (torre de escalada)", 93.5f, 19.6f, id: "fort_torre", value: 1);
            Add(o, "sentinel", "Guarda da torre de vigia", 99f, 0f, facing: 1, a: 95f, b: 103f);
            Add(o, "plate", "Placa de pressão do portão", 104f, 0f, w: 2f, link: "Lanças do portão");
            Add(o, "spears", "Lanças do portão", 105f, 0f);
            Add(o, "sanctuary", "Pátio de treino", 108f, 0f, id: "fortaleza_patio", text: "Pátio de treino");
            Add(o, "wall", "Porta rachada do estábulo", 112.5f, 0f, id: "fortaleza_estabulo");
            Add(o, "sign", "Placa das rachaduras", 110.5f, 5.2f, text: "rachaduras douradas... precisa de força");
            Add(o, "coin", "Moeda do estábulo 1", 115f, 0.6f, id: "fort_estabulo_1", value: 5);
            Add(o, "coin", "Moeda do estábulo 2", 117f, 0.6f, id: "fort_estabulo_2", value: 5);
            Add(o, "lore", "Placa do estábulo", 116f, 0f, id: "D-PLACA-05");

            // ===== 4) Escalada das vinhas
            Add(o, "sign", "Placa das vinhas", 109.5f, 7.5f, text: "W: escalar as vinhas");
            Add(o, "coin", "Moeda (vinha 1)", 111.4f, 10f, id: "fort_hera", value: 1);
            Add(o, "coin", "Moeda (muralha 1)", 116f, 15.6f, id: "fort_muralha1", value: 1);
            Add(o, "launcher", "Seteira da muralha", 119.6f, 16.2f, facing: -1, a: 2.2f);
            Add(o, "sign", "Placa da seteira", 115.5f, 18.3f, text: "flechas: agache");
            Add(o, "sentinel", "Vigia da muralha", 123f, 29f, facing: 1, a: 121f, b: 126f);
            Add(o, "coin", "Moeda (muralha 3)", 132f, WallWalk + 0.6f, id: "fort_muralha3", value: 1);

            // ===== 5) Galeria da muralha (alojamento dos templários)
            Add(o, "sanctuary", "Portão da muralha", 139f, WallWalk, id: "fortaleza_muralha", text: "Portão da muralha");
            Add(o, "lore", "Placa do alojamento", 144f, WallWalk, id: "D-PLACA-02");
            Add(o, "shadow", "Sombra da galeria A", 148f, WallWalk + 2.5f, w: 10f, h: 5f);
            Add(o, "bush", "Caixotes", 150f, WallWalk, text: "caixotes"); // esconderijo dentro do castelo: caixotes, não moita
            Add(o, "sign", "Placa da sombra", 146f, WallWalk + 3.4f, text: "sombra e caixotes: agache para sumir");
            Add(o, "sentinel", "Sentinela da galeria", 159f, WallWalk, facing: -1, a: 155f, b: 164f);
            Add(o, "coin", "Moeda (passarela)", 160f, 48.6f, id: "fort_passarela", value: 1);
            Add(o, "shadow", "Sombra da galeria B", 176f, WallWalk + 2.5f, w: 8f, h: 5f);
            Add(o, "tripwire", "Fio de alarme da galeria", 179f, WallWalk, w: 1.6f, a: 16f);
            Add(o, "archer", "Atirador do alojamento", 183f, 48f, facing: -1);
            Add(o, "sentinel", "Sentinela do alojamento", 190f, WallWalk, facing: -1, a: 185f, b: 195f);
            Add(o, "plate", "Placa de pressão da galeria", 198f, WallWalk, w: 2f, link: "Lanças da galeria");
            Add(o, "spears", "Lanças da galeria", 199f, WallWalk);
            Add(o, "sentinel", "Guarda da torre do sino", 202f, WallWalk, facing: 1, a: 200f, b: 206f);
            Add(o, "coin", "Moeda (pé da torre)", 205.5f, WallWalk + 0.6f, id: "fort_sino_base", value: 1);

            // ===== 6) Torre do Sino
            Add(o, "sign", "Placa da torre", 206f, WallWalk + 4.5f, text: "↑ pule de parede em parede");
            Add(o, "launcher", "Seteira da torre do sino", 211.4f, 57.2f, facing: -1, a: 2.5f);
            Add(o, "saw", "Serra da torre do sino", 209.5f, 64.5f, a: 208.6f, b: 210.4f);
            Add(o, "coin", "Moeda (torre do sino)", 209f, 68f, id: "fort_sino", value: 1);

            // ===== 7) Salão dos Votos e Galeria das Armadilhas (andar 72, para a esquerda)
            Add(o, "sanctuary", "Torre do sino", 214.5f, BellHall, id: "fortaleza_sino", text: "Torre do sino");
            Add(o, "lore", "Placa do juramento", 205f, BellHall, id: "D-PLACA-03");
            Add(o, "sign", "Placa do salão", 208.5f, BellHall + 3.2f, text: "← clique: adagas · por trás dói mais");
            Add(o, "sentinel", "Sentinela do salão A", 200f, BellHall, facing: -1, a: 196f, b: 204f);
            Add(o, "sentinel", "Sentinela do salão B", 189f, BellHall, facing: 1, a: 185f, b: 194f);
            Add(o, "sentinel", "Sentinela do salão C", 179.5f, BellHall, facing: -1, a: 176f, b: 183f);
            Add(o, "coin", "Moeda (plataforma do salão)", 191.5f, 75.6f, id: "fort_salao", value: 1);
            Add(o, "archer", "Atirador da sacada", 201f, 78f, facing: -1);
            Add(o, "sign", "Placa das armadilhas", 176f, BellHall + 4.5f, text: "← cuidado: armadilhas");
            for (int i = 0; i < 6; i++)
                Add(o, "flame", "Jato " + (i + 1), 172.5f - 2f * i, BellHall, a: i * 0.35f);
            Add(o, "saw", "Serra da galeria", 155f, BellHall + 0.55f, a: 152f, b: 158f);
            Add(o, "spikes", "Espinhos da galeria", 148f, BellHall - 2f, w: 6f);
            Add(o, "crumble", "Frágil 1", 146.5f, BellHall - 0.25f);
            Add(o, "crumble", "Frágil 2", 149.5f, BellHall - 0.25f);
            Add(o, "coin", "Moeda (fosso da galeria)", 148f, BellHall + 2.6f, id: "fort_fosso_alto", value: 1);

            // ===== 8) Torre do Selo
            Add(o, "sign", "Placa da torre do selo", 142f, BellHall + 4f, text: "↑ a Torre do Selo");
            Add(o, "launcher", "Seteira da torre do selo", 139.6f, 89.2f, facing: 1, a: 2f);
            Add(o, "saw", "Serra da torre do selo", 141.5f, 90.5f, a: 140.6f, b: 142.4f);
            Add(o, "coin", "Moeda (torre do selo)", 141f, 97f, id: "fort_selo", value: 1);

            // ===== 9) Câmara do Selo (o chefe e o tomo vêm do BossSetup.PlaceArena)
            Add(o, "lore", "Placa da câmara", 143.5f, SealFloor, id: "D-PLACA-04");

            // ===== 10) Depois do Paladino: porta rachada do tomo, passagem, poço e cofres, estrada
            Add(o, "wall", "Porta rachada da sala do tomo", 185.5f, SealFloor, id: "fortaleza_tomo_fundos");
            Add(o, "sign", "Placa da porta do tomo", 183f, SealFloor + 4.6f, text: "rachaduras douradas →");
            Add(o, "sentinel", "Guarda da retaguarda", 192f, SealFloor, facing: -1, a: 188f, b: 197f);
            Add(o, "wall", "Parede rachada do cofre do guardião", 200.5f, SealFloor, id: "fortaleza_cofre_guardiao");
            Add(o, "coin", "Moeda do guardião 1", 202.5f, SealFloor + 0.6f, id: "fort_guardiao_1", value: 5);
            Add(o, "coin", "Moeda do guardião 2", 204.5f, SealFloor + 0.6f, id: "fort_guardiao_2", value: 5);
            Add(o, "lore", "Diário do guardião", 206.5f, SealFloor, id: "D-PLACA-06");
            Add(o, "sign", "Placa do poço", 216f, SealFloor + 3f, text: "↓ desça pelos patamares");
            Add(o, "launcher", "Seteira do poço 1", 217.6f, 58.2f, facing: 1, a: 2.2f);
            Add(o, "launcher", "Seteira do poço 2", 217.6f, 29.2f, facing: 1, a: 2f);
            Vault(o, BellHall, "fortaleza_cofre_72", "fort_cofre72", "D-PLACA-07", "Mural da Marcha Dourada");
            Vault(o, WallWalk, "fortaleza_cofre_43", "fort_cofre43", "D-PLACA-08", "Pedra dos votos");
            Vault(o, 15, "fortaleza_cofre_15", "fort_cofre15", "D-PLACA-09", "Lista dos guardiões");
            Add(o, "wall", "Portão dos fundos", 223.5f, 0f, id: "fortaleza_portao_fundos");
            Add(o, "sign", "Placa da estrada", 238f, 3.2f, text: "estrada para Miaravéu →");
            Add(o, "trigger", "Saída da fortaleza", 245f, 2f, w: 4f, h: 4f, id: "D-FIM", text: "menu");
            return o;
        }

        static void Vault(List<Obj> o, int floor, string wallId, string coinId, string loreKey, string loreName)
        {
            Add(o, "wall", "Parede rachada (" + loreName + ")", 223.5f, floor, id: wallId);
            Add(o, "coin", "Moeda do cofre " + floor + " (1)", 225.5f, floor + 0.6f, id: coinId + "_1", value: 5);
            Add(o, "coin", "Moeda do cofre " + floor + " (2)", 227f, floor + 0.6f, id: coinId + "_2", value: 5);
            Add(o, "lore", loreName, 229f, floor, id: loreKey);
        }

        static void Add(List<Obj> o, string kind, string name, float x, float y, float w = 0f, float h = 0f, float a = 0f, float b = 0f,
                        int facing = 1, int value = 0, string id = "", string text = "", string link = "")
        {
            o.Add(new Obj { Kind = kind, Name = name, X = x, Y = y, W = w, H = h, A = a, B = b, Facing = facing, Value = value, Id = id, Text = text, Link = link });
        }
    }
}
