using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Atalho de verdade (pedido do usuário, 13/11/2026: "tipo Dark Souls, abrir um lugar conectando a outro lugar
    /// anterior"): nada de porta que teletransporta. É um pedaço do cenário que só se abre de um lado:
    /// <list type="bullet">
    /// <item><b>Cipó enrolado</b>: lá em cima, o gato solta o cipó, que desce pela parede até a sala de baixo; de baixo
    /// ele aparece enrolado, fora de alcance.</item>
    /// <item><b>Alçapão</b>: uma tampa de raízes sobre um poço; abre por cima (e o cipó do poço desce junto).</item>
    /// <item><b>Portão</b>: tranca de um lado só.</item>
    /// </list>
    /// Aberto, grava a flag no save; os dois lados (às vezes em cenas diferentes) olham a mesma flag ao carregar.
    /// <see cref="closedObjects"/> ficam ligados enquanto fechado (tampa, portão, cipó enrolado) e
    /// <see cref="openObjects"/> quando aberto (o cipó estendido, que é escalável: layer Climbable).
    /// </summary>
    [SelectionBase]
    public class ShortcutLink : MonoBehaviour
    {
        [SerializeField] string flag = "";
        [Tooltip("Deste lado dá para abrir (o mais adiantado na fase). Do outro, só aparece fechado")]
        [SerializeField] bool opensHere;
        [Tooltip("De que lado o gato tem que estar para abrir (a partir deste objeto): (0, 1) = em cima, (1, 0) = à direita")]
        [SerializeField] Vector2 openSide = Vector2.up;
        [SerializeField] GameObject[] closedObjects = new GameObject[0];
        [SerializeField] GameObject[] openObjects = new GameObject[0];
        [SerializeField] TextMesh prompt;
        [SerializeField] string verb = "soltar o cipó";
        [SerializeField] string closedLine = "Preso do outro lado.";
        [Tooltip("Distância (tiles) em que o gato alcança a alavanca / a tampa")]
        [SerializeField, Min(0.5f)] float reach = 1.6f;

        bool _open;
        float _barkAt = -10f;

        public string Flag => flag;
        public bool OpensHere => opensHere;
        public bool IsOpen => _open;

        public void Configure(string shortcutFlag, bool canOpenHere, Vector2 side, GameObject[] whenClosed, GameObject[] whenOpen, string verbText, string closedText)
        {
            flag = shortcutFlag ?? "";
            opensHere = canOpenHere;
            openSide = side;
            closedObjects = whenClosed ?? new GameObject[0];
            openObjects = whenOpen ?? new GameObject[0];
            verb = verbText;
            closedLine = closedText;
        }

        void Start()
        {
            if (prompt != null) prompt.text = "E: " + verb;
            WorldPrompt.Upgrade(prompt);
            Apply(ShortcutRules.IsOpen(SaveManager.Data, flag));
        }

        void Apply(bool open)
        {
            _open = open;
            foreach (var g in closedObjects) if (g != null) g.SetActive(!open);
            foreach (var g in openObjects) if (g != null) g.SetActive(open);
            if (prompt != null && open) prompt.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_open) return;
            var h = PlayerHealth.Active;
            var runner = DialogueRunner.Active;
            if (h == null || h.IsDead || SceneTravel.IsTraveling || (runner != null && runner.IsOpen)) { ShowPrompt(false); return; }
            Vector2 d = h.transform.position - transform.position;
            bool near = d.magnitude <= reach + 0.5f;
            bool can = near && ShortcutRules.CanOpen(_open, opensHere, d, openSide);
            ShowPrompt(can);
            if (!near) return;
            var input = h.GetComponent<PlayerInputReader>();
            if (input == null || !input.isActiveAndEnabled || !input.InteractPressedThisFrame) return;

            if (ShortcutRules.IsOpen(SaveManager.Data, flag)) { Apply(true); return; }   // aberto agora há pouco, pelo outro ponto
            if (!can)
            {
                if (Time.time - _barkAt > 2f) { SpeechBubble.Show(h.transform, closedLine, 2.2f); _barkAt = Time.time; }
                SeteVidas.Audio.Sfx.Play("adaga_tink", transform.position, 0.6f, 0.7f);
                return;
            }
            var data = SaveManager.Data;
            if (data != null && !string.IsNullOrEmpty(flag))
            {
                data.SetFlag(flag);
                SaveManager.Save();
            }
            Apply(true);
            SeteVidas.Audio.Sfx.Play("grade_abre", transform.position, 0.8f, 0.8f);     // provisório: madeira/cipó
            SeteVidas.Audio.Sfx.Play("plataforma_cai", transform.position, 0.6f, 0.9f);
            SpeechBubble.Show(h.transform, "Pronto. Um caminho mais curto.", 2.2f);
        }

        void ShowPrompt(bool on)
        {
            if (prompt != null && prompt.gameObject.activeSelf != on) prompt.gameObject.SetActive(on);
        }
    }

    /// <summary>Regras puras do atalho (EditMode).</summary>
    public static class ShortcutRules
    {
        public static bool IsOpen(SaveData data, string flag) =>
            string.IsNullOrEmpty(flag) || (data != null && data.HasFlag(flag));

        /// <summary>
        /// Abre agora? Só fechado, só no lado que abre e com o gato do lado certo (<paramref name="openSide"/>, a partir do
        /// atalho; vale um pouco de folga: o gato em pé na borda do alçapão está "em cima" dele).
        /// </summary>
        public static bool CanOpen(bool open, bool opensHere, Vector2 toPlayer, Vector2 openSide)
        {
            if (open || !opensHere) return false;
            if (openSide.sqrMagnitude < 0.0001f) return true;
            return Vector2.Dot(toPlayer, openSide.normalized) > -0.2f;
        }
    }
}
