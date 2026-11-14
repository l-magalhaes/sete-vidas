using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// O mapa do mundo (melhorias 14/11/2026, estilo Hollow Knight): a silhueta de cada cena (onde há espaço para o
    /// gato andar, tirado do tilemap "Ground"), já posta no lugar dela no mundo: a Fortaleza à esquerda, a Estrada e
    /// a Floresta à direita. Gerado no editor pelo menu "22. Mapa do mundo: gerar silhuetas"
    /// (<c>Editor/WorldMapSetup.cs</c>), fica em <c>Resources/WorldMap.asset</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Sete Vidas/World Map Data", fileName = "WorldMap")]
    public class WorldMapData : ScriptableObject
    {
        public const string ResourcePath = "WorldMap";

        /// <summary>Um ícone fixo do mapa (santuário, baú), em coordenadas do mapa.</summary>
        [Serializable]
        public class Pin
        {
            public string kind = "";
            public string id = "";
            public float x, y;
        }

        /// <summary>A silhueta de uma cena.</summary>
        [Serializable]
        public class SceneMap
        {
            public string scene = "";
            /// <summary>Nome do lugar (título do mapa): "Cidadela do Sol Eterno", "Floresta Silenciosa"...</summary>
            public string region = "";
            /// <summary>Onde a origem da cena fica no mapa (tiles).</summary>
            public int offsetX, offsetY;
            /// <summary>Canto de baixo à esquerda da grade, já em coordenadas do mapa.</summary>
            public int x0, y0;
            public int w, h;
            /// <summary>Células com espaço (1 bit cada, linha a linha de baixo para cima), em base64.</summary>
            public string bits = "";
            public List<Pin> pins = new List<Pin>();
            /// <summary>Sala da Floresta: entrar nela revela a sala inteira.</summary>
            public bool wholeRoom;
            /// <summary>
            /// Salas dentro de uma peça grande (a Fortaleza, uma peça só): entrar numa delas revela o retângulo inteiro
            /// (coordenadas do mapa).
            /// </summary>
            public List<RectInt> reveals = new List<RectInt>();

            [NonSerialized] bool[] _cells;

            public Vector2 Offset => new Vector2(offsetX, offsetY);

            /// <summary>A célula do mapa (mx, my) é espaço desta cena?</summary>
            public bool Space(int mx, int my)
            {
                int x = mx - x0, y = my - y0;
                if (x < 0 || y < 0 || x >= w || y >= h) return false;
                _cells ??= WorldMapRules.Unpack(bits, w * h);
                return _cells[y * w + x];
            }

            /// <summary>Guarda as células (editor).</summary>
            public void SetCells(bool[] cells)
            {
                bits = WorldMapRules.Pack(cells);
                _cells = null;
            }
        }

        /// <summary>Ligação entre duas peças (porta, passagem): um traço do ponto a ao ponto b, em coordenadas do mapa.</summary>
        [Serializable]
        public class Link
        {
            public float ax, ay, bx, by;
        }

        /// <summary>
        /// As peças do mapa. Uma cena pode ter várias: na Floresta, cada sala é uma peça, afastada das vizinhas
        /// (o mapa "explode" a floresta para cada sala ler sozinha; ver <c>WorldMapSetup.Spread</c>).
        /// </summary>
        public List<SceneMap> scenes = new List<SceneMap>();
        public List<Link> links = new List<Link>();

        /// <summary>A primeira peça da cena.</summary>
        public SceneMap Find(string scene)
        {
            foreach (var s in scenes) if (s != null && s.scene == scene) return s;
            return null;
        }

        /// <summary>A peça da cena onde fica o ponto (coordenadas da cena); fora de todas, a mais próxima.</summary>
        public SceneMap FindAt(string scene, Vector2 scenePoint)
        {
            SceneMap best = null;
            float bestD = float.MaxValue;
            foreach (var s in scenes)
            {
                if (s == null || s.scene != scene) continue;
                float x0 = s.x0 - s.offsetX, y0 = s.y0 - s.offsetY;
                float dx = Mathf.Max(0f, Mathf.Max(x0 - scenePoint.x, scenePoint.x - (x0 + s.w)));
                float dy = Mathf.Max(0f, Mathf.Max(y0 - scenePoint.y, scenePoint.y - (y0 + s.h)));
                float d = dx * dx + dy * dy;
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        /// <summary>Retângulo do mapa inteiro (tiles), com 2 de margem.</summary>
        public RectInt Bounds()
        {
            if (scenes.Count == 0) return new RectInt(0, 0, 1, 1);
            int ax = int.MaxValue, ay = int.MaxValue, bx = int.MinValue, by = int.MinValue;
            foreach (var s in scenes)
            {
                if (s == null || s.w <= 0) continue;
                ax = Math.Min(ax, s.x0); ay = Math.Min(ay, s.y0);
                bx = Math.Max(bx, s.x0 + s.w); by = Math.Max(by, s.y0 + s.h);
            }
            if (ax == int.MaxValue) return new RectInt(0, 0, 1, 1);
            return new RectInt(ax - 2, ay - 2, bx - ax + 4, by - ay + 4);
        }

        static WorldMapData s_cached;
        static bool s_loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_cached = null; s_loaded = false; } // Play sem recarregar o domínio

        /// <summary>O mapa gerado (null se o menu 22 ainda não rodou).</summary>
        public static WorldMapData Load()
        {
            if (!s_loaded)
            {
                s_cached = Resources.Load<WorldMapData>(ResourcePath);
                s_loaded = true;
            }
            return s_cached != null && s_cached.scenes.Count > 0 ? s_cached : null;
        }
    }
}
