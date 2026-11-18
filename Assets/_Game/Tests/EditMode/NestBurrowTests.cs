using NUnit.Framework;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>
    /// F7, o Ninho (17/11/2026): a Isyathy embaixo da terra (<see cref="BurrowRules"/>), o desenho dela em código
    /// (<see cref="IsyathyArt"/>) e a montagem do corpo (<see cref="SerpentRaster"/>).
    /// </summary>
    public class NestBurrowTests
    {
        // ------------------------------------------------------------ o corpo

        [Test]
        public void Body_IsThickBehindTheHead_AndTapersToTheTail()
        {
            Assert.AreEqual(12f, BurrowRules.Radius(0f), 0.01f, "o pescoço casa com a cabeça (24 px de altura)");
            Assert.Greater(BurrowRules.Radius(6f), BurrowRules.Radius(0f), "mais grossa logo atrás da cabeça");
            Assert.Less(BurrowRules.Radius(BurrowRules.BodyLength), 4f, "o rabo afina");
            for (float d = 6f; d < BurrowRules.BodyLength; d += 1f)
                Assert.LessOrEqual(BurrowRules.Radius(d + 1f), BurrowRules.Radius(d) + 1e-4f);
        }

        [Test]
        public void Wave_IsPinnedAtTheNeck()
        {
            Assert.AreEqual(0f, BurrowRules.Wave(0f, 0.3f, 1.2f), 1e-5f);
            Assert.Greater(Mathf.Abs(BurrowRules.Wave(4f, 0.3f, 0.4f)), 0.01f);
        }

        [Test]
        public void Path_ContinuesStraight_BeforeTheStart()
        {
            var path = new[] { new Vector2(0f, -10f), new Vector2(0f, 0f), new Vector2(2f, 0f) };
            Assert.AreEqual(new Vector2(0f, -13f), BurrowRules.PointAt(path, -3f), "o corpo continua para o fundo da terra");
            Assert.AreEqual(new Vector2(1f, 0f), BurrowRules.PointAt(path, 11f));
            Assert.AreEqual(new Vector2(4f, 0f), BurrowRules.PointAt(path, 14f), "e para a frente depois do fim");
            Assert.AreEqual(Vector2.up, BurrowRules.DirectionAt(path, 5f));
            Assert.AreEqual(10f, BurrowRules.LengthTo(path, 1), 1e-4f);
        }

        // ------------------------------------------------------------ o bote

        [Test]
        public void Spot_TriesAheadOfTheCatFirst()
        {
            var right = BurrowRules.Offsets(1);
            Assert.AreEqual(BurrowRules.SpotOffset, right[0], "na frente de quem vai para a direita");
            Assert.AreEqual(-BurrowRules.SpotOffset, right[1]);
            Assert.AreEqual(-BurrowRules.SpotOffset, BurrowRules.Offsets(-1)[0]);
            Assert.Greater(BurrowRules.SpotOffset, 2.5f, "nunca embaixo do gato: o buraco abre ao lado");
        }

        [Test]
        public void Rise_ComesFromBelow_AndLeansAwayFromTheCat()
        {
            var hole = new Vector2(10f, 2f);
            var p = BurrowRules.RisePath(hole, 1);
            Assert.AreEqual(hole.y - BurrowRules.Depth, p[0].y, 1e-4f);
            Assert.AreEqual(hole, p[2], "passa pela boca do buraco");
            var rear = p[p.Length - 1];
            Assert.Less(rear.x, hole.x, "o gato à direita: ela se arma inclinada para a esquerda");
            Assert.AreEqual(hole.y + BurrowRules.RearHeight, rear.y, 1e-4f);
            var left = BurrowRules.RisePath(hole, -1);
            Assert.Greater(left[left.Length - 1].x, hole.x);
        }

        [Test]
        public void Lunge_ReachesTheCat_ButNotTooFar_NorIntoTheGround()
        {
            var rear = new Vector2(0f, 5f);
            var near = BurrowRules.LungeTarget(rear, new Vector2(3f, 3f), 2f);
            Assert.Greater(near.x, 3f, "passa um pouco do ponto");
            var far = BurrowRules.LungeTarget(rear, new Vector2(30f, 5f), 2f);
            Assert.LessOrEqual(Vector2.Distance(rear, far), BurrowRules.Reach + BurrowRules.Overshoot + 1e-3f, "alcance limitado");
            var low = BurrowRules.LungeTarget(rear, new Vector2(2f, 0f), 2f);
            Assert.GreaterOrEqual(low.y, 2f + BurrowRules.MinAbove - 1e-4f, "não morde dentro do chão");
        }

        [Test]
        public void Bite_OnlyWhenTheSnoutTouches()
        {
            Assert.IsTrue(BurrowRules.Bites(new Vector2(1f, 1f), new Vector2(1.5f, 1.2f)));
            Assert.IsFalse(BurrowRules.Bites(new Vector2(1f, 1f), new Vector2(3f, 1f)));
            Assert.AreEqual(2, BurrowRules.Damage, "a mordida tira um coração");
        }

        [Test]
        public void SafeZone_AroundSanctuaries()
        {
            var safe = new[] { new Vector2(10f, 0f) };
            Assert.IsTrue(BurrowRules.InSafeZone(new Vector2(12f, 1f), safe));
            Assert.IsFalse(BurrowRules.InSafeZone(new Vector2(20f, 1f), safe));
            Assert.IsFalse(BurrowRules.InSafeZone(new Vector2(12f, 1f), null));
        }

        [Test]
        public void Timing_GivesTimeToDodge()
        {
            Assert.GreaterOrEqual(BurrowRules.WarnTime + BurrowRules.RiseTime + BurrowRules.AimTime, 1.5f, "do tremor ao bote: tempo de sair");
            Assert.GreaterOrEqual(BurrowRules.CooldownMin, 2f, "não emenda um bote no outro");
            Assert.LessOrEqual(BurrowRules.CooldownMax, 4f, "4 a 5 botes no caminho (era 6–10 s)");
        }

        [Test]
        public void Smooth_PassesThroughThePoints_WithoutCorners()
        {
            var p = new[] { new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 4f), new Vector2(8f, 4f) };
            var s = BurrowRules.Smooth(p);
            for (int i = 0; i < p.Length; i++) Assert.Less(Vector2.Distance(p[i], s[i * BurrowRules.SmoothSteps]), 1e-4f);
            // na quina (4, 0) a direção muda aos poucos: nenhum passo vira mais que 50°
            for (int i = 2; i < s.Length; i++)
            {
                Vector2 a = s[i - 1] - s[i - 2], b = s[i] - s[i - 1];
                Assert.Less(Vector2.Angle(a, b), 50f, $"passo {i}");
            }
        }

        [Test]
        public void Body_WavesMore_WhenMovingFast()
        {
            Assert.Greater(BurrowRules.WaveSpeed(10f), BurrowRules.WaveSpeed(0f));
            Assert.Greater(BurrowRules.WaveAmplitude(0.1f, 10f), BurrowRules.WaveAmplitude(0.1f, 0f));
            Assert.LessOrEqual(BurrowRules.WaveAmplitude(0.1f, 1000f), 0.32f, "com teto");
            Assert.Greater(BurrowRules.EaseOutBack(0.7f), 1f, "a cabeça passa do ponto e volta");
            Assert.AreEqual(1f, BurrowRules.EaseOutBack(1f), 1e-4f);
        }

        [Test]
        public void Lunge_IsAnArc()
        {
            var arc = BurrowRules.LungeArc(new Vector2(0f, 3f), new Vector2(6f, 3f));
            Assert.AreEqual(new Vector2(6f, 3f), arc[arc.Length - 1]);
            Assert.Greater(arc[arc.Length / 2].y, 3f, "passa um pouco por cima");
        }

        // ------------------------------------------------------------ o desenho

        static uint At(uint[] px, int x, int y) => px[y * IsyathyArt.HeadW + x];

        [Test]
        public void Head_GreenEye_WithPurpleGlow_ClosedWhenAsleep()
        {
            var awake = IsyathyArt.Head(IsyathyArt.FrameAwake, true);
            var asleep = IsyathyArt.Head(IsyathyArt.FrameSleep, true);
            int purple = 0, green = 0;
            for (int y = IsyathyArt.EyeY - 8; y <= IsyathyArt.EyeY + 8; y++)
                for (int x = IsyathyArt.EyeX - 11; x <= IsyathyArt.EyeX + 11; x++)
                {
                    uint c = At(awake, x, y);
                    if (c == IsyathyArt.C("#c47bff") || c == IsyathyArt.C("#f0c9ff")) purple++;
                    if (c == IsyathyArt.C("#7ae05a")) green++;
                    Assert.AreNotEqual(IsyathyArt.C("#7ae05a"), At(asleep, x, y), "dormindo, o olho fica fechado");
                }
            Assert.Greater(purple, 6, "o halo roxo em volta do olho");
            Assert.Greater(green, 4);
        }

        [Test]
        public void Head_Bite_ShowsTeethAndTheDarkMouth()
        {
            var bite = IsyathyArt.Head(IsyathyArt.FrameBite, true);
            int teeth = 0, mouth = 0;
            foreach (var c in bite)
            {
                if (c == IsyathyArt.C("#fbf6ea")) teeth++;   // as presas (refeitas em 18/11/2026)
                if (c == IsyathyArt.C("#3a1430")) mouth++;
            }
            Assert.Greater(teeth, 5);
            Assert.Greater(mouth, 40);
        }

        [Test]
        public void Head_NeckMeetsTheBody()
        {
            var head = IsyathyArt.Head(IsyathyArt.FrameAwake, true);
            Assert.AreNotEqual(IsyathyArt.Clear, At(head, IsyathyArt.NeckX, IsyathyArt.NeckY));
            // do pescoço para baixo (em cima, o penacho cai para trás por cima do corpo e não conta), dobrado
            int below = 0;
            for (int y = IsyathyArt.NeckY; y < IsyathyArt.HeadH && At(head, IsyathyArt.NeckX, y) != IsyathyArt.Clear; y++) below++;
            int rows = 2 * below;
            Assert.AreEqual(2f * BurrowRules.Radius(0f), rows, 4.5f, "a altura do pescoço é a grossura do corpo ali (com o contorno)");
        }

        [Test]
        public void Body_PurpleShadowsTakeIt_NoWounds()
        {
            int raw = 0, vein = 0;
            for (float u = 0f; u < 22f * 16f; u += 1f)
                for (float v = -1f; v <= 1f; v += 0.1f)
                {
                    uint c = IsyathyArt.BodyColor(u, v, 0.2f, 14f);
                    if (c == IsyathyArt.C("#7a2232") || c == IsyathyArt.C("#b4465a")) raw++;
                    if (c == IsyathyArt.C("#3e2458") || c == IsyathyArt.C("#5c3680")) vein++;
                }
            Assert.AreEqual(0, raw, "sem ferimentos (pedido de 18/11/2026)");
            Assert.Greater(vein, 200, "as sombras roxas da praga tomando o corpo");
        }

        [Test]
        public void Raster_HidesTheBody_BelowTheGround_AtTheHole()
        {
            var r = new SerpentRaster(128, 128);
            r.Begin(0f, 0f);
            r.SetClip(true, 40f, 88f, 64f);
            var discs = new SerpentDisc[20];
            for (int i = 0; i < 20; i++) discs[i] = new SerpentDisc { X = 64f, Y = 30f + i * 3f, DirX = 0f, DirY = 1f, R = 12f, U = (19 - i) * 3f };
            r.Body(discs, 20);
            r.Finish();
            Assert.AreEqual(0u, r.Px[40 * 128 + 64], "embaixo do chão, no buraco: nada");
            Assert.AreNotEqual(0u, r.Px[80 * 128 + 64], "em cima do chão: o corpo");
            Assert.AreEqual(IsyathyArt.Outline, r.Px[80 * 128 + 64 - 13], "o contorno da silhueta");
        }
    }
}
