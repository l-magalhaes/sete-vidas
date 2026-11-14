using NUnit.Framework;
using SeteVidas.Classes;
using SeteVidas.Player;
using SeteVidas.Progression;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Pedidos do usuário de 14/11/2026 (depois da F4b): corações temporários nos baús, escorregar nas rampas e a roda
    /// de troca rápida de classe.
    /// </summary>
    public class TempHeartSlideWheelTests
    {
        // ---------------------------------------------------------------- corações temporários

        [Test]
        public void TempHearts_AbsorbDamageFirst()
        {
            int temp = 2;
            int hits = HealthRules.ApplyDamage(4, ref temp, 1);
            Assert.AreEqual(4, hits, "O temporário segura o golpe");
            Assert.AreEqual(1, temp);
            hits = HealthRules.ApplyDamage(hits, ref temp, 2);
            Assert.AreEqual(3, hits, "Sobrou 1 de temporário: o outro sai da vida");
            Assert.AreEqual(0, temp);
        }

        [Test]
        public void TempHearts_CapAtTwo_AndOnlyInFreeSlots()
        {
            Assert.AreEqual(4, HealthRules.TempCap(3), "Até 2 corações");
            Assert.AreEqual(2, HealthRules.TempCap(6), "Com 6 corações, sobra uma vaga");
            Assert.AreEqual(0, HealthRules.TempCap(7), "Com 7, nenhuma");
            Assert.AreEqual(4, HealthRules.AddTemp(2, 4, 3), "Não passa do limite");
            Assert.AreEqual(2, HealthRules.AddTemp(0, 2, 2));
        }

        [Test]
        public void TempHearts_DrawFullThenHalf()
        {
            Assert.AreEqual(HeartState.Full, HealthRules.TempHeart(0, 3));
            Assert.AreEqual(HeartState.Half, HealthRules.TempHeart(1, 3));
            Assert.AreEqual(HeartState.Locked, HealthRules.TempHeart(2, 3));
            Assert.AreEqual(HeartState.Locked, HealthRules.TempHeart(0, 0));
        }

        // ---------------------------------------------------------------- escorregar nas rampas

        static readonly Vector2 RisesRight = SlopeMath.NormalFor(1f, 1f);   // / : desce para a esquerda

        [Test]
        public void Slide_DownhillUnlessPushingUphill()
        {
            Assert.AreEqual(-1, SlopeMath.DownhillDir(RisesRight));
            Assert.AreEqual(0, SlopeMath.DownhillDir(Vector2.up), "Plano não escorrega");
            Assert.IsTrue(SlopeMath.WantsSlide(RisesRight, 0f), "Parado: escorrega");
            Assert.IsTrue(SlopeMath.WantsSlide(RisesRight, -1f), "Rampa abaixo: escorrega");
            Assert.IsFalse(SlopeMath.WantsSlide(RisesRight, 1f), "Rampa acima: sobe andando");
        }

        [Test]
        public void Slide_GainsSpeed_UpToTheMax_FasterOnSteeper()
        {
            float vx = 0f;
            for (int i = 0; i < 10; i++) vx = SlopeMath.SlideStep(vx, RisesRight, 0f, 0.02f, 45f, 13f);
            Assert.Less(vx, 0f, "Desce para a esquerda");
            for (int i = 0; i < 500; i++) vx = SlopeMath.SlideStep(vx, RisesRight, 0f, 0.02f, 45f, 13f);
            Assert.AreEqual(-13f, vx, 1e-3f, "Para na máxima");
            float steep = SlopeMath.SlideStep(0f, RisesRight, 0f, 0.1f, 45f, 13f);
            float gentle = SlopeMath.SlideStep(0f, SlopeMath.NormalFor(1f, 2f), 0f, 0.1f, 45f, 13f);
            Assert.Greater(Mathf.Abs(steep), Mathf.Abs(gentle), "Mais inclinada, mais rápido");
        }

        [Test]
        public void Slide_Momentum_DecaysSlowly_ButTurningIsInstant()
        {
            float walk = 7f;
            Assert.AreEqual(12f - 10f * 0.1f, SlopeMath.Momentum(12f, 7f, 10f, 0.1f, walk), 1e-4f, "Embalo cai devagar");
            Assert.AreEqual(-3f, SlopeMath.Momentum(12f, -7f, 10f, 0.1f, -3f), "Virou: vale o andar normal");
            Assert.AreEqual(5f, SlopeMath.Momentum(5f, 7f, 10f, 0.1f, 5f), "Abaixo da de andar: normal");
        }

        [Test]
        public void Uphill_IsSlower_SteeperIsSlowest()
        {
            Assert.AreEqual(0.45f, SlopeMath.UphillFactor(RisesRight, 1f, 0.45f), 1e-3f, "Rampa de 45° subindo: 45% da velocidade");
            Assert.AreEqual(1f, SlopeMath.UphillFactor(RisesRight, -1f, 0.45f), "Descendo: normal");
            Assert.AreEqual(1f, SlopeMath.UphillFactor(Vector2.up, 1f, 0.45f), "Plano: normal");
            float gentle = SlopeMath.UphillFactor(SlopeMath.NormalFor(1f, 2f), 1f, 0.45f);
            Assert.Greater(gentle, 0.45f, "Rampa suave: menos lenta");
            Assert.Less(gentle, 1f);
        }

        // ---------------------------------------------------------------- roda de classes

        [Test]
        public void Wheel_SlotsGoClockwiseFromTheTop()
        {
            Assert.AreEqual(0, ClassRules.WheelSlotAt(Vector2.up, 7, 0.3f), "Em cima: Ladino");
            Assert.AreEqual(1, ClassRules.WheelSlotAt(new Vector2(0.8f, 0.6f), 7, 0.3f), "Direita-acima: Paladino");
            Assert.AreEqual(5, ClassRules.WheelSlotAt(new Vector2(-1f, -0.25f), 7, 0.3f), "Esquerda, um pouco abaixo: Caçador");
            Assert.AreEqual(-1, ClassRules.WheelSlotAt(new Vector2(0.1f, 0.1f), 7, 0.3f), "Zona morta");
            for (int i = 0; i < 7; i++)
            {
                float a = ClassRules.WheelSlotAngle(i, 7) * Mathf.Deg2Rad;
                Assert.AreEqual(i, ClassRules.WheelSlotAt(new Vector2(Mathf.Sin(a), Mathf.Cos(a)), 7, 0.3f), "O meio da fatia " + i);
            }
        }
    }
}
