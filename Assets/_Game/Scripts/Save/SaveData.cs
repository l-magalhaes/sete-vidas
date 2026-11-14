using System;
using System.Collections.Generic;
using SeteVidas.Classes;

namespace SeteVidas.Save
{
    /// <summary>
    /// Conteúdo do save em JSON. Classes ficam como texto (nome do enum) para que reordenar o enum
    /// não corrompa saves antigos. Mudou o formato? Suba <see cref="CurrentVersion"/> e trate em SaveStore.Migrate.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        /// <summary>Prólogo: o Ladino começa com as 7 vidas. O tomo amaldiçoado apaga 6 (sobra 1); cada vínculo devolve uma.</summary>
        public int heartsRecovered = 7;
        public List<string> unlockedClasses = new List<string> { nameof(CharacterClass.Rogue) };
        public string activeClass = nameof(CharacterClass.Rogue);
        /// <summary>Santuário onde o gato descansou por último ("" = nenhum: usa o início da cena).</summary>
        public string lastSanctuaryId = "";
        /// <summary>Nome do último santuário para mostrar na tela inicial ("" = sem nome).</summary>
        public string lastSanctuaryName = "";
        /// <summary>Nome da cena do último santuário.</summary>
        public string scene = "";
        /// <summary>Moedas na bolsa do gato (gastas na loja). As moedas pegas no mundo viram marcas "moeda:&lt;id&gt;" em <see cref="flags"/>.</summary>
        public int coins;
        /// <summary>Usos da magia de cura (Paladino). Recarrega ao descansar no santuário.</summary>
        public int healCharges = MaxHealCharges;
        public const int MaxHealCharges = 3;
        /// <summary>Marcas do mundo: paredes quebradas, missões, diálogos vistos.</summary>
        public List<string> flags = new List<string>();
        /// <summary>Santuários já visitados, de todas as cenas: os destinos da viagem rápida (Floresta, 13/11/2026).</summary>
        public List<SanctuaryVisit> visitedSanctuaries = new List<SanctuaryVisit>();
        /// <summary>Salas já vistas, de todas as cenas: o que a aba Mapa desenha (Docs/Design/floresta-mecanicas.md §6).</summary>
        public List<MapRoomRecord> mapRooms = new List<MapRoomRecord>();
        /// <summary>Pedaços do mapa do mundo já explorados (WorldMapRules.ChunkKey; melhorias 14/11/2026).</summary>
        public List<int> mapChunks = new List<int>();

        public static SaveData NewGame() => new SaveData();

        public bool HasFlag(string flag) => flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !flags.Contains(flag)) flags.Add(flag);
        }

        public bool IsUnlocked(CharacterClass c) => unlockedClasses.Contains(c.ToString());
    }

    /// <summary>Uma sala do mapa já vista: retângulo em coordenadas do mapa da região (tiles) e o que há nela.</summary>
    [Serializable]
    public class MapRoomRecord
    {
        public string scene = "";
        public string id = "";
        /// <summary>Nome da região (agrupa as salas numa página do mapa).</summary>
        public string region = "";
        public float x, y, w, h;
        public List<MapMarker> markers = new List<MapMarker>();
    }

    /// <summary>Ícone no mapa: santuário ou baú (o estado do baú vem das flags).</summary>
    [Serializable]
    public class MapMarker
    {
        public const string Sanctuary = "santuario";
        public const string Chest = "bau";
        public string kind = "";
        public string id = "";
        public float x, y;
    }

    /// <summary>Um santuário visitado: o que a viagem rápida precisa saber sem carregar a cena dele.</summary>
    [Serializable]
    public class SanctuaryVisit
    {
        public string id = "";
        public string name = "";
        public string scene = "";
        /// <summary>Zona de viagem ("fortaleza", "floresta"): zonas diferentes só se ligam quando o caminho existe (ponte).</summary>
        public string zone = "";
    }
}
