using NUnit.Framework;
using UnityEngine;
using R = SeteVidas.Enemies.ForestEnemyRules;

namespace SeteVidas.Tests
{
    /// <summary>
    /// F4 da Floresta (bestiário): a variante infectada de cada inimigo (Docs/Design/floresta-bestiario.md). A normal
    /// continua coberta pelos testes da F1 e da F2; aqui só o que muda com a praga.
    /// </summary>
    public class ForestBestiaryTests
    {
        static R.BranchState RunBranchUntilStuck(bool infected, out float windup, out float stuck)
        {
            var s = R.BranchState.Awake;
            float t = 0f;
            s = R.StepBranch(s, ref t, 0f, 1f, false, infected);
            Assert.AreEqual(R.BranchState.Windup, s);
            windup = t;
            while (s == R.BranchState.Windup) s = R.StepBranch(s, ref t, 0.01f, 1f, false, infected);
            stuck = t;
            return s;
        }

        [Test]
        public void Branch_Infected_WarnsShorter_AndStaysStuckLess()
        {
            Assert.AreEqual(R.BranchState.Stuck, RunBranchUntilStuck(false, out float w0, out float s0));
            Assert.AreEqual(R.BranchState.Stuck, RunBranchUntilStuck(true, out float w1, out float s1));
            Assert.AreEqual(0.6f, w0, 1e-4f, "Normal: aviso de 0,6 s");
            Assert.AreEqual(0.4f, w1, 1e-4f, "Infectado: aviso de 0,4 s");
            Assert.AreEqual(1f, s0, 1e-4f);
            Assert.AreEqual(0.7f, s1, 1e-4f, "Infectado: braço preso só 0,7 s");
            Assert.Less(R.BranchHealthFor(false), R.BranchHealthFor(true));
        }

        [Test]
        public void Branch_Infected_EyesBlink_NowAndThen()
        {
            int lit = 0;
            for (float t = 0f; t < R.BranchBlinkEvery * 4f; t += 0.05f) if (R.BranchEyesLit(t)) lit++;
            Assert.Greater(lit, 0, "Pisca de vez em quando: dá para notar o tronco falso");
            Assert.Less(lit, 30, "Mas fica apagado a maior parte do tempo");
        }

        [Test]
        public void Plant_Infected_ReachesFarther_AndRecoversFaster()
        {
            Assert.Greater(R.PlantReachFor(true), R.PlantReachFor(false));
            float t = 0f;
            var s = R.StepPlant(R.PlantState.Idle, ref t, 0f, true, infected: true);
            Assert.AreEqual(R.PlantWindupInfected, t, 1e-4f);
            // até a volta (pogo)
            while (s != R.PlantState.Retract) s = R.StepPlant(s, ref t, 0.01f, true, infected: true);
            Assert.AreEqual(R.PlantRetractInfected, t, 0.011f);
            Assert.AreEqual(1f, R.PlantExtension(s, t, true), 0.03f, "A volta começa lá na ponta");
            Assert.IsTrue(R.PlantVulnerable(s), "Pogo só na volta, como a normal");
        }

        [Test]
        public void Snake_Infected_SeesFarther_ButFoliageStillHides()
        {
            float t = 0f;
            Assert.AreEqual(R.SnakeState.Hidden, R.StepSnake(R.SnakeState.Hidden, ref t, 0.1f, 3.5f, false), "Normal: 3,5 tiles é longe");
            Assert.AreEqual(R.SnakeState.Warn, R.StepSnake(R.SnakeState.Hidden, ref t, 0.1f, 3.5f, false, infected: true), "Infectada: vê a 4");
            Assert.AreEqual(R.SnakeState.Hidden, R.StepSnake(R.SnakeState.Hidden, ref t, 0.1f, 2f, true, infected: true), "Escondido na folhagem, nem a infectada vê");
        }

        [Test]
        public void Snake_Infected_ShorterWindow_AndBackOutSooner()
        {
            float t = 0f;
            var s = R.StepSnake(R.SnakeState.Strike, ref t, 1f, 2f, false, infected: true);
            Assert.AreEqual(R.SnakeState.Stretched, s);
            Assert.AreEqual(0.4f, t, 1e-4f, "Esticada só 0,4 s");
            s = R.StepSnake(s, ref t, 0.5f, 2f, false, infected: true);
            Assert.AreEqual(R.SnakeState.Retreat, s);
            Assert.AreEqual(1f, t, 1e-4f, "No buraco 1 s");
            Assert.AreEqual(1f, R.SnakeExtension(s, t, true), 1e-4f, "Começa a voltar do bote inteiro");
            Assert.AreEqual(0f, R.SnakeExtension(s, 0.5f, true), 1e-4f, "E já entrou no meio do recuo");
        }

        [Test]
        public void Bat_Infected_WakesEasier_FliesFaster_ComesBackSooner()
        {
            Assert.Less(R.BatNoiseFor(true), R.BatNoiseFor(false));
            Assert.AreEqual(9f, R.BatWakeRangeFor(true), 1e-4f);
            Assert.AreEqual(4f, R.BatWakeRangeFor(false, 4f), 1e-4f, "Normal: o alcance do lugar");
            Assert.Greater(R.BatWakeRangeFor(true, 4f), 4f, "Infectado: mais longe que o do lugar");
            Assert.Greater(R.BatSpeedFor(true), R.BatSpeedFor(false));
            float t = 0f;
            var s = R.StepBat(R.BatState.Chase, ref t, 0.1f, 0.5f, false, false, true, false, R.BatWakeRange, infected: true);
            Assert.AreEqual(R.BatState.Recoil, s);
            Assert.AreEqual(R.BatRecoilInfected, t, 1e-4f, "Volta logo: investidas seguidas");
            Assert.AreEqual(3, R.BatHealthFor(true));
        }

        [Test]
        public void Bat_Infected_Zigzag_ChangesSide()
        {
            int a = R.BatZigzagSide(0.1f), b = R.BatZigzagSide(0.1f + 1f / R.BatZigzagRate);
            Assert.AreEqual(-a, b, "Troca de lado a cada degrau");
        }

        [Test]
        public void Cactus_Infected_SwellsHalfASecond_ThenFiresInACross()
        {
            Assert.IsFalse(R.CactusSwelling(0f));
            Assert.IsFalse(R.CactusSwelling(2.4f));
            Assert.IsTrue(R.CactusSwelling(2.6f), "Incha 0,5 s antes (aviso)");
            Assert.IsFalse(R.CactusFires(2.9f));
            Assert.IsTrue(R.CactusFires(3f));
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < 4; i++) sum += R.CactusSpikeDir(i);
            Assert.AreEqual(Vector2.zero, sum, "Cruz: as quatro direções se anulam");
            Assert.AreNotEqual(R.CactusSpikeDir(0), R.CactusSpikeDir(1));
        }

        [Test]
        public void Cactus_Infected_JitterStaysWithinHalfATile()
        {
            for (float t = 0f; t < 20f; t += 0.37f)
            {
                var o = R.CactusJitterOffset(t, 12.3f);
                Assert.LessOrEqual(Mathf.Abs(o.x), R.CactusJitter + 1e-4f);
                Assert.LessOrEqual(Mathf.Abs(o.y), R.CactusJitter + 1e-4f);
            }
        }

        [Test]
        public void ToxicCloud_PoisonsOnce_AndOnlyInside()
        {
            var cloud = new Rect(0f, 0f, 2f, 2f);
            Assert.IsTrue(R.CloudPoisons(false, cloud, new Vector2(1f, 1f)));
            Assert.IsFalse(R.CloudPoisons(true, cloud, new Vector2(1f, 1f)), "Uma vez por nuvem: não deixa o veneno letal sozinha");
            Assert.IsFalse(R.CloudPoisons(false, cloud, new Vector2(3f, 1f)), "Fora da nuvem");
        }
    }
}
