using System.Collections.Generic;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Fundos da Floresta (F5) como DADOS: um conjunto por cena (céu, copas distantes, árvores médias, troncos próximos),
    /// na versão saudável ou na da praga. As camadas quase não se mexem na vertical (paralaxe y de 0,02 a 0,05): as salas
    /// sobem ~100 tiles por cena e a composição da tela (céu em cima, troncos embaixo) fica a mesma em toda sala. A âncora
    /// vertical é o meio das salas da cena.
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell para a prévia).
    /// </summary>
    public static class ForestBackgrounds
    {
        public class Layer
        {
            public string Id;
            public float FactorX, FactorY;
            public int Order;
            /// <summary>Cor da base da camada, continuada para baixo (como o preencher_abaixo do manifesto). "" = nada.</summary>
            public string FillBelow;
            /// <summary>Deriva horizontal contínua (tiles/s): a névoa anda devagar. 0 = parada.</summary>
            public float Drift;
            /// <summary>Altura da âncora (tiles, mundo) no lugar da do meio da cena; NaN = a da cena.</summary>
            public float AnchorY = float.NaN;
        }

        /// <summary>Cenas onde a praga já tomou a mata (o céu tem o véu roxo e os troncos, veias).</summary>
        public static bool Blighted(string scene) { return scene == ForestLayout.E || scene == ForestLayout.F; }

        public static List<Layer> Layers(string scene)
        {
            if (scene == ForestLayout.Road) return RoadLayers();
            bool p = Blighted(scene);
            string s = p ? "_praga" : "";
            var list = new List<Layer>();
            list.Add(new Layer { Id = p ? "bg_flo_ceu_praga" : "bg_flo_ceu_noite", FactorX = 0f, FactorY = 0f, Order = -100, FillBelow = "" });
            list.Add(new Layer { Id = "bg_flo_copas_longe" + s, FactorX = 0.04f, FactorY = 0.02f, Order = -90, FillBelow = p ? "#160f1c" : "#121e1e" });
            list.Add(new Layer { Id = "bg_flo_arvores_medio" + s, FactorX = 0.12f, FactorY = 0.03f, Order = -80, FillBelow = p ? "#120c10" : "#140f0e" });
            // névoa animada em duas alturas: uma atrás dos troncos próximos, outra na frente, andando em sentidos opostos.
            // Paralaxe vertical 0: fica sempre no pé da tela (nas salas altas ela flutuava no meio do ar)
            list.Add(new Layer { Id = "bg_flo_nevoa_longe" + s, FactorX = 0.2f, FactorY = 0f, Order = -70, FillBelow = "", Drift = 0.12f });
            list.Add(new Layer { Id = "bg_flo_troncos_perto" + s, FactorX = 0.28f, FactorY = 0.05f, Order = -60, FillBelow = p ? "#191016" : "#1a1412" });
            list.Add(new Layer { Id = "bg_flo_nevoa_perto" + s, FactorX = 0.34f, FactorY = 0f, Order = -55, FillBelow = "", Drift = -0.2f });
            return list;
        }

        /// <summary>
        /// A Estrada de Soluki continua a noite do fim da Fortaleza (pedido do usuário, 14/11/2026: as duas fases devem
        /// parecer ligadas): o mesmo céu, a mesma cordilheira e os telhados de Miaravéu no horizonte (o destino), e só lá
        /// embaixo as copas da floresta que a estrada atravessa, com a névoa. Sem troncos próximos: é estrada aberta.
        /// Âncoras em y do mundo (o chão da estrada fica em y 0).
        /// </summary>
        static List<Layer> RoadLayers()
        {
            var list = new List<Layer>();
            list.Add(new Layer { Id = "bg_ceu_noite", FactorX = 0f, FactorY = 0f, Order = -100, FillBelow = "" });
            list.Add(new Layer { Id = "bg_ext_cordilheira_noite", FactorX = 0.06f, FactorY = 0.08f, Order = -95, FillBelow = "", AnchorY = 7f });
            list.Add(new Layer { Id = "bg_mia_telhados_horizonte", FactorX = 0.2f, FactorY = 0.15f, Order = -92, FillBelow = "", AnchorY = 2f });
            list.Add(new Layer { Id = "bg_flo_copas_longe", FactorX = 0.1f, FactorY = 0.1f, Order = -90, FillBelow = "#121e1e", AnchorY = -45f });   // (âncora − câmera) × 0,1: as copas descem ~5 tiles
            list.Add(new Layer { Id = "bg_flo_nevoa_longe", FactorX = 0.2f, FactorY = 0f, Order = -70, FillBelow = "", Drift = 0.12f });
            list.Add(new Layer { Id = "bg_flo_nevoa_perto", FactorX = 0.34f, FactorY = 0f, Order = -55, FillBelow = "", Drift = -0.2f });
            return list;
        }

        /// <summary>Âncora (tiles): no meio das salas da cena, na horizontal e na vertical.</summary>
        public static void Anchor(ForestLayout.Region reg, out float x, out float y)
        {
            int x0, y0, x1, y1;
            reg.Bounds(out x0, out y0, out x1, out y1);
            x = (x0 + x1 + 1) * 0.5f;
            y = (y0 + y1 + 1) * 0.5f;
        }
    }
}
