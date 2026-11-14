using System.Collections;
using SeteVidas.Combat;
using SeteVidas.Enemies.Zombie;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A Ponte do Último Marco (Floresta F3, plano §1.3 "cutscene 1"): o gato entra na ponte de pedra, a horda de
    /// infectados aparece atrás, a pedra treme e racha; no meio, o centro desaba e ele cai. O gatilho de baixo
    /// (RoomExit com a flag <see cref="FallenFlag"/>) leva ao fundo do vale (A1). Com a flag no save, a ponte já está
    /// caída: os blocos do meio não existem e a queda leva direto à A1, dos dois lados.
    /// Estados: Esperando → Fuga (horda, tremor) → Desabando (blocos caem) → Caído.
    /// </summary>
    public class BridgeCollapse : MonoBehaviour
    {
        public const string FallenFlag = "ponte_caiu";

        public enum Phase { Waiting, Chase, Collapsing, Fallen }

        [Tooltip("Blocos de pedra do trecho que desaba (Rigidbody2D + colisor no Ground)")]
        [SerializeField] Rigidbody2D[] segments = new Rigidbody2D[0];
        [Tooltip("x onde a fuga começa (o gato pisou na ponte)")]
        [SerializeField] float startX;
        [Tooltip("x onde o centro desaba (o gato chegou ao meio)")]
        [SerializeField] float collapseX;
        [Tooltip("Desaba sozinha depois deste tempo de fuga, mesmo se o gato não chegar ao meio")]
        [SerializeField, Min(1f)] float maxChaseTime = 10f;
        [SerializeField] ZombieSquad horde;

        Phase _phase = Phase.Waiting;
        float _chaseTime;
        Vector2[] _rest;

        public Phase Current => _phase;

        public void Configure(Rigidbody2D[] blocks, float start, float collapse, ZombieSquad hordeSquad)
        {
            segments = blocks ?? new Rigidbody2D[0];
            startX = start;
            collapseX = collapse;
            horde = hordeSquad;
        }

        void Start()
        {
            _rest = new Vector2[segments.Length];
            for (int i = 0; i < segments.Length; i++) if (segments[i] != null) _rest[i] = segments[i].position;
            if (SaveManager.Data != null && SaveManager.Data.HasFlag(FallenFlag))
            {
                foreach (var s in segments) if (s != null) s.gameObject.SetActive(false);
                _phase = Phase.Fallen;
            }
        }

        void Update()
        {
            var cat = PlayerHealth.Active;
            if (cat == null || cat.IsDead) return;
            float x = cat.transform.position.x;
            switch (_phase)
            {
                case Phase.Waiting:
                    if (x >= startX) BeginChase(cat);
                    break;
                case Phase.Chase:
                    _chaseTime += Time.deltaTime;
                    Rumble(Mathf.Clamp01(_chaseTime / maxChaseTime));
                    if (x >= collapseX || _chaseTime >= maxChaseTime) StartCoroutine(Collapse(cat));
                    break;
            }
        }

        void BeginChase(PlayerHealth cat)
        {
            _phase = Phase.Chase;
            if (horde != null) horde.SpawnAll(alerted: true);
            SpeechBubble.Show(cat.transform, "Ah, não. Eles me seguiram.", 2f);
            SeteVidas.Audio.Sfx.Play("zumbi_sente", (Vector2)cat.transform.position + Vector2.left * 6f, 1f, 0.8f);
            CameraShake.Shake(1f, 0.6f);
        }

        // a pedra treme cada vez mais (aviso): os blocos vibram em volta do lugar
        void Rumble(float k)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                var s = segments[i];
                if (s == null) continue;
                float j = 0.02f + 0.06f * k;
                s.position = _rest[i] + new Vector2(0f, Mathf.Sin(Time.time * 40f + i * 1.7f) * j);
            }
            if (Random.value < Time.deltaTime * (1f + 3f * k)) SeteVidas.Audio.Sfx.Play("parede_racha", segments.Length > 0 && segments[0] != null ? segments[0].position : (Vector2)transform.position, 0.6f, 0.8f);
        }

        IEnumerator Collapse(PlayerHealth cat)
        {
            _phase = Phase.Collapsing;
            var data = SaveManager.Data;
            if (data != null) { data.SetFlag(FallenFlag); SaveManager.Save(); } // libera o gatilho da queda
            SpeechBubble.Show(cat.transform, "Não, não, não—", 1.6f);
            CameraShake.Shake(3f, 1.2f);
            SeteVidas.Audio.Sfx.Play("plataforma_cai", cat.transform.position, 1f, 0.6f);
            // do meio para as pontas, um bloco de cada vez
            var order = new System.Collections.Generic.List<int>();
            for (int i = 0; i < segments.Length; i++) order.Add(i);
            float mid = (segments.Length - 1) * 0.5f;
            order.Sort((a, b) => Mathf.Abs(a - mid).CompareTo(Mathf.Abs(b - mid)));
            foreach (int i in order)
            {
                var s = segments[i];
                if (s == null) continue;
                s.position = _rest[i];
                s.bodyType = RigidbodyType2D.Dynamic;
                s.gravityScale = 3f;
                s.angularVelocity = Random.Range(-120f, 120f);
                var col = s.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;   // o gato cai junto
                yield return new WaitForSeconds(0.06f);
            }
            _phase = Phase.Fallen;
            yield return new WaitForSeconds(4f);
            foreach (var s in segments) if (s != null) s.gameObject.SetActive(false);
        }
    }
}
