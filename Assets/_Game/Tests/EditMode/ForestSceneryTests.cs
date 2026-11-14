using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SeteVidas.World.Scenery;

namespace SeteVidas.Tests
{
    /// <summary>
    /// F5 da Floresta (cenário, parte 1): a regra dos tiles da floresta (<see cref="ForestAutotile"/>) e a arte gerada
    /// por Tools/EnvironmentSprites/gerar.ps1. A cena inteira (todo tile pedido tem arte) é conferida por
    /// Tools/LevelPreview/conferir_arte_floresta.ps1.
    /// </summary>
    public class ForestSceneryTests
    {
        const string Art = "Assets/_Game/Art/Environments/Forest/Terrain";
        const string Bg = "Assets/_Game/Art/Environments/Backgrounds/Flo";

        static TerrainAutotile.SolidAt Cells(HashSet<(int, int)> set) => (x, y) => set.Contains((x, y));

        [Test]
        public void GeneratedArt_HasEveryTileTheRuleCanAskFor()
        {
            var missing = new List<string>();
            foreach (var m in ForestAutotile.Materials)
                foreach (var n in ForestAutotile.AllTileNames(m))
                    if (!File.Exists($"{Art}/{n}.png")) missing.Add(n);
            foreach (var n in ForestAutotile.AllExtraNames()) if (!File.Exists($"{Art}/{n}.png")) missing.Add(n);
            foreach (var n in ForestAutotile.AllDetailNames()) if (!File.Exists($"{Art}/{n}.png")) missing.Add(n);
            foreach (var n in new[] { "bg_flo_ceu_noite", "bg_flo_ceu_praga", "bg_flo_copas_longe", "bg_flo_copas_longe_praga",
                                      "bg_flo_arvores_medio", "bg_flo_arvores_medio_praga", "bg_flo_troncos_perto", "bg_flo_troncos_perto_praga",
                                      "bg_flo_nevoa_longe", "bg_flo_nevoa_longe_praga", "bg_flo_nevoa_perto", "bg_flo_nevoa_perto_praga" })
                if (!File.Exists($"{Bg}/{n}.png")) missing.Add(n);
            Assert.IsEmpty(missing, "Rode Tools/EnvironmentSprites/gerar.ps1. Faltam: " + string.Join(", ", missing));
        }

        [Test]
        public void Floor_HasMossOnTop_EarthBelow_RockDeep()
        {
            var set = new HashSet<(int, int)>();
            for (int x = -5; x <= 5; x++) for (int y = -10; y <= 0; y++) set.Add((x, y));
            var s = Cells(set);
            StringAssert.Contains("_topo_", ForestAutotile.TileFor(ForestAutotile.Soil, s, 0, 0));
            StringAssert.Contains("_terra_", ForestAutotile.TileFor(ForestAutotile.Soil, s, 0, -1));
            StringAssert.Contains("_transicao_", ForestAutotile.TileFor(ForestAutotile.Soil, s, 0, -3));
            StringAssert.Contains("_rocha_", ForestAutotile.TileFor(ForestAutotile.Soil, s, 0, -6));
            StringAssert.Contains("_miolo_", ForestAutotile.TileFor(ForestAutotile.Concrete, s, 0, -6), "Concreto: laje, sem rocha");
        }

        [Test]
        public void NarrowTallColumn_IsATrunk_AndTheFloorBelowStaysEarth()
        {
            var set = new HashSet<(int, int)>();
            for (int x = -6; x <= 6; x++) for (int y = -6; y <= 0; y++) set.Add((x, y));   // chão
            for (int y = 1; y <= 8; y++) set.Add((0, y));                                     // coluna de 1 de largura
            var s = Cells(set);
            Assert.IsTrue(ForestAutotile.IsTrunk(s, 0, 4));
            StringAssert.Contains("_tronco_", ForestAutotile.TileFor(ForestAutotile.Soil, s, 0, 4));
            StringAssert.Contains("_terra_", ForestAutotile.TileFor(ForestAutotile.Soil, s, 0, 0), "Embaixo do tronco o piso continua terra");
            Assert.IsFalse(ForestAutotile.IsTrunk(s, 3, -2), "O chão largo não é tronco");
        }

        [Test]
        public void Variants_NeverRepeatThreeTimesInARow()
        {
            for (int y = -20; y < 20; y++)
                for (int x = -50; x < 50; x++)
                {
                    int a = ForestAutotile.Variant(ForestAutotile.Soil, "topo", x, y);
                    int b = ForestAutotile.Variant(ForestAutotile.Soil, "topo", x + 1, y);
                    int c = ForestAutotile.Variant(ForestAutotile.Soil, "topo", x + 2, y);
                    Assert.IsFalse(a == b && b == c, $"3 iguais em ({x}, {y})");
                }
        }

        [Test]
        public void Details_BlightOnlyWhereThePlagueArrived()
        {
            bool sproutClean = false, sproutBlight = false, fungusBlight = false;
            for (int x = 0; x < 400; x++)
            {
                foreach (var d in ForestAutotile.TopDetails(ForestAutotile.Soil, 0, x, 0)) if (d.Sprite.StartsWith("env_flo_praga_broto_")) sproutClean = true;
                foreach (var d in ForestAutotile.TopDetails(ForestAutotile.Blight, 2, x, 0))
                {
                    if (d.Sprite.StartsWith("env_flo_praga_broto_")) sproutBlight = true;
                    if (d.Sprite.StartsWith("env_flo_veg_fungo_")) fungusBlight = true;
                }
            }
            Assert.IsFalse(sproutClean, "Mata saudável sem brotos da praga");
            Assert.IsTrue(sproutBlight, "Mata tomada tem brotos");
            Assert.IsFalse(fungusBlight, "Fungos azuis só na mata saudável");
        }

        [Test]
        public void SlopePieces_OnlyForSupportedRatios_AndNotConcrete()
        {
            Assert.IsNotEmpty(ForestAutotile.SlopePieces(ForestAutotile.Blight, 0, 0, 8, 4, true));
            Assert.IsNotEmpty(ForestAutotile.SlopePieces(ForestAutotile.Soil, 0, 0, 3, 3, false));
            Assert.IsEmpty(ForestAutotile.SlopePieces(ForestAutotile.Soil, 0, 0, 5, 3, true));
            Assert.IsEmpty(ForestAutotile.SlopePieces(ForestAutotile.Concrete, 0, 0, 4, 4, true));
        }
    }
}
