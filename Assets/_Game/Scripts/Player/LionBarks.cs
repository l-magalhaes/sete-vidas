using System.Collections.Generic;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Saco de falas curtas: sorteia sem repetir até todas terem saído, e respeita um intervalo mínimo entre falas
    /// (regras puras, EditMode). Docs/Narrativa/floresta-roteiro.md, "Falas curtas do Lion".
    /// </summary>
    public class BarkPool
    {
        readonly string[] _lines;
        readonly List<int> _bag = new List<int>();
        readonly System.Random _rng;
        int _last = -1;

        public BarkPool(string[] lines, int seed = 0)
        {
            _lines = lines ?? new string[0];
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        public int Count => _lines.Length;

        /// <summary>A próxima fala (nunca a mesma duas vezes seguidas). Vazio se não houver falas.</summary>
        public string Next()
        {
            if (_lines.Length == 0) return "";
            if (_bag.Count == 0) for (int i = 0; i < _lines.Length; i++) _bag.Add(i); // tudo saiu: o saco recomeça
            int at = _rng.Next(_bag.Count);
            if (_bag.Count > 1 && _bag[at] == _last) at = (at + 1) % _bag.Count; // o saco novo não começa pela última
            int pick = _bag[at];
            _bag.RemoveAt(at);
            _last = pick;
            return _lines[pick];
        }

        /// <summary>Já passou o intervalo mínimo desde a última fala?</summary>
        public static bool CooldownOver(float now, float lastTime, float cooldown) => lastTime < 0f || now - lastTime >= cooldown;
    }

    /// <summary>
    /// Falas curtas do Lion sem parar o jogo: quedas longas (10 tiles ou mais) e o primeiro inimigo infectado de cada
    /// tipo (chamado de fora por <see cref="SayOnce"/>). No máximo uma fala por minuto, sem repetir.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class LionBarks : MonoBehaviour
    {
        public static readonly string[] FallLines =
        {
            "Caí de pé. Claro.",
            "Sete vidas, dois corações e zero noção de altura.",
            "Se cair fosse profissão, eu estava rico.",
            "Chão. Velho amigo.",
            "Essa nem doeu. Mentira, doeu.",
        };

        public static readonly string[] BreakLines =
        {
            "Ah, qual é. Até os galhos?",
            "Nada nessa floresta aguenta um gato.",
        };

        public const float FallBarkHeight = 10f;
        public const float Cooldown = 60f;

        [Tooltip("Só fala na Floresta (as cenas da Fortaleza não têm estas falas)")]
        [SerializeField] bool onlyInForest = true;

        static LionBarks s_active;
        public static LionBarks Active => s_active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_active = null; s_lastTime = -1f; } // Play sem recarregar o domínio

        static float s_lastTime = -1f; // entre cenas: trocar de sala não zera o intervalo

        PlayerController _player;
        PlayerHealth _health;
        BarkPool _fall, _break;
        bool _wasGrounded = true;
        float _peakY;

        void Awake()
        {
            _player = GetComponent<PlayerController>();
            _health = GetComponent<PlayerHealth>();
            _fall = new BarkPool(FallLines);
            _break = new BarkPool(BreakLines);
        }

        void OnEnable() => s_active = this;
        void OnDisable() { if (s_active == this) s_active = null; }

        bool Allowed => !onlyInForest || gameObject.scene.name.StartsWith("Forest_", System.StringComparison.Ordinal);

        void FixedUpdate()
        {
            if (_player.Body == null) return;
            float y = _player.Body.position.y;
            bool grounded = _player.IsGrounded;
            if (!grounded) _peakY = _wasGrounded ? y : Mathf.Max(_peakY, y);
            else if (!_wasGrounded && _peakY - y >= FallBarkHeight && (_health == null || !_health.IsDead)) Say(_fall.Next());
            _wasGrounded = grounded;
        }

        /// <summary>Um galho podre quebrou embaixo do gato.</summary>
        public void OnBranchBroke() => Say(_break.Next());

        /// <summary>Fala uma vez por jogo (flag "fala_&lt;key&gt;"), ignorando o intervalo: o primeiro infectado de cada tipo.</summary>
        public void SayOnce(string key, string line)
        {
            var data = SeteVidas.Save.SaveManager.Data;
            string flag = "fala_" + key;
            if (data == null || data.HasFlag(flag) || !Allowed) return;
            data.SetFlag(flag);
            Show(line);
        }

        /// <summary>Fala se o intervalo mínimo já passou.</summary>
        public void Say(string line)
        {
            if (!Allowed || string.IsNullOrEmpty(line) || !BarkPool.CooldownOver(Time.time, s_lastTime, Cooldown)) return;
            Show(line);
        }

        void Show(string line)
        {
            s_lastTime = Time.time;
            SpeechBubble.Show(transform, line, 2.2f);
        }
    }
}
