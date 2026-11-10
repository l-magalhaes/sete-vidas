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
