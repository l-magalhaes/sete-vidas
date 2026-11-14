using System.Collections;
using NUnit.Framework;
using SeteVidas.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Rampas (trecho L da Playground, x -110..-77): parado numa rampa o gato escorrega (pedido de 14/11/2026) e o desenho
    /// encosta na rampa; numa rampa íngreme ele escorrega até o chão.
    /// Se falhar por falta das rampas, rode Tools > Sete Vidas > 2. Criar cenas.
    /// </summary>
    public class SlopeTests
    {
        IEnumerator LoadPlayer(System.Action<PlayerController> found)
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player, "Sem jogador na Playground. Rode Tools > Sete Vidas > 4. Criar jogador e câmera");
            Assert.IsNotNull(GameObject.Find("Rampas/Rampa 1x1"), "Playground sem o trecho de rampas. Rode Tools > Sete Vidas > 2. Criar cenas");
            yield return new WaitForFixedUpdate();
            found(player);
        }

        /// <summary>
        /// Pedido do usuário (14/11/2026): parado numa rampa, o gato escorrega rampa abaixo (antes ficava parado,
        /// como numa plataforma). O desenho continua encostado na rampa enquanto desce.
        /// </summary>
        [UnityTest]
        public IEnumerator StandingOnWalkableSlope_SlidesDown()
        {
            PlayerController player = null;
            yield return LoadPlayer(p => player = p);

            // Rampa 1:1 subindo para a direita de (-98, 0) a (-94, 4): em x -96 a superfície está em 2
            player.Teleport(new Vector2(-96f, 2.6f));
            yield return new WaitForSeconds(0.2f); // cai e encosta
            Assert.IsTrue(player.IsGrounded, "Deveria estar na rampa");
            Assert.IsTrue(player.IsOnSlope, "Deveria reconhecer a rampa (normal inclinada)");
            Assert.IsTrue(player.IsSliding, "Parado na rampa: escorrega");
            Assert.Less(player.Velocity.x, 0f, "Desce para a esquerda (para onde a rampa desce)");

            // A caixa encosta pela quina de cima (meia largura × tg 45° = 0,375); o desenho desce isso
            var visual = player.GetComponentInChildren<PlayerVisual>();
            Assert.IsNotNull(visual);
            Assert.AreEqual(-0.375f, visual.transform.localPosition.y, 1f / 16f + 0.001f,
                "O sprite deveria descer até as patas tocarem a rampa");

            yield return new WaitForSeconds(1.5f);
            Assert.Less(player.Body.position.x, -98f, "Escorregou para fora da rampa (para a esquerda)");
            Assert.Less(player.Body.position.y, 0.1f, "No chão, embaixo");
            Assert.IsFalse(player.IsOnSlope);
        }

        [UnityTest]
        public IEnumerator SteepSlope_IsNotGround_AndSlidesDown()
        {
            PlayerController player = null;
            yield return LoadPlayer(p => player = p);

            // Rampa 2:1 (63°) subindo para a esquerda de (-104, 0) a (-106, 4)
            player.Teleport(new Vector2(-104.9f, 3f));
            yield return new WaitForSeconds(1.5f);

            Assert.Less(player.Body.position.y, 0.1f, "Na rampa íngreme o gato deveria escorregar até o chão");
            Assert.Greater(player.Body.position.x, -104.6f, "Deveria ter escorregado para fora da rampa (para a direita)");
            Assert.IsTrue(player.IsGrounded, "No fim, de pé no chão plano");
            Assert.IsFalse(player.IsOnSlope);
        }
    }
}
