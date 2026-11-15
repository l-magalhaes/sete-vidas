using System.Collections.Generic;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// Menu da viagem rápida (Docs/Design/floresta-mecanicas.md §5): os santuários visitados e ligados a este.
    /// Abre pelo botão de ataque no santuário; descansar tem o próprio botão (melhorias 14/11/2026).
    /// Pausa o jogo enquanto está aberto, no mesmo visual do inventário.
    /// <para>
    /// Melhorias (14/11/2026): a lista vem NA ORDEM EM QUE OS SANTUÁRIOS FORAM ENCONTRADOS, numerada, com o santuário
    /// de onde se parte no lugar dele ("você está aqui", não escolhível). Ao lado, um mini mapa centrado no destino
    /// escolhido, com o título da região e da área, para saber para onde o gato vai.
    /// </para>
    /// </summary>
    public class TravelMenu : MonoBehaviour
    {
        // janela (pixels da referência 480×270)
        const float W = 340f, H = 196f, Pad = 9f;
        const float HeaderH = 22f, FooterH = 16f;
        const float ListW = 138f, RowH = 14f;
        const float MapX = Pad + ListW + 8f, MapW = W - MapX - Pad;
        const float MapTitleH = 24f, MapNameH = 12f;
        const float MapZoom = 2f;

        static readonly Color Cream = new Color32(0xec, 0xdc, 0xb0, 0xff);
        static readonly Color Faint = new Color32(0x8a, 0x7a, 0x96, 0xff);
        static readonly Color Gold = new Color32(0xff, 0xe0, 0x8a, 0xff);
        static readonly Color Here = new Color32(0x9c, 0xc4, 0xa8, 0xff);
        static readonly Color RowSel = new Color32(0x4a, 0x36, 0x24, 0xff);
        static readonly Color MapBack = new Color32(0x0c, 0x0a, 0x12, 0xff);
        static readonly Color PinLit = new Color32(0xff, 0xd3, 0x4d, 0xff);
        static readonly Color PinDim = new Color32(0x8a, 0x62, 0x1e, 0xff);
        static readonly Color PinHere = new Color32(0xff, 0xf4, 0xdc, 0xff);

        static TravelMenu s_instance;
        public static bool IsOpen => s_instance != null && s_instance._open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_instance = null; // Play sem recarregar o domínio

        Font _font;
        RectTransform _window, _mapArea;
        PromptRow _help;
        Text _title, _count, _placeTitle, _placeName, _noMap, _scrollUp, _scrollDown;
        Image _rowHighlight;
        RawImage _mapImage;
        Sprite _white;
        readonly List<Text> _rows = new List<Text>();
        readonly List<Image> _pins = new List<Image>();
        readonly List<Texture2D> _textures = new List<Texture2D>();
        List<SanctuaryVisit> _entries = new List<SanctuaryVisit>();
        string _fromId, _fromScene;
        PlayerHealth _player;
        PlayerInputReader _input;
        int _cursor, _scroll;
        bool _open;
        float _savedScale = 1f;
        int _openedFrame;

        Texture2D _worldTex;
        RectInt _worldBounds;
        HashSet<int> _explored = new HashSet<int>();
        int _exploredCount = -1;
        Image _selectedPin;

        int VisibleRows => Mathf.FloorToInt((H - HeaderH - FooterH - 4f) / RowH);

        /// <summary>
        /// Abre o menu no santuário <paramref name="from"/>. <paramref name="entries"/>: a lista de
        /// <see cref="TravelRules.MenuList"/> (ordem da descoberta, com o santuário de partida no meio).
        /// </summary>
        public static void Open(Sanctuary from, PlayerHealth player, List<SanctuaryVisit> entries)
        {
            if (s_instance == null)
            {
                var go = new GameObject("Menu do santuário", typeof(RectTransform));
                s_instance = go.AddComponent<TravelMenu>();
                s_instance.Build();
            }
            s_instance.Show(from, player, entries);
        }

        // ------------------------------------------------------------ montagem

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 96;
            canvas.pixelPerfect = true;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 270f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "ui_viagem_branco" };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false);
            _textures.Add(tex);
            _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);

            var dim = NewImage("Escuro", (RectTransform)transform, _white);
            dim.color = new Color(0.03f, 0.02f, 0.05f, 0.65f);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;

            var frame = NewImage("Janela", (RectTransform)transform, PanelSprite(PanelLook.Main, InventoryIcons.PanelBorder));
            frame.type = Image.Type.Sliced;
            _window = frame.rectTransform;
            _window.anchorMin = _window.anchorMax = _window.pivot = new Vector2(0.5f, 0.5f);
            _window.sizeDelta = new Vector2(W, H);

            // cabeçalho: título e quantos santuários já foram encontrados
            _title = NewText("Título", _window, 9, TextAnchor.UpperLeft, Gold);
            Place(_title.rectTransform, Pad + 2f, -Pad + 1f, ListW, 12f);
            _title.text = "Viagem rápida";
            _count = NewText("Encontrados", _window, 7, TextAnchor.UpperRight, Faint);
            Place(_count.rectTransform, W - Pad - 160f, -Pad + 1f, 160f, 12f);
            var line = NewImage("Linha", _window, _white);
            line.color = new Color32(0x5a, 0x48, 0x34, 0xff);
            Place(line.rectTransform, Pad, -(HeaderH - 1f), W - 2f * Pad, 1f);

            // lista
            _rowHighlight = NewImage("Seleção", _window, _white);
            _rowHighlight.color = RowSel;
            for (int i = 0; i < VisibleRows; i++)
            {
                var t = NewText("Opção " + i, _window, 7, TextAnchor.MiddleLeft, Cream);
                Place(t.rectTransform, Pad + 4f, -(HeaderH + 2f + i * RowH), ListW - 6f, RowH);
                _rows.Add(t);
            }
            _scrollUp = NewText("Mais acima", _window, 7, TextAnchor.MiddleCenter, Faint);
            Place(_scrollUp.rectTransform, Pad, -(HeaderH - 6f), ListW, 8f);
            _scrollUp.text = "▲";
            _scrollDown = NewText("Mais abaixo", _window, 7, TextAnchor.MiddleCenter, Faint);
            Place(_scrollDown.rectTransform, Pad, -(H - FooterH - 6f), ListW, 8f);
            _scrollDown.text = "▼";

            // mini mapa: título da região e da área em cima, o mapa no meio, o nome do santuário embaixo
            var inset = NewImage("Mapa", _window, PanelSprite(PanelLook.Inset, InventoryIcons.InsetBorder));
            inset.type = Image.Type.Sliced;
            float mapTop = HeaderH + 2f, mapH = H - HeaderH - FooterH - 4f;
            Place(inset.rectTransform, MapX, -mapTop, MapW, mapH);
            _placeTitle = NewText("Região", inset.rectTransform, 8, TextAnchor.MiddleCenter, Gold);
            Place(_placeTitle.rectTransform, 4f, -3f, MapW - 8f, MapTitleH - 4f);   // até 2 linhas: região · área
            _placeTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            _placeTitle.lineSpacing = 0.85f;
            var areaGo = new GameObject("Área do mapa", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            areaGo.transform.SetParent(inset.rectTransform, false);
            var areaImg = areaGo.GetComponent<Image>();
            areaImg.sprite = _white;
            areaImg.color = MapBack;
            areaImg.raycastTarget = false;
            _mapArea = (RectTransform)areaGo.transform;
            Place(_mapArea, 5f, -MapTitleH, MapW - 10f, mapH - MapTitleH - MapNameH - 2f);
            var world = new GameObject("Mapa do mundo", typeof(RectTransform), typeof(RawImage));
            world.transform.SetParent(_mapArea, false);
            _mapImage = world.GetComponent<RawImage>();
            _mapImage.raycastTarget = false;
            _mapImage.rectTransform.anchorMin = _mapImage.rectTransform.anchorMax = _mapImage.rectTransform.pivot = Vector2.zero;
            _noMap = NewText("Sem mapa", _mapArea, 7, TextAnchor.MiddleCenter, Faint);
            _noMap.rectTransform.anchorMin = Vector2.zero;
            _noMap.rectTransform.anchorMax = Vector2.one;
            _noMap.rectTransform.offsetMin = _noMap.rectTransform.offsetMax = Vector2.zero;
            _noMap.horizontalOverflow = HorizontalWrapMode.Wrap;
            _placeName = NewText("Santuário", inset.rectTransform, 7, TextAnchor.MiddleCenter, Cream);
            Place(_placeName.rectTransform, 2f, -(mapH - MapNameH), MapW - 4f, 10f);

            gameObject.SetActive(false);
        }

        void Show(Sanctuary from, PlayerHealth player, List<SanctuaryVisit> entries)
        {
            _player = player;
            _fromId = from != null ? from.Id : "";
            _fromScene = from != null ? from.gameObject.scene.name : "";
            _entries = entries ?? new List<SanctuaryVisit>();
            _cursor = FirstSelectable();
            _scroll = 0;

            if (_help != null) Destroy(_help.gameObject);
            _help = PromptRow.Create(_window, "Ajuda", _font, 7, Faint, new Vector2(0f, -(H - FooterH + 2f)), W);
            _help.Set(new PromptRow.Item(GameAction.Navigate, "escolher"), new PromptRow.Item(GameAction.Confirm, "viajar"),
                      new PromptRow.Item(GameAction.Back, "voltar"));
            int found = SaveManager.Data?.visitedSanctuaries?.Count ?? 0;
            _count.text = found == 1 ? "1 santuário encontrado" : found + " santuários encontrados";

            _savedScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _input = player != null ? player.GetComponent<PlayerInputReader>() : null;
            if (_input != null) _input.SetLock(this, true);
            _open = true;
            _openedFrame = Time.frameCount;
            gameObject.SetActive(true);
            SeteVidas.Audio.Sfx.Play("ui_inv_abre");
            PaintWorld();
            Redraw();
        }

        bool Selectable(int i) => i >= 0 && i < _entries.Count && !TravelRules.IsHere(_entries[i], _fromId, _fromScene);

        int FirstSelectable()
        {
            for (int i = 0; i < _entries.Count; i++) if (Selectable(i)) return i;
            return -1;
        }

        /// <summary>Próxima opção escolhível na direção <paramref name="dir"/> (dá a volta; pula o "você está aqui").</summary>
        int Step(int from, int dir)
        {
            int n = _entries.Count;
            if (n == 0) return -1;
            int i = from;
            for (int k = 0; k < n; k++)
            {
                i = (i + dir + n) % n;
                if (Selectable(i)) return i;
            }
            return from;
        }

        void Redraw()
        {
            int visible = _rows.Count;
            if (_cursor >= 0)
            {
                if (_cursor < _scroll) _scroll = _cursor;
                if (_cursor >= _scroll + visible) _scroll = _cursor - visible + 1;
            }
            _scroll = Mathf.Clamp(_scroll, 0, Mathf.Max(0, _entries.Count - visible));
            _rowHighlight.enabled = false;

            for (int r = 0; r < visible; r++)
            {
                int i = _scroll + r;
                var t = _rows[r];
                if (_entries.Count == 0 && r == 0) { t.text = "nenhum santuário"; t.color = Faint; continue; }
                if (i >= _entries.Count) { t.text = ""; continue; }
                var v = _entries[i];
                bool here = TravelRules.IsHere(v, _fromId, _fromScene);
                bool sel = i == _cursor;
                string num = (i + 1).ToString().PadLeft(2);
                t.text = num + "  " + NameOf(v) + (here ? "  · aqui" : "");
                t.color = here ? Here : sel ? Gold : Cream;
                if (sel)
                {
                    _rowHighlight.enabled = true;
                    Place(_rowHighlight.rectTransform, Pad + 1f, -(HeaderH + 2f + r * RowH) - 1f, ListW - 1f, RowH - 1f);
                }
            }
            _scrollUp.enabled = _scroll > 0;
            _scrollDown.enabled = _scroll + visible < _entries.Count;
            RedrawMap();
        }

        static string NameOf(SanctuaryVisit v) => string.IsNullOrEmpty(v.name) ? v.id : v.name;

        // ------------------------------------------------------------ mini mapa

        /// <summary>Pinta o mapa do mundo explorado (o mesmo da aba Mapa) numa textura, 1 pixel por tile.</summary>
        void PaintWorld()
        {
            var map = WorldMapData.Load();
            if (map == null) return;
            var data = SaveManager.Data;
            int count = data?.mapChunks?.Count ?? 0;
            if (_worldTex != null && count == _exploredCount) return;
            _worldBounds = map.Bounds();
            _explored = WorldMapRules.ExploredSet(data);
            var px = WorldMapRules.Paint(map, _worldBounds, _explored);
            if (_worldTex == null || _worldTex.width != _worldBounds.width || _worldTex.height != _worldBounds.height)
            {
                if (_worldTex != null) { _textures.Remove(_worldTex); Destroy(_worldTex); }
                _worldTex = new Texture2D(_worldBounds.width, _worldBounds.height, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "ui_viagem_mapa" };
                _textures.Add(_worldTex);
            }
            _worldTex.SetPixels32(px);
            _worldTex.Apply(false);
            _mapImage.texture = _worldTex;
            _exploredCount = count;
        }

        void RedrawMap()
        {
            foreach (var p in _pins) p.enabled = false;
            _selectedPin = null;
            var v = _cursor >= 0 && _cursor < _entries.Count ? _entries[_cursor] : null;
            var map = WorldMapData.Load();

            string region = "";
            bool found = false;
            Vector2 center = default;
            if (v != null && map != null && WorldMapRules.FindPin(map, v.scene, MapMarker.Sanctuary, v.id, out var piece, out center))
            {
                found = true;
                region = piece.region;
            }
            string title = v != null ? TravelRules.PlaceTitle(region, v.scene) : "";
            _placeTitle.text = string.IsNullOrEmpty(title) ? (v != null ? ZoneTitle(v) : "Viagem rápida") : title;
            _placeName.text = v != null ? NameOf(v) : "";

            bool showMap = map != null && _worldTex != null && found;
            _mapImage.enabled = showMap;
            _noMap.enabled = !showMap;
            _noMap.text = v == null ? "Nenhum outro santuário ligado a este." : "Mapa indisponível";
            if (!showMap) return;

            var panel = _mapArea.sizeDelta;
            var c = WorldMapRules.ClampCenter(center, _worldBounds, panel, MapZoom);
            Vector2 P(Vector2 m) => panel * 0.5f + (m - c) * MapZoom;
            var bl = P(new Vector2(_worldBounds.x, _worldBounds.y));
            _mapImage.rectTransform.anchoredPosition = new Vector2(Mathf.Round(bl.x), Mathf.Round(bl.y));
            _mapImage.rectTransform.sizeDelta = new Vector2(_worldBounds.width * MapZoom, _worldBounds.height * MapZoom);

            // os santuários da lista: o escolhido maior (piscando), o de partida em creme, os outros em âmbar
            int used = 0;
            Image selected = null;
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (!WorldMapRules.FindPin(map, e.scene, MapMarker.Sanctuary, e.id, out _, out var at)) continue;
                bool here = TravelRules.IsHere(e, _fromId, _fromScene);
                bool sel = i == _cursor;
                float size = sel ? 6f : 4f;
                Color col = here ? PinHere : sel ? PinLit : PinDim;
                var p = P(at + Vector2.up * 0.5f);
                var img = Pin(used++);
                img.color = col;
                img.rectTransform.anchoredPosition = new Vector2(Mathf.Round(p.x - size * 0.5f), Mathf.Round(p.y - size * 0.5f));
                img.rectTransform.sizeDelta = new Vector2(size, size);
                if (sel) selected = img;
            }
            if (selected != null) { selected.transform.SetAsLastSibling(); _selectedPin = selected; }
        }

        static string ZoneTitle(SanctuaryVisit v)
        {
            string zone = string.IsNullOrEmpty(v.zone) ? TravelRules.Zone(v.scene) : v.zone;
            return zone == TravelRules.Forest ? "Floresta" : "Fortaleza";
        }

        Image Pin(int index)
        {
            while (_pins.Count <= index)
            {
                var img = NewImage("Santuário no mapa " + _pins.Count, _mapArea, _white);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = Vector2.zero;
                _pins.Add(img);
            }
            var pin = _pins[index];
            pin.enabled = true;
            return pin;
        }

        // ------------------------------------------------------------ jogo

        void Close()
        {
            _open = false;
            gameObject.SetActive(false);
            Time.timeScale = _savedScale;
            if (_input != null) _input.SetLock(this, false);
            _input = null;
        }

        void Update()
        {
            if (!_open) return;
            // o destino escolhido pisca no mini mapa (tempo real: o jogo está pausado)
            if (_selectedPin != null) _selectedPin.enabled = ((int)(Time.unscaledTime * 3f) & 1) == 0;
            if (Time.frameCount == _openedFrame) return; // o mesmo botão que abriu não confirma
            var k = Keyboard.current; var p = Gamepad.current;
            int dy = 0;
            bool ok = false, back = false;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) dy = -1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) dy = 1;
                ok |= k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame;
                back |= k.escapeKey.wasPressedThisFrame;
            }
            if (p != null)
            {
                if (p.dpad.up.wasPressedThisFrame || p.leftStick.up.wasPressedThisFrame) dy = -1;
                if (p.dpad.down.wasPressedThisFrame || p.leftStick.down.wasPressedThisFrame) dy = 1;
                ok |= p.buttonSouth.wasPressedThisFrame;
                back |= p.buttonEast.wasPressedThisFrame;
            }

            if (dy != 0 && _cursor >= 0)
            {
                int next = Step(_cursor, dy);
                if (next != _cursor)
                {
                    _cursor = next;
                    SeteVidas.Audio.Sfx.Play("ui_cursor_inv");
                    Redraw();
                }
            }
            if (back) { SeteVidas.Audio.Sfx.Play("ui_inv_fecha"); Close(); return; }
            if (!ok) return;

            var player = _player;
            int choice = _cursor;
            Close();
            if (!Selectable(choice)) return;
            var dest = _entries[choice];
            SeteVidas.Audio.Sfx.Play("santuario_descansa");
            SceneTravel.ToSanctuary(dest.scene, dest.id, player);
        }

        void OnDestroy()
        {
            if (_open) Close();
            if (s_instance == this) s_instance = null;
            foreach (var t in _textures) if (t != null) Destroy(t);
        }

        // ------------------------------------------------------------ peças

        Sprite PanelSprite(PanelLook look, float border)
        {
            var grid = InventoryIcons.Panel(look);
            int h = grid.GetLength(0), w = grid.GetLength(1);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    string hex = grid[y, x];
                    px[(h - 1 - y) * w + x] = hex != "" && ColorUtility.TryParseHtmlString(hex, out var c) ? (Color32)c : new Color32(0, 0, 0, 0);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "ui_viagem_" + look };
            tex.SetPixels32(px);
            tex.Apply(false);
            _textures.Add(tex);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Image NewImage(string name, RectTransform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        Text NewText(string name, RectTransform parent, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
