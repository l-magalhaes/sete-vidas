using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using SeteVidas.Save;
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
        [Tooltip("\"E: descansar\" quando o gato está perto")]
        [SerializeField] TextMesh prompt;

        PlayerHealth _playerInRange;

        public string Id => sanctuaryId;
        public Vector2 SpawnPoint => (Vector2)transform.position + spawnOffset;
        /// <summary>É o último santuário onde o gato descansou.</summary>
        public bool IsActiveCheckpoint => Application.isPlaying && SaveManager.Data.lastSanctuaryId == sanctuaryId;

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }
        void OnDisable() => s_all.Remove(this);

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
            bool canRest = _playerInRange != null && !_playerInRange.IsDead;
            if (prompt != null) prompt.gameObject.SetActive(canRest);
            if (spriteRenderer != null) spriteRenderer.sprite = IsActiveCheckpoint && litSprite != null ? litSprite : unlitSprite;

            if (!canRest) return;
            var input = _playerInRange.GetComponent<PlayerInputReader>();
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame) Rest(_playerInRange);
        }

        /// <summary>Descansa aqui: vida cheia, magia de cura recarregada, salva, vira o checkpoint e os inimigos voltam.</summary>
        public void Rest(PlayerHealth health)
        {
            if (health == null || health.IsDead) return;
            health.RestoreFull();
            var heal = health.GetComponent<HealingSpell>();
            if (heal != null) heal.Refill();                         // magia de cura volta aos 3 usos
            SaveManager.SaveAtSanctuary(sanctuaryId, displayName);
            GameEvents.RaiseWorldReset();
            DamagePopup.Spawn(transform.position + Vector3.up * 2.4f, "Salvo", new Color32(0xff, 0xd3, 0x4d, 0xff));
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
