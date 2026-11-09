using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Teste da Fase 0: a cena Playground carrega e tem chão com colisão.
    /// A cena precisa estar no Build Profile (o setup automático já faz isso).
    /// </summary>
    public class PlaygroundSceneTests
    {
        [UnityTest]
        public IEnumerator Playground_LoadsWithGroundCollision()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);

            var ground = GameObject.Find("Grid/Ground");
            Assert.IsNotNull(ground, "Tilemap Grid/Ground não encontrado na Playground");
            Assert.IsNotNull(ground.GetComponent<TilemapCollider2D>(), "Ground sem TilemapCollider2D");
            Assert.Greater(ground.GetComponent<Tilemap>().GetUsedTilesCount(), 0, "Ground sem tiles pintados");

            var hit = Physics2D.Raycast(new Vector2(0.5f, 5f), Vector2.down, 20f);
            Assert.IsTrue(hit.collider != null, "Raycast a partir do ponto de spawn não encontrou chão");
        }
    }
}
