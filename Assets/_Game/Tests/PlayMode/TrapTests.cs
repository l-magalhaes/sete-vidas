using System.Collections;
using NUnit.Framework;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World.Traps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Armadilhas na Playground: espinhos devolvem ao chão seguro, agachar não dispara fio nem placa.</summary>
    public class TrapTests
    {
        PlayerController _player;
        PlayerHealth _health;

        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            _health = PlayerHealth.Active;
            _player = _health.GetComponent<PlayerController>();
            Assert.IsNotNull(GameObject.Find("TrapCourse"), "Sem circuito de armadilhas. Rode Tools > Sete Vidas > 11");
            Assert.IsNotNull(_player.GetComponent<SafeGroundTracker>(), "Gato sem SafeGroundTracker. Rode Tools > Sete Vidas > 11");
        }

        [TearDown]
        public void TearDown()
        {
            if (_player != null) _player.HoldCrouch = false;
            SaveManager.DeleteSave();
            Time.timeScale = 1f;
        }

        static IEnumerator Physics(int steps)
        {
            for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
        }

        static T Find<T>() where T : Component => GameObject.Find("TrapCourse").GetComponentInChildren<T>();

        /// <summary>Move o corpo sem o Teleport (que levanta o gato), para testar agachado.</summary>
        void Slide(float x) => _player.Body.position = new Vector2(x, 0.01f);

        [UnityTest]
        public IEnumerator Course_HasEveryTrapType()
        {
            yield return Load();
            Assert.IsNotNull(Find<SpikeStrip>());
            Assert.IsNotNull(Find<TrapTrigger>());
            Assert.IsNotNull(Find<ArrowLauncher>());
            Assert.IsNotNull(Find<SpearTrap>());
            Assert.IsNotNull(Find<FlameJet>());
            Assert.IsNotNull(Find<SawBlade>());
            Assert.IsNotNull(Find<CrumblingPlatform>());
        }

        [UnityTest]
        public IEnumerator Spikes_Hurt_AndReturnToLastSafeGround()
        {
            yield return Load();
            _player.Teleport(new Vector2(-6f, 0.01f));
            yield return Physics(15); // pisa firme: vira o chão seguro
            Assert.AreEqual(-6f, _player.GetComponent<SafeGroundTracker>().LastSafe.x, 0.3f);
            int before = _health.CurrentHits;

            _player.Teleport(new Vector2(-11f, 0.01f)); // em cima dos espinhos
            yield return Physics(3);
            Assert.AreEqual(before - 1, _health.CurrentHits, "Espinho tira 1 hit");

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(-6f, _player.Body.position.x, 0.4f, "Volta ao último chão seguro");
        }

        [UnityTest]
        public IEnumerator Tripwire_CrouchedPassesUnder_StandingTriggers()
        {
            yield return Load();
            var wire = GameObject.Find("Fio de alarme").GetComponent<TrapTrigger>();
            float x = wire.transform.position.x;

            _player.Teleport(new Vector2(x + 2.5f, 0.01f));
            _player.HoldCrouch = true;
            yield return Physics(5);
            Assert.IsTrue(_player.IsCrouching);
            Slide(x);
            yield return Physics(5);
            Assert.IsTrue(wire.IsArmed, "Agachado passa por baixo do fio");

            _player.HoldCrouch = false; // levanta embaixo do fio
            yield return Physics(5);
            Assert.IsFalse(wire.IsArmed, "De pé esbarra no fio");
        }

        [UnityTest]
        public IEnumerator PressurePlate_CrouchedDoesNotTrigger_StandingDoes()
        {
            yield return Load();
            var plate = GameObject.Find("Placa de pressão").GetComponent<TrapTrigger>();
            float x = plate.transform.position.x;

            _player.Teleport(new Vector2(x + 3f, 0.01f));
            _player.HoldCrouch = true;
            yield return Physics(5);
            Slide(x);
            yield return Physics(5);
            Assert.IsTrue(plate.IsArmed, "Agachado: passos leves não afundam a placa");

            _player.HoldCrouch = false;
            yield return Physics(5);
            Assert.IsFalse(plate.IsArmed, "De pé: dispara");
        }

        [UnityTest]
        public IEnumerator CrumblingPlatform_FallsUnderTheCat_AndComesBack()
        {
            yield return Load();
            var platform = Find<CrumblingPlatform>();
            Vector2 top = (Vector2)platform.transform.position + new Vector2(0f, 0.3f);
            _player.Teleport(top);
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(platform.IsFallen, "Pisou: treme e cai");

            SeteVidas.Core.GameEvents.RaiseWorldReset();
            Assert.IsFalse(platform.IsFallen, "Descansar/morrer devolve a plataforma");
        }
    }
}
