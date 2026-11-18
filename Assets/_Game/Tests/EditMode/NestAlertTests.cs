using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>
    /// F7 da Floresta, o Ninho Adormecido: o medidor de alerta da Isyathy, o bote de busca e os galhos frágeis
    /// (Docs/Design/floresta-mecanicas.md §4). Só as regras puras (<see cref="IsyathyRules"/>); o resto é o roteiro manual
    /// em Docs/QA/floresta-f7-plano-de-teste.md.
    /// </summary>
    public class NestAlertTests
    {
        const string Art = "Assets/_Game/Art/Environments/Forest/Placeholder";

        [Test]
        public void GeneratedArt_HasStrikeHeadAndBits()
        {
            var missing = new List<string>();
            foreach (var n in new[] { "isy_bote_0", "isy_bote_1", "isy_aviso", "isy_escama", "folha_caindo", "isy_olhos_0", "isy_olhos_1", "isy_olhos_2" })
                if (!File.Exists($"{Art}/{n}.png")) missing.Add(n);
            Assert.IsEmpty(missing, "Rode Tools/EnvironmentSprites/gerar_ninho.ps1. Faltam: " + string.Join(", ", missing));
        }

        [TestCase(0f, NestState.Asleep)]
        [TestCase(24.9f, NestState.Asleep)]
        [TestCase(25f, NestState.Restless)]
        [TestCase(49.9f, NestState.Restless)]
        [TestCase(50f, NestState.Agitated)]
        [TestCase(79.9f, NestState.Agitated)]
        [TestCase(80f, NestState.Searching)]
        [TestCase(100f, NestState.Searching)]
        public void States_FollowTheBands(float alert, NestState expected) =>
            Assert.AreEqual(expected, IsyathyRules.StateOf(alert));

        [Test]
        public void Breath_GetsFasterAsSheWakes()
        {
            Assert.AreEqual(4f, IsyathyRules.BreathPeriod(NestState.Asleep));
            Assert.Greater(IsyathyRules.BreathPeriod(NestState.Asleep), IsyathyRules.BreathPeriod(NestState.Restless));
            Assert.Greater(IsyathyRules.BreathPeriod(NestState.Restless), IsyathyRules.BreathPeriod(NestState.Agitated));
            Assert.Greater(IsyathyRules.BreathPeriod(NestState.Agitated), IsyathyRules.BreathPeriod(NestState.Searching));
        }

        [Test]
        public void Motion_ClassifiesHowTheCatMoves()
        {
            const float speed = 7f, run = 0.6f;
            Assert.AreEqual(NestMotion.Still, IsyathyRules.MotionOf(0f, 0f, true, false, false, speed, run));
            Assert.AreEqual(NestMotion.CrouchMove, IsyathyRules.MotionOf(3f, 0f, true, true, false, speed, run));
            Assert.AreEqual(NestMotion.Walk, IsyathyRules.MotionOf(3f, 0f, true, false, false, speed, run));
            Assert.AreEqual(NestMotion.Run, IsyathyRules.MotionOf(7f, 0f, true, false, false, speed, run));
            Assert.AreEqual(NestMotion.Air, IsyathyRules.MotionOf(7f, -5f, false, false, false, speed, run));
            Assert.AreEqual(NestMotion.Climb, IsyathyRules.MotionOf(0f, 3f, false, false, true, speed, run));
        }

        [Test]
        public void Gains_MatchTheDesignTable()
        {
            Assert.AreEqual(0f, IsyathyRules.MotionGain(NestMotion.Still));
            Assert.AreEqual(1f, IsyathyRules.MotionGain(NestMotion.CrouchMove));
            Assert.AreEqual(4f, IsyathyRules.MotionGain(NestMotion.Walk));
            Assert.AreEqual(10f, IsyathyRules.MotionGain(NestMotion.Run));
            Assert.AreEqual(0f, IsyathyRules.MotionGain(NestMotion.Air), "no ar não conta: a aterrissagem é que faz barulho");
            Assert.AreEqual(15f, IsyathyRules.NoiseGain("aterrissagem", 0.4f));
            Assert.AreEqual(20f, IsyathyRules.NoiseGain("golpe", 0.3f));
            Assert.AreEqual(5f, IsyathyRules.NoiseGain("bau", 0.4f));
            Assert.AreEqual(0f, IsyathyRules.NoiseGain("passo", 0.15f), "os passos já entram no ganho contínuo");
        }

        [Test]
        public void Trunk_ThreeHitsAndTheBreak_CostAboutSixty()
        {
            // cada golpe no tronco: "golpe" (+20) e logo "tronco" (+12, devolvendo o golpe); a quebra: "golpe" e "tronco_quebra"
            float alert = 0f;
            for (int i = 0; i < 3; i++)
            {
                alert = IsyathyRules.Add(alert, IsyathyRules.NoiseGain("golpe", 0.3f));
                alert = IsyathyRules.Add(alert, IsyathyRules.NoiseGain("tronco", 0.6f) - IsyathyRules.SwingRefund("tronco", 0.1f, true));
            }
            alert = IsyathyRules.Add(alert, IsyathyRules.NoiseGain("golpe", 0.3f));
            alert = IsyathyRules.Add(alert, IsyathyRules.NoiseGain("tronco_quebra", 0.6f) - IsyathyRules.SwingRefund("tronco_quebra", 0.1f, true));
            Assert.AreEqual(61f, alert, 0.01f);
            Assert.Less(alert, IsyathyRules.SearchAt, "a rota do martelo custa caro, mas sozinha não dispara o bote");
        }

        [Test]
        public void SwingRefund_OnlyRightAfterASwing()
        {
            Assert.AreEqual(0f, IsyathyRules.SwingRefund("tronco", 2f, true), "barulho do tronco muito depois do golpe");
            Assert.AreEqual(0f, IsyathyRules.SwingRefund("tronco", 0.1f, false), "o golpe já foi devolvido");
            Assert.AreEqual(0f, IsyathyRules.SwingRefund("bau", 0.1f, true));
        }

        [Test]
        public void Decay_DependsOnCoverAndStillness()
        {
            Assert.AreEqual(30f, IsyathyRules.DecayRate(moving: true, hidden: false, stillFor: 0f, calm: true), "clareira do musgo");
            Assert.AreEqual(0f, IsyathyRules.DecayRate(moving: true, hidden: true, stillFor: 0f, calm: false));
            Assert.AreEqual(5f, IsyathyRules.DecayRate(moving: false, hidden: true, stillFor: 0.5f, calm: false), "escondido, mas parado há pouco");
            Assert.AreEqual(15f, IsyathyRules.DecayRate(moving: false, hidden: true, stillFor: 1f, calm: false));
            Assert.AreEqual(5f, IsyathyRules.DecayRate(moving: false, hidden: false, stillFor: 3f, calm: false));
        }

        [Test]
        public void Step_ClampsBetweenZeroAndHundred()
        {
            Assert.AreEqual(0f, IsyathyRules.Step(2f, 0f, 15f, 1f));
            Assert.AreEqual(100f, IsyathyRules.Step(95f, 10f, 0f, 1f));
            Assert.AreEqual(14f, IsyathyRules.Step(10f, 4f, 0f, 1f), 0.001f);
        }

        [Test]
        public void Running_ReachesSearchInEightSeconds_CrouchingNever()
        {
            float run = 0f, crouch = 0f;
            for (int i = 0; i < 80; i++)
            {
                run = IsyathyRules.Step(run, IsyathyRules.RunGain, 0f, 0.1f);
                crouch = IsyathyRules.Step(crouch, IsyathyRules.CrouchMoveGain, 0f, 0.1f);
            }
            Assert.GreaterOrEqual(run, IsyathyRules.SearchAt - 0.01f);
            Assert.AreEqual(NestState.Asleep, IsyathyRules.StateOf(crouch), "8 s agachado mal acorda a Isyathy");
        }

        [Test]
        public void Strike_NeedsSearchCooldownAndNoStrikeInProgress()
        {
            Assert.IsTrue(IsyathyRules.CanStrike(80f, 0f, false));
            Assert.IsFalse(IsyathyRules.CanStrike(79f, 0f, false));
            Assert.IsFalse(IsyathyRules.CanStrike(90f, 1f, false), "4 s sem bote depois de um");
            Assert.IsFalse(IsyathyRules.CanStrike(90f, 0f, true));
            Assert.Less(IsyathyRules.AfterStrike, IsyathyRules.SearchAt, "depois do bote o alerta fica abaixo da busca");
            Assert.GreaterOrEqual(IsyathyRules.AfterStrike, IsyathyRules.AgitatedAt, "e não zera");
        }

        [Test]
        public void Strike_HitsAFourByThreeArea_ForHalfAHeart()
        {
            var ground = new Vector2(10f, 5f);
            Assert.IsTrue(IsyathyRules.InStrike(new Vector2(10f, 5f), ground));
            Assert.IsTrue(IsyathyRules.InStrike(new Vector2(11.9f, 7.9f), ground));
            Assert.IsFalse(IsyathyRules.InStrike(new Vector2(12.5f, 5f), ground), "fora da largura");
            Assert.IsFalse(IsyathyRules.InStrike(new Vector2(10f, 8.5f), ground), "acima da área");
            Assert.IsFalse(IsyathyRules.InStrike(new Vector2(10f, 4f), ground), "num patamar de baixo");
            Assert.AreEqual(1, IsyathyRules.StrikeDamage, "meio coração = 1 hit [APROVADO]");
            Assert.AreEqual(1.2f, IsyathyRules.WarningTime);
            var area = IsyathyRules.StrikeArea(ground);
            Assert.IsTrue(area.Contains(new Vector2(8.2f, 6f)));
            Assert.IsFalse(area.Contains(new Vector2(7.5f, 6f)));
        }

        [Test]
        public void FragileBranch_FallsOnlyWhenAgitatedAndStill()
        {
            Assert.IsFalse(IsyathyRules.BranchFalls(5f, NestState.Restless), "inquieta: o galho aguenta");
            Assert.IsFalse(IsyathyRules.BranchFalls(1.4f, NestState.Agitated));
            Assert.IsTrue(IsyathyRules.BranchFalls(1.5f, NestState.Agitated));
            Assert.IsTrue(IsyathyRules.BranchFalls(1.5f, NestState.Searching));
            Assert.AreEqual(0f, IsyathyRules.TrembleAmplitude(NestState.Restless));
            Assert.Greater(IsyathyRules.TrembleAmplitude(NestState.Agitated), 0f);
            Assert.Greater(IsyathyRules.TrembleAmplitude(NestState.Searching), IsyathyRules.TrembleAmplitude(NestState.Agitated));
            Assert.AreEqual(8f, IsyathyRules.BranchRegrow, "cresce de volta: nunca bloqueia a rota");
        }

        [Test]
        public void HudEye_OpensWithTheBands()
        {
            Assert.AreEqual(0, IsyathyRules.EyeStage(NestState.Asleep));
            Assert.AreEqual(1, IsyathyRules.EyeStage(NestState.Restless));
            Assert.AreEqual(2, IsyathyRules.EyeStage(NestState.Agitated));
            Assert.AreEqual(2, IsyathyRules.EyeStage(NestState.Searching));
        }

        [Test]
        public void RootBark_UsesTheScriptLines()
        {
            CollectionAssert.AreEqual(new[] { "...Isso não é raiz.", "Devagar, Lion. Bem devagar." }, IsyathyAlert.RootLines);
        }
    }
}
