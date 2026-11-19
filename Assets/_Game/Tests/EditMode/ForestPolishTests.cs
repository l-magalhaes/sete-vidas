using System.Collections.Generic;
using NUnit.Framework;
using SeteVidas.Audio;
using SeteVidas.Dialogue;
using SeteVidas.Progression;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Melhorias da Floresta de 19/11/2026: a chegada das levas da arena, a lojinha da Tobi, o espírito pela floresta, a
    /// corrupção da Isyathy por fase, o resgate da Vulpis (F6) e o som da Floresta (F10).
    /// </summary>
    public class ForestPolishTests
    {
        // ------------------------------------------------------------ a arena

        [Test]
        public void Arena_LevaVemDoFundoETerminaNoLugar()
        {
            Assert.AreEqual(ArenaRoom.ArriveFarScale, ArenaRoom.ArriveScale(0f), 1e-4f, "lá no fundo, pequena");
            Assert.AreEqual(1f, ArenaRoom.ArriveScale(1f), 1e-4f, "chega no tamanho do jogo");
            Assert.Greater(ArenaRoom.ArriveOffset(0f).y, 1f, "sai mais alto (o horizonte do fundo)");
            Assert.AreEqual(Vector2.zero, ArenaRoom.ArriveOffset(1f), "termina exatamente no lugar dela");
            float last = 0f;
            for (int i = 0; i <= 10; i++)
            {
                float s = ArenaRoom.ArriveScale(i / 10f);
                Assert.GreaterOrEqual(s, last - 1e-5f, "só cresce");
                last = s;
            }
        }

        // ------------------------------------------------------------ a Tobi

        [Test]
        public void Tobi_ConvidaParaALojinhaUmaVezDepoisDoEncontro()
        {
            Assert.IsFalse(TobiRules.ShopPending(false, false), "antes do encontro, não");
            Assert.IsTrue(TobiRules.ShopPending(true, false), "a primeira conversa depois do encontro");
            Assert.IsFalse(TobiRules.ShopPending(true, true), "uma vez só");
            Assert.IsFalse(TobiRules.ShopPending((SeteVidas.Save.SaveData)null));
        }

        // ------------------------------------------------------------ o espírito

        [Test]
        public void Espirito_PrimeiroAsNaoOuvidas_Depois_Rodizio_E_02eSoComAPonte()
        {
            var heard = new bool[5];
            Assert.AreEqual(0, IsyathySpiritNpc.NextTalk(heard, true, false, 0));
            heard[0] = true; heard[1] = true;
            Assert.AreEqual(2, IsyathySpiritNpc.NextTalk(heard, true, false, 0), "a próxima que não ouviu");
            heard[2] = true; heard[3] = true;
            // ouvidas as 4 de sempre, sem ponte: a 02e não vale, volta o rodízio
            Assert.AreEqual(1, IsyathySpiritNpc.NextTalk(heard, true, false, 1));
            Assert.Less(IsyathySpiritNpc.NextTalk(heard, true, false, 7), 4, "nunca a da ponte sem a ponte");
            Assert.AreEqual(4, IsyathySpiritNpc.NextTalk(heard, true, true, 0), "com a ponte, a 02e");
            Assert.AreEqual(-1, IsyathySpiritNpc.NextTalk(new bool[0], false, false, 0));
        }

        // ------------------------------------------------------------ a corrupção

        [Test]
        public void Isyathy_CorrupcaoCresceACadaFase()
        {
            Assert.Greater(IsyathyArt.BlightThreshold(0), IsyathyArt.BlightThreshold(1));
            Assert.Greater(IsyathyArt.BlightThreshold(1), IsyathyArt.BlightThreshold(2));
            int[] purple = new int[3];
            for (int stage = 0; stage <= 2; stage++)
            {
                var seen = new HashSet<uint>();
                for (int u = 0; u < 600; u += 3)
                    for (float v = -0.5f; v <= 1f; v += 0.25f)
                    {
                        uint c = IsyathyArt.BodyColor(u, v, 0.2f, 12f, stage);
                        // roxo: o vermelho e o azul acima do verde (as cores da praga)
                        int r = (int)((c >> 16) & 0xff), g = (int)((c >> 8) & 0xff), b = (int)(c & 0xff);
                        if (b > g + 20 && r > g) purple[stage]++;
                    }
            }
            Assert.Greater(purple[1], purple[0], "a fase II tem mais praga que a I");
            Assert.Greater(purple[2], purple[1], "a fase III tem mais praga que a II");
            Assert.AreEqual(IsyathyArt.BodyColor(40f, 0.5f, 0.2f, 12f), IsyathyArt.BodyColor(40f, 0.5f, 0.2f, 12f, 0), "o Ninho continua igual");
        }

        [Test]
        public void Isyathy_CabecaMaisRoxaNaFaseIII()
        {
            int Count(uint[] px)
            {
                int n = 0;
                foreach (var c in px)
                {
                    int r = (int)((c >> 16) & 0xff), g = (int)((c >> 8) & 0xff), b = (int)(c & 0xff);
                    if ((c >> 24) != 0 && b > g + 20 && r > g) n++;
                }
                return n;
            }
            var a = IsyathyArt.Head(IsyathyArt.FrameAwake, true, 0);
            var c3 = IsyathyArt.Head(IsyathyArt.FrameAwake, true, 2);
            Assert.Greater(Count(c3), Count(a));
        }

        [Test]
        public void Isyathy_CuspeCobre40PorCentoDaArena()
        {
            var inside = new Rect(0f, 0f, 40f, 20f);
            float hw = SeteVidas.Enemies.Boss.PoisonPuddle.HalfWidth;
            Assert.AreEqual(15, SeteVidas.Enemies.Boss.IsyathyBossRules.SpitShots, "o triplo das 5 de antes");
            foreach (float aim in new[] { 20f, 1f, 39f })
            {
                var xs = SeteVidas.Enemies.Boss.IsyathyBossRules.SpitTargets(inside, aim, hw);
                var sorted = (float[])xs.Clone();
                System.Array.Sort(sorted);
                float from = sorted[0] - hw, to = sorted[sorted.Length - 1] + hw;
                Assert.AreEqual(16f, to - from, 1e-3f, "40% de 40 tiles");
                Assert.GreaterOrEqual(from, inside.xMin - 1e-3f, "dentro da arena");
                Assert.LessOrEqual(to, inside.xMax + 1e-3f);
                for (int i = 1; i < sorted.Length; i++)
                {
                    Assert.Greater(sorted[i] - sorted[i - 1], 1e-3f, "cada bola num lugar");
                    Assert.LessOrEqual(sorted[i] - sorted[i - 1], hw * 2f, "sem buraco entre as poças");
                }
            }
        }

        // ------------------------------------------------------------ o resgate (F6)

        [Test]
        public void Resgate_UmaVezSoEDepoisDoPonto()
        {
            Assert.IsTrue(VulpisRescue.ShouldStart(false, true, 10f, 9f));
            Assert.IsFalse(VulpisRescue.ShouldStart(true, true, 10f, 9f), "já vista");
            Assert.IsFalse(VulpisRescue.ShouldStart(false, false, 10f, 9f), "gato morto");
            Assert.IsFalse(VulpisRescue.ShouldStart(false, true, 8f, 9f), "antes do ponto");
        }

        [Test]
        public void Vulpis_VaiParaOTemploSoComAChave()
        {
            Assert.IsTrue(VulpisDeparture.ShouldLeave(true, true, false, true));
            Assert.IsFalse(VulpisDeparture.ShouldLeave(false, true, false, true), "antes do vínculo, não");
            Assert.IsFalse(VulpisDeparture.ShouldLeave(true, false, false, true), "sem a chave, fica no posto");
            Assert.IsFalse(VulpisDeparture.ShouldLeave(true, true, true, true), "já foi");
            Assert.IsFalse(VulpisDeparture.ShouldLeave(true, true, false, false), "o gato longe");
        }

        [Test]
        public void TrilhaDeFlechas_DesenhosTemCor()
        {
            var arrow = VulpisTrailArt.Arrow(150f);
            Assert.AreEqual(VulpisTrailArt.ArrowSize * VulpisTrailArt.ArrowSize, arrow.Length);
            Assert.Greater(System.Array.FindAll(arrow, c => c != 0u).Length, 10);
            var wasp = VulpisTrailArt.DeadWasp();
            Assert.Greater(System.Array.FindAll(wasp, c => c != 0u).Length, 40);
        }

        // ------------------------------------------------------------ o som (F10)

        [Test]
        public void Som_InfectadoMixaIgualAoNormal()
        {
            var a = SfxCatalog.For("galho_estalo");
            var b = SfxCatalog.For("galho_estalo" + SfxCatalog.InfectedSuffix);
            Assert.AreEqual(a.Volume, b.Volume);
            Assert.AreEqual(a.Range, b.Range);
            // telégrafos acima dos golpes (a regra da mixagem da Floresta)
            Assert.Greater(SfxCatalog.For("isyathy_tremor").Volume, SfxCatalog.For("isyathy_bote").Volume);
            Assert.Greater(SfxCatalog.For("vulpis_arco_tensiona").Volume, SfxCatalog.For("vulpis_flecha_voa").Volume);
        }

        [Test]
        public void Som_RespiracaoDoNinhoAceleraPertoDoBote()
        {
            Assert.AreEqual(0, IsyathyBurrow.BreathLevel(true, 10f), "dormindo: 4 s");
            Assert.AreEqual(1, IsyathyBurrow.BreathLevel(false, 3f));
            Assert.AreEqual(2, IsyathyBurrow.BreathLevel(false, 1.5f), "o bote vem aí");
            Assert.Greater(IsyathyBurrow.BreathCycle[0], IsyathyBurrow.BreathCycle[2]);
        }

        [Test]
        public void Som_AmbienciaDaMenorZona()
        {
            var areas = new List<Rect> { new Rect(0, 0, 100, 100), new Rect(10, 10, 20, 20) };
            Assert.AreEqual(1, AmbienceZone.Pick(areas, new Vector2(15, 15)), "a sala dentro da outra vence");
            Assert.AreEqual(0, AmbienceZone.Pick(areas, new Vector2(50, 50)));
            Assert.AreEqual(-1, AmbienceZone.Pick(areas, new Vector2(-5, 0)));
        }

        // ------------------------------------------------------------ a sombra projetada

        [Test]
        public void Sombra_SegueASilhuetaECaiParaADireita()
        {
            // um bloco de 4 × 4 px, 6 px acima do chão
            int mw = 4, mh = 4;
            var mask = new bool[mw * mh];
            for (int i = 0; i < mask.Length; i++) mask[i] = true;
            var lv = ShadowCast.Sweep(mask, mw, mh, 6, 0.5f, false, 0, out int w, out int h);
            Assert.AreEqual(10, h, "do chão até o alto do objeto");
            Assert.Greater(w, mw, "a luz empurra a sombra para a direita");
            for (int y = 6; y < 10; y++) for (int x = 0; x < 4; x++) Assert.AreEqual(0, lv[y * w + x], "o objeto não fica embaixo da própria sombra");
            Assert.Greater(lv[0 * w + 4], 0, "no chão, a sombra chega deslocada para a direita");
            Assert.AreEqual(0, lv[0 * w + 0], "e não fica embaixo do lado esquerdo (a lua está à esquerda)");
            Assert.Greater(lv[5 * w + 1], 0, "logo embaixo do objeto, sombra");
            // uma silhueta com um recorte: a sombra tem o recorte também (segue o desenho, não um retângulo)
            var notch = new bool[8 * 4];
            for (int y = 0; y < 4; y++) for (int x = 0; x < 8; x++) notch[y * 8 + x] = !(x >= 3 && x <= 4);
            var lv2 = ShadowCast.Sweep(notch, 8, 4, 1, 0f, false, 0, out int w2, out int h2);
            Assert.AreEqual(0, lv2[0 * w2 + 3], "pelo vão do desenho a luz passa");
            Assert.Greater(lv2[0 * w2 + 1], 0);
        }

        [Test]
        public void Som_VozesDaFloresta()
        {
            var have = new HashSet<string> { "vox_vulpis_neutro", "vox_vulpis_serio", "vox_tobi_neutro", "vox_tobi_gananciosa", "vox_isyathy_neutro" };
            System.Func<string, bool> has = have.Contains;
            SpeakerProfile S(string n) { var s = ScriptableObject.CreateInstance<SpeakerProfile>(); s.DisplayName = n; return s; }
            Assert.AreEqual("vox_vulpis_serio", DialogueRunner.VoiceClipId(S("Vulpis"), "serio", "Agora, gato!", has));
            Assert.AreEqual("vox_vulpis_neutro", DialogueRunner.VoiceClipId(S("Vulpis"), "triste", "...", has));
            Assert.AreEqual("vox_tobi_gananciosa", DialogueRunner.VoiceClipId(S("Tobi"), "gananciosa", "Vinte moedas.", has));
            Assert.AreEqual("vox_isyathy_neutro", DialogueRunner.VoiceClipId(S("Isyathy"), "", "Pequeno que cai.", has));
        }
    }
}
