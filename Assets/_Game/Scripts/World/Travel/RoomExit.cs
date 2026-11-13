using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Porta entre salas (borda da sala, buraco, topo de um poço): ao encostar, o gato vai para a entrada
    /// <see cref="TargetEntrance"/> da cena <see cref="TargetScene"/> (vazia = esta cena). Pode exigir uma flag
    /// (ex.: "ponte_reconstruida"); sem ela, a porta não faz nada.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomExit : MonoBehaviour
    {
        [Tooltip("Cena de destino (vazio = a cena atual)")]
        [SerializeField] string targetScene = "";
        [Tooltip("Id da RoomEntrance de chegada")]
        [SerializeField] string targetEntrance = "";
        [Tooltip("Só funciona com esta flag no save (vazio = sempre)")]
        [SerializeField] string requiredFlag = "";

        public string TargetScene => targetScene;
        public string TargetEntrance => targetEntrance;

        void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

        /// <summary>Para o setup do editor.</summary>
        public void Configure(string scene, string entrance, string flag = "")
        {
            targetScene = scene;
            targetEntrance = entrance;
            requiredFlag = flag;
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsDead || SceneTravel.IsTraveling) return;
            if (!string.IsNullOrEmpty(requiredFlag) && (SaveManager.Data == null || !SaveManager.Data.HasFlag(requiredFlag))) return;
            SceneTravel.ToEntrance(targetScene, targetEntrance, health);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireCube(transform.TransformPoint(col.offset), col.size);
        }
#endif
    }
}
