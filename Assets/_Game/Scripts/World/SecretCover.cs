using SeteVidas.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SeteVidas.World
{
    /// <summary>
    /// Esconde o que há atrás de uma parede quebrável (pedido do usuário, 14/11/2026: "lugares atrás de paredes normais e
    /// rachadas, seria legal se não tivéssemos visão do que tem lá atrás"). Um tilemap só de desenho, com a mesma pedra
    /// (ou terra) das paredes em volta, cobre o outro lado. Montado por <see cref="SecretCovers"/>.
    /// <list type="bullet">
    /// <item><b>Vão fechado</b> (<see cref="side"/> = 0, um cofre): coberto até a parede quebrar ou o gato entrar nele.</item>
    /// <item><b>Área maior</b> (<see cref="side"/> = −1 esquerda, 1 direita): coberta enquanto o gato está do OUTRO lado da
    /// parede; se ele chega lá por outro caminho, a cobertura sai (e a do lado de cá aparece).</item>
    /// </list>
    /// Quebrada a parede, tudo aparece e não volta.
    /// </summary>
    public class SecretCover : MonoBehaviour
    {
        [SerializeField] BreakableWall wall;
        [SerializeField] Tilemap cover;
        [Tooltip("A área escondida (mundo)")]
        [SerializeField] Rect area;
        [Tooltip("0 = vão fechado; −1/1 = a área à esquerda/direita da parede, escondida quando o gato está do outro lado")]
        [SerializeField] int side;
        [SerializeField] float wallX;
        [SerializeField, Min(0.05f)] float fadeTime = 0.35f;
        [Tooltip("Regra de desenho da cobertura (SecretCovers.CoverVersion); mais velha que a do código, é refeita ao carregar")]
        [SerializeField] int version;

        float _alpha = 1f;
        bool _done;

        public void Configure(BreakableWall breakable, Tilemap map, Rect hidden) => Configure(breakable, map, hidden, 0, 0f);

        public void Configure(BreakableWall breakable, Tilemap map, Rect hidden, int areaSide, float x)
        {
            wall = breakable; cover = map; area = hidden; side = areaSide; wallX = x;
            version = SecretCovers.CoverVersion;
        }

        public bool Revealed => _done || _alpha <= 0f;
        public BreakableWall Wall => wall;
        public Tilemap CoverMap => cover;
        public int Version => version;
        public int Side => side;

        void Start()
        {
            // parede já quebrada no save: começa revelado
            if (wall == null || wall.IsBroken) { Finish(); return; }
            // área lateral: já começa certa (sem a pedra piscando em cima do gato ao entrar na cena)
            var h = PlayerHealth.Active;
            if (side != 0 && h != null)
            {
                _alpha = (h.transform.position.x < wallX ? -1 : 1) == side ? 0f : 1f;
                Fade(_alpha);
            }
        }

        void Update()
        {
            if (cover == null || _done) return;
            if (wall != null && wall.IsBroken) { Fade(0f); if (_alpha <= 0f) Finish(); return; }
            var h = PlayerHealth.Active;
            if (h == null) return;
            Vector2 p = (Vector2)h.transform.position + Vector2.up * 0.5f;
            if (side == 0)
            {
                if (area.Contains(p) || _alpha < 1f) { Fade(0f); if (_alpha <= 0f) Finish(); }   // começou a sumir: termina
                return;
            }
            int playerSide = p.x < wallX ? -1 : 1;
            Fade(playerSide == side ? 0f : 1f);
        }

        void Fade(float target)
        {
            _alpha = Mathf.MoveTowards(_alpha, target, Time.deltaTime / fadeTime);
            cover.color = new Color(1f, 1f, 1f, _alpha);
            // o que vai por cima da cobertura (a janela da cabaninha) some junto
            foreach (var sr in cover.GetComponentsInChildren<SpriteRenderer>(true)) sr.color = new Color(1f, 1f, 1f, _alpha);
            bool show = _alpha > 0f;
            if (cover.gameObject.activeSelf != show) cover.gameObject.SetActive(show);
        }

        void Finish()
        {
            _done = true;
            _alpha = 0f;
            if (cover != null) cover.gameObject.SetActive(false);
        }
    }
}
