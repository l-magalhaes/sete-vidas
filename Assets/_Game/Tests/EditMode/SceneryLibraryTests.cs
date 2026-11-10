using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SeteVidas.World.Scenery;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Biblioteca de cenário (Etapa 1 do plano de assets): manifestos válidos, regras de variação por semente
    /// e camadas. O validador completo (imagens e cores reservadas) é o Tools/EnvironmentSprites/gerar.ps1.
    /// </summary>
    public class SceneryLibraryTests
    {
        const string ArtRoot = "Assets/_Game/Art/Environments";

        static IEnumerable<string> Manifests() =>
            Directory.GetFiles(ArtRoot, "manifest.json", SearchOption.AllDirectories);

        [Test]
        public void Manifests_Exist()
        {
            CollectionAssert.IsNotEmpty(Manifests(), "Nenhum manifest.json em " + ArtRoot);
        }

        [Test]
        public void Manifests_AreValid_WithUniqueIds_AndExistingImages()
        {
            var ids = new HashSet<string>();
            var problems = new List<string>();
            foreach (var path in Manifests())
            {
                var m = EnvManifest.Parse(File.ReadAllText(path));
                string dir = Path.GetDirectoryName(path);
                foreach (var e in m.assets)
                {
                    foreach (var p in EnvManifest.Validate(e)) problems.Add($"{e.id} ({path}): {p}");
                    if (!ids.Add(e.id)) problems.Add($"id repetido: {e.id}");
                    foreach (var img in e.sprites)
                        if (!File.Exists(Path.Combine(dir, img)) && !path.Replace('\\', '/').Contains("/Citadel/"))
                            problems.Add($"{e.id}: falta {img}"); // os da Citadel são desenhados ao gerar a cena
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Parse_FillsRegionFromManifest_AndDefaults()
        {
            var m = EnvManifest.Parse("{\"regiao\":\"ext\",\"assets\":[{\"id\":\"a\",\"sprites\":[\"a.png\"]}]}");
            Assert.AreEqual(1, m.assets.Length);
            Assert.AreEqual("ext", m.assets[0].regiao);
            Assert.AreEqual("decor_atras", m.assets[0].camada);
            Assert.AreEqual("livre", m.assets[0].posicionamento.apoio);
            Assert.IsEmpty(EnvManifest.Validate(m.assets[0]));
        }

        [Test]
        public void Validate_CatchesUnknownLayer_AndBackgroundWithoutParallax()
        {
            var bad = EnvManifest.Parse("{\"regiao\":\"ext\",\"assets\":[{\"id\":\"b\",\"categoria\":\"plx\",\"camada\":\"fundo_errado\",\"sprites\":[\"b.png\"]}]}").assets[0];
            var problems = EnvManifest.Validate(bad);
            Assert.IsTrue(problems.Exists(p => p.Contains("camada")), "camada desconhecida");
            Assert.IsTrue(problems.Exists(p => p.Contains("paralaxe")), "fundo sem paralaxe");
        }

        [Test]
        public void Layers_DefaultOrder_FollowsThePlan()
        {
            Assert.AreEqual(-100, SceneryLayers.DefaultOrder(SceneryLayers.Sky));
            Assert.AreEqual(-90, SceneryLayers.DefaultOrder(SceneryLayers.Far));
            Assert.AreEqual(-80, SceneryLayers.DefaultOrder(SceneryLayers.Mid));
            Assert.AreEqual(-10, SceneryLayers.DefaultOrder(SceneryLayers.BackWall));
            Assert.AreEqual(-5, SceneryLayers.DefaultOrder(SceneryLayers.DecorBehind));
            Assert.AreEqual(10, SceneryLayers.DefaultOrder(SceneryLayers.DecorFront));
            Assert.AreEqual(20, SceneryLayers.DefaultOrder(SceneryLayers.Foreground));
        }

        // ------------------------------------------------------------ variações

        [Test]
        public void Seed_IsStable_AndDependsOnPosition()
        {
            Assert.AreEqual(DressingRules.Seed("x", 10, 20), DressingRules.Seed("x", 10, 20));
            Assert.AreNotEqual(DressingRules.Seed("x", 10, 20), DressingRules.Seed("x", 11, 20));
            Assert.AreNotEqual(DressingRules.Seed("x", 10, 20), DressingRules.Seed("y", 10, 20));
        }

        [Test]
        public void PickVariant_NeverThreeInARow()
        {
            for (int count = 2; count <= 4; count++)
            {
                int p1 = -1, p2 = -1;
                for (int i = 0; i < 500; i++)
                {
                    int v = DressingRules.PickVariant(DressingRules.Seed("tile", i * 16, 0), count, p1, p2);
                    Assert.That(v, Is.InRange(0, count - 1));
                    Assert.IsFalse(v == p1 && v == p2, $"3 iguais seguidas (count {count}, i {i})");
                    p2 = p1; p1 = v;
                }
            }
        }

        [Test]
        public void PickVariant_UsesAllVariants()
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < 200; i++) seen.Add(DressingRules.PickVariant(DressingRules.Seed("a", i * 16, 0), 3, -1, -1));
            Assert.AreEqual(3, seen.Count);
        }

        [Test]
        public void Flip_OnlyWhenAllowed_AndOffsetWithinBounds()
        {
            bool anyFlip = false;
            for (int i = 0; i < 100; i++)
            {
                uint s = DressingRules.Seed("b", i * 16, 0);
                Assert.IsFalse(DressingRules.PickFlip(s, false));
                anyFlip |= DressingRules.PickFlip(s, true);
                Assert.That(DressingRules.PickOffsetPx(s, 3), Is.InRange(-3, 3));
                Assert.AreEqual(0, DressingRules.PickOffsetPx(s, 0));
            }
            Assert.IsTrue(anyFlip, "espelho permitido deveria acontecer às vezes");
        }
    }
}
