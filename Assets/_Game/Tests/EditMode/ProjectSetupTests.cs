using NUnit.Framework;
using SeteVidas.Core;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Testes da Fase 0: confirmam que o projeto foi configurado como o GDD pede.
    /// Rode em Window > General > Test Runner > EditMode.
    /// </summary>
    public class ProjectSetupTests
    {
        [Test]
        public void ReferenceResolution_ScalesToFullHdByAnInteger()
        {
            Assert.AreEqual(0, 1920 % GameConstants.ReferenceWidth, "1920 deve ser múltiplo inteiro da largura interna");
            Assert.AreEqual(0, 1080 % GameConstants.ReferenceHeight, "1080 deve ser múltiplo inteiro da altura interna");
            Assert.AreEqual(1920 / GameConstants.ReferenceWidth, 1080 / GameConstants.ReferenceHeight, "A escala deve ser igual nos dois eixos");
        }

        [Test]
        public void MaxHits_WithAllHearts_IsFourteen()
        {
            Assert.AreEqual(14, GameConstants.MaxHearts * GameConstants.HitsPerHeart);
        }

        [Test]
        public void PhysicsLayers_ExistWithExpectedIndices()
        {
            for (int i = 0; i < GameConstants.Layers.All.Length; i++)
            {
                string name = GameConstants.Layers.All[i];
                Assert.AreEqual(GameConstants.Layers.FirstIndex + i, LayerMask.NameToLayer(name),
                    $"Layer '{name}' ausente ou fora do índice. Rode Tools > Sete Vidas > 1. Configurar projeto.");
            }
        }

        [Test]
        public void EnemiesDoNotCollideWithEachOther()
        {
            int enemy = LayerMask.NameToLayer(GameConstants.Layers.Enemy);
            Assume.That(enemy, Is.GreaterThanOrEqualTo(0), "Layer Enemy ainda não criada");
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(enemy, enemy));
        }

        [Test]
        public void PlayerPassesThroughEnemies()
        {
            int enemy = LayerMask.NameToLayer(GameConstants.Layers.Enemy);
            int player = LayerMask.NameToLayer(GameConstants.Layers.Player);
            Assume.That(enemy >= 0 && player >= 0, "Layers ainda não criadas");
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(player, enemy),
                "O gato precisa atravessar inimigos para chegar às costas. Rode Tools > Sete Vidas > 6");
        }
    }
}
