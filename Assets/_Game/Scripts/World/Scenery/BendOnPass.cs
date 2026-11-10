using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World.Scenery
{
    /// <summary>
    /// Planta que se curva quando o gato passa por ela (plano de assets, 6.2: tufos e grama). Só visual: não faz
    /// barulho nem afeta a furtividade. Troca o sprite para o lado do movimento por um instante e volta.
    /// </summary>
    public class BendOnPass : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite rest, bentLeft, bentRight;
        [Tooltip("Distância horizontal até o gato para curvar (tiles)")]
        [SerializeField, Min(0.1f)] float radius = 0.6f;
        [Tooltip("Tempo curvado depois que o gato passa (s)")]
        [SerializeField, Min(0f)] float hold = 0.3f;

        // um gato só na cena: procurado uma vez e compartilhado por todas as plantas
        static PlayerController s_player;
        static float s_nextLookup;

        float _until;

        public void Configure(SpriteRenderer renderer, Sprite restSprite, Sprite left, Sprite right)
        {
            spriteRenderer = renderer; rest = restSprite; bentLeft = left; bentRight = right;
        }

        void Update()
        {
            if (spriteRenderer == null) return;
            if (s_player == null)
            {
                if (Time.time < s_nextLookup) return;
                s_nextLookup = Time.time + 1f;
                s_player = FindAnyObjectByType<PlayerController>();
                if (s_player == null) return;
            }
            Vector2 d = s_player.Body.position - (Vector2)transform.position;
            float vx = s_player.Velocity.x;
            if (Mathf.Abs(d.x) < radius && d.y > -0.4f && d.y < 1f && Mathf.Abs(vx) > 0.5f)
            {
                spriteRenderer.sprite = vx > 0f ? bentRight : bentLeft;
                _until = Time.time + hold;
            }
            else if (Time.time > _until && spriteRenderer.sprite != rest)
                spriteRenderer.sprite = rest;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_player = null; s_nextLookup = 0f; }
    }
}
