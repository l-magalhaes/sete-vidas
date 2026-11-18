using SeteVidas.Combat;
using SeteVidas.Progression;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A Ponte do Último Marco reconstruída em madeira pela Tobi (F9, cutscene 2; Docs/Floresta/plano-implementacao.md
    /// §1.3). Ocupa o lugar da ponte de pedra que caiu (os mesmos 104 tiles). Só existe com a flag
    /// <see cref="ReconstructionWood.BridgeFlag"/>: na cena, ela se monta tábua por tábua atrás da nuvem de obra
    /// (<see cref="SetFront"/>), do lado da Fortaleza para o leste. A corda no começo dela (a volta para as Raízes) aparece
    /// junto. Pisar faz as tábuas rangerem um pouco (<see cref="Creak"/>).
    /// </summary>
    public class RebuiltBridge : MonoBehaviour
    {
        [Tooltip("Os blocos de tábuas (2 tiles cada), do oeste para o leste: cada um com a colisão e o desenho num filho")]
        [SerializeField] Transform[] planks = new Transform[0];
        [Tooltip("A corda pendurada no começo da ponte (lado da Fortaleza), descendo para as Raízes")]
        [SerializeField] SpriteRenderer rope;

        float[] _born;
        float _front = float.MinValue, _creakUntil, _creakFor;
        bool _building;
        static RebuiltBridge s_active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_active = null; }   // Play sem recarregar o domínio

        public static RebuiltBridge Active => s_active;

        public float StartX => planks.Length > 0 && planks[0] != null ? planks[0].position.x - 1f : transform.position.x;
        public float EndX => planks.Length > 0 && planks[planks.Length - 1] != null ? planks[planks.Length - 1].position.x + 1f : transform.position.x;
        /// <summary>O topo do tabuleiro (mundo).</summary>
        public float DeckY => planks.Length > 0 && planks[0] != null ? planks[0].position.y + 0.5f : transform.position.y;
        public bool IsBuilding => _building;

        public void Configure(Transform[] blocks, SpriteRenderer ropeRenderer) { planks = blocks; rope = ropeRenderer; }

        void Awake() => s_active = this;

        void OnDestroy() { if (s_active == this) s_active = null; }

        void Start()
        {
            _born = new float[planks.Length];
            bool built = ReconstructionWood.BridgeBuilt(SaveManager.Data);
            ShowAll(built);
        }

        void ShowAll(bool on)
        {
            for (int i = 0; i < planks.Length; i++) if (planks[i] != null) planks[i].gameObject.SetActive(on);
            if (rope != null) rope.enabled = on;
            _front = on ? float.MaxValue : float.MinValue;
        }

        /// <summary>A cena da Tobi começa: a ponte some e vai sendo montada por <see cref="SetFront"/>.</summary>
        public void BeginBuild()
        {
            _building = true;
            ShowAll(false);
        }

        /// <summary>A nuvem de obra chegou em <paramref name="x"/>: as tábuas até ali aparecem, cada uma com um pulinho.</summary>
        public void SetFront(float x)
        {
            if (rope != null && !rope.enabled && x > StartX) rope.enabled = true;
            for (int i = 0; i < planks.Length; i++)
            {
                var p = planks[i];
                if (p == null || p.gameObject.activeSelf || !TobiRules.Built(p.position.x, x)) continue;
                p.gameObject.SetActive(true);
                _born[i] = Time.time;
                HitSparks.Burst((Vector2)p.position + Vector2.up * 0.4f, 4, new Color32(0xd2, 0x9a, 0x5c, 0xff), new Color32(0x7a, 0x4e, 0x26, 0xff), 1f, 3f, 0.3f, 15f, 0.4f);
            }
            _front = x;
        }

        /// <summary>Pronta (fim da cena, ou a cena foi interrompida).</summary>
        public void Finish()
        {
            SetFront(float.MaxValue);
            _building = false;
        }

        /// <summary>A ponte range: as tábuas balançam um pouco por <paramref name="seconds"/> s.</summary>
        public void Creak(float seconds)
        {
            _creakUntil = Time.time + seconds;
            _creakFor = seconds;
        }

        void Update()
        {
            if (_born == null) return;
            float creak = Time.time < _creakUntil ? (_creakUntil - Time.time) / Mathf.Max(0.01f, _creakFor) : 0f;
            for (int i = 0; i < planks.Length; i++)
            {
                var p = planks[i];
                if (p == null || !p.gameObject.activeSelf || p.childCount == 0) continue;
                var visual = p.GetChild(0);
                float age = Time.time - _born[i];
                // o pulinho ao aparecer (0,25 s) e o rangido (as tábuas descem e sobem em onda)
                float pop = age < 0.25f ? Mathf.Sin(age / 0.25f * Mathf.PI) * 0.25f : 0f;
                float wave = creak > 0f ? Mathf.Sin(Time.time * 18f + i * 0.7f) * 0.06f * creak : 0f;
                var lp = visual.localPosition;
                lp.y = VisualOffset + pop + wave;
                visual.localPosition = lp;
            }
        }

        /// <summary>Onde fica o desenho no bloco (o topo do tabuleiro = o topo da colisão).</summary>
        public const float VisualOffset = 0.25f;   // o sprite (32 × 24) tem o topo do tabuleiro 4 px acima do meio
    }
}
