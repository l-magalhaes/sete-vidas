using SeteVidas.Combat;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>Chave no chão: encostar pega (flag "chave_&lt;id&gt;", salva na hora). Flutua e brilha de leve.</summary>
    [SelectionBase]
    [RequireComponent(typeof(BoxCollider2D))]
    public class KeyPickup : MonoBehaviour
    {
        [SerializeField] string keyId = "cabana";
        [SerializeField] Transform visual;

        Vector3 _rest;

        void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

        public void Configure(string id) { keyId = id; GetComponent<BoxCollider2D>().isTrigger = true; }

        void Start()
        {
            if (visual != null) _rest = visual.localPosition;
            if (KeyRules.HasKey(SaveManager.Data, keyId)) gameObject.SetActive(false);
        }

        void Update()
        {
            if (visual != null) visual.localPosition = _rest + Vector3.up * (Mathf.Round(Mathf.Sin(Time.time * 2.5f) * 2f) / 16f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h == null || h.IsDead) return;
            var data = SaveManager.Data;
            if (data == null || KeyRules.HasKey(data, keyId)) return;
            data.SetFlag(KeyRules.KeyFlag(keyId));
            SaveManager.Save();
            Celebrate(transform.position + Vector3.up * 1.2f, keyId, h.transform);
            gameObject.SetActive(false);
        }

        public static readonly Color32 Gold = new Color32(0xff, 0xd3, 0x4d, 0xff);
        public static readonly Color32 Turquoise = new Color32(0x5c, 0xe0, 0xc8, 0xff);

        /// <summary>
        /// Aviso de chave pega (no chão ou num baú): nome e brilho dourados na da cabana; turquesa, mais forte e com
        /// fala do Lion na do templo (F4b: as duas chaves se distinguem à primeira vista).
        /// </summary>
        public static void Celebrate(Vector3 at, string keyId, Transform cat)
        {
            bool temple = KeyRules.IsTempleKey(keyId);
            Color32 c = temple ? Turquoise : Gold;
            PixelPopup.Spawn(at, KeyRules.Name(keyId), c);
            int n = temple ? 16 : 8;
            for (int i = 0; i < n; i++)
                HitSparks.Rise((Vector2)at + new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(-1.4f, -0.6f)), c, Random.Range(1f, 2.5f), 0.7f);
            SeteVidas.Audio.Sfx.Play(temple ? "classe_desbloqueada" : "madeira_pega", at, temple ? 0.8f : 1f, temple ? 0.8f : 1.2f);   // F10
            if (temple && cat != null)
                SeteVidas.UI.SpeechBubble.Show(cat, "Pesada. E me olhando. Ótimo.", 2.4f);
        }
    }
}
