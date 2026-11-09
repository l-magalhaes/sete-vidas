using SeteVidas.Player;
using SeteVidas.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Os 7 corações no canto superior esquerdo: cheio (2 hits), meio (1), vazio (0) e escurecido (ainda não
    /// recuperado). Lê o <see cref="PlayerHealth.Active"/>.
    /// </summary>
    public class HeartsHud : MonoBehaviour
    {
        [SerializeField] Image[] hearts = new Image[7];
        [SerializeField] Sprite full;
        [SerializeField] Sprite half;
        [SerializeField] Sprite empty;
        [SerializeField] Sprite locked;

        void LateUpdate()
        {
            var health = PlayerHealth.Active;
            if (health == null) return;

            var states = HealthRules.Hearts(health.HeartsRecovered, health.CurrentHits);
            for (int i = 0; i < hearts.Length && i < states.Length; i++)
            {
                if (hearts[i] == null) continue;
                var sprite = states[i] switch
                {
                    HeartState.Full => full,
                    HeartState.Half => half,
                    HeartState.Empty => empty,
                    _ => locked,
                };
                if (hearts[i].sprite != sprite) hearts[i].sprite = sprite;
            }
        }
    }
}
