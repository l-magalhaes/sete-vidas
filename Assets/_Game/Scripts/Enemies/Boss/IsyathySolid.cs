using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// Marca as bolas sólidas do corpo da Isyathy (camada do chão, <see cref="IsyathyBoss"/>): o gato não atravessa, mas o
    /// que é dela (o Cuspe, as poças) não trata o corpo como chão.
    /// </summary>
    public class IsyathySolid : MonoBehaviour
    {
        /// <summary>O colisor é um pedaço do corpo dela (não é chão de verdade).</summary>
        public static bool Is(Collider2D c) => c != null && c.GetComponent<IsyathySolid>() != null;
    }
}
