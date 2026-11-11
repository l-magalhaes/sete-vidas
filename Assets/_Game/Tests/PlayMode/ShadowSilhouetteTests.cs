using System.Collections;
using NUnit.Framework;
using SeteVidas.Player;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SeteVidas.Tests
{
    /// <summary>Melhoria 1: na sombra, o gato vira silhueta (só os olhos e o contorno); na luz, volta.</summary>
    public class ShadowSilhouetteTests
    {
        [UnityTest]
        public IEnumerator InShadow_BecomesSilhouette_AndBackInLight()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player);
            var silhouette = player.GetComponent<ShadowSilhouette>();
            Assert.IsNotNull(silhouette, "A furtividade adiciona a silhueta ao gato");

            LightZone shadow = null;
            foreach (var z in Object.FindObjectsByType<LightZone>(FindObjectsSortMode.None))
                if (z.Exposure < silhouette.ShadowExposure) { shadow = z; break; }
            Assert.IsNotNull(shadow, "Sem sombra jogável na Playground. Rode Tools > Sete Vidas > 6");

            var area = shadow.Area;
            player.Teleport(new Vector2(area.center.x, area.center.y)); // cai até o chão dentro da sombra
            for (float t = 0f; t < silhouette.FadeTime + 0.3f; t += Time.deltaTime) yield return null;
            Assert.Greater(silhouette.Amount, 0.95f, "Na sombra: silhueta");

            player.Teleport(new Vector2(area.xMax + 6f, area.center.y));
            for (float t = 0f; t < silhouette.FadeTime + 0.3f; t += Time.deltaTime) yield return null;
            Assert.Less(silhouette.Amount, 0.05f, "Na luz: cores normais");
        }
    }
}
