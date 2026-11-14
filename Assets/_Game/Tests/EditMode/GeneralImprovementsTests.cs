using System.Collections.Generic;
using NUnit.Framework;
using SeteVidas.Combat;
using SeteVidas.Enemies.Boss;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Melhorias gerais de 14/11/2026 (antes da Floresta F4): ataque para cima, dano pelos corações, repulsa do Koda,
    /// mapa do mundo e transições de cena.
    /// </summary>
    public class GeneralImprovementsTests
    {
        // ------------------------------------------------------------ 1. ataque para cima

        [Test]
        public void UpAttack_NeedsUpHeld_AndNotCrouch()
        {
            Assert.IsTrue(UpAttackRules.WantsUpAttack(1f, false));
            Assert.IsFalse(UpAttackRules.WantsUpAttack(0.3f, false), "Leve demais: golpe normal");
            Assert.IsFalse(UpAttackRules.WantsUpAttack(1f, true), "Agachado vence");
            Assert.IsFalse(UpAttackRules.WantsUpAttack(-1f, false));
        }

        [Test]
        public void UpAttack_BoxSitsOnTopOfTheHead()
        {
            var box = UpAttackRules.Box(new Vector2(10f, 0f), 1.375f, 1, new Vector2(1.4f, 1.3f));
            Assert.That(box.yMin, Is.EqualTo(1.175f).Within(1e-4f), "A base entra 0,2 na cabeça");
            Assert.That(box.yMax, Is.EqualTo(2.475f).Within(1e-4f));
            Assert.That(box.center.x, Is.EqualTo(10.15f).Within(1e-4f), "Um pouco à frente");
            Assert.IsTrue(box.Contains(new Vector2(10f, 2f)), "Pega quem está em cima");
        }

        // ------------------------------------------------------------ 2. dano pelos corações

        [Test]
        public void HeartDamage_Grows13PerNewHeart_AfterTheCurse()
        {
            Assert.AreEqual(1.0, HeartDamageRules.Multiplier(1, true), 1e-9);
            Assert.AreEqual(1.3, HeartDamageRules.Multiplier(2, true), 1e-9);
            Assert.AreEqual(1.69, HeartDamageRules.Multiplier(3, true), 1e-9);
            Assert.AreEqual(1.0, HeartDamageRules.Multiplier(7, false), 1e-9, "Antes da maldição: sem bônus");
        }

        [Test]
        public void HeartDamage_RoundsHalfUp_AndNeverZero()
        {
            Assert.AreEqual(3, HeartDamageRules.Apply(2, 1.3), "2 × 1,3 = 2,6");
            Assert.AreEqual(5, HeartDamageRules.Apply(4, 1.3), "4 × 1,3 = 5,2");
            Assert.AreEqual(2, HeartDamageRules.Apply(2, 1.0));
            Assert.AreEqual(0, HeartDamageRules.Apply(0, 1.69));
        }

        // ------------------------------------------------------------ 3. Koda: repulsa sagrada

        [Test]
        public void Koda_SeesTheCatOnHisHead()
        {
            // Koda com os pés em y 0, corpo 1,4 × 2
            Assert.IsTrue(BossRules.PlayerAbove(10f, 0f, 1.4f, 2f, 10.2f, 2f), "Parado em cima");
            Assert.IsTrue(BossRules.PlayerAbove(10f, 0f, 1.4f, 2f, 9f, 6f), "No alto do quique");
            Assert.IsFalse(BossRules.PlayerAbove(10f, 0f, 1.4f, 2f, 12.5f, 0f), "Do lado, no chão");
            Assert.IsFalse(BossRules.PlayerAbove(10f, 0f, 1.4f, 2f, 10f, 0.5f), "Embaixo do salto");
            Assert.IsFalse(BossRules.PlayerAbove(10f, 0f, 1.4f, 2f, 10f, 15f), "Longe demais acima");
        }

        [Test]
        public void Koda_RepelsAfterTimeOnTop_OrAfterHitsFromAbove()
        {
            Assert.IsTrue(BossRules.ShouldRepel(0.7f, 0.6f, 0, 2, 5f, 1.2f), "Tempo demais em cima");
            Assert.IsTrue(BossRules.ShouldRepel(0f, 0.6f, 2, 2, 5f, 1.2f), "Duas estocadas seguidas");
            Assert.IsFalse(BossRules.ShouldRepel(0.3f, 0.6f, 1, 2, 5f, 1.2f), "Um golpe só passa");
            Assert.IsFalse(BossRules.ShouldRepel(2f, 0.6f, 3, 2, 0.5f, 1.2f), "Respeita a recarga");
        }

        // ------------------------------------------------------------ 5. mapa do mundo

        [Test]
        public void WorldMap_PackUnpack_RoundTrips()
        {
            var cells = new bool[37];
            cells[0] = cells[8] = cells[36] = cells[17] = true;
            var back = WorldMapRules.Unpack(WorldMapRules.Pack(cells), cells.Length);
            CollectionAssert.AreEqual(cells, back);
            Assert.AreEqual(5, WorldMapRules.Unpack("lixo%%", 5).Length, "Texto inválido não quebra");
        }

        [Test]
        public void WorldMap_MarksChunksAroundTheCat_Once()
        {
            var data = new SaveData();
            int first = WorldMapRules.MarkAround(data, new Vector2(4f, 4f));
            Assert.AreEqual((2 * WorldMapRules.RevealX + 1) * (2 * WorldMapRules.RevealY + 1), first);
            Assert.AreEqual(0, WorldMapRules.MarkAround(data, new Vector2(5f, 5f)), "Mesmo pedaço: nada novo");
            var set = WorldMapRules.ExploredSet(data);
            Assert.IsTrue(WorldMapRules.Explored(set, 0, 0));
            Assert.IsTrue(WorldMapRules.Explored(set, -9, -1), "Vizinho à esquerda e embaixo (números negativos)");
            Assert.IsFalse(WorldMapRules.Explored(set, 40, 0));
        }

        [Test]
        public void WorldMap_NegativeCellsFallInTheRightChunk()
        {
            Assert.AreEqual(new Vector2Int(-1, -1), WorldMapRules.ChunkOfCell(-1, -8));
            Assert.AreEqual(new Vector2Int(-2, 0), WorldMapRules.ChunkOfCell(-9, 7));
            Assert.AreEqual(WorldMapRules.ChunkOf(new Vector2(-0.5f, 3f)), WorldMapRules.ChunkOfCell(-1, 3));
        }

        [Test]
        public void WorldMap_OpenAir_KeepsTheBandAboveTheGround()
        {
            // coluna de 20: chão em y 0, ar daí para cima
            const int w = 1, h = 20;
            var solid = new bool[w * h];
            solid[0] = true;
            var space = WorldMapRules.OpenAirSpace(solid, w, h, 10);
            Assert.IsFalse(space[0], "O chão não é espaço");
            Assert.IsTrue(space[1] && space[10]);
            Assert.IsFalse(space[11], "O céu alto sai do mapa");
        }

        [Test]
        public void WorldMap_FillIslands_RemovesPlatforms_KeepsFloorAndWalls()
        {
            // sala 10×6: chão na linha 0 (encosta na borda), plataforma solta na linha 3, coluna presa no chão em x 7
            const int w = 10, h = 6;
            var space = new bool[w * h];
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++) space[y * w + x] = true;
            for (int x = 3; x <= 5; x++) space[3 * w + x] = false;   // plataforma
            space[1 * w + 7] = space[2 * w + 7] = false;               // coluna presa no chão
            WorldMapRules.FillIslands(space, w, h);
            Assert.IsTrue(space[3 * w + 4], "A plataforma some");
            Assert.IsFalse(space[1 * w + 7], "A coluna presa no chão fica");
            Assert.IsFalse(space[0], "O chão fica");
        }

        [Test]
        public void WorldMap_PaintsExploredBright_AndUnexploredAsGhostOutline()
        {
            var map = ScriptableObject.CreateInstance<WorldMapData>();
            try
            {
                var s = new WorldMapData.SceneMap { scene = "x", region = "Floresta Silenciosa", x0 = 0, y0 = 0, w = 20, h = 3 };
                var cells = new bool[20 * 3];
                for (int i = 0; i < cells.Length; i++) cells[i] = true;
                s.SetCells(cells);
                map.scenes.Add(s);
                var bounds = map.Bounds();
                var explored = new HashSet<int> { WorldMapRules.ChunkKey(0, 0) };   // só x 0..7
                var px = WorldMapRules.Paint(map, bounds, explored);
                var pal = WorldMapRules.PaletteFor("Floresta Silenciosa");
                Color32 At(int mx, int my) => px[(my - bounds.y) * bounds.width + (mx - bounds.x)];
                Assert.AreEqual(pal.Fill, At(3, 1), "Miolo explorado");
                Assert.AreEqual(pal.Edge, At(3, 0), "Borda explorada");
                Assert.AreEqual(0, At(15, 0).a, "Onde o gato não foi, nem o contorno aparece");
                Assert.AreEqual(0, At(15, 1).a, "Miolo não explorado fica vazio");
            }
            finally { Object.DestroyImmediate(map); }
        }

        [Test]
        public void WorldMap_MarkRect_RevealsTheWholeRoom()
        {
            var data = new SaveData();
            WorldMapRules.MarkRect(data, 0, 0, 30, 17);   // 30 × 17 tiles = 4 × 3 pedaços de 8
            var set = WorldMapRules.ExploredSet(data);
            Assert.AreEqual(12, set.Count);
            Assert.IsTrue(WorldMapRules.Explored(set, 29, 16));
            Assert.IsFalse(WorldMapRules.Explored(set, 33, 0));
        }

        [Test]
        public void WorldMap_Spread_PushesRoomsApart_WithoutOverlap()
        {
            // duas salas encostadas (centros a 30 tiles): afastadas 25%, abre um vão de ~7 tiles
            var a = WorldMapRules.SpreadDelta(new Vector2(15f, 0f), 1.25f);
            var b = WorldMapRules.SpreadDelta(new Vector2(45f, 0f), 1.25f);
            int growth = (b.x + 45) - (a.x + 15) - 30;   // 4 e 11 de deslocamento
            Assert.AreEqual(7, growth, "O vão entre elas cresce 7 tiles");
            Assert.AreEqual(0, a.y);
        }

        [Test]
        public void WorldMap_LinkPixels_GoesAcrossThenUp()
        {
            var px = WorldMapRules.LinkPixels(new Vector2Int(0, 0), new Vector2Int(3, 2));
            Assert.AreEqual(6, px.Count);
            Assert.AreEqual(new Vector2Int(3, 0), px[3], "Primeiro na horizontal");
            Assert.AreEqual(new Vector2Int(3, 2), px[5]);
        }

        [Test]
        public void WorldMap_ClampCenter_KeepsTheMapOnThePanel()
        {
            var bounds = new RectInt(0, 0, 400, 100);
            var c = WorldMapRules.ClampCenter(new Vector2(-50f, 500f), bounds, new Vector2(270f, 260f), 2f);
            Assert.AreEqual(67.5f, c.x, 1e-3f, "Encosta na borda esquerda");
            Assert.AreEqual(50f, c.y, 1e-3f, "Mais baixo que o painel: centrado");
        }

        // ------------------------------------------------------------ 8. transições

        [Test]
        public void Transition_SidePassage_WalksOut_HoleFallsOut()
        {
            var side = new Rect(29f, 0f, 1f, 4f);
            Assert.AreEqual(new Vector2(1f, 0f), TransitionRules.ExitDirection(side, new Vector2(28f, 0f), -1), "Anda para o lado da passagem");
            var hole = new Rect(0f, -3f, 28f, 3f);
            Assert.AreEqual(new Vector2(0f, -1f), TransitionRules.ExitDirection(hole, new Vector2(10f, -1f), 1), "Buraco: continua caindo");
            var top = new Rect(0f, 20f, 4f, 2f);
            Assert.AreEqual(new Vector2(0f, 1f), TransitionRules.ExitDirection(top, new Vector2(2f, 17f), 1), "Topo: sobe");
        }

        [Test]
        public void Transition_FallIn_NeverStartsInsideTheCeiling()
        {
            Assert.AreEqual(9f, TransitionRules.FallHeight(9f, -1f), "Nada acima: altura toda");
            Assert.AreEqual(4f, TransitionRules.FallHeight(9f, 6f), "Teto a 6: cai de 4");
            Assert.AreEqual(0f, TransitionRules.FallHeight(9f, 1f), "Teto colado: chega de pé");
        }
    }
}
