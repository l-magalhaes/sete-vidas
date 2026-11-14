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
    [SelectionBase] // clicar no desenho seleciona o objeto inteiro (desenho e colisão andam juntos)
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
        [Tooltip("Zona da viagem rápida (vazio = pela cena: Forest_* = floresta, o resto = fortaleza)")]
        [SerializeField] string travelZone = "";

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
        // viagem rápida (melhorias 14/11/2026): os dois avisos juntos, [Interagir] descansar e [Ataque] viagem rápida
        SeteVidas.UI.WorldPrompt _travelPrompt;
        List<SanctuaryVisit> _destinations = new List<SanctuaryVisit>();
        float _nextDestinationCheck;
        bool _travelReady;

        static Sanctuary s_travelHere;

        /// <summary>
        /// O gato está num santuário com viagem rápida: o botão de ataque abre o menu de viagem em vez de golpear
        /// (o <see cref="SeteVidas.Combat.CombatController"/> consulta isto).
        /// </summary>
        public static bool AttackOpensTravel => s_travelHere != null && s_travelHere._travelReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() // Play sem recarregar o domínio
        {
            s_all.Clear();
            s_travelHere = null;
        }

        /// <summary>O aviso "viagem rápida" está aparecendo (testes).</summary>
        public bool TravelAvailable => _travelReady;

        /// <summary>A sequência de descanso (fade, cochilo, levantar) está tocando.</summary>
        public bool IsResting => _resting;
        public Vector2 SandboxPoint => (Vector2)transform.position + sandboxOffset;

        public string Id => sanctuaryId;
        public string DisplayName => displayName;
        /// <summary>Zona da viagem rápida (ver <see cref="TravelRules"/>).</summary>
        public string Zone => TravelRules.Zone(gameObject.scene.name, travelZone);
        public Vector2 SpawnPoint => (Vector2)transform.position + spawnOffset;
        /// <summary>É o último santuário onde o gato descansou.</summary>
        public bool IsActiveCheckpoint => Application.isPlaying && SaveManager.Data.lastSanctuaryId == sanctuaryId;

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }

        void Start()
        {
            if (prompt != null)
            {
                // segundo aviso, logo acima do de descansar: [botão de ataque] viagem rápida
                var copy = Instantiate(prompt.gameObject, prompt.transform.parent);
                copy.name = "Aviso: viagem rápida";
                copy.transform.localPosition = prompt.transform.localPosition + Vector3.up * 0.75f;
                var text = copy.GetComponent<TextMesh>();
                text.text = "X: viagem rápida";
                _travelPrompt = SeteVidas.UI.WorldPrompt.Upgrade(text, SeteVidas.UI.GameAction.Attack);
                copy.SetActive(false);
            }
            SeteVidas.UI.WorldPrompt.Upgrade(prompt); // o botão do aparelho em uso no lugar de "E: ..."
        }

        void OnDisable()
        {
            s_all.Remove(this);
            if (s_travelHere == this) s_travelHere = null;
        }

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
            if (health != null) RecordVisit(); // passou por aqui: vira destino da viagem rápida
            _nextDestinationCheck = 0f;        // confere os destinos na hora
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null && health == _playerInRange) _playerInRange = null;
        }

        void Update()
        {
            bool canRest = _playerInRange != null && !_playerInRange.IsDead && !_resting
                           && !SeteVidas.UI.TravelMenu.IsOpen && !SceneTravel.IsTraveling;
            if (prompt != null) prompt.gameObject.SetActive(canRest);
            if (spriteRenderer != null) spriteRenderer.sprite = CurrentSprite();
            // a borda da frente some quando o gato sai da caixinha
            if (sandboxFront != null && sandboxFront.enabled && !_resting && !CatInSandbox()) sandboxFront.enabled = false;

            // viagem rápida: só com outro santuário visitado e ligado a este, e sem veneno
            if (canRest && Time.time >= _nextDestinationCheck)
            {
                _nextDestinationCheck = Time.time + 0.5f;
                _destinations = _playerInRange.IsPoisoned
                    ? new List<SanctuaryVisit>()
                    : TravelRules.Destinations(SaveManager.Data, sanctuaryId, gameObject.scene.name, Zone);
            }
            _travelReady = canRest && _destinations.Count > 0 && !_playerInRange.IsPoisoned;
            if (_travelPrompt != null) _travelPrompt.gameObject.SetActive(_travelReady);
            if (_travelReady) s_travelHere = this;
            else if (s_travelHere == this) s_travelHere = null;

            if (!canRest) return;
            var input = _playerInRange.GetComponent<PlayerInputReader>();
            if (input == null || !input.isActiveAndEnabled) return;
            if (input.InteractPressedThisFrame) Interact(_playerInRange);
            else if (_travelReady && input.AttackPressedThisFrame) OpenTravel(_playerInRange);
        }

        Sprite CurrentSprite()
        {
            if (!IsActiveCheckpoint) return unlitSprite;
            if (litFrames != null && litFrames.Length > 0) return litFrames[(int)(Time.time * litFps) % litFrames.Length];
            return litSprite != null ? litSprite : unlitSprite;
        }

        /// <summary>Marca este santuário como destino da viagem rápida (vai para o disco no próximo salvamento).</summary>
        public void RecordVisit()
        {
            if (!Application.isPlaying || SaveManager.Data == null) return;
            TravelRules.RecordVisit(SaveManager.Data, sanctuaryId, displayName, gameObject.scene.name, Zone);
        }

        /// <summary>
        /// Apertou Interagir (E / B): descansa direto. A viagem rápida tem o próprio botão (<see cref="OpenTravel"/>),
        /// os dois avisos aparecem juntos (melhorias 14/11/2026).
        /// </summary>
        public void Interact(PlayerHealth health)
        {
            if (health == null || health.IsDead || _resting) return;
            RecordVisit();
            BeginRest(health);
        }

        /// <summary>
        /// Apertou o botão de ataque (X / clique) no santuário: abre a lista dos santuários para onde viajar.
        /// Envenenado não viaja (Docs/Design/floresta-mecanicas.md §5). Retorna se abriu.
        /// </summary>
        public bool OpenTravel(PlayerHealth health)
        {
            if (health == null || health.IsDead || _resting || health.IsPoisoned || SceneTravel.IsTraveling) return false;
            RecordVisit();
            var destinations = TravelRules.Destinations(SaveManager.Data, sanctuaryId, gameObject.scene.name, Zone);
            if (destinations.Count == 0) return false;
            SeteVidas.UI.TravelMenu.Open(this, health, destinations);
            return true;
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

            SavedFx();
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
            if (popup) SavedFx();
        }

        /// <summary>
        /// "Salvo": anéis de luz dourada que se abrem do santuário, centelhas subindo e o aviso em pixel
        /// (no lugar do texto na fonte do Unity).
        /// </summary>
        void SavedFx()
        {
            var gold = new Color32(0xff, 0xd3, 0x4d, 0xff);
            var light = new Color32(0xff, 0xf4, 0xc2, 0xff);
            Vector2 c = (Vector2)transform.position + Vector2.up * 1.2f;
            PixelRing.Spawn(c, 4, 22, 0.45f, light, 1);
            PixelRing.Spawn(c, 2, 14, 0.4f, gold, 1, 0.12f);
            for (int i = 0; i < 10; i++)
                HitSparks.Rise(c + new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(-1f, 0.2f)), i % 2 == 0 ? (Color)gold : light,
                               Random.Range(1.2f, 2.4f), Random.Range(0.5f, 0.9f));
            PixelPopup.Spawn(transform.position + Vector3.up * 2.4f, "Salvo", gold);
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
