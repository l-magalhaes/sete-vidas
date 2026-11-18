using System.Collections.Generic;
using NUnit.Framework;
using SeteVidas.World;
using SeteVidas.World.Scenery;

namespace SeteVidas.Tests
{
    /// <summary>
    /// A cobertura do segredo atrás de uma parede quebrável (pedido do usuário, 17/11/2026: "padrão de assets errado
    /// repetindo... precisa ser orgânico"). A cabine da A5 tem paredes de 1 tile: copiando o tile de borda mais próximo,
    /// o vão virava anéis concêntricos. Agora o vão é parede maciça pelo autotile das salas.
    /// </summary>
    public class SecretCoverRulesTests
    {
        // a cabine da A5 (local): parede de trás x 14, teto y 14, passarela y 9, o vão x 15..18 × y 10..13, a parede de
        // ruínas em x 19 (conta como sólida para o vão)
        static TerrainAutotile.SolidAt Cabin()
        {
            var s = new HashSet<(int, int)>();
            for (int x = 14; x <= 29; x++) s.Add((x, 9));
            for (int y = 10; y <= 13; y++) s.Add((14, y));
            for (int x = 14; x <= 19; x++) s.Add((x, 14));
            for (int x = 15; x <= 19; x++) for (int y = 10; y <= 13; y++) s.Add((x, y));   // vão + parede
            return (x, y) => s.Contains((x, y));
        }

        [Test]
        public void Pocket_IsSolidInterior_NoEdges()
        {
            var solid = Cabin();
            for (int x = 15; x <= 18; x++)
                for (int y = 10; y <= 13; y++)
                {
                    string n = SecretCovers.CoverTileName("tile_pri_concreto_miolo_a_1", solid, x, y);
                    StringAssert.StartsWith("tile_pri_concreto_miolo_0_", n, $"({x},{y}) sem borda dentro do vão");
                }
        }

        [Test]
        public void Pocket_Variants_AreMostlyPlainAndNotAllEqual()
        {
            var solid = Cabin();
            var seen = new HashSet<string>();
            for (int x = 15; x <= 18; x++)
                for (int y = 10; y <= 13; y++)
                    seen.Add(SecretCovers.CoverTileName("tile_pri_concreto_miolo_a_1", solid, x, y));
            Assert.Greater(seen.Count, 1, "variações espalhadas, não um tile só repetido");
        }

        [Test]
        public void Walls_LoseTheEdgeFacingThePocket()
        {
            var solid = Cabin();
            // a parede de trás (x 14) só fica com a borda da esquerda (o ar de fora)
            Assert.AreEqual("tile_pri_concreto_miolo_8_", Prefix(SecretCovers.CoverTileName("tile_pri_concreto_miolo_a_0", solid, 14, 12)));
            // o teto: topo, com o céu em cima e nada embaixo exposto
            StringAssert.StartsWith("tile_pri_concreto_topo_1_", SecretCovers.CoverTileName("tile_pri_concreto_topo_5_0", solid, 16, 14));
        }

        [Test]
        public void NotGround_KeepsTheOldTile()
        {
            var solid = Cabin();
            Assert.IsNull(SecretCovers.CoverTileName("tile_flo_solo_tronco_a_1", solid, 15, 10));
            Assert.IsNull(SecretCovers.CoverTileName("tile_pri_concreto_fundo_0", solid, 15, 10));
            Assert.IsNull(SecretCovers.CoverTileName("algo_qualquer", solid, 15, 10));
        }

        [Test]
        public void FortressMasonry_UsesTheFortressRule()
        {
            TerrainAutotile.SolidAt all = (x, y) => true;
            Assert.AreEqual(TerrainAutotile.TileFor(TerrainAutotile.Ruin, all, null, 3, 3),
                SecretCovers.CoverTileName("tile_cid_abandono_miolo_2_1", all, 3, 3));
        }

        static string Prefix(string name) { return name.Substring(0, name.LastIndexOf('_') + 1); }
    }
}
