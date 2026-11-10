using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SeteVidas.World.Scenery;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Terreno natural (Etapa 2 do plano de cenário): escolha do tile pela vizinhança e profundidade, estratos
    /// contínuos, rampas e a arte gerada (todo nome que a regra pode pedir existe em disco).
    /// </summary>
    public class TerrainAutotileTests
    {
        const string Soil = TerrainAutotile.Soil, Rock = TerrainAutotile.Rock;
        const string TerrainArt = "Assets/_Game/Art/Environments/Exterior/Terrain";

        // chão plano com topo em y = 0 (células y <= -1), de x -20 a 20
        static bool Flat(int x, int y) => y <= -1 && x >= -20 && x <= 20;

        [Test]
        public void Bands_FollowDepth()
        {
            Assert.AreEqual("topo", TerrainAutotile.Band(Soil, 0));
            Assert.AreEqual("terra", TerrainAutotile.Band(Soil, 1));
            Assert.AreEqual("terra", TerrainAutotile.Band(Soil, 2));
            Assert.AreEqual("transicao", TerrainAutotile.Band(Soil, 3));
            Assert.AreEqual("rocha", TerrainAutotile.Band(Soil, 4));
            Assert.AreEqual("rocha", TerrainAutotile.Band(Rock, 2), "material rocha não tem terra");
        }

        [Test]
        public void FlatGround_SurfaceIsGrassWithSkyAbove_DeepIsRock()
        {
            StringAssert.StartsWith("tile_ext_solo_topo_1_", TerrainAutotile.TileFor(Soil, Flat, null, 0, -1));
            StringAssert.StartsWith("tile_ext_solo_terra_0_", TerrainAutotile.TileFor(Soil, Flat, null, 0, -2));
            StringAssert.StartsWith("tile_ext_solo_transicao_0_", TerrainAutotile.TileFor(Soil, Flat, null, 0, -4));
            StringAssert.StartsWith("tile_ext_solo_rocha_0_", TerrainAutotile.TileFor(Soil, Flat, null, 0, -8));
        }

        [Test]
        public void Edges_MarkExposedSides()
        {
            // borda direita do chão (x 20): céu em cima e à direita = 1 | 2 = 3
            StringAssert.StartsWith("tile_ext_solo_topo_3_", TerrainAutotile.TileFor(Soil, Flat, null, 20, -1));
            // face direita, mais funda: só a direita exposta
            StringAssert.StartsWith("tile_ext_solo_rocha_2_", TerrainAutotile.TileFor(Soil, Flat, null, 20, -8));
        }

        [Test]
        public void StrataPhase_FollowsColumn_SoLinesContinueBetweenTiles()
        {
            for (int x = -8; x <= 8; x++)
                Assert.AreEqual(TerrainAutotile.Mod(x, TerrainAutotile.StrataPhases), TerrainAutotile.Variant(Soil, "rocha", x, -8));
        }

        [Test]
        public void Variants_NeverThreeInARow()
        {
            foreach (var band in new[] { "topo", "terra" })
                for (int y = -3; y <= 0; y++)
                    for (int x = -50; x < 50; x++)
                    {
                        int a = TerrainAutotile.Variant(Soil, band, x, y), b = TerrainAutotile.Variant(Soil, band, x + 1, y), c = TerrainAutotile.Variant(Soil, band, x + 2, y);
                        Assert.IsFalse(a == b && b == c, $"{band}: 3 iguais em x {x}..{x + 2}, y {y}");
                    }
        }

        [Test]
        public void SoilUnderARock_IsDirt_NotADeepRockColumn()
        {
            // pedra (material rocha) de 2 de altura em x 0, em cima do chão plano
            bool Solid(int x, int y) => Flat(x, y) || (x == 0 && (y == 0 || y == 1));
            string MaterialAt(int x, int y) => x == 0 && y >= 0 ? Rock : Soil;
            StringAssert.StartsWith("tile_ext_solo_terra_", TerrainAutotile.TileFor(Soil, Solid, MaterialAt, 0, -1));
            StringAssert.StartsWith("tile_ext_solo_terra_", TerrainAutotile.TileFor(Soil, Solid, MaterialAt, 0, -2));
        }

        [Test]
        public void SlopePieces_OneToOne_DiagonalAndFill()
        {
            var pieces = TerrainAutotile.SlopePieces(Soil, 0, 0, 4, 4, true);
            Assert.AreEqual(10, pieces.Count, "4 na diagonal + 6 embaixo");
            Assert.AreEqual(4, pieces.FindAll(p => p.Sprite == "env_ext_ter_rampa_11_r").Count);
            Assert.IsTrue(pieces.Exists(p => p.X == 3 && p.Y == 3 && p.Sprite.Contains("rampa")), "lado alto à direita, no topo");
        }

        [Test]
        public void SlopePieces_OneToTwo_HalfPiecesAndSolidity()
        {
            var pieces = TerrainAutotile.SlopePieces(Soil, -7, 60, 2, 1, false);
            Assert.AreEqual(2, pieces.Count);
            var low = pieces.Find(p => p.X == -6); // sobe para a esquerda: o lado baixo é o da direita
            Assert.AreEqual("env_ext_ter_rampa_12a_l", low.Sprite);
            Assert.IsFalse(low.Solid, "metade baixa (menos de meia célula) não esconde a grama de baixo");
            Assert.AreEqual("env_ext_ter_rampa_12b_l", pieces.Find(p => p.X == -7).Sprite);
        }

        [Test]
        public void SlopePieces_UnsupportedRatio_FallsBackToPlaceholder()
        {
            CollectionAssert.IsEmpty(TerrainAutotile.SlopePieces(Soil, 0, 0, 3, 2, true));
            CollectionAssert.IsEmpty(TerrainAutotile.SlopePieces("", 0, 0, 2, 2, true));
        }

        [Test]
        public void GeneratedArt_HasEveryTileTheRuleCanAskFor()
        {
            var missing = new List<string>();
            foreach (var mat in TerrainAutotile.Materials)
                foreach (var name in TerrainAutotile.AllTileNames(mat))
                    if (!File.Exists($"{TerrainArt}/{name}.png")) missing.Add(name);
            foreach (var kind in TerrainAutotile.SlopePieceKinds)
                foreach (bool right in new[] { true, false })
                {
                    string name = TerrainAutotile.SlopePieceName(kind, right);
                    if (!File.Exists($"{TerrainArt}/{name}.png")) missing.Add(name);
                }
            for (int v = 0; v < TerrainAutotile.TuftVariants; v++)
                if (!File.Exists($"{TerrainArt}/{TerrainAutotile.TuftName(v)}.png")) missing.Add(TerrainAutotile.TuftName(v));
            Assert.IsEmpty(missing, "Rode Tools/EnvironmentSprites/gerar.ps1. Faltam: " + string.Join(", ", missing));
        }

        [Test]
        public void TopDetails_AreStable_AndSitOnTheSurface()
        {
            int tufts = 0;
            for (int x = 0; x < 100; x++)
            {
                var a = TerrainAutotile.TopDetails(Soil, x, -1);
                var b = TerrainAutotile.TopDetails(Soil, x, -1);
                Assert.AreEqual(a.Count, b.Count, "mesma posição, mesmo resultado");
                foreach (var d in a)
                {
                    Assert.AreEqual(0f, d.Y, 0.0001f, "base do detalhe na superfície (y = célula + 1)");
                    if (d.Sprite.Contains("tufo")) tufts++;
                }
            }
            Assert.That(tufts, Is.InRange(45, 80), "cerca de 62% das células de superfície com tufo");
        }
    }
}
