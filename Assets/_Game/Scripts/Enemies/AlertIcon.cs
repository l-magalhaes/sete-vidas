using System.Collections.Generic;
using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// "?" e "!" sobre a cabeça do inimigo em pixel art (pedido do usuário, 12/11/2026: no estilo do jogo, não na
    /// fonte do Unity). Contorno #140f18 como os ícones do inventário, luz no alto e sombra embaixo.
    /// O "?" se enche de baixo para cima com a suspeita: vazio (silhueta escura) = acabou de estranhar,
    /// cheio = vai investigar. Ao trocar de estado, o ícone dá um pulo de 2 px.
    /// </summary>
    public class AlertIcon : MonoBehaviour
    {
        public enum Mark { None, Question, Exclamation }

        static readonly Color32 Yellow = new Color32(0xff, 0xd8, 0x4d, 0xff);
        static readonly Color32 Orange = new Color32(0xff, 0x8a, 0x3b, 0xff);
        static readonly Color32 Red = new Color32(0xff, 0x3b, 0x3b, 0xff);
        static readonly Color32 Hollow = new Color32(0x2e, 0x26, 0x38, 0xff);   // o "vazio" dos slots do inventário

        // 5×8 (o "?") e 2×8 (o "!"), linha 0 em cima
        static readonly string[] QuestionMap = { ".###.", "##.##", "...##", "..##.", ".##..", ".##..", ".....", ".##.." };
        static readonly string[] ExclamationMap = { "##", "##", "##", "##", "##", "..", "##", ".." };
        const int Rows = 8;

        static readonly Dictionary<int, Sprite> s_sprites = new Dictionary<int, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_sprites.Clear(); // Play sem recarregar o domínio

        SpriteRenderer _renderer;
        Mark _mark;
        float _popTime = 99f;

        /// <summary>O que está aparecendo agora (testes).</summary>
        public Mark Showing => _mark;

        /// <summary>Cria o ícone como filho de <paramref name="parent"/>, com a base em <paramref name="localPos"/>.</summary>
        public static AlertIcon Create(Transform parent, Vector3 localPos, int sortingOrder = 40)
        {
            var go = new GameObject("Ícone de alerta");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var icon = go.AddComponent<AlertIcon>();
            icon._basePos = localPos;
            icon._renderer = go.AddComponent<SpriteRenderer>();
            icon._renderer.sortingOrder = sortingOrder;
            icon._renderer.enabled = false;
            return icon;
        }

        /// <summary>Mostra a marca. <paramref name="fill01"/> enche o "?" (0 = vazio, 1 = cheio); o "!" é sempre cheio.</summary>
        public void Show(Mark mark, bool hunting, float fill01)
        {
            if (mark != _mark && mark != Mark.None) _popTime = 0f;
            _mark = mark;
            if (mark == Mark.None) { _renderer.enabled = false; return; }
            Color32 color = mark == Mark.Exclamation ? Red : hunting ? Orange : Yellow;
            int level = mark == Mark.Exclamation || hunting ? Rows : Mathf.Clamp(Mathf.CeilToInt(fill01 * Rows), 1, Rows);
            _renderer.sprite = Get(mark, color, level);
            _renderer.enabled = true;
        }

        void LateUpdate()
        {
            if (_renderer == null || !_renderer.enabled) return;
            _popTime += Time.deltaTime;
            // pulo de 2 px que volta em passos (sem escala, sem meio pixel)
            int px = _popTime < 0.05f ? 2 : _popTime < 0.1f ? 3 : _popTime < 0.16f ? 1 : 0;
            transform.localPosition = _basePos + new Vector3(0f, px / (float)PixelFont.Ppu, 0f);
        }

        Vector3 _basePos;

        static Sprite Get(Mark mark, Color32 color, int level)
        {
            int key = ((int)mark * 16 + level) * 16777216 + (color.r << 16 | color.g << 8 | color.b);
            if (s_sprites.TryGetValue(key, out var s) && s != null) return s;
            var map = mark == Mark.Question ? QuestionMap : ExclamationMap;
            int gw = map[0].Length;
            int w = gw + 2, h = Rows + 2;
            var fill = new bool[w, h];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < gw; c++)
                    if (map[r][c] == '#') fill[c + 1, Rows - r] = true; // y para cima, com 1 px de margem

            var light = new Color32((byte)Mathf.Min(255, color.r + 50), (byte)Mathf.Min(255, color.g + 60), (byte)Mathf.Min(255, color.b + 70), 255);
            var shade = new Color32((byte)(color.r * 0.72f), (byte)(color.g * 0.6f), (byte)(color.b * 0.7f), 255);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (fill[x, y])
                    {
                        int rowFromBottom = y - 1; // 0 = linha de baixo
                        bool filled = rowFromBottom < level;
                        if (!filled) c = Hollow;
                        else if (y >= h - 3 && !fill[x - 1, y]) c = light;              // luz na borda esquerda do alto
                        else if (!fill[x, y - 1] || x + 1 < w && !fill[x + 1, y]) c = shade; // sombra embaixo e à direita
                        else c = color;
                    }
                    else if (Near(fill, x, y, w, h)) c = PixelFont.Outline;
                    px[y * w + x] = c;
                }
            s = PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0f), "alerta " + mark + " " + level);
            s_sprites[key] = s;
            return s;
        }

        static bool Near(bool[,] g, int x, int y, int w, int h)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < w && ny < h && g[nx, ny]) return true;
                }
            return false;
        }
    }
}
