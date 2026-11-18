using System.Collections.Generic;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Galho frágil do Ninho (F7, floresta-mecanicas.md §4). Vai junto de um galho de mão única que já existe (colisor e
    /// o filho "Visual"). Com a Isyathy agitada ele treme; se o gato ficar <see cref="IsyathyRules.BranchStandTime"/>
    /// parado em cima, ele cai. O bote também o quebra. Cresce de volta em <see cref="IsyathyRules.BranchRegrow"/> s
    /// (nunca bloqueia a rota: cair leva a um patamar de baixo).
    /// </summary>
    public class FragileBranch : MonoBehaviour
    {
        static readonly List<FragileBranch> s_all = new List<FragileBranch>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_all.Clear(); // Play sem recarregar o domínio

        Collider2D _col;
        Transform _art;
        SpriteRenderer _sr;
        Vector3 _artRest;
        float _standFor, _brokenUntil = -1f, _fall;
        bool _broken;
        PlayerController _player;

        public bool IsBroken => _broken;

        void Awake()
        {
            _col = GetComponent<Collider2D>();
            _sr = GetComponentInChildren<SpriteRenderer>();
            _art = _sr != null ? _sr.transform : null;
            if (_art != null) _artRest = _art.localPosition;
        }

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); GameEvents.WorldReset += Restore; }
        void OnDisable() { s_all.Remove(this); GameEvents.WorldReset -= Restore; }

        /// <summary>O bote quebra os galhos frágeis dentro da área.</summary>
        public static int BreakInside(Rect area)
        {
            int n = 0;
            foreach (var b in s_all.ToArray())
            {
                if (b == null || b._broken || b._col == null) continue;
                var bb = b._col.bounds;
                if (area.Overlaps(new Rect(bb.min.x, bb.min.y, bb.size.x, bb.size.y))) { b.Break(); n++; }
            }
            return n;
        }

        bool Occupied()
        {
            if (_col == null) return false;
            if (_player == null)
            {
                var h = PlayerHealth.Active;
                if (h == null) return false;
                _player = h.GetComponent<PlayerController>();
                if (_player == null) return false;
            }
            if (!_player.IsGrounded) return false;
            Vector2 feet = _player.Body.position;
            var b = _col.bounds;
            return feet.x >= b.min.x - 0.2f && feet.x <= b.max.x + 0.2f && Mathf.Abs(feet.y - b.max.y) < 0.25f;
        }

        void Update()
        {
            var state = IsyathyAlert.Active != null ? IsyathyAlert.Active.State : NestState.Asleep;
            if (_broken)
            {
                // caindo e sumindo; depois cresce de volta (aparece devagar)
                if (_art != null)
                {
                    _fall += Time.deltaTime;
                    _art.localPosition = _artRest + Vector3.down * (_fall * _fall * 12f);
                    if (_sr != null) _sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - _fall * 2f));
                }
                if (Time.time >= _brokenUntil) Restore();
                return;
            }

            // fica parado em cima: com a Isyathy agitada, cai
            bool on = Occupied() && Mathf.Abs(_player.Velocity.x) < IsyathyRules.StillSpeed;
            _standFor = on ? _standFor + Time.deltaTime : 0f;
            if (IsyathyRules.BranchFalls(_standFor, state)) { Break(); return; }

            // treme (pixel inteiro), mais forte quanto mais tempo o gato fica em cima
            float amp = IsyathyRules.TrembleAmplitude(state);
            if (_art != null)
            {
                if (amp > 0f)
                {
                    float k = on ? 1f + _standFor : 1f;
                    float dx = Mathf.Sin(Time.time * 47f + transform.position.x) > 0f ? amp * k : -amp * k;
                    _art.localPosition = _artRest + new Vector3(Mathf.Round(dx * 16f) / 16f, 0f, 0f);
                }
                else _art.localPosition = _artRest;
                if (_sr != null) _sr.color = Color.white;
            }
        }

        public void Break()
        {
            if (_broken) return;
            _broken = true;
            _fall = 0f;
            _standFor = 0f;
            _brokenUntil = Time.time + IsyathyRules.BranchRegrow;
            if (_col != null) _col.enabled = false;
            NoiseSystem.Emit((Vector2)transform.position, 4f, 0.3f, gameObject, "galho");
        }

        void Restore()
        {
            _broken = false;
            _fall = 0f;
            _standFor = 0f;
            if (_col != null) _col.enabled = true;
            if (_art != null) _art.localPosition = _artRest;
            if (_sr != null) _sr.color = Color.white;
        }
    }
}
