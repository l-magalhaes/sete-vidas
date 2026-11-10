using System.Collections;
using NUnit.Framework;
using SeteVidas.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Rampas (trecho L da Playground, x -110..-77): parado numa rampa andável o gato não escorrega e o desenho
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

        [UnityTest]
        public IEnumerator StandingOnWalkableSlope_DoesNotSlide()
        {
            PlayerController player = null;
            yield return LoadPlayer(p => player = p);

            // Rampa 1:1 subindo para a direita de (-98, 0) a (-94, 4): em x -96 a superfície está em 2
            player.Teleport(new Vector2(-96f, 2.6f));
            yield return new WaitForSeconds(0.4f); // cai e assenta
            Assert.IsTrue(player.IsGrounded, "Deveria estar de pé na rampa");
            Assert.IsTrue(player.IsOnSlope, "Deveria reconhecer a rampa (normal inclinada)");
            Vector2 settled = player.Body.position;

            yield return new WaitForSeconds(1f);
            Assert.AreEqual(settled.x, player.Body.position.x, 0.02f, "Parado na rampa, o gato não pode escorregar");
            Assert.AreEqual(settled.y, player.Body.position.y, 0.02f);

            // A caixa encosta pela quina de cima (meia largura × tg 45° = 0,375); o desenho desce isso
            var visual = player.GetComponentInChildren<PlayerVisual>();
            Assert.IsNotNull(visual);
            Assert.AreEqual(-0.375f, visual.transform.localPosition.y, 1f / 16f + 0.001f,
                "O sprite deveria descer até as patas tocarem a rampa");
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
