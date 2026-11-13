using SeteVidas.Combat;
using SeteVidas.Enemies.Boss;
using UnityEngine;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Barra de vida do chefe no topo da tela, só durante a luta. A parte clara mostra a vida que acabou de perder.
    /// Com <see cref="pixelStyle"/>, troca a barra lisa pela versão em pixel art (<see cref="BossBarArt"/>), montada
    /// sozinha em cima da barra antiga: o golpe pisca branco e treme a barra; na fase furiosa o ouro vira brasa.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Image fill;
        [SerializeField] Image trail;
        [SerializeField] Text title;
        [SerializeField] string bossTitle = "Koda, o Dourado";
        [Tooltip("Velocidade com que a parte clara alcança a vida atual (fração/s)")]
        [SerializeField] float trailSpeed = 0.5f;
        [Tooltip("Barra em pixel art (medalhão, moldura dourada, nome na fonte do jogo). Desligado: a barra lisa antiga")]
        [SerializeField] bool pixelStyle = true;
        [Tooltip("De quanto em quanto tempo um brilho passa pelo ouro (s)")]
        [SerializeField, Min(0.5f)] float glintEvery = 3f;

        const float FlashTime = 0.1f, ShakeTime = 0.22f, GlintTime = 0.5f;

        float _trail = 1f;
        Image _art;
        RectTransform _artRt;
        Texture2D _tex;
        Color32[] _px;
        long _lastKey = long.MinValue;
        int _lastHealth = -1;
        float _flashUntil, _shakeUntil, _glintAt;

        void Awake()
        {
            if (root != null) root.SetActive(false);
            if (pixelStyle && root != null) BuildArt();
        }

        void OnDestroy()
        {
            if (_tex != null) Destroy(_tex);
            if (_art != null && _art.sprite != null) Destroy(_art.sprite);
        }

        void BuildArt()
        {
            // esconde a barra lisa (o enchimento continua sendo atualizado, só não aparece)
            if (title != null) title.enabled = false;
            if (fill != null && fill.transform.parent != null && fill.transform.parent != root.transform)
                fill.transform.parent.gameObject.SetActive(false);
            else
            {
                if (fill != null) fill.enabled = false;
                if (trail != null) trail.enabled = false;
            }

            _tex = new Texture2D(BossBarArt.Width, BossBarArt.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "barra do chefe",
            };
            _px = new Color32[BossBarArt.Width * BossBarArt.Height];
            var go = new GameObject("BarraPixel", typeof(RectTransform), typeof(Image));
            _artRt = (RectTransform)go.transform;
            _artRt.SetParent(root.transform, false);
            _artRt.anchorMin = _artRt.anchorMax = _artRt.pivot = new Vector2(0.5f, 1f);
            _artRt.anchoredPosition = Vector2.zero;
            _artRt.sizeDelta = new Vector2(BossBarArt.Width, BossBarArt.Height);
            _art = go.GetComponent<Image>();
            _art.raycastTarget = false;
            _art.sprite = Sprite.Create(_tex, new Rect(0, 0, BossBarArt.Width, BossBarArt.Height), new Vector2(0.5f, 1f), PixelFont.Ppu);
        }

        void LateUpdate()
        {
            var boss = GoldenBoss.Active;
            bool show = boss != null && boss.IsFighting;
            if (root != null && root.activeSelf != show) root.SetActive(show);
            if (!show) { _trail = 1f; _lastHealth = -1; return; }

            float f = boss.Health / (float)boss.MaxHealth;
            if (fill != null) fill.fillAmount = f;
            _trail = f > _trail ? f : Mathf.MoveTowards(_trail, f, trailSpeed * Time.unscaledDeltaTime);
            if (trail != null) trail.fillAmount = _trail;
            if (title != null) title.text = boss.InSecondPhase ? bossTitle + "  (furioso)" : bossTitle;
            if (_art != null) DrawArt(boss, f);
        }

        void DrawArt(GoldenBoss boss, float f)
        {
            float now = Time.unscaledTime;
            if (_lastHealth >= 0 && boss.Health < _lastHealth) { _flashUntil = now + FlashTime; _shakeUntil = now + ShakeTime; }
            if (_lastHealth < 0) _glintAt = now + glintEvery;
            _lastHealth = boss.Health;

            // brilho passando pelo ouro, da esquerda até a frente do enchimento
            int glint = -1;
            if (now >= _glintAt)
            {
                float t = (now - _glintAt) / GlintTime;
                if (t >= 1f) _glintAt = now + glintEvery;
                else glint = Mathf.RoundToInt(t * (BossBarArt.Columns(f) + 4)) - 2;
            }

            var s = new BossBarArt.State
            {
                Health = f,
                Trail = _trail,
                PhaseAt = boss.SecondPhaseFraction,
                Furious = boss.InSecondPhase,
                Flash = now < _flashUntil,
                Glint = glint,
                Name = bossTitle,
            };
            long key = BossBarArt.Columns(f) | (long)BossBarArt.Columns(_trail) << 9 | (long)(glint + 8) << 18
                       | (s.Flash ? 1L << 28 : 0) | (s.Furious ? 1L << 29 : 0);
            if (key != _lastKey)
            {
                _lastKey = key;
                BossBarArt.Draw(_px, s);
                _tex.SetPixels32(_px);
                _tex.Apply(false);
            }

            // tremida do golpe: 1 px para os lados, em passos
            int shake = now < _shakeUntil ? ((int)(now * 40f) % 2 == 0 ? 1 : -1) : 0;
            _artRt.anchoredPosition = new Vector2(shake, 0f);
        }
    }
}
