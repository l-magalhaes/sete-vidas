using SeteVidas.Enemies.Boss;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>Barra de vida do chefe no topo da tela, só durante a luta. A parte clara mostra a vida que acabou de perder.</summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Image fill;
        [SerializeField] Image trail;
        [SerializeField] Text title;
        [SerializeField] string bossTitle = "Koda, o Dourado";
        [Tooltip("Velocidade com que a parte clara alcança a vida atual (fração/s)")]
        [SerializeField] float trailSpeed = 0.5f;

        float _trail = 1f;

        void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        void LateUpdate()
        {
            var boss = GoldenBoss.Active;
            bool show = boss != null && boss.IsFighting;
            if (root != null && root.activeSelf != show) root.SetActive(show);
            if (!show) { _trail = 1f; return; }

            float f = boss.Health / (float)boss.MaxHealth;
            if (fill != null) fill.fillAmount = f;
            _trail = f > _trail ? f : Mathf.MoveTowards(_trail, f, trailSpeed * Time.unscaledDeltaTime);
            if (trail != null) trail.fillAmount = _trail;
            if (title != null) title.text = boss.InSecondPhase ? bossTitle + "  (furioso)" : bossTitle;
        }
    }
}
