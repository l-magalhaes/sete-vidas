using SeteVidas.World.Scenery;
using System.Collections.Generic;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Material do terreno por região do mapa (plano de assets, Etapa 2). Só muda o DESENHO dos tiles de chão
    /// do <see cref="FortressLayout"/>; a colisão continua a mesma. Células sem material ficam com o tile
    /// provisório (a arquitetura da Cidadela entra na Etapa 3). Regiões listadas depois valem por cima.
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell para a prévia).
    /// </summary>
    public static class FortressTerrain
    {
        public class Region
        {
            public string Material; public int X0, Y0, X1, Y1; public string Note;
        }

        public static List<Region> Regions()
        {
            var r = new List<Region>();
            // 1) Montanha e 2) Trilha: fora do castelo (o pátio começa em x 44)
            R(r, TerrainAutotile.Soil, -18, -12, 43, 90, "exterior: montanha, penhasco e trilha");
            R(r, TerrainAutotile.Rock, -18, -12, -15, 90, "rocha à esquerda");
            R(r, TerrainAutotile.Rock, -14, -12, -11, 90, "rocha atrás do início (rocha-mãe: desce até a base do penhasco)");
            R(r, TerrainAutotile.Rock, 13, -2, 21, -2, "leito da lagoa (pedras e seixos sob a água)");
            R(r, TerrainAutotile.Rock, 26, 0, 28, 1, "pedra de 2 (tutorial de pulo)");
            R(r, TerrainAutotile.Rock, 31, 0, 33, 2, "pedra de 3 (tutorial de pulo)");

            // ===== Etapa 3: a Cidadela. A pedra conta a idade: quanto mais baixo e antigo, mais pesado e escuro (plano, 1.1)
            const int WallWalk = FortressLayout.WallWalk, BellHall = FortressLayout.BellHall, SealFloor = FortressLayout.SealFloor;
            // 3) Pátio de treino: chão de terra batida; arquibancada de madeira; torre de escalada dos escudeiros em Soluki
            R(r, TerrainAutotile.PackedEarth, 44, -12, 111, -1, "pátio de treino (terra batida)");
            R(r, TerrainAutotile.Wood, 50, 0, 55, 6, "arquibancada (inclui o fundo de tábuas da passagem baixa)");
            R(r, TerrainAutotile.Soluki, 88, 0, 94, 18, "torre de escalada dos escudeiros");
            // 4) Muralhas em degrau: cada etapa uma época (fundação embaixo, Soluki no meio, reforma em cima)
            R(r, TerrainAutotile.Foundation, 112, -12, 254, 14, "base de tudo: fundação de Golden");
            R(r, TerrainAutotile.Soluki, 112, 15, 135, 28, "muralha 2: Soluki");
            R(r, TerrainAutotile.Reform, 112, 29, 135, WallWalk - 1, "muralha 3: reforma (andaimes, pedra nova)");
            // 5–9) O maciço do castelo por altura
            R(r, TerrainAutotile.Foundation, 136, 15, 234, WallWalk - 1, "maciço baixo: fundação");
            R(r, TerrainAutotile.Soluki, 136, WallWalk, 234, BellHall - 1, "galeria e Torre do Sino: Soluki");
            R(r, TerrainAutotile.Soluki, 136, BellHall, 175, SealFloor - 1, "galeria das armadilhas: Soluki (piso antigo)");
            R(r, TerrainAutotile.Reform, 176, BellHall, 234, SealFloor - 1, "Salão dos Votos: reforma (o mais bem cuidado)");
            R(r, TerrainAutotile.Foundation, 136, 82, 145, SealFloor - 1, "Torre do Selo: fundação");
            R(r, TerrainAutotile.Foundation, 136, 92, 234, 116, "Câmara do Selo e o piso dela: fundação (a parte mais antiga)");
            // 10) Os fundos: a parte esquecida
            R(r, TerrainAutotile.Ruin, 186, SealFloor, 234, 116, "passagem dos fundos: abandono");
            R(r, TerrainAutotile.Ruin, 217, -12, 234, SealFloor - 1, "poço e cofres: abandono");
            // Fora do castelo, depois do portão dos fundos: a estrada para Miaravéu
            R(r, TerrainAutotile.Soil, 235, -12, 250, 0, "estrada para Miaravéu");
            R(r, TerrainAutotile.Rock, 251, -12, 254, 40, "rocha à direita");
            return r;
        }

        /// <summary>Retângulos sem tufos nem seixos (dentro da lagoa, por exemplo).</summary>
        public static List<Region> NoDetails()
        {
            var r = new List<Region>();
            R(r, "", 12, -1, 24, -1, "fundo da lagoa");
            return r;
        }

        /// <summary>Material da célula, ou "" (tile provisório).</summary>
        public static string MaterialAt(int x, int y)
        {
            string m = "";
            foreach (var r in Regions())
                if (x >= r.X0 && x <= r.X1 && y >= r.Y0 && y <= r.Y1) m = r.Material;
            return m;
        }

        public static bool DetailsAllowed(int x, int y)
        {
            foreach (var r in NoDetails())
                if (x >= r.X0 && x <= r.X1 && y >= r.Y0 && y <= r.Y1) return false;
            return true;
        }

        static void R(List<Region> list, string material, int x0, int y0, int x1, int y1, string note)
        {
            list.Add(new Region { Material = material, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Note = note });
        }
    }
}
