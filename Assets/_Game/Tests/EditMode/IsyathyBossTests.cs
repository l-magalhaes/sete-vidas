using NUnit.Framework;
using SeteVidas.Enemies.Boss;
using UnityEngine;
using R = SeteVidas.Enemies.Boss.IsyathyBossRules;

namespace SeteVidas.Tests
{
    /// <summary>F8, a luta com a Isyathy (<see cref="IsyathyBossRules"/>).</summary>
    public class IsyathyBossTests
    {
        [Test]
        public void Phases_At65And30()
        {
            Assert.AreEqual(1, R.Phase(R.MaxHealth, R.MaxHealth));
            Assert.AreEqual(1, R.Phase(Mathf.CeilToInt(R.MaxHealth * 0.66f), R.MaxHealth));
            Assert.AreEqual(2, R.Phase(Mathf.FloorToInt(R.MaxHealth * 0.6f), R.MaxHealth));
            Assert.AreEqual(3, R.Phase(Mathf.FloorToInt(R.MaxHealth * 0.25f), R.MaxHealth));
        }

        [Test]
        public void Vulpis_ShootsWhenCrossing65And30_OnlyOnce()
        {
            int max = 100;
            Assert.IsTrue(R.CrossedVulpisMark(66, 64, max));
            Assert.IsFalse(R.CrossedVulpisMark(64, 60, max), "já tinha passado");
            Assert.IsTrue(R.CrossedVulpisMark(31, 28, max));
            Assert.IsFalse(R.CrossedVulpisMark(90, 80, max));
        }

        [Test]
        public void Moves_PhaseOne_FromTheFloorWallsAndCeiling_NeverTwiceInARow()
        {
            for (float roll = 0f; roll < 1f; roll += 0.05f)
            {
                var m = R.Pick(1, IsyathyMove.Lunge, roll);
                Assert.AreNotEqual(IsyathyMove.Lunge, m, "nunca o mesmo seguido");
                Assert.That(m == IsyathyMove.WallStrike || m == IsyathyMove.CeilingDrop, $"fase I: {m}");
            }
        }

        [Test]
        public void Moves_PhaseTwo_AddsTheArchAndTheSpit_NoMoreClouds()
        {
            var seen = new System.Collections.Generic.HashSet<IsyathyMove>();
            for (float roll = 0f; roll < 1f; roll += 0.01f) seen.Add(R.Pick(2, IsyathyMove.Lunge, roll));
            Assert.IsTrue(seen.Contains(IsyathyMove.Arch) && seen.Contains(IsyathyMove.Spit));
            Assert.IsFalse(System.Enum.IsDefined(typeof(IsyathyMove), "Spores"), "sem nuvens de fumaça: o cuspe vira poça");
        }

        [Test]
        public void Damage_BiggerWarning_BiggerDamage()
        {
            Assert.AreEqual(2, R.Damage(IsyathyMove.Lunge), "a mordida: um coração (18/11/2026, mais difícil)");
            Assert.AreEqual(2, R.Damage(IsyathyMove.WallStrike));
            Assert.AreEqual(1, R.Damage(IsyathyMove.Spit));
            Assert.AreEqual(2, R.Damage(IsyathyMove.CeilingDrop), "um coração");
            Assert.AreEqual(3, R.Damage(IsyathyMove.Lunge, finisher: true), "o fim da combinação da fase III: um coração e meio");
            Assert.Greater(R.Warning(IsyathyMove.CeilingDrop, 1), R.Warning(IsyathyMove.Lunge, 1));
            Assert.Less(R.Warning(IsyathyMove.CeilingDrop, 3), R.Warning(IsyathyMove.CeilingDrop, 1), "a fase III encurta");
        }

        [Test]
        public void Timings_AreReadable()
        {
            // pedido do usuário: "deixe a animação numa velocidade que possamos prever"
            for (int phase = 1; phase <= 3; phase++)
            {
                foreach (IsyathyMove m in System.Enum.GetValues(typeof(IsyathyMove)))
                {
                    Assert.GreaterOrEqual(R.Warning(m, phase), 0.6f, $"aviso (ainda dá para ver) de {m} na fase {phase}");
                    Assert.GreaterOrEqual(R.Exposed(m, phase), 0.8f, $"espera de {m} na fase {phase}");
                }
                Assert.GreaterOrEqual(R.AimTime(phase), 0.4f, "a mira antes do bote (agressiva, mas avisada)");
                Assert.LessOrEqual(R.TravelSpeed(phase), 16f, "o montinho dá para acompanhar com os olhos");
                Assert.Greater(R.TravelSpeed(phase, R.FarRun + 1f), R.TravelSpeed(phase, 4f), "de longe ela corre; perto, chega devagar");
                Assert.AreEqual(R.TravelSpeed(phase), R.TravelSpeed(phase, 4f));
                // do fim da caminhada ao bote: aviso + subida + mira, sem demorar (pedido: "demorando para sair e atacar")
                Assert.LessOrEqual(R.Warning(IsyathyMove.Lunge, phase) + R.RiseTime + R.AimTime(phase), 2.1f);
            }
            // o caminho mais longo (meia volta na F1, 64 tiles) leva menos de 4 s
            float far = 64f, time = (far - R.FarRun) / (R.TravelSpeed(1) * R.FarBoost) + R.FarRun / R.TravelSpeed(1);
            Assert.Less(time, 4f);
            Assert.GreaterOrEqual(R.StrikeTime, 0.2f, "o bote rápido (pede dash), mas visível");
            Assert.GreaterOrEqual(SeteVidas.World.BurrowRules.RiseTime + SeteVidas.World.BurrowRules.AimTime, 1.4f, "o bote do Ninho também ficou mais lento");
        }

        [Test]
        public void Head_TakesDamageAlways_MoreWhenTired()
        {
            Assert.AreEqual(2, R.HeadDamage(2, false, false), "fora da terra, a cabeça sempre leva dano (pedido de 18/11/2026)");
            Assert.AreEqual(3, R.HeadDamage(2, true, false), "cansada, na espera: o ponto fraco leva mais");
            Assert.AreEqual(3, R.HeadDamage(2, true, true), "atordoada pela flecha: leva mais");
            Assert.Greater(R.Exposed(IsyathyMove.Arch), R.Exposed(IsyathyMove.Lunge), "o Arco fica mais tempo: dá para ir no rabo e na cabeça");
        }

        [Test]
        public void Tripled_Life_AndReachesTheCat()
        {
            Assert.AreEqual(324, R.MaxHealth, "o dobro dos 162 (18/11/2026: \"dobre a vida\")");
            // da parede, alcança o meio da arena (38 de largo): não ataca o ar
            Assert.GreaterOrEqual(R.WallReach + R.RearOut, 18f);
            Assert.GreaterOrEqual(R.LungeReach, 10f, "do vão, alcança o vão vizinho");
            Assert.AreEqual(0.38f, R.AimDelay(1), 1e-4f, "mira onde o gato estava 0,38 s atrás (dá tempo do dash, mas apertado)");
        }

        [Test]
        public void Art_FangsWhenBiting_MayanAdornments_NoWounds()
        {
            uint tooth = SeteVidas.World.IsyathyArt.C("#fbf6ea"), gold = SeteVidas.World.IsyathyArt.C("#f0c040"), jade = SeteVidas.World.IsyathyArt.C("#36c8a0");
            var bite = SeteVidas.World.IsyathyArt.Head(SeteVidas.World.IsyathyArt.FrameBite, true);
            var awake = SeteVidas.World.IsyathyArt.Head(SeteVidas.World.IsyathyArt.FrameAwake, true);
            System.Func<uint[], uint, int> count = (px, c) => { int n = 0; foreach (var p in px) if (p == c) n++; return n; };
            Assert.Greater(count(bite, tooth), 25, "a boca aberta mostra as presas grandes");
            Assert.AreEqual(0, count(awake, tooth), "de boca fechada, nada de dente");
            Assert.Greater(count(awake, gold), 15, "a tiara, o medalhão e o brinco de ouro");
            Assert.Greater(count(awake, jade), 4, "o jade da deidade");
            // o corpo: sem feridas (a carne viva de antes) e com os anéis de ouro
            uint raw = SeteVidas.World.IsyathyArt.C("#7a2232");
            int golds = 0;
            for (float u = 0f; u < 400f; u += 1f)
                for (float v = -1f; v <= 1f; v += 0.1f)
                {
                    uint c = SeteVidas.World.IsyathyArt.BodyColor(u, v, 0.2f, 12f);
                    Assert.AreNotEqual(raw, c, "sem ferimentos");
                    if (c == gold) golds++;
                }
            Assert.Greater(golds, 20, "os anéis de ouro ao longo do corpo");
        }

        [Test]
        public void Body_TakesDamageEverywhere()
        {
            Assert.AreEqual(2, R.BodyDamage(2, false, false), "o corpo todo recebe dano");
            Assert.AreEqual(2, R.BodyDamage(2, true, false));
            Assert.AreEqual(3, R.BodyDamage(2, false, true), "atordoada: mais");
            Assert.LessOrEqual(R.BodyDamage(4, true, false), R.HeadDamage(4, true, false), "a cabeça continua sendo o ponto fraco");
            Assert.AreEqual(0, R.BodyDamage(4, true, true, true), "a casca da praga também cobre o corpo");
            Assert.GreaterOrEqual(R.StunTime, 5f, "a flecha que quebra a casca atordoa mais tempo");
        }

        [Test]
        public void Perimeter_GoesAroundTheFourSides()
        {
            var r = new Rect(0f, 0f, 38f, 26f);
            Assert.AreEqual(128f, R.Perimeter(r), 1e-3f);
            Vector2 p = R.SurfaceAt(r, 10f, out var n, out var side);
            Assert.AreEqual(ArenaSide.Floor, side); Assert.AreEqual(Vector2.up, n); Assert.AreEqual(new Vector2(10f, 0f), p);
            R.SurfaceAt(r, 38f + 5f, out n, out side);
            Assert.AreEqual(ArenaSide.Right, side); Assert.AreEqual(Vector2.left, n);
            p = R.SurfaceAt(r, 38f + 26f + 8f, out n, out side);
            Assert.AreEqual(ArenaSide.Ceiling, side); Assert.AreEqual(Vector2.down, n); Assert.AreEqual(new Vector2(30f, 26f), p);
            R.SurfaceAt(r, 128f - 3f, out n, out side);
            Assert.AreEqual(ArenaSide.Left, side); Assert.AreEqual(Vector2.right, n);
            // ida e volta: o ponto da borda devolve a mesma posição
            foreach (float u in new[] { 3f, 50f, 80f, 120f })
                Assert.AreEqual(u, R.ParamOf(r, R.SurfaceAt(r, u, out _, out _)), 1e-3f);
            Assert.AreEqual(1, R.ShortestDir(r, 0f, 10f));
            Assert.AreEqual(-1, R.ShortestDir(r, 0f, 120f), "pela parede da esquerda é mais perto");
            Assert.AreEqual(118f, R.Distance(r, 10f, 0f, 1), 1e-3f);
            Assert.AreEqual(0f, R.CraterAngle(Vector2.up), 1e-3f);
            Assert.AreEqual(180f, Mathf.Abs(Mathf.DeltaAngle(0f, R.CraterAngle(Vector2.down))), 1e-3f);
        }

        [Test]
        public void Sides_WallNearTheCat_SpitFromAfar()
        {
            var r = new Rect(0f, 0f, 38f, 26f);
            Assert.AreEqual(ArenaSide.Left, R.SideOf(IsyathyMove.WallStrike, 5f, r));
            Assert.AreEqual(ArenaSide.Right, R.SideOf(IsyathyMove.Spit, 5f, r), "o cuspe vem de longe: o arco se lê");
            Assert.AreEqual(ArenaSide.Ceiling, R.SideOf(IsyathyMove.CeilingDrop, 5f, r));
            Assert.AreEqual(ArenaSide.Floor, R.SideOf(IsyathyMove.Arch, 5f, r));
            Assert.GreaterOrEqual(R.WallHeight(-10f, r), 1.2f, "não sai rente ao chão");
            Assert.LessOrEqual(R.WallHeight(99f, r), 22f, "nem colada no teto");
        }

        [Test]
        public void Arch_HeadNearTheCat_TailOnTheOtherSide_NeverTheWholeBody()
        {
            var holes = new[] { 1.5f, 9f, 20f, 28f, 36f };
            var a = R.ArchHoles(holes, 14f);
            Assert.AreNotEqual(a.x, a.y);
            Assert.GreaterOrEqual(Mathf.Abs(holes[a.x] - 14f), 3f, "a cabeça não sai embaixo do gato");
            Assert.Greater(Mathf.Abs(holes[a.y] - 14f), Mathf.Abs(holes[a.x] - 14f), "o rabo sai do lado de longe");
            Assert.AreEqual(1, Mathf.Abs(a.x - a.y), "vãos vizinhos");
            // o corpo (22 tiles) nunca sai inteiro: a cabeça e o rabo para fora são menos da metade
            float tail = R.TailOut(SeteVidas.World.BurrowRules.BodyLength, 15f, R.RearOut);
            Assert.Less(tail + R.RearOut, SeteVidas.World.BurrowRules.BodyLength * 0.5f);
            Assert.AreEqual(0, R.ClosestHole(holes, 0f));
        }

        [Test]
        public void PoisonPuddle_HurtsOnlyWhileFresh_AndWhereItIs()
        {
            Assert.IsFalse(PoisonPuddle.Hurts(0.05f), "nascendo: ainda não");
            Assert.IsTrue(PoisonPuddle.Hurts(2f));
            Assert.IsFalse(PoisonPuddle.Hurts(PoisonPuddle.Life - 0.1f), "sumindo (pisca): já não");
            Vector2 c = new Vector2(10f, 3f);
            Assert.IsTrue(PoisonPuddle.Inside(c, new Vector2(10.8f, 3.05f)));
            Assert.IsFalse(PoisonPuddle.Inside(c, new Vector2(12f, 3f)), "fora da largura");
            Assert.IsFalse(PoisonPuddle.Inside(c, new Vector2(10f, 4.5f)), "pulando por cima");
            Assert.AreEqual(1, R.PuddleDamage, "meio coração");
        }

        [Test]
        public void Phase3_Chains_AndRestsLess()
        {
            Assert.IsTrue(R.Chains(1, 1), "mais agressiva: emenda 3 golpes já na fase I");
            Assert.IsFalse(R.Chains(1, 2));
            Assert.IsTrue(R.Chains(2, 2), "4 seguidos na fase II");
            Assert.IsTrue(R.Chains(3, 3));
            Assert.IsFalse(R.Chains(3, 4), "no máximo 5 seguidos");
            Assert.IsFalse(R.DashDodges, "o dash não dá invencibilidade: se a boca encostar, fere");
            Assert.Less(R.Rest(3), R.Rest(1));
        }

        [Test]
        public void Shell_BlocksEverything_UntilVulpisBreaksIt()
        {
            Assert.AreEqual(0, R.HeadDamage(2, true, false, true), "a casca da praga: nem exposta");
            Assert.AreEqual(0, R.HeadDamage(2, true, true, true), "nem atordoada");
            Assert.AreEqual(3, R.HeadDamage(2, true, false, false), "sem casca, cansada: leva (e mais)");
            Assert.IsFalse(R.VulpisBreaksShell(true, R.ShellTime - 0.1f), "a Vulpis ainda se prepara");
            Assert.IsTrue(R.VulpisBreaksShell(true, R.ShellTime));
            Assert.IsFalse(R.VulpisBreaksShell(false, 99f), "sem casca, sem flecha forte");
            Assert.Greater(R.ShellTime, 2f, "dá tempo de ver que nada passa");
        }

        [Test]
        public void CinematicZoom_StepsKeepTheAspect()
        {
            Assert.AreEqual(new Vector2Int(480, 270), SeteVidas.World.CinematicCamera.Reference(480, 270, 1f));
            Assert.AreEqual(new Vector2Int(320, 180), SeteVidas.World.CinematicCamera.Reference(480, 270, 1.5f));
            Assert.AreEqual(new Vector2Int(240, 135), SeteVidas.World.CinematicCamera.Reference(480, 270, 2f));
            Assert.AreEqual(new Vector2Int(480, 270), SeteVidas.World.CinematicCamera.Reference(480, 270, 0.5f), "nunca menor que a tela normal");
        }

        [Test]
        public void Vulpis_HasEveryAnimation_AndPortrait()
        {
            string dir = "Assets/_Game/Art/Characters/Vulpis";
            foreach (var clip in new[] { "idle", "ready", "talk", "sad", "smile", "run", "shoot", "crouch", "leap", "air_draw", "air_release", "fall", "land" })
            {
                Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(dir, clip)), clip);
                Assert.Greater(System.IO.Directory.GetFiles(System.IO.Path.Combine(dir, clip), "*.png").Length, 0, clip);
            }
            foreach (var p in new[] { "portrait_vulpis", "portrait_vulpis_serious", "portrait_vulpis_ironic", "portrait_vulpis_sad", "portrait_vulpis_smile" })
                Assert.IsTrue(System.IO.File.Exists("Assets/_Game/Art/UI/Portraits/" + p + ".png"), p);
        }

        [Test]
        public void Holes_NeverUnderTheCat()
        {
            var holes = new[] { 1.5f, 9f, 20f, 28f, 36f };
            Assert.AreEqual(4, R.FarHole(holes, 3f));
            Assert.AreEqual(0, R.FarHole(holes, 30f));
            int near = R.NearHole(holes, 9.5f, 3.5f);
            Assert.GreaterOrEqual(Mathf.Abs(holes[near] - 9.5f), 3.5f);
            Assert.AreEqual(1, R.NearHole(holes, 13f, 3.5f));
        }

        [Test]
        public void Spit_LandsWhereAimed()
        {
            Vector2 from = new Vector2(0f, 5f), to = new Vector2(6f, 0f);
            Vector2 v = PlagueSpit.LaunchVelocity(from, to, 0.9f);
            // simula a queda com a mesma gravidade (22)
            Vector2 p = from; float t = 0f;
            while (t < 0.9f) { float dt = 0.001f; v.y -= 22f * dt; p += v * dt; t += dt; }
            Assert.Less(Vector2.Distance(p, to), 0.15f);
        }

        [Test]
        public void Mound_DoesNotHurt()
        {
            Assert.IsFalse(R.MoundHurts, "o montinho de terra é só o aviso de onde ela está (pedido de 18/11/2026)");
        }

        [Test]
        public void Body_NeverLeavesTheGroundWhole()
        {
            Assert.GreaterOrEqual(R.MinBuried, 3f, "um pedaço do rabo fica sempre na terra");
            Assert.AreEqual(R.BodyLength - R.MinBuried, R.MaxOut, 1e-4f);
            // a arena F1 tem 26 de altura por dentro: a descida do teto vai até o chão + 3, com até 4,5 para o lado
            float drop = new Vector2(4.5f, 26f - 3f).magnitude + 1.5f;   // (+ a curva do trilho)
            Assert.LessOrEqual(drop, R.MaxOut, "do teto ao chão, o rabo ainda fica na terra");
            // o avanço da parede: arma (RearOut) + o alcance + a queda no fim
            Assert.LessOrEqual(R.RearOut + R.WallReach + 0.7f + 1.6f + 1f, R.MaxOut);
            Assert.AreEqual(10f + R.MaxOut, R.MaxHead(10f), 1e-4f);
        }

        [Test]
        public void Spirit_IsTheWholeSerpent_NotAFloatingHead()
        {
            var px = SeteVidas.World.IsyathySpiritArt.Pixels();
            int w = SeteVidas.World.IsyathySpiritArt.W, h = SeteVidas.World.IsyathySpiritArt.H, rows = 0;
            for (int y = 0; y < h; y++)
            {
                bool any = false;
                for (int x = 0; x < w && !any; x++) any = px[y * w + x].a > 150;
                if (any) rows++;
            }
            Assert.Greater(rows, 85, "o corpo inteiro, do rabo à cabeça (a cabeça sozinha tem ~25 px de altura)");
            Assert.Less(SeteVidas.World.IsyathySpiritArt.Radius(0f), 2f, "o rabo afina e se desfaz");
            var portrait = SeteVidas.World.IsyathySpiritArt.Portrait(48);
            int filled = 0;
            foreach (var p in portrait) if (p.a > 0) filled++;
            Assert.Greater(filled, 48 * 48 / 4, "o retrato mostra a cabeça");
        }

        [Test]
        public void AimsAtTheTrail_MouthHurts_BodyIsSolid()
        {
            // a mira atrasada: o golpe vai onde o gato estava (o rastro), não onde ele está
            Assert.GreaterOrEqual(R.AimDelay(1), R.AimDelay(2));
            Assert.GreaterOrEqual(R.AimDelay(2), R.AimDelay(3));
            Assert.Greater(R.AimDelay(3), 0.2f, "mesmo na fase III dá tempo de sair (o dash dura 0,2 s)");
            Assert.IsTrue(R.MouthOnly, "só a boca fere");
            Assert.IsTrue(R.BodyIsSolid, "o corpo não fere, mas não dá para atravessar");
        }
    }
}
