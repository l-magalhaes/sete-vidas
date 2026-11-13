using System.Collections.Generic;
using SeteVidas.Progression;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Regras puras da aba Mapa (Docs/Design/floresta-mecanicas.md §6, EditMode): salas vistas por região, o
    /// enquadramento no painel e o estado dos ícones. As salas ficam no save (<see cref="SaveData.mapRooms"/>), então
    /// o mapa mostra também as de cenas que não estão carregadas.
    /// </summary>
    public static class MapRules
    {
        /// <summary>Grava a sala (substitui a anterior com a mesma cena e id: a sala pode ter mudado de forma).</summary>
        public static bool Record(SaveData data, MapRoomRecord room)
        {
            if (data == null || room == null || string.IsNullOrEmpty(room.id) || string.IsNullOrEmpty(room.scene)) return false;
            data.mapRooms ??= new List<MapRoomRecord>();
            for (int i = 0; i < data.mapRooms.Count; i++)
            {
                var r = data.mapRooms[i];
                if (r.scene != room.scene || r.id != room.id) continue;
                data.mapRooms[i] = room;
                return false; // já era conhecida
            }
            data.mapRooms.Add(room);
            return true;
        }

        public static bool Seen(SaveData data, string scene, string id)
        {
            if (data?.mapRooms == null) return false;
            foreach (var r in data.mapRooms) if (r.scene == scene && r.id == id) return true;
            return false;
        }

        /// <summary>Regiões com alguma sala vista, na ordem em que foram descobertas.</summary>
        public static List<string> Regions(SaveData data)
        {
            var list = new List<string>();
            if (data?.mapRooms == null) return list;
            foreach (var r in data.mapRooms)
                if (!string.IsNullOrEmpty(r.region) && !list.Contains(r.region)) list.Add(r.region);
            return list;
        }

        public static List<MapRoomRecord> RoomsIn(SaveData data, string region)
        {
            var list = new List<MapRoomRecord>();
            if (data?.mapRooms == null) return list;
            foreach (var r in data.mapRooms) if (r.region == region) list.Add(r);
            return list;
        }

        /// <summary>Retângulo que envolve todas as salas (tiles do mapa).</summary>
        public static Rect Bounds(List<MapRoomRecord> rooms)
        {
            if (rooms == null || rooms.Count == 0) return new Rect(0f, 0f, 1f, 1f);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var r in rooms)
            {
                x0 = Mathf.Min(x0, r.x); y0 = Mathf.Min(y0, r.y);
                x1 = Mathf.Max(x1, r.x + r.w); y1 = Mathf.Max(y1, r.y + r.h);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        /// <summary>
        /// Escala (pixels por tile, inteira para o pixel art não borrar quando dá) que faz <paramref name="world"/> caber em
        /// <paramref name="panel"/> com margem.
        /// </summary>
        public static float FitScale(Rect world, Vector2 panel, float margin = 6f)
        {
            float sx = (panel.x - 2f * margin) / Mathf.Max(1f, world.width);
            float sy = (panel.y - 2f * margin) / Mathf.Max(1f, world.height);
            float s = Mathf.Min(sx, sy);
            return s >= 1f ? Mathf.Floor(s) : s;
        }

        /// <summary>Ponto do mapa (tiles) para o painel (pixels a partir do canto de baixo à esquerda), centralizado.</summary>
        public static Vector2 ToPanel(Vector2 point, Rect world, Vector2 panel, float scale)
        {
            Vector2 used = world.size * scale;
            Vector2 origin = (panel - used) * 0.5f;
            return origin + (point - world.min) * scale;
        }

        /// <summary>O baú do ícone já foi aberto?</summary>
        public static bool ChestOpen(SaveData data, string chestId) =>
            data != null && data.HasFlag(ReconstructionWood.ChestFlag(chestId));

        /// <summary>O santuário do ícone já foi visitado (aceso no mapa)?</summary>
        public static bool SanctuaryLit(SaveData data, string scene, string sanctuaryId) =>
            TravelRules.Visited(data, sanctuaryId, scene);
    }
}
