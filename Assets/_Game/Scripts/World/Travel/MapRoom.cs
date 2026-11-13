using System.Collections.Generic;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Uma sala do mapa (Docs/Design/floresta-mecanicas.md §6): retângulo centrado neste objeto. Quando o gato entra,
    /// a sala vai para o save com os santuários e baús que estão dentro dela, e passa a aparecer na aba Mapa.
    /// <see cref="mapOffset"/> põe as salas desta cena no lugar certo do mapa da região (cada cena tem as próprias
    /// coordenadas; a mesma região pode ter várias cenas lado a lado).
    /// </summary>
    public class MapRoom : MonoBehaviour
    {
        static readonly List<MapRoom> s_all = new List<MapRoom>();

        [Tooltip("Id único na cena (ex.: A4)")]
        [SerializeField] string roomId = "sala";
        [Tooltip("Região do mapa (uma página da aba Mapa)")]
        [SerializeField] string region = "Floresta Silenciosa";
        [Tooltip("Tamanho da sala (tiles), centrada neste objeto")]
        [SerializeField] Vector2 size = new Vector2(30f, 17f);
        [Tooltip("Onde a origem desta cena fica no mapa da região (tiles)")]
        [SerializeField] Vector2 mapOffset = Vector2.zero;

        public string Id => roomId;
        public string Region => region;
        public Vector2 MapOffset => mapOffset;
        public Rect Area => new Rect((Vector2)transform.position - size * 0.5f, size);

        /// <summary>As salas da cena carregada (para o gato no mapa).</summary>
        public static IReadOnlyList<MapRoom> All => s_all;

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }
        void OnDisable() => s_all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_all.Clear(); // Play sem recarregar o domínio

        /// <summary>Para o setup do editor.</summary>
        public void Configure(string id, string regionName, Vector2 roomSize, Vector2 offset)
        {
            roomId = id;
            region = regionName;
            size = roomSize;
            mapOffset = offset;
        }

        float _nextCheck;

        void Update()
        {
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + 0.25f;
            var h = PlayerHealth.Active;
            if (h == null || !Area.Contains(h.transform.position)) return;
            var data = SaveManager.Data;
            if (data == null || MapRules.Seen(data, gameObject.scene.name, roomId)) return;
            MapRules.Record(data, Build());
        }

        /// <summary>A sala como vai para o save: retângulo no mapa e os ícones de dentro.</summary>
        public MapRoomRecord Build()
        {
            var a = Area;
            var rec = new MapRoomRecord
            {
                scene = gameObject.scene.name, id = roomId, region = region,
                x = a.x + mapOffset.x, y = a.y + mapOffset.y, w = a.width, h = a.height,
            };
            foreach (var s in Sanctuary.All)
                if (s != null && s.gameObject.scene == gameObject.scene && a.Contains(s.transform.position))
                    rec.markers.Add(Marker(MapMarker.Sanctuary, s.Id, s.transform.position));
            foreach (var c in FindObjectsByType<WoodChest>(FindObjectsSortMode.None))
                if (c.gameObject.scene == gameObject.scene && a.Contains(c.transform.position))
                    rec.markers.Add(Marker(MapMarker.Chest, c.Id, c.transform.position));
            return rec;
        }

        MapMarker Marker(string kind, string id, Vector2 at) =>
            new MapMarker { kind = kind, id = id, x = at.x + mapOffset.x, y = at.y + mapOffset.y };

        /// <summary>Onde o gato está no mapa da região, se estiver numa sala desta cena.</summary>
        public static bool TryPlayerOnMap(out string regionName, out Vector2 point)
        {
            regionName = null;
            point = default;
            var h = PlayerHealth.Active;
            if (h == null) return false;
            Vector2 p = h.transform.position;
            foreach (var r in s_all)
            {
                if (r == null || !r.Area.Contains(p)) continue;
                regionName = r.region;
                point = p + r.mapOffset;
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.35f);
            Gizmos.DrawWireCube(transform.position, size);
        }
#endif
    }
}
