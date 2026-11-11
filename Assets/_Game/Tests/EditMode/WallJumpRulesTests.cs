using NUnit.Framework;
using SeteVidas.Abilities;

namespace SeteVidas.Tests
{
    /// <summary>Escalada: o Espaço na parede vai para o lado apontado (pedido do usuário).</summary>
    public class WallJumpRulesTests
    {
        const float T = 0.5f;

        [Test]
        public void AwayFromTheWall_JumpsAway()
        {
            Assert.AreEqual(WallJumpRules.Kind.Away, WallJumpRules.Choose(-1f, 0f, 1, T), "Parede à direita, segurando A");
            Assert.AreEqual(WallJumpRules.Kind.Away, WallJumpRules.Choose(1f, 0f, -1, T), "Parede à esquerda, segurando D");
            Assert.AreEqual(WallJumpRules.Kind.Away, WallJumpRules.Choose(-1f, 1f, 1, T), "Diagonal para longe e para cima: para longe");
        }

        [Test]
        public void NoDirection_JumpsAway_AsBefore()
        {
            Assert.AreEqual(WallJumpRules.Kind.Away, WallJumpRules.Choose(0f, 0f, 1, T));
        }

        [Test]
        public void UpOrTowardTheWall_HopsUpAndGrabsAgain()
        {
            Assert.AreEqual(WallJumpRules.Kind.Hop, WallJumpRules.Choose(0f, 1f, 1, T), "W + Espaço");
            Assert.AreEqual(WallJumpRules.Kind.Hop, WallJumpRules.Choose(1f, 0f, 1, T), "Segurando a direção da parede");
            Assert.AreEqual(WallJumpRules.Kind.Hop, WallJumpRules.Choose(-1f, 0f, -1, T));
        }
    }
}
