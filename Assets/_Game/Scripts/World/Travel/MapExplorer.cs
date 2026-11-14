using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Vai marcando no save os pedaços do mapa do mundo por onde o gato passa (melhorias 14/11/2026). Nasce sozinho
    /// no começo do Play e atravessa as trocas de cena. Sem o mapa gerado (menu 22), não faz nada.
    /// </summary>
    public class MapExplorer : MonoBehaviour
    {
        static MapExplorer s_instance;
        float _next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_instance = null; // Play sem recarregar o domínio

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (s_instance != null) return;
            var go = new GameObject("[MapExplorer]");
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<MapExplorer>();
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.2f;
            if (!TryCatOnMap(out Vector2 p, out var piece)) return;
            WorldMapRules.MarkAround(SaveManager.Data, p);
            // na Floresta, entrar numa sala revela a sala inteira (como no Hollow Knight)
            if (piece.wholeRoom) WorldMapRules.MarkRect(SaveManager.Data, piece.x0, piece.y0, piece.w, piece.h);
            // na Fortaleza (uma peça só), a sala do FortressLayout onde o gato está aparece inteira
            if (piece.reveals != null)
                foreach (var r in piece.reveals)
                    if (r.Contains(new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y))))
                        WorldMapRules.MarkRect(SaveManager.Data, r.x, r.y, r.width, r.height);
        }

        /// <summary>Onde o gato está no mapa do mundo, e em que cena do mapa.</summary>
        public static bool TryCatOnMap(out Vector2 mapPoint, out WorldMapData.SceneMap scene)
        {
            mapPoint = default;
            scene = null;
            var h = PlayerHealth.Active;
            var map = WorldMapData.Load();
            if (h == null || map == null || h.IsDead) return false;
            scene = map.FindAt(h.gameObject.scene.name, h.transform.position);
            if (scene == null) return false;
            mapPoint = (Vector2)h.transform.position + scene.Offset;
            return true;
        }
    }
}
