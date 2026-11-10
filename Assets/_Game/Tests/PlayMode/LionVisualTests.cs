using System.Collections;
using NUnit.Framework;
using SeteVidas.Combat;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Arte do Lion (SeteVidasDocs/Lion_Arte_e_Animacoes.md): o gato usa as animações geradas,
    /// tem cauda procedural, e na morte some em centelhas e volta inteiro no santuário.
    /// </summary>
    public class LionVisualTests
    {
        PlayerHealth _health;
        PlayerVisual _visual;
        SpriteRenderer _body;

        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            _health = PlayerHealth.Active;
            _visual = _health.GetComponentInChildren<PlayerVisual>();
            Assert.IsNotNull(_visual);
            _body = _visual.GetComponent<SpriteRenderer>();
            // espera o gato pousar
            var controller = _health.GetComponent<PlayerController>();
            float t = 0f;
            while (!controller.IsGrounded && t < 3f) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(0.4f);
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Standing_ShowsLionIdle_WithTail()
        {
            yield return Load();
            Assert.That(_body.sprite != null && _body.sprite.name.StartsWith("chr_lion_"),
                "O gato deveria usar a arte do Lion. Rode Tools/LionSprites/gerar.ps1 e Tools > Sete Vidas > Player");
            StringAssert.StartsWith("idle", _visual.CurrentClipName);
            var tail = _health.transform.Find("Tail");
            Assert.IsNotNull(tail, "Cauda procedural criada ao lado do Visual");
            Assert.IsTrue(tail.GetComponent<SpriteRenderer>().enabled, "Cauda visível com o gato parado");
            var cape = _health.transform.Find("Cape");
            Assert.IsNotNull(cape, "Aba da capa criada ao lado do Visual");
            Assert.IsTrue(cape.GetComponent<SpriteRenderer>().enabled, "Capa visível com o gato parado");
        }

        [UnityTest]
        public IEnumerator Death_BurstsIntoSparks_ThenRespawnsVisible()
        {
            yield return Load();
            _health.ApplyCurse(1); // 1 hit de vida
            _health.TakeDamage(new DamageInfo(1, Vector2.right, DamageType.Normal, AttackAngle.Front, null));
            Assert.IsTrue(_health.IsDead);

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(_body.enabled, "Depois do flash, o gato vira centelhas e some");

            float t = 0f;
            while (_health.IsDead && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsFalse(_health.IsDead);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(_body.enabled, "O gato se forma de novo no santuário");
            Assert.AreNotEqual("respawn", _visual.CurrentClipName, "O renascimento termina e volta ao normal");
        }
    }
}
