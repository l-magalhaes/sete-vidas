using System.Collections;
using SeteVidas.Dialogue;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// O tomo corrompido, atrás da grade da arena. E pega o tomo e dispara a maldição (spec:
    /// Docs/Design/chefe-golden.md): clarão roxo, seis corações se apagam um a um (7 → 1), o gato cai quase
    /// morto e o contratante (capivara necromante) surge, toma o tomo e joga uma moeda de ouro. A tela escurece e o
    /// Golden aparece ao lado do gato. A conversa de vínculo (juramento de destruir o
    /// tomo) devolve o 2º coração e dá o Paladino. Estado recuperável: fechar o jogo no meio não trava nada.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CursedTome : MonoBehaviour
    {
        public const string TakenFlag = "tomo_pego";
        /// <summary>O contratante (capivara necromante) levou o tomo e deixou a moeda.</summary>
        public const string StolenFlag = "contratante_levou_tomo";

        [SerializeField] GoldenBoss golden;
        [SerializeField] CompanionBond goldenBond;
        [Tooltip("Livro sobre o pedestal (some ao ser pego)")]
        [SerializeField] SpriteRenderer book;
        [SerializeField] TextMesh prompt;

        [Header("Conversas")]
        [Tooltip("Ao pegar o tomo (narração)")]
        [SerializeField] DialogueDefinition takeDialogue;
        [Tooltip("Depois dos corações se apagarem (narração)")]
        [SerializeField] DialogueDefinition curseDialogue;
        [Tooltip("O contratante aparece e revela o plano (D-CAPIVARA-01)")]
        [SerializeField] DialogueDefinition villainDialogue;
        [Tooltip("A moeda e a partida (D-CAPIVARA-02)")]
        [SerializeField] DialogueDefinition villainLeaveDialogue;

        [Header("O contratante")]
        [Tooltip("Capivara necromante (desligada até a cena)")]
        [SerializeField] SpriteRenderer contractor;
        [Tooltip("O tomo nas patas dele")]
        [SerializeField] SpriteRenderer contractorBook;
        [Tooltip("Moeda de ouro jogada no chão (\"o pagamento\")")]
        [SerializeField] Transform coin;
        [SerializeField] Color teleportColor = new Color32(0x4a, 0x22, 0x66, 0xff);

        [Header("Ritmo da maldição")]
        [SerializeField] float heartBreakInterval = 0.3f;
        [SerializeField] Color curseColor = new Color32(0x6b, 0x2a, 0x8a, 0xff);
        [Tooltip("Corações que sobram (GDD: a maldição apaga 6 das 7 vidas)")]
        [SerializeField, Min(1)] int heartsLeft = 1;

        PlayerHealth _playerInRange;
        Vector3 _bookRest;
        bool _running;

        public bool IsTaken => SaveManager.Data.HasFlag(TakenFlag);
        public bool IsRunning => _running;

        void Start()
        {
            if (book != null) _bookRest = book.transform.localPosition;
            if (IsTaken) AfterCurseState();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h != null) _playerInRange = h;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h != null && h == _playerInRange) _playerInRange = null;
        }

        void Update()
        {
            if (book != null && book.enabled)
                book.transform.localPosition = _bookRest + Vector3.up * (0.08f * Mathf.Sin(Time.time * 2.5f)); // flutua

            bool canTake = !_running && !IsTaken && _playerInRange != null && !_playerInRange.IsDead &&
                           (golden == null || golden.IsDefeated);
            if (prompt != null) prompt.gameObject.SetActive(canTake && (DialogueRunner.Active == null || !DialogueRunner.Active.IsOpen));
            if (!canTake) return;

            var input = _playerInRange.GetComponent<PlayerInputReader>();
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame)
                StartCoroutine(Take(_playerInRange));
        }

        /// <summary>A sequência inteira (também chamada pelos testes).</summary>
        public IEnumerator Take(PlayerHealth player)
        {
            if (_running || IsTaken || player == null) yield break;
            _running = true;
            var input = player.GetComponent<PlayerInputReader>();
            if (input != null) input.SetLock(this, true);
            var runner = DialogueRunner.Active;

            yield return Talk(runner, takeDialogue, input);

            // Pega o tomo: o selo se rompe
            var visual = player.GetComponentInChildren<PlayerVisual>();
            if (visual != null && visual.PlayAction("grab_tome", hold: true)) yield return new WaitForSeconds(0.6f);
            if (book != null) book.enabled = false;
            if (visual != null) visual.PlayAction("curse_kneel", hold: true);
            var data = SaveManager.Data;
            data.SetFlag(TakenFlag);
            var fader = ScreenFader.Active;
            if (fader != null) StartCoroutine(fader.Flash(curseColor, 0.7f, 0.15f, 0.6f));

            // Seis vidas se apagam, uma a uma (o HUD mostra cada coração quebrando)
            for (int h = player.HeartsRecovered - 1; h >= heartsLeft; h--)
            {
                yield return new WaitForSeconds(heartBreakInterval);
                player.SetHeartsRecovered(h, save: false);
            }
            player.ApplyCurse(heartsLeft); // grava: corações e flag do tomo
            player.SetCollapsed(true);

            yield return new WaitForSeconds(0.8f);
            yield return Talk(runner, curseDialogue, input);

            // O contratante: surge num clarão roxo, revela o plano, toma o tomo, paga com uma moeda e some
            yield return ContractorScene(player, runner, input, fader);

            // O Golden chega
            if (fader != null) yield return fader.FadeTo(1f, 0.5f);
            if (golden != null)
            {
                float side = player.transform.position.x - 1.6f;
                golden.StandAsCompanion(new Vector2(side, player.transform.position.y), +1);
            }
            if (goldenBond != null) goldenBond.enabled = true;
            yield return new WaitForSeconds(0.3f);
            if (fader != null) yield return fader.FadeTo(0f, 0.5f);

            // Vínculo: juramento de destruir o tomo → 2º coração + Paladino
            bool done = goldenBond == null;
            if (goldenBond != null) goldenBond.StartBond(player, () => done = true);
            while (!done) yield return null;

            player.SetCollapsed(false);
            if (input != null) input.SetLock(this, false);
            _running = false;
        }

        IEnumerator ContractorScene(PlayerHealth player, DialogueRunner runner, PlayerInputReader input, ScreenFader fader)
        {
            if (contractor == null) yield break;
            Vector3 feet = player.transform.position;
            var t = contractor.transform;
            t.position = feet + new Vector3(2.4f, 0f, 0f);
            contractor.flipX = true; // olha para o gato caído
            if (contractorBook != null) contractorBook.enabled = false;

            // Teleporte de chegada
            if (fader != null) StartCoroutine(fader.Flash(teleportColor, 0.8f, 0.08f, 0.5f));
            contractor.gameObject.SetActive(true);
            yield return FadeSprite(contractor, 0f, 1f, 0.35f);
            yield return Talk(runner, villainDialogue, input);

            // Toma o tomo das patas do gato
            if (contractorBook != null) contractorBook.enabled = true;
            yield return new WaitForSeconds(0.4f);

            // Joga a moeda: "o pagamento está feito"
            if (coin != null)
            {
                coin.gameObject.SetActive(true);
                yield return Toss(coin, t.position + Vector3.up * 1.2f, feet + new Vector3(0.9f, 0.05f, 0f), 0.45f);
            }
            yield return Talk(runner, villainLeaveDialogue, input);

            // Teleporte de saída, levando o tomo
            if (fader != null) StartCoroutine(fader.Flash(teleportColor, 0.8f, 0.08f, 0.5f));
            yield return FadeSprite(contractor, 1f, 0f, 0.3f);
            contractor.gameObject.SetActive(false);
            var data = SaveManager.Data;
            data.SetFlag(StolenFlag);
            SaveManager.Save();
            yield return new WaitForSeconds(0.6f);
        }

        static IEnumerator FadeSprite(SpriteRenderer sr, float from, float to, float duration)
        {
            var renderers = sr.GetComponentsInChildren<SpriteRenderer>();
            for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(0.01f, duration))
            {
                foreach (var r in renderers) { var c = r.color; c.a = Mathf.Lerp(from, to, k); r.color = c; }
                yield return null;
            }
            foreach (var r in renderers) { var c = r.color; c.a = to; r.color = c; }
        }

        /// <summary>Arremesso em arco com um quique ao cair.</summary>
        static IEnumerator Toss(Transform obj, Vector3 from, Vector3 to, float duration)
        {
            for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
            {
                obj.position = Vector3.Lerp(from, to, k) + Vector3.up * (1.5f * 4f * k * (1f - k));
                yield return null;
            }
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.2f) // quique
            {
                obj.position = to + Vector3.up * (0.25f * 4f * k * (1f - k));
                yield return null;
            }
            obj.position = to;
        }

        IEnumerator Talk(DialogueRunner runner, DialogueDefinition dialogue, PlayerInputReader input)
        {
            if (runner == null || dialogue == null) yield break;
            runner.Play(dialogue, input);
            while (runner.IsOpen) yield return null;
        }

        /// <summary>Save carregado depois do tomo: pedestal vazio e o Golden de pé ao lado, pronto para conversar.</summary>
        void AfterCurseState()
        {
            if (book != null) book.enabled = false;
            if (golden != null) golden.StandAsCompanion((Vector2)transform.position + Vector2.left * 1.6f, +1);
            if (goldenBond != null) goldenBond.enabled = true; // se o vínculo não terminou, falar com ele recomeça
            if (contractor != null) contractor.gameObject.SetActive(false);
            if (coin != null) coin.gameObject.SetActive(true); // a moeda continua no chão: o "pagamento"
        }
    }
}
