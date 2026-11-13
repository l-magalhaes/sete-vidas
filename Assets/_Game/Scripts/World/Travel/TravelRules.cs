using System.Collections.Generic;
using SeteVidas.Progression;
using SeteVidas.Save;

namespace SeteVidas.World
{
    /// <summary>
    /// Regras puras da viagem rápida entre santuários (Docs/Design/floresta-mecanicas.md §5, EditMode).
    /// Cada santuário pertence a uma zona; zonas diferentes só se ligam quando o caminho existe de verdade:
    /// a Floresta e a Fortaleza, depois da ponte reconstruída (a queda da ponte é de mão única).
    /// </summary>
    public static class TravelRules
    {
        public const string Fortress = "fortaleza";
        public const string Forest = "floresta";

        /// <summary>Zona padrão pela cena (Forest_* = floresta; o resto, fortaleza). <paramref name="overrideZone"/> vence.</summary>
        public static string Zone(string scene, string overrideZone = null)
        {
            if (!string.IsNullOrEmpty(overrideZone)) return overrideZone;
            return scene != null && scene.StartsWith("Forest_", System.StringComparison.Ordinal) ? Forest : Fortress;
        }

        /// <summary>Dá para viajar de uma zona para a outra com o save atual?</summary>
        public static bool ZonesLinked(string from, string to, SaveData data)
        {
            if (from == to) return true;
            bool fortressForest = (from == Fortress && to == Forest) || (from == Forest && to == Fortress);
            return fortressForest && ReconstructionWood.BridgeBuilt(data);
        }

        /// <summary>Marca o santuário como visitado (atualiza nome e cena se ele já estava na lista).</summary>
        public static void RecordVisit(SaveData data, string id, string name, string scene, string zone)
        {
            if (data == null || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(scene)) return;
            data.visitedSanctuaries ??= new List<SanctuaryVisit>();
            foreach (var v in data.visitedSanctuaries)
            {
                if (v.id != id || v.scene != scene) continue;
                v.name = name ?? "";
                v.zone = zone ?? "";
                return;
            }
            data.visitedSanctuaries.Add(new SanctuaryVisit { id = id, name = name ?? "", scene = scene, zone = zone ?? "" });
        }

        public static bool Visited(SaveData data, string id, string scene)
        {
            if (data?.visitedSanctuaries == null) return false;
            foreach (var v in data.visitedSanctuaries) if (v.id == id && v.scene == scene) return true;
            return false;
        }

        /// <summary>Destinos possíveis a partir de um santuário: os visitados, ligados à zona dele, menos ele mesmo. Na ordem da visita.</summary>
        public static List<SanctuaryVisit> Destinations(SaveData data, string fromId, string fromScene, string fromZone)
        {
            var list = new List<SanctuaryVisit>();
            if (data?.visitedSanctuaries == null) return list;
            foreach (var v in data.visitedSanctuaries)
            {
                if (v.id == fromId && v.scene == fromScene) continue;
                if (!ZonesLinked(fromZone, string.IsNullOrEmpty(v.zone) ? Zone(v.scene) : v.zone, data)) continue;
                list.Add(v);
            }
            return list;
        }
    }
}
