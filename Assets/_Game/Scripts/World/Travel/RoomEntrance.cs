using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Ponto de chegada de uma porta (<see cref="RoomExit"/>) vinda de outra sala ou cena. O id é único na cena
    /// (ex.: "A4_de_A3"). O gato aparece nos pés deste objeto, olhando para <see cref="FacingDirection"/>.
    /// </summary>
    public class RoomEntrance : MonoBehaviour
    {
        static readonly List<RoomEntrance> s_all = new List<RoomEntrance>();

        [Tooltip("Identificador único na cena. As portas apontam para ele")]
        [SerializeField] string entranceId = "entrada";
        [Tooltip("Para onde o gato olha ao chegar: 1 = direita, -1 = esquerda")]
        [SerializeField] int facing = 1;

        public string Id => entranceId;
        public int FacingDirection => facing >= 0 ? 1 : -1;
        public Vector2 SpawnPoint => transform.position;

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }
        void OnDisable() => s_all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_all.Clear(); // Play sem recarregar o domínio

        public static RoomEntrance Find(string id)
        {
            foreach (var e in s_all) if (e != null && e.entranceId == id) return e;
            return null;
        }

        /// <summary>Para o setup do editor.</summary>
        public void Configure(string id, int facingDir)
        {
            entranceId = id;
            facing = facingDir;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.8f);
            Vector3 p = transform.position;
            Gizmos.DrawWireCube(p + Vector3.up * 0.7f, new Vector3(0.75f, 1.4f, 0f));
            Gizmos.DrawLine(p + Vector3.up * 0.7f, p + new Vector3(FacingDirection * 0.8f, 0.7f, 0f));
        }
#endif
    }
}
