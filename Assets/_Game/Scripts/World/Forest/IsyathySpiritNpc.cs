using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// O espírito da Isyathy pela floresta (D-ISYATHY-ESPIRITO-02; mapa §6 "Depois da morte"). Pedido do usuário
    /// (19/11/2026): nos 5 lugares onde antes surgiam os olhos doentes (A4, B4, C4, C6, D4), depois da luta, o espírito
    /// fica no primeiro plano, calmo, e dá para falar com ele (E). As conversas vão em rodízio: primeiro as que o gato
    /// ainda não ouviu (em qualquer um dos 5 lugares), depois de novo; a 02e só depois da ponte nova.
    /// <para>Sem colisão, sem perigo. Antes da morte dela (flag <see cref="IsyathySighting.DeadFlag"/>), não existe.
    /// O desenho é o mesmo do fim da luta (<see cref="IsyathySpiritArt"/>), menor.</para>
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class IsyathySpiritNpc : MonoBehaviour
    {
        /// <summary>Prefixo das flags das conversas já ouvidas ("espirito_ouviu_" + índice).</summary>
        public const string HeardPrefix = "espirito_ouviu_";
        /// <summary>A fala do Lion na primeira vez que ele vê o espírito pela floresta.</summary>
        public const string FirstSeenFlag = "fala_espirito_floresta";
        /// <summary>O tamanho aqui (o do fim da luta é 1).</summary>
        public const float Scale = 0.6f;
        /// <summary>Quanto ele flutua acima do chão (tiles).</summary>
        public const float Hover = 0.35f;

        [Tooltip("D-ISYATHY-ESPIRITO-02a..d (em rodízio)")]
        [SerializeField] DialogueDefinition[] talks = new DialogueDefinition[0];
        [Tooltip("D-ISYATHY-ESPIRITO-02e: só com a ponte nova")]
        [SerializeField] DialogueDefinition afterBridge;
        [Tooltip("A sala (mundo): ao entrar nela, o espírito aparece")]
        [SerializeField] Rect room;
        [SerializeField] TextMesh prompt;

        static Sprite s_sprite;
        static int s_rotation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_sprite = null; s_rotation = 0; s_visible = 0; }   // Play sem recarregar o domínio

        static int s_visible;
        bool _counted;

        /// <summary>Algum espírito está à vista do gato agora (a música do espírito, MUS-F14).</summary>
        public static bool Shown => s_visible > 0;

        void Count(bool on)
        {
            if (on == _counted) return;
            _counted = on;
            s_visible += on ? 1 : -1;
        }

        void OnDisable() => Count(false);

        SpriteRenderer _art;
        PlayerHealth _near;
        Vector3 _home;
        float _alpha, _t;
        bool _dead, _shown;

        public void Configure(DialogueDefinition[] conversations, DialogueDefinition bridgeTalk, Rect area, TextMesh promptText)
        {
            talks = conversations; afterBridge = bridgeTalk; room = area; prompt = promptText;
        }

        // ------------------------------------------------------------ regras (testadas no EditMode)

        /// <summary>
        /// Qual conversa agora: a primeira das disponíveis que ainda não foi ouvida; ouvidas todas, o rodízio
        /// (<paramref name="rotation"/>). A última da lista (<paramref name="hasBridgeTalk"/>) só vale com a ponte. -1 = nenhuma.
        /// </summary>
        public static int NextTalk(bool[] heard, bool hasBridgeTalk, bool bridgeBuilt, int rotation)
        {
            if (heard == null || heard.Length == 0) return -1;
            int available = heard.Length - (hasBridgeTalk && !bridgeBuilt ? 1 : 0);
            if (available <= 0) return -1;
            for (int i = 0; i < available; i++) if (!heard[i]) return i;
            return ((rotation % available) + available) % available;
        }

        // ------------------------------------------------------------ jogo

        static Sprite SpiritSprite()
        {
            if (s_sprite != null) return s_sprite;
            var tex = new Texture2D(IsyathySpiritArt.W, IsyathySpiritArt.H, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "isyathy_espirito_npc", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(IsyathySpiritArt.Pixels());
            tex.Apply(false);
            s_sprite = Sprite.Create(tex, new Rect(0, 0, IsyathySpiritArt.W, IsyathySpiritArt.H), new Vector2(0.5f, 0.05f), 16f);
            s_sprite.hideFlags = HideFlags.DontSave;
            return s_sprite;
        }

        void Start()
        {
            _home = transform.position;
            WorldPrompt.Upgrade(prompt);
            var go = new GameObject("Espírito (desenho)");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, Hover, 0f);
            go.transform.localScale = new Vector3(Scale, Scale, 1f);
            _art = go.AddComponent<SpriteRenderer>();
            _art.sprite = SpiritSprite();
            _art.sortingOrder = 2;               // primeiro plano, atrás do gato
            _art.color = new Color(1f, 1f, 1f, 0f);
            go.AddComponent<SeteVidas.Enemies.Boss.SpiritFloat>();
            Refresh();
        }

        void Refresh()
        {
            var data = SaveManager.Data;
            _dead = data != null && data.HasFlag(IsyathySighting.DeadFlag);
            if (_art != null) _art.enabled = _dead;
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.enabled = _dead;
            if (!_dead && prompt != null) prompt.gameObject.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h != null) _near = h;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h != null && h == _near) _near = null;
        }

        void Update()
        {
            if (!_dead || _art == null) return;
            var cat = PlayerHealth.Active;
            bool inRoom = cat != null && !cat.IsDead && room.Contains(cat.transform.position);
            // aparece devagar quando o gato entra na sala (luz subindo do chão) e some quando ele sai
            float want = inRoom ? 0.85f : 0f;
            _alpha = Mathf.MoveTowards(_alpha, want, Time.unscaledDeltaTime * (inRoom ? 0.6f : 1.5f));
            _t += Time.unscaledDeltaTime;
            float pulse = 0.08f * Mathf.Sin(_t * 1.7f);
            _art.color = new Color(1f, 1f, 1f, Mathf.Clamp01(_alpha + (_alpha > 0.5f ? pulse : 0f)));
            transform.position = _home + new Vector3(0f, 0.12f * Mathf.Sin(_t * 1.3f), 0f);
            if (cat != null && Mathf.Abs(cat.transform.position.x - _home.x) > 0.6f) _art.flipX = cat.transform.position.x < _home.x;   // olha para o gato
            if (inRoom && !_shown && _alpha > 0.5f)
            {
                _shown = true;
                SeteVidas.Audio.Sfx.Play("espirito_surge", _home);
                var data = SaveManager.Data;
                if (data != null && !data.HasFlag(FirstSeenFlag))
                {
                    data.SetFlag(FirstSeenFlag);
                    SpeechBubble.Show(cat.transform, "Os olhos dela. Agora calmos.", 2.4f);
                }
            }
            if (!inRoom) _shown = false;
            Count(inRoom && _alpha > 0.3f);

            var runner = DialogueRunner.Active;
            bool talking = runner != null && runner.IsOpen;
            bool near = _near != null && !_near.IsDead && _alpha > 0.5f;
            if (prompt != null) prompt.gameObject.SetActive(near && !talking);
            if (!near || talking) return;
            var input = _near.GetComponent<PlayerInputReader>();
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame) Talk(_near);
        }

        /// <summary>E perto dele: a próxima conversa do rodízio.</summary>
        public void Talk(PlayerHealth cat)
        {
            var runner = DialogueRunner.Active;
            var data = SaveManager.Data;
            if (runner == null || runner.IsOpen || data == null || cat == null) return;
            int n = talks.Length + (afterBridge != null ? 1 : 0);
            var heard = new bool[n];
            for (int i = 0; i < n; i++) heard[i] = data.HasFlag(HeardPrefix + i);
            bool bridge = data.HasFlag(SeteVidas.Progression.ReconstructionWood.BridgeFlag);
            int pick = NextTalk(heard, afterBridge != null, bridge, s_rotation);
            if (pick < 0) return;
            var d = pick < talks.Length ? talks[pick] : afterBridge;
            if (d == null) return;
            if (!heard[pick]) { data.SetFlag(HeardPrefix + pick); SaveManager.Save(); }
            else s_rotation++;
            SeteVidas.Audio.Sfx.Play("espirito_fala", _home);
            runner.Play(d, cat.GetComponent<PlayerInputReader>());
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireCube(room.center, room.size);
        }
#endif
    }
}
