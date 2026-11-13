using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Porta trancada (a cabana da Vulpis): sólida até abrir. Perto dela, E abre se o gato tem a chave; sem chave, o
    /// Lion reclama num balão. Aberta fica aberta para sempre (flag "porta_aberta_&lt;id&gt;", salva na hora).
    /// </summary>
    [SelectionBase]
    [RequireComponent(typeof(BoxCollider2D))]
    public class LockedDoor : MonoBehaviour
    {
        [SerializeField] string doorId = "cabana_vulpis";
        [SerializeField] string keyId = "cabana";
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite closedSprite;
        [SerializeField] Sprite openSprite;
        [Tooltip("\"E: abrir\" quando o gato está perto")]
        [SerializeField] TextMesh prompt;
        [Tooltip("Distância (tiles) em que o gato pode abrir")]
        [SerializeField, Min(0.5f)] float reach = 1.6f;

        BoxCollider2D _solid;
        float _barkAt = -10f;

        public bool IsOpen { get; private set; }

        public void Configure(string door, string key) { doorId = door; keyId = key; }

        void Awake() => _solid = GetComponent<BoxCollider2D>();

        void Start()
        {
            WorldPrompt.Upgrade(prompt);
            SetOpen(KeyRules.DoorOpen(SaveManager.Data, doorId));
        }

        void SetOpen(bool open)
        {
            IsOpen = open;
            _solid.enabled = !open;
            if (spriteRenderer != null) spriteRenderer.sprite = open && openSprite != null ? openSprite : closedSprite;
            if (prompt != null && open) prompt.gameObject.SetActive(false);
        }

        void Update()
        {
            if (IsOpen) return;
            var h = PlayerHealth.Active;
            var runner = DialogueRunner.Active;
            bool near = h != null && !h.IsDead && (runner == null || !runner.IsOpen) &&
                        Mathf.Abs(h.transform.position.x - transform.position.x) <= reach &&
                        h.transform.position.y >= transform.position.y - 0.5f && h.transform.position.y <= transform.position.y + 3f;
            if (prompt != null) prompt.gameObject.SetActive(near);
            if (!near) return;
            var input = h.GetComponent<PlayerInputReader>();
            if (input == null || !input.isActiveAndEnabled || !input.InteractPressedThisFrame) return;

            var data = SaveManager.Data;
            if (!KeyRules.CanOpen(data, doorId, keyId))
            {
                if (Time.time - _barkAt > 2f) { SpeechBubble.Show(h.transform, "Trancada. Deve ter uma chave por aí.", 2.2f); _barkAt = Time.time; }
                SeteVidas.Audio.Sfx.Play("adaga_tink", transform.position, 0.6f, 0.7f); // provisório: maçaneta presa
                return;
            }
            data.SetFlag(KeyRules.DoorFlag(doorId));
            SaveManager.Save();
            SetOpen(true);
            SeteVidas.Audio.Sfx.Play("grade_abre", transform.position, 0.8f, 1.2f); // provisório
            SpeechBubble.Show(h.transform, "Abriu. Com licença, Vulpis.", 2.2f);
        }
    }
}
