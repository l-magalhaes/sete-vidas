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
            var gold = new Color32(0xff, 0xd3, 0x4d, 0xff);
            PixelPopup.Spawn(transform.position + Vector3.up * 1.2f, KeyRules.Name(keyId), gold);
            for (int i = 0; i < 8; i++)
                HitSparks.Rise((Vector2)transform.position + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.2f, 0.4f)), gold, Random.Range(1f, 2f), 0.6f);
            SeteVidas.Audio.Sfx.Play("moeda", transform.position, 1f, 0.8f); // provisório
            gameObject.SetActive(false);
        }
    }
}
