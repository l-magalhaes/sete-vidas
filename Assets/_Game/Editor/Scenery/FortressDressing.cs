using System.Collections.Generic;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Enfeites da Fortaleza como DADOS (plano de assets, seção 3 e Etapa 8), separados do mapa jogável
    /// (<see cref="FortressLayout"/>): só cenário, sem colisão nem lógica. Cada item aponta para um id da
    /// biblioteca (manifest.json); variação, espelho e deslocamento saem de uma semente fixa pela posição
    /// (<c>DressingRules</c>), então o resultado é sempre o mesmo.
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell para a prévia do mapa).
    /// Convenção de Y: apoio "chao" = y do chão; "teto" = y do teto; "parede"/"livre" = centro.
    /// </summary>
    public static class FortressDressing
    {
        public class Item
        {
            public string AssetId;
            public string Name;
            public float X, Y;
            /// <summary>Repetições em fileira (escudos), a cada <see cref="Step"/> tiles para a direita.</summary>
            public int Count = 1;
            public float Step;
            /// <summary>Área do mapa (1–10), para a montagem e a revisão área por área.</summary>
            public int Area;
            /// <summary>Variação fixa (-1 = sorteio pela posição). Use quando o tamanho importa para a leitura.</summary>
            public int Variant = -1;
        }

        const string Statue = "env_cid_cul_statue_paladin";
        const string Banner = "env_cid_cul_banner_order";
        const string Window = "env_cid_arq_stained_glass";
        const string Shield = "env_cid_cul_shield_paladin";
        const string Torch = "env_com_obj_wall_torch";
        const string Rack = "env_sol_obj_weapon_rack";
        const string Target = "env_sol_obj_archery_target";

        const string Pebble = "env_ext_roc_pequena", Medium = "env_ext_roc_media", Boulder = "env_ext_roc_rochedo";
        // Fósseis (variações em ordem): 0 crânio 20 px, 1 costelas 22 px, 2 osso longo 14 px, 3 coluna 24 px.
        // Só dentro da terra/rocha, nunca na superfície do caminho nem saindo de parede.
        const string Fossil = "env_ext_roc_fossil";
        const int Skull = 0, Ribs = 1, Bone = 2, Spine = 3;

        public static List<Item> Items()
        {
            var d = new List<Item>();
            const int WallWalk = FortressLayout.WallWalk, BellHall = FortressLayout.BellHall, SealFloor = FortressLayout.SealFloor;
            const int Cliff = FortressLayout.CliffTop;

            // ===== 1) Montanha (Etapa 2). A corrida até o salto (x 4..12) fica limpa: nada que pareça degrau.
            Add(d, 1, Boulder, "Rochedo atrás do início", -3.5f, Cliff);
            Add(d, 1, Pebble, "Pedra pequena do morrinho", -5.6f, Cliff);
            Fixed(d, 1, Fossil, Skull, "Fóssil na terra do início", 0.6f, Cliff - 1.6f);
            Fixed(d, 1, Fossil, Ribs, "Fóssil na parede de rocha", -12.5f, Cliff + 6f);   // 22 px: cabe em x -14..-11
            // Face do penhasco: o gato vê na queda de 60 tiles (x <= 12, dentro da rocha)
            Fixed(d, 1, Fossil, Spine, "Fóssil no penhasco (coluna)", 6f, 45f);
            Fixed(d, 1, Fossil, Skull, "Fóssil no penhasco (crânio)", 9.5f, 28f);
            Fixed(d, 1, Fossil, Ribs, "Fóssil no penhasco (costelas)", 4f, 12f);

            // ===== 2) Trilha (Etapa 2). Pedras jogáveis (2 e 3) são tiles de rocha; estas são só cenário, atrás.
            Fixed(d, 2, Boulder, 0, "Rochedo na lagoa", 17.2f, -1f);      // sem musgo; no fundo da bacia, meio dentro d'água
            Add(d, 2, Medium, "Pedra média do marco", 24.1f, 0f);         // na margem, depois da rampa (x 23..)
            // Osso de 14 px, todo dentro da parede esquerda do fosso (célula x 36): a vala descobriu o fóssil.
            // Nunca maior: saindo da parede, pareceria uma plataforma em cima dos espinhos.
            Fixed(d, 2, Fossil, Bone, "Fóssil no fosso", 36.45f, -1.5f);
            Fixed(d, 2, Fossil, Spine, "Fóssil na terra da trilha", 29.5f, -2.4f);

            // ===== 3) Pátio de treino
            Add(d, 3, Statue, "Estátua do guardião", 44.5f, 0f);
            Add(d, 3, Rack, "Suporte de armas", 67f, 0f);
            Add(d, 3, Target, "Alvo de arco", 70f, 0f);

            // ===== 5) Galeria da muralha
            Add(d, 5, Torch, "Tocha da galeria 1", 165f, WallWalk + 2.6f);
            Add(d, 5, Torch, "Tocha da galeria 2", 186f, WallWalk + 2.6f);
            Add(d, 5, Banner, "Estandarte da galeria", 170f, 51.9f);

            // ===== 7) Salão dos Votos
            Add(d, 7, Window, "Vitral do salão 1", 186f, BellHall + 6f);
            Add(d, 7, Window, "Vitral do salão 2", 198f, BellHall + 6f);
            Add(d, 7, Banner, "Estandarte do salão", 192f, 81.9f);
            Row(d, 7, Shield, "Escudos do salão", 178f, BellHall + 3.3f, 4, 8f);

            // ===== 9) Câmara do Selo
            Add(d, 9, Statue, "Estátua da câmara 1", 149f, SealFloor);
            Add(d, 9, Statue, "Estátua da câmara 2", 174.5f, SealFloor);
            Add(d, 9, Window, "Vitral da câmara 1", 155f, SealFloor + 5.5f);
            Add(d, 9, Window, "Vitral da câmara 2", 168f, SealFloor + 5.5f);
            Add(d, 9, Banner, "Estandarte do selo", 161.5f, 109.9f);
            Add(d, 9, Banner, "Estandarte do tomo", 181.5f, 106.9f);
            return d;
        }

        static void Add(List<Item> d, int area, string id, string name, float x, float y)
        {
            d.Add(new Item { Area = area, AssetId = id, Name = name, X = x, Y = y });
        }

        static void Fixed(List<Item> d, int area, string id, int variant, string name, float x, float y)
        {
            d.Add(new Item { Area = area, AssetId = id, Name = name, X = x, Y = y, Variant = variant });
        }

        static void Row(List<Item> d, int area, string id, string name, float x, float y, int count, float step)
        {
            d.Add(new Item { Area = area, AssetId = id, Name = name, X = x, Y = y, Count = count, Step = step });
        }
    }
}
