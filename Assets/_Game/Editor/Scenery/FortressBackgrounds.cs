using System.Collections.Generic;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Conjuntos de fundo da Fortaleza (plano de assets, seção 7 e Etapa 6) como DADOS: cada conjunto é a vista real
    /// de um trecho (de onde o gato olha, a que altura e em que momento) e as zonas dizem qual vale onde.
    ///
    /// Âncora de uma camada: onde fica o centro do desenho quando a câmera está nela. Com paralaxe f, o centro aparece
    /// na tela deslocado (âncora − câmera) × f; por isso as âncoras abaixo foram escolhidas a partir de uma câmera de
    /// referência por conjunto (o horizonte, a montanha do início e a Cidadela na posição certa da tela).
    ///
    /// Zonas: a primeira que contém o gato vale; enquanto ele continua dentro da zona atual, nada troca. As zonas
    /// vizinhas se sobrepõem 2–4 tiles e mudam em soleiras naturais (queda do penhasco, pé da vinha, entrada da
    /// galeria, porta do tomo, poço).
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell para a prévia).
    /// </summary>
    public static class FortressBackgrounds
    {
        public class Layer
        {
            public string Id;
            public float AnchorX, AnchorY;
            /// <summary>Paralaxe [x, y] diferente da do manifesto (0 = a do manifesto).</summary>
            public float FactorX, FactorY;
            /// <summary>Ordem de desenho diferente da da camada do manifesto (0 = a do manifesto).</summary>
            public int Order;
            /// <summary>
            /// Céu que aparece aos poucos com a altura da câmera (0 em <see cref="BlendFromY"/>, inteiro em <see cref="BlendToY"/>),
            /// por cima do céu de baixo: a luz muda com a subida sem troca de conjunto. Iguais = sempre inteiro.
            /// </summary>
            public float BlendFromY, BlendToY;
        }

        public class Set
        {
            public string Key, Name;
            public List<Layer> Layers = new List<Layer>();
        }

        public class Zone
        {
            public string Set;
            public float X0, Y0, X1, Y1;
            public bool Contains(float x, float y) { return x >= X0 && x <= X1 && y >= Y0 && y <= Y1; }
        }

        /// <summary>O conjunto em que a cena começa (o gato nasce no alto da montanha).</summary>
        public const string Start = "A";

        /// <summary>Aetherion, a torre dos magos corrompida (marco distante; os efeitos ficam no AetherionFx).</summary>
        public const string AetherionId = "bg_ext_aetherion", AetherionNightId = "bg_ext_aetherion_noite";
        /// <summary>Atrás da cordilheira (-90) e na frente do céu (-100 e -99).</summary>
        public const int AetherionOrder = -95;

        public static List<Set> Sets()
        {
            var s = new List<Set>();
            // A. Alto da montanha (área 1): pôr do sol, sol tocando o horizonte. Câmera de referência (2, 64).
            // Sem a Cidadela ao longe (pedido do usuário, 12/11/2026: daqui ela já está perto, só falta cair): no lugar,
            // Aetherion, a torre dos magos corrompida, à direita da tela (a área final do jogo).
            Add(s, "A", "Alto da montanha",
                L("bg_ceu_por_do_sol", 0, 0),
                Lo(AetherionId, 152f, 68.2f, 0, 0, AetherionOrder),
                L("bg_ext_cordilheira", -139.7f, 50.7f),
                L("bg_ext_floresta_vale", 2f, 45.7f));
            // V. Vale, muralhas e galeria (áreas 2 a 5): UM conjunto só para todo o exterior de baixo (a galeria vê esse
            // mesmo exterior pela entrada). Nada troca com a altura: o céu do crepúsculo vira a borda do sol aos poucos
            // (câmera de y 10 a 40) e a paralaxe vertical baixa o horizonte. A Cidadela grande à direita. (A montanha do
            // início saiu, pedido do usuário de 12/11/2026: no lugar do penhasco escuro fica a faixa uniforme da floresta.)
            // Câmera de referência (60, 6). (Revisão: com conjuntos separados para trilha, pátio e muralha, subir poucos
            // tiles trocava o ambiente inteiro.)
            Add(s, "V", "Vale, muralhas e galeria",
                L("bg_ceu_crepusculo", 0, 0),
                Lb("bg_ceu_borda_do_sol", 10f, 40f),
                L("bg_ext_cordilheira", 60f, 2.7f),
                L("bg_cid_fachada_perto", 106f, 3f),
                Lo("bg_ext_morros_pinheiros", 60f, -9f, 0.35f, 0.4f, -70));   // borda da floresta, na frente da base do castelo
            // F. Andar 72 (salão, armadilhas, torres): meio sol, horizonte baixo, mar de copas embaixo.
            Add(s, "F", "Interior alto",
                L("bg_ceu_meio_sol", 0, 0),
                Lo(AetherionId, 308f, 72.5f, 0, 0, AetherionOrder),
                L("bg_ext_cordilheira", 170f, 55f),
                L("bg_ext_copas_de_cima", 170f, 51.7f));       // o topo das copas fica abaixo do horizonte
            // G. Topo (câmara): o último raio e Aetherion ao longe (a montanha do início saiu, pedido do usuário).
            Add(s, "G", "Topo",
                L("bg_ceu_ultimo_raio", 0, 0),
                Lo(AetherionId, 293f, 101.5f, 0, 0, AetherionOrder),
                L("bg_ext_cordilheira", 150f, 84f),
                L("bg_ext_copas_de_cima", 150f, 81.7f));
            // H. Noite e saída (área 10, depois da maldição): lua, cordilheira escura e Miaravéu na estrada.
            Add(s, "H", "Noite e saída",
                L("bg_ceu_noite", 0, 0),
                Lo(AetherionNightId, 63f, 22.5f, 0, 0, AetherionOrder),    // à esquerda: Miaravéu fica à direita
                L("bg_ext_cordilheira_noite", 240f, 5f),
                L("bg_mia_telhados_horizonte", 230f, 1.7f));
            return s;
        }

        public static List<Zone> Zones()
        {
            var z = new List<Zone>();
            Z(z, "A", -20f, 26f, 30f, 130f);     // alto da montanha e a primeira metade da queda
            Z(z, "V", 8f, -6f, 217f, 62f);       // lagoa, trilha, pátio, muralhas e galeria (troca só na queda do penhasco)
            Z(z, "H", 183f, 95f, 262f, 130f);    // depois da porta do tomo (já é noite)
            Z(z, "H", 215f, -6f, 262f, 99f);     // o poço dos fundos e a estrada
            Z(z, "G", 133f, 95f, 185f, 130f);    // a câmara do selo
            Z(z, "F", 133f, 58f, 217f, 97f);     // salão, armadilhas, torres
            return z;
        }

        public class Window
        {
            /// <summary>"seteira" (só céu), "janela" (céu e cordilheira) ou "janela_grande" (a câmara: o último raio).</summary>
            public string Kind;
            /// <summary>Centro da moldura, em tiles.</summary>
            public float X, Y;
            public string Name;
        }

        /// <summary>
        /// Janelas dos interiores (7.5): recortes na parede do fundo que deixam ver o conjunto de fundo daquela altura.
        /// Sempre no alto da parede, longe da passarela, das plataformas, das sombras jogáveis e do atirador da sacada.
        /// </summary>
        public static List<Window> Windows()
        {
            const int WallWalk = FortressLayout.WallWalk, BellHall = FortressLayout.BellHall, SealFloor = FortressLayout.SealFloor;
            var w = new List<Window>();
            // galeria (andar 43): a borda do sol
            W(w, "janela", 141.5f, WallWalk + 5.6f, "Janela da entrada da galeria");
            W(w, "seteira", 167.6f, WallWalk + 5.8f, "Seteira entre a passarela e a alcova");
            W(w, "janela", 191f, WallWalk + 5.6f, "Janela do alojamento");
            W(w, "janela", 205f, WallWalk + 5.6f, "Janela do pé da torre");
            // salão (andar 72): meio sol
            W(w, "janela", 182f, BellHall + 6.6f, "Janela do salão");
            W(w, "janela", 213.5f, BellHall + 6.6f, "Janela sobre o santuário do sino");
            // galeria das armadilhas: só seteiras
            W(w, "seteira", 151.5f, BellHall + 7f, "Seteira das armadilhas 1");
            W(w, "seteira", 160.5f, BellHall + 7f, "Seteira das armadilhas 2");
            W(w, "seteira", 168.5f, BellHall + 7f, "Seteira das armadilhas 3");
            // câmara (o topo): janelas amplas, o último raio
            W(w, "janela_grande", 158.5f, SealFloor + 5.2f, "Janela da câmara 1");
            W(w, "janela_grande", 171.4f, SealFloor + 5.2f, "Janela da câmara 2");
            // fundos (já é noite)
            W(w, "seteira", 210.6f, SealFloor + 2.8f, "Seteira dos fundos");
            return w;
        }

        static void W(List<Window> w, string kind, float x, float y, string name)
        {
            w.Add(new Window { Kind = kind, X = x, Y = y, Name = name });
        }

        /// <summary>O conjunto de um ponto: o atual, se ainda vale; senão a primeira zona que contém o ponto.</summary>
        public static string Pick(List<Zone> zones, string current, float x, float y)
        {
            if (current != null)
                foreach (var zone in zones) if (zone.Set == current && zone.Contains(x, y)) return current;
            foreach (var zone in zones) if (zone.Contains(x, y)) return zone.Set;
            return current;
        }

        static Layer L(string id, float ax, float ay) { return new Layer { Id = id, AnchorX = ax, AnchorY = ay }; }
        static Layer Lb(string id, float fromY, float toY) { return new Layer { Id = id, BlendFromY = fromY, BlendToY = toY, Order = -99 }; }
        static Layer Lo(string id, float ax, float ay, float fx, float fy, int order)
        {
            return new Layer { Id = id, AnchorX = ax, AnchorY = ay, FactorX = fx, FactorY = fy, Order = order };
        }

        static void Add(List<Set> s, string key, string name, params Layer[] layers)
        {
            s.Add(new Set { Key = key, Name = name, Layers = new List<Layer>(layers) });
        }

        static void Z(List<Zone> z, string set, float x0, float y0, float x1, float y1)
        {
            z.Add(new Zone { Set = set, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1 });
        }
    }
}
