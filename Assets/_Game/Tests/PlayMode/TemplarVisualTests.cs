using System.Collections;
using NUnit.Framework;
using SeteVidas.Combat;
using SeteVidas.Enemies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Arte do Cão Templário (SeteVidasDocs/Inimigos_Arte.md): a Sentinela usa as animações geradas e, ao morrer,
    /// toca a queda inteira antes de sumir.
    /// </summary>
    public class TemplarVisualTests
    {
        EnemyAI _enemy;
        SpriteRenderer _sprite;

        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();
            _enemy = Object.FindAnyObjectByType<EnemyAI>();
            Assert.IsNotNull(_enemy, "Sem Sentinela na Playground");
            _sprite = _enemy.GetComponentInChildren<SpriteRenderer>();
            yield return null;
        }

        [UnityTest]
        public IEnumerator UsesTemplarArt()
        {
            yield return Load();
            Assert.That(_sprite.sprite != null && _sprite.sprite.name.StartsWith("enm_templar_"),
                "A Sentinela deveria usar a arte do templário. Rode Tools/EnemySprites/gerar.ps1 e Tools > Sete Vidas > 6");
            Assert.That(_enemy.CurrentClipName == "idle" || _enemy.CurrentClipName == "walk",
                "Patrulhando: parado ou andando, não " + _enemy.CurrentClipName);
        }

        [UnityTest]
        public IEnumerator Death_PlaysFall_ThenDisappears()
        {
            yield return Load();
            _enemy.TakeDamage(new DamageInfo(999, Vector2.right, DamageType.Normal, AttackAngle.Front, null));
            Assert.IsTrue(_enemy.IsDead);
            yield return null;
            Assert.AreEqual("death", _enemy.CurrentClipName);
            Assert.IsTrue(_sprite.enabled, "O corpo cai antes de sumir");

            yield return new WaitForSeconds(2f);
            Assert.IsFalse(_sprite.enabled, "Depois de cair, o corpo some");
        }
    }
}
