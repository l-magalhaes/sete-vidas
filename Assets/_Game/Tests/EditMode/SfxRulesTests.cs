using System.Collections.Generic;
using NUnit.Framework;
using SeteVidas.Audio;
using SeteVidas.Dialogue;
using UnityEngine;

namespace SeteVidas.Tests.EditMode
{
    /// <summary>Regras puras dos SFX e das vozes (Docs/Audio/plano-sfx.md).</summary>
    public class SfxRulesTests
    {
        [Test]
        public void IdOf_TiraPrefixoEVariacao()
        {
            Assert.AreEqual("gato_passo", Sfx.IdOf("sfx_gato_passo_03"));
            Assert.AreEqual("coracao_arrancado", Sfx.IdOf("sfx_coracao_arrancado_06"));
            Assert.AreEqual("ui_cursor", Sfx.IdOf("ui_cursor_01"));
            Assert.AreEqual("vox_lion_ironico", Sfx.IdOf("vox_lion_ironico_02"));
            Assert.AreEqual("serra_loop", Sfx.IdOf("sfx_serra_loop_01"));
        }

        [Test]
        public void Atenuacao_CheioPertoSomeNoAlcance()
        {
            Assert.AreEqual(1f, Sfx.Attenuation(0f, 20f));
            Assert.AreEqual(1f, Sfx.Attenuation(5f, 20f));          // até 35% do alcance: volume cheio
            Assert.AreEqual(0f, Sfx.Attenuation(20f, 20f));
            Assert.AreEqual(0f, Sfx.Attenuation(30f, 20f));
            Assert.Less(Sfx.Attenuation(15f, 20f), Sfx.Attenuation(10f, 20f));
            Assert.AreEqual(1f, Sfx.Attenuation(100f, 0f));         // alcance 0 = não espacial
        }

        [Test]
        public void Alcance_SegueORaioDoRuido()
        {
            // cacos (raio 10) soam mais longe que folhas (raio 3); sininhos (11) também
            Assert.Greater(SfxCatalog.For("cacos").Range, SfxCatalog.For("folhas").Range);
            Assert.Greater(SfxCatalog.For("sininhos").Range, SfxCatalog.For("folhas").Range);
            // o gato e a interface não são espaciais
            Assert.AreEqual(0f, SfxCatalog.For("gato_passo").Range);
            Assert.AreEqual(0f, SfxCatalog.For("ui_qualquer").Range);
            // o passo agachado é bem mais baixo que o normal (agachar = ruído 0)
            Assert.Less(SfxCatalog.For("gato_passo_agachado").Volume, SfxCatalog.For("gato_passo").Volume * 0.5f);
        }

        [Test]
        public void Mixagem_VolumeConformeAAcao()
        {
            float V(string id) => SfxCatalog.For(id).Volume;
            // o que pede reação vem acima do combate, que vem acima do corpo e da interface
            Assert.Greater(V("guarda_alerta"), V("acerto"));
            Assert.Greater(V("gato_dano"), V("acerto"));
            Assert.Greater(V("acerto_furtivo"), V("acerto_costas"));
            Assert.Greater(V("acerto_costas"), V("acerto"));
            Assert.Greater(V("acerto"), V("gato_passo"));
            Assert.Greater(V("koda_salto_queda"), V("koda_passo"));
            // o aviso de uma armadilha é mais alto que o golpe dela
            Assert.Greater(V("lanca_aviso"), V("lanca_sobe"));
            Assert.Greater(V("flecha_aviso"), V("flecha"));
            Assert.Greater(V("besta_trava"), V("besta_disparo"));
            // passos e interface ficam no fundo
            Assert.Less(V("gato_passo"), 0.2f);
            Assert.Less(V("ui_cursor"), 0.2f);
        }

        [Test]
        public void Ducking_AbaixaEVoltaDevagar()
        {
            Assert.AreEqual(0.5f, AudioMix.DuckAt(1f, 0.5f, 2f, 1f));            // segurando
            Assert.AreEqual(0.75f, AudioMix.DuckAt(2.5f, 0.5f, 2f, 1f), 1e-4f);  // voltando
            Assert.AreEqual(1f, AudioMix.DuckAt(5f, 0.5f, 2f, 1f));              // voltou
            Assert.Less(AudioMix.MusicLevel, 0.75f);                              // a trilha é fundo
            // só os momentos fortes abaixam a música; passos não
            Assert.Less(SfxCatalog.For("guarda_alerta").Duck, 1f);
            Assert.Less(SfxCatalog.For("gato_dano").Duck, 1f);
            Assert.AreEqual(1f, SfxCatalog.For("gato_passo").Duck);
        }

        [Test]
        public void Respingo_VolumePelaAlturaDaQueda()
        {
            float jump = SeteVidas.World.WaterSurface.SplashLoudness(4f);     // pulo normal dentro da lagoa
            float leap = SeteVidas.World.WaterSurface.SplashLoudness(60f);    // o salto de fé
            Assert.AreEqual(1f, leap);
            Assert.Less(jump, 0.15f);                                         // "bem baixinho"
            Assert.Greater(SeteVidas.World.WaterSurface.SplashLoudness(15f), jump);
            Assert.AreEqual(1f, SeteVidas.World.WaterSurface.SplashLoudness(-1f)); // sem altura: como antes
            // pulos seguidos não são descartados enquanto o anterior ainda toca
            Assert.GreaterOrEqual(SfxCatalog.For("gato_splash_grande").MaxInstances, 2);
        }

        static SpeakerProfile Speaker(string name, string voice = "")
        {
            var s = ScriptableObject.CreateInstance<SpeakerProfile>();
            s.DisplayName = name;
            s.VoiceId = voice;
            return s;
        }

        [Test]
        public void Voz_PelaExpressaoComApelidoENeutro()
        {
            var have = new HashSet<string> { "vox_lion_neutro", "vox_lion_ironico", "vox_lion_pergunta", "vox_koda_neutro", "vox_koda_serio", "vox_koda_selo" };
            System.Func<string, bool> has = have.Contains;
            var lion = Speaker("Lionel");
            var koda = Speaker("Koda");

            Assert.AreEqual("vox_lion_ironico", DialogueRunner.VoiceClipId(lion, "ironico", "Claro.", has));
            Assert.AreEqual("vox_lion_pergunta", DialogueRunner.VoiceClipId(lion, "miuda", "Hm.", has));       // apelido
            Assert.AreEqual("vox_lion_ironico", DialogueRunner.VoiceClipId(lion, "deboche", "Hm.", has));      // apelido
            Assert.AreEqual("vox_lion_pergunta", DialogueRunner.VoiceClipId(lion, "", "Colecionador, é?", has)); // pergunta pelo "?"
            Assert.AreEqual("vox_lion_neutro", DialogueRunner.VoiceClipId(lion, "", "Água.", has));
            Assert.AreEqual("vox_koda_selo", DialogueRunner.VoiceClipId(koda, "selo", "Agora!", has));
            Assert.AreEqual("vox_koda_neutro", DialogueRunner.VoiceClipId(koda, "inexistente", "Ladrão.", has));

            Object.DestroyImmediate(lion);
            Object.DestroyImmediate(koda);
        }

        [Test]
        public void Voz_SemBancoNaoToca()
        {
            System.Func<string, bool> none = _ => false;
            var lion = Speaker("Lionel");
            var stranger = Speaker("Estranho");
            Assert.IsNull(DialogueRunner.VoiceClipId(lion, "ironico", "Oi.", none));
            Assert.IsNull(DialogueRunner.VoiceClipId(stranger, "", "Oi.", s => true));
            Assert.IsNull(DialogueRunner.VoiceClipId(null, "", "Oi.", s => true));
            Object.DestroyImmediate(lion);
            Object.DestroyImmediate(stranger);
        }

        [Test]
        public void Voz_VoiceIdVenceONome()
        {
            var s = Speaker("Mensageiro", "capivara");
            Assert.AreEqual("vox_capivara_neutro", DialogueRunner.VoiceClipId(s, "", "Mm.", id => id == "vox_capivara_neutro"));
            Object.DestroyImmediate(s);
        }
    }
}
