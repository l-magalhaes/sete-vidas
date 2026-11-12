using System.Collections;
using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Santuário felino (checkpoint). Com o gato perto, Interagir (E) descansa: vida cheia, salva o jogo,
    /// vira o ponto de volta ao morrer e os inimigos reaparecem. Encostar sem apertar E não salva.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Sanctuary : MonoBehaviour
    {
        static readonly List<Sanctuary> s_all = new List<Sanctuary>();

        [Tooltip("Identificador único na cena. Vai para o save: não mude depois de lançado")]
        [SerializeField] string sanctuaryId = "santuario";
        [Tooltip("Nome mostrado na tela inicial (Continuar)")]
        [SerializeField] string displayName = "Santuário";
        [Tooltip("Onde o gato reaparece, relativo ao santuário (tiles)")]
        [SerializeField] Vector2 spawnOffset = Vector2.zero;

        [Header("Visual")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite unlitSprite;
        [SerializeField] Sprite litSprite;
        [Tooltip("Quadros do santuário ativo (a fonte correndo). Vazio = usa o litSprite parado")]
        [SerializeField] Sprite[] litFrames;
        [SerializeField, Min(0.1f)] float litFps = 8f;
        [Tooltip("\"E: descansar\" quando o gato está perto")]
        [SerializeField] TextMesh prompt;

        [Header("Descanso (fade + cochilo na caixinha de areia)")]
        [Tooltip("Onde o gato cochila, relativo ao santuário (tiles): o meio da caixinha de areia")]
        [SerializeField] Vector2 sandboxOffset = new Vector2(1.8f, 0f);
        [Tooltip("Borda da frente da caixinha, desenhada na frente do gato enquanto ele está dentro dela")]
        [SerializeField] SpriteRenderer sandboxFront;
        [SerializeField, Min(0f)] float fadeOutTime = 0.35f;
        [SerializeField, Min(0f)] float blackHoldTime = 0.25f;
        [SerializeField, Min(0f)] float fadeInTime = 0.5f;
        [Tooltip("Quanto tempo o gato fica deitado na areia depois que a tela clareia")]
        [SerializeField, Min(0f)] float napTime = 0.4f;
        [Tooltip("Duração do nap_wake: abre os olhos, boceja, espreguiça e levanta (9 quadros × 0,12 s)")]
        [SerializeField, Min(0f)] float getUpTime = 1.1f;

        PlayerHealth _playerInRange;
        bool _resting;

        /// <summary>A sequência de descanso (fade, cochilo, levantar) está tocando.</summary>
        public bool IsResting => _resting;
        public Vector2 SandboxPoint => (Vector2)transform.position + sandboxOffset;

        public string Id => sanctuaryId;
        public Vector2 SpawnPoint => (Vector2)transform.position + spawnOffset;
        /// <summary>É o último santuário onde o gato descansou.</summary>
        public bool IsActiveCheckpoint => Application.isPlaying && SaveManager.Data.lastSanctuaryId == sanctuaryId;

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }
        void OnDisable() => s_all.Remove(this);

        public static IReadOnlyList<Sanctuary> All => s_all;

        public static Sanctuary Find(string id)
        {
            foreach (var s in s_all)
                if (s != null && s.sanctuaryId == id) return s;
            return null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) _playerInRange = health;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null && health == _playerInRange) _playerInRange = null;
        }

        void Update()
        {
            bool canRest = _playerInRange != null && !_playerInRange.IsDead && !_resting;
            if (prompt != null) prompt.gameObject.SetActive(canRest);
            if (spriteRenderer != null) spriteRenderer.sprite = CurrentSprite();
            // a borda da frente some quando o gato sai da caixinha
            if (sandboxFront != null && sandboxFront.enabled && !_resting && !CatInSandbox()) sandboxFront.enabled = false;

            if (!canRest) return;
            var input = _playerInRange.GetComponent<PlayerInputReader>();
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame) BeginRest(_playerInRange);
        }

        Sprite CurrentSprite()
        {
            if (!IsActiveCheckpoint) return unlitSprite;
            if (litFrames != null && litFrames.Length > 0) return litFrames[(int)(Time.time * litFps) % litFrames.Length];
            return litSprite != null ? litSprite : unlitSprite;
        }

        /// <summary>Começa o descanso com cena (o mesmo que apertar E perto do santuário).</summary>
        public void BeginRest(PlayerHealth health)
        {
            if (_resting || health == null || health.IsDead) return;
            _playerInRange = health;
            StartCoroutine(RestRoutine(health));
        }

        bool CatInSandbox()
        {
            if (_playerInRange == null) return false;
            Vector2 d = (Vector2)_playerInRange.transform.position - SandboxPoint;
            return Mathf.Abs(d.x) < 0.6f && Mathf.Abs(d.y) < 0.5f;
        }

        /// <summary>
        /// Descanso com cena: escurece (o jogador não vê os inimigos voltarem), descansa e salva no escuro, põe o gato
        /// deitado na caixinha de areia, clareia e ele se levanta. Sem controle e sem dano durante tudo.
        /// </summary>
        IEnumerator RestRoutine(PlayerHealth health)
        {
            _resting = true;
            SeteVidas.Audio.Sfx.Play("santuario_descansa");
            var input = health.GetComponent<PlayerInputReader>();
            var player = health.GetComponent<PlayerController>();
            if (input != null) input.SetLock(this, true);
            health.Shielded = true;                                      // de pé, parado e sem dano enquanto escurece
            var fader = ScreenFader.Active;
            if (fader != null) yield return fader.FadeTo(1f, fadeOutTime);

            Rest(health, popup: false);
            if (player != null) { player.Teleport(SandboxPoint); player.Face(-1); } // de frente para a fonte
            // só no escuro ele deita: aparece já enrolado na caixinha de areia
            health.SetCollapsed(true);
            var visual = health.GetComponentInChildren<PlayerVisual>();
            if (visual != null) visual.PlayAction("nap");
            var safe = health.GetComponent<SafeGroundTracker>();
            if (safe != null) safe.ResetTo(SandboxPoint);
            if (sandboxFront != null) sandboxFront.enabled = true;
            yield return new WaitForSeconds(blackHoldTime);

            if (fader != null) yield return fader.FadeTo(0f, fadeInTime);
            yield return new WaitForSeconds(napTime);
            health.SetCollapsed(false, graceAfter: 0f);                  // o visual toca "nap_wake"
            yield return new WaitForSeconds(getUpTime);

            DamagePopup.Spawn(transform.position + Vector3.up * 2.4f, "Salvo", new Color32(0xff, 0xd3, 0x4d, 0xff));
            if (input != null) input.SetLock(this, false);
            health.Shielded = false;
            _resting = false;
        }

        void OnDestroy()
        {
            // se a cena fechar no meio do descanso, devolve o controle
            if (_resting && _playerInRange != null)
            {
                var input = _playerInRange.GetComponent<PlayerInputReader>();
                if (input != null) input.SetLock(this, false);
                _playerInRange.Shielded = false;
            }
        }

        /// <summary>Descansa aqui (na hora, sem cena): vida cheia, magia de cura recarregada, salva, vira o checkpoint e os inimigos voltam.</summary>
        public void Rest(PlayerHealth health, bool popup = true)
        {
            if (health == null || health.IsDead) return;
            health.RestoreFull();
            var heal = health.GetComponent<HealingSpell>();
            if (heal != null) heal.Refill();                         // magia de cura volta aos 3 usos
            SaveManager.SaveAtSanctuary(sanctuaryId, displayName);
            GameEvents.RaiseWorldReset();
            if (popup) DamagePopup.Spawn(transform.position + Vector3.up * 2.4f, "Salvo", new Color32(0xff, 0xd3, 0x4d, 0xff));
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(SpawnPoint + Vector2.up * 0.1f, 0.15f);
        }
#endif
    }
}
