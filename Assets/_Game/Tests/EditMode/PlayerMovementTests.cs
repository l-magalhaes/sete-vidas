using NUnit.Framework;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Fase 1: conta do pulo, coyote time, jump buffer e tamanhos do colisor.</summary>
    public class PlayerMovementTests
    {
        const float Dt = 0.02f; // passo de física padrão (50 Hz)

        // Simula a física do PlayerController: gravidade aplicada antes de mover.
        static float SimulateApex(float launch, float gravity, float dt)
        {
            float v = launch, y = 0f, max = 0f;
            for (int i = 0; i < 1000 && v > 0f; i++)
            {
                v -= gravity * dt;
                y += v * dt;
                max = Mathf.Max(max, y);
            }
            return max;
        }

        [TestCase(3f, 0.35f)]
        [TestCase(4f, 0.38f)]
        [TestCase(5f, 0.45f)]
        public void JumpApex_MatchesJumpHeightInTiles(float height, float timeToApex)
        {
            float g = JumpMath.Gravity(height, timeToApex);
            float v = JumpMath.LaunchVelocity(height, timeToApex, Dt);
            Assert.AreEqual(height, SimulateApex(v, g, Dt), 0.05f, "Altura real do pulo difere da configurada");
        }

        [Test]
        public void Coyote_AllowsJumpShortlyAfterLeavingGround()
        {
            var t = new JumpTimers();
            t.OnGrounded(0.1f);
            t.Tick(0.06f);              // saiu da borda há 0,06 s
            t.OnJumpPressed(0.1f);
            Assert.IsTrue(t.CanJump);
        }

        [Test]
        public void Coyote_ExpiresAfterCoyoteTime()
        {
            var t = new JumpTimers();
            t.OnGrounded(0.1f);
            t.Tick(0.12f);
            t.OnJumpPressed(0.1f);
            Assert.IsFalse(t.CanJump);
        }

        [Test]
        public void Buffer_JumpPressedBeforeLanding_JumpsOnLanding()
        {
            var t = new JumpTimers();
            t.OnJumpPressed(0.1f);
            t.Tick(0.08f);              // ainda no ar
            t.OnGrounded(0.1f);         // aterrissou
            Assert.IsTrue(t.CanJump);
        }

        [Test]
        public void Buffer_ExpiresIfPressedTooEarly()
        {
            var t = new JumpTimers();
            t.OnJumpPressed(0.1f);
            t.Tick(0.15f);
            t.OnGrounded(0.1f);
            Assert.IsFalse(t.CanJump);
        }

        [Test]
        public void Consume_PreventsSecondJumpInAir()
        {
            var t = new JumpTimers();
            t.OnGrounded(0.1f);
            t.OnJumpPressed(0.1f);
            t.Consume();
            t.Tick(0.02f);
            t.OnJumpPressed(0.1f);      // apertou de novo no ar
            Assert.IsFalse(t.CanJump);
        }

        [Test]
        public void BufferedJump_WorksWithoutGround_ForClimbingLedges()
        {
            var t = new JumpTimers();
            t.OnJumpPressed(0.1f);      // pendurado: não há chão, mas o pedido de pulo fica guardado
            Assert.IsTrue(t.HasBufferedJump);
            Assert.IsFalse(t.CanJump, "Sem chão nem coyote, não é um pulo normal");
            t.Consume();
            Assert.IsFalse(t.HasBufferedJump);
        }

        [Test]
        public void DefaultSettings_LedgeReach_StaysBelowSixTiles()
        {
            // Gates de pulo duplo terão 6 tiles: o Ladino não pode alcançá-los agarrando a borda.
            var s = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            try
            {
                float reach = s.JumpHeight + s.LedgeHandHeight + s.LedgeGrabWindow;
                Assert.Less(reach, 6f, $"Alcance agarrando borda = {reach:0.00} tiles");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void DefaultSettings_CrouchFitsOneTilePassage_AndMatchApprovedArt()
        {
            var s = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            try
            {
                Assert.Less(s.CrouchHeight, 1f, "Agachado precisa caber numa passagem de 1 tile");
                Assert.AreEqual(22f / 16f, s.StandingHeight, 1e-4f, "Gato em pé = 22 px");
                Assert.Less(s.ColliderWidth, 1f, "Gato precisa caber num vão de 1 tile de largura");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }
    }
}
