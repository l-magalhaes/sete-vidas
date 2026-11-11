using System.Collections;
using NUnit.Framework;
using SeteVidas.Combat;
using SeteVidas.Enemies;
using SeteVidas.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Fase 3: a Sentinela da Playground vê, ignora o gato escondido e morre com o furtivo.</summary>
    public class SentinelTests
    {
        EnemyAI _sentinel;
        PlayerController _player;
        MeleeAttackSettings _daggers;

        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();
            _sentinel = Object.FindAnyObjectByType<EnemyAI>();
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(_sentinel, "Sem Sentinela na Playground. Rode Tools > Sete Vidas > 6");
            Assert.IsNotNull(_player, "Sem jogador na Playground");
            Assert.IsNotNull(_player.GetComponent<StealthController>(), "Gato sem StealthController. Rode Tools > Sete Vidas > 6");
        }

        [TearDown]
        public void TearDown()
        {
            if (_daggers != null) Object.Destroy(_daggers);
            Time.timeScale = 1f;
        }

        /// <summary>Coloca o gato a <paramref name="distance"/> tiles da Sentinela, do lado indicado em relação ao olhar dela.</summary>
        void PlacePlayer(float distance, bool inFront)
        {
            int side = inFront ? _sentinel.FacingDirection : -_sentinel.FacingDirection;
            var pos = new Vector2(_sentinel.transform.position.x + side * distance, 0.01f);
            _player.Body.position = pos;
            _player.Body.linearVelocity = Vector2.zero;
            _player.transform.position = pos;
        }

        [UnityTest]
        public IEnumerator SeesStandingCatInFront_AndEntersCombat()
        {
            yield return Load();
            Assert.AreEqual(AlertState.Patrolling, _sentinel.State);

            float t = 0f;
            while (t < 3f && _sentinel.State != AlertState.Combat)
            {
                PlacePlayer(3f, inFront: true); // a Sentinela anda: reposiciona a cada passo
                yield return new WaitForFixedUpdate();
                t += Time.fixedDeltaTime;
            }
            Assert.AreEqual(AlertState.Combat, _sentinel.State, "Gato em pé na luz a 3 tiles à frente deveria ser detectado");
            Assert.Less(t, 2f, $"Detecção demorou {t:0.00} s");
        }

        [UnityTest]
        public IEnumerator DoesNotSeeCatBehindHer()
        {
            yield return Load();
            for (float t = 0f; t < 1.5f; t += Time.fixedDeltaTime)
            {
                PlacePlayer(1.5f, inFront: false);
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(AlertState.Patrolling, _sentinel.State, "Parado atrás dela (sem correr), não há o que ver nem ouvir");
        }

        [UnityTest]
        public IEnumerator StealthBackstab_KillsInOneHit()
        {
            yield return Load();
            _daggers = ScriptableObject.CreateInstance<MeleeAttackSettings>();
            PlacePlayer(1f, inFront: false);
            yield return new WaitForFixedUpdate();

            var info = _daggers.ResolveHit(_player.Body.position, _sentinel, _player.gameObject);
            Assert.AreEqual(AttackAngle.StealthBack, info.Angle);
            _sentinel.TakeDamage(info);
            Assert.IsTrue(_sentinel.IsDead, "Furtivo pelas costas (5) deveria derrubar a Sentinela (5 de vida)");
        }

        [UnityTest]
        public IEnumerator Playground_HasBushInsideThePatrol()
        {
            yield return Load();
            var section = GameObject.Find("StealthSection");
            Assert.IsNotNull(section, "Trecho H ausente. Rode Tools > Sete Vidas > 6");
            var spots = section.GetComponentsInChildren<SeteVidas.World.HidingSpot>();
            bool inPatrol = false;
            foreach (var s in spots) inPatrol |= s.transform.position.x > 133f && s.transform.position.x < 140f;
            Assert.IsTrue(inPatrol, "Falta o matinho no meio da patrulha (x 133–140). Rode Tools > Sete Vidas > 6");
            Assert.AreEqual(1f, SeteVidas.World.HidingSpot.ConcealmentAt(new Vector2(120f, 0.5f)), "Fora do matinho não esconde");
            Assert.Less(SeteVidas.World.HidingSpot.ConcealmentAt(new Vector2(138f, 0.5f)), 1f, "Dentro do matinho esconde");
        }

        float Overlap()
        {
            float w = _player.Settings.ColliderWidth;
            var cat = new Rect(_player.Body.position.x - w * 0.5f, _player.Body.position.y, w, _player.CurrentHeight);
            return BodyBlockRules.HorizontalOverlap(cat, _sentinel.Bounds);
        }

        /// <summary>Melhoria 6: empurrado contra a Sentinela (recuo, corrida), o gato para nela e não atravessa.</summary>
        [UnityTest]
        public IEnumerator CatCannotPassThroughSentinel()
        {
            yield return Load();
            PlacePlayer(2f, inFront: false);
            yield return new WaitForFixedUpdate();
            int toward = _sentinel.transform.position.x > _player.Body.position.x ? 1 : -1;
            _player.ApplyKnockback(new Vector2(toward * 12f, 0f), 0.6f);
            float worst = -9f;
            for (float t = 0f; t < 0.6f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                worst = Mathf.Max(worst, Overlap());
            }
            Assert.LessOrEqual(worst, 0.06f, "O gato entrou no corpo da Sentinela");
            Assert.AreEqual(toward, _sentinel.transform.position.x > _player.Body.position.x ? 1 : -1, "O gato passou para o outro lado");
        }

        /// <summary>Melhoria 6: o gato que ficou por dentro (caiu em cima) sai para o lado.</summary>
        [UnityTest]
        public IEnumerator CatInsideSentinel_IsPushedOut()
        {
            yield return Load();
            PlacePlayer(0.2f, inFront: false);
            for (float t = 0f; t < 0.5f; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
            Assert.LessOrEqual(Overlap(), 0.06f, "Continuou dentro da Sentinela");
        }

        /// <summary>Morta, a Sentinela não bloqueia mais.</summary>
        [UnityTest]
        public IEnumerator DeadSentinel_IsNotSolid()
        {
            yield return Load();
            _daggers = ScriptableObject.CreateInstance<MeleeAttackSettings>();
            PlacePlayer(1f, inFront: false);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(_sentinel.IsSolid);
            _sentinel.TakeDamage(_daggers.ResolveHit(_player.Body.position, _sentinel, _player.gameObject));
            Assert.IsTrue(_sentinel.IsDead);
            Assert.IsFalse(_sentinel.IsSolid);
        }

        /// <summary>Melhoria 3: o golpe de frente empurra a Sentinela para trás e a põe na pose de dor.</summary>
        [UnityTest]
        public IEnumerator Hit_PushesSentinelBack_AndStuns()
        {
            yield return Load();
            _daggers = ScriptableObject.CreateInstance<MeleeAttackSettings>();
            _daggers.BaseDamage = 1; // não mata
            PlacePlayer(1f, inFront: true);
            yield return new WaitForFixedUpdate();
            float before = _sentinel.transform.position.x;
            int away = before > _player.Body.position.x ? 1 : -1;

            _sentinel.TakeDamage(_daggers.ResolveHit(_player.Body.position, _sentinel, _player.gameObject));
            Assert.IsTrue(_sentinel.IsStunned, "Sem pose de dor");
            for (float t = 0f; t < 0.5f && _sentinel.IsStunned; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate(); // mede no fim da dor, antes de voltar a andar

            float moved = (_sentinel.transform.position.x - before) * away;
            Assert.Greater(moved, 0.15f, "A Sentinela deveria recuar para longe do gato");
            Assert.Less(moved, 2f, "Recuo grande demais");
            Assert.Greater(_sentinel.transform.position.y, -1f, "Caiu para fora do mapa");
        }

        [UnityTest]
        public IEnumerator FrontHit_DoesNotKill_AndStartsCombat()
        {
            yield return Load();
            _daggers = ScriptableObject.CreateInstance<MeleeAttackSettings>();
            PlacePlayer(1f, inFront: true);
            yield return new WaitForFixedUpdate();

            _sentinel.TakeDamage(_daggers.ResolveHit(_player.Body.position, _sentinel, _player.gameObject));
            Assert.IsFalse(_sentinel.IsDead);
            Assert.AreEqual(_sentinel.Settings.MaxHealth - 2, _sentinel.Health);
            Assert.AreEqual(AlertState.Combat, _sentinel.State);
        }
    }
}
