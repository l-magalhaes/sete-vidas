using System.Collections.Generic;
using SeteVidas.Save;

namespace SeteVidas.World
{
    /// <summary>
    /// Chaves e portas trancadas (regras puras, EditMode). A chave é a flag "chave_&lt;id&gt;"; a porta aberta é a flag
    /// "porta_aberta_&lt;id da porta&gt;" e fica aberta para sempre. A chave usada continua no save, mas some do inventário.
    /// </summary>
    public static class KeyRules
    {
        public const string KeyPrefix = "chave_";
        public const string DoorPrefix = "porta_aberta_";

        /// <summary>Nomes das chaves para o inventário e o aviso ao pegar.</summary>
        static readonly Dictionary<string, string> s_names = new Dictionary<string, string>
        {
            { "cabana", "Chave da cabana" },
        };

        public static string KeyFlag(string keyId) => KeyPrefix + keyId;
        public static string DoorFlag(string doorId) => DoorPrefix + doorId;
        public static string Name(string keyId) => s_names.TryGetValue(keyId ?? "", out var n) ? n : "Chave";

        public static bool HasKey(SaveData data, string keyId) => data != null && data.HasFlag(KeyFlag(keyId));
        public static bool DoorOpen(SaveData data, string doorId) => data != null && data.HasFlag(DoorFlag(doorId));

        /// <summary>A porta abre agora? Já aberta, ou o gato tem a chave dela.</summary>
        public static bool CanOpen(SaveData data, string doorId, string keyId) =>
            DoorOpen(data, doorId) || string.IsNullOrEmpty(keyId) || HasKey(data, keyId);

        /// <summary>Chaves no bolso: pegas e ainda sem porta aberta com elas (<paramref name="doorOfKey"/> diz qual porta cada uma abre).</summary>
        public static List<string> KeysInPocket(SaveData data, IDictionary<string, string> doorOfKey)
        {
            var list = new List<string>();
            if (data?.flags == null) return list;
            foreach (var f in data.flags)
            {
                if (!f.StartsWith(KeyPrefix, System.StringComparison.Ordinal)) continue;
                string key = f.Substring(KeyPrefix.Length);
                if (doorOfKey != null && doorOfKey.TryGetValue(key, out var door) && DoorOpen(data, door)) continue;
                list.Add(key);
            }
            return list;
        }

        /// <summary>Que porta cada chave abre (para o inventário esconder a chave já usada).</summary>
        public static readonly Dictionary<string, string> DoorOfKey = new Dictionary<string, string>
        {
            { "cabana", "cabana_vulpis" },
        };
    }

    /// <summary>Sala-arena (regras puras): fecha ao entrar, abre quando todos os inimigos caem.</summary>
    public static class ArenaRules
    {
        public const string ClearedPrefix = "arena_vencida_";
        public static string ClearedFlag(string arenaId) => ClearedPrefix + arenaId;

        public enum State { Waiting, Fighting, Cleared }

        /// <summary>O próximo estado da arena.</summary>
        public static State Step(State state, bool playerInside, bool playerDead, int enemiesAlive)
        {
            switch (state)
            {
                case State.Waiting: return playerInside && !playerDead && enemiesAlive > 0 ? State.Fighting : State.Waiting;
                case State.Fighting:
                    if (playerDead) return State.Waiting;          // morreu: a arena reabre e espera de novo
                    return enemiesAlive <= 0 ? State.Cleared : State.Fighting;
                default: return State.Cleared;
            }
        }
    }
}
