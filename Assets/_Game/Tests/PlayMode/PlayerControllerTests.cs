using System.Collections;
using NUnit.Framework;
using SeteVidas.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Fase 1: o gato existe na Playground, cai no chão e fica parado sem entrada.</summary>
    public class PlayerControllerTests
    {
        [UnityTest]
        public IEnumerator Player_SpawnsAndLandsOnGround()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);

            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player, "Sem jogador na Playground. Rode Tools > Sete Vidas > 4. Criar jogador e câmera");
            Vector3 start = player.transform.position;

            yield return new WaitForSeconds(1f);

            Assert.IsTrue(player.IsGrounded, "O gato deveria estar no chão depois de 1 s");
            Assert.IsFalse(player.IsCrouching);
            Assert.AreEqual(0f, player.transform.position.y, 0.05f, "Os pés deveriam estar na superfície do chão (y = 0)");
            Assert.AreEqual(start.x, player.transform.position.x, 0.05f, "Sem entrada, o gato não deve deslizar");
        }
    }
}
