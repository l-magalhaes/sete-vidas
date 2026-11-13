using System.Collections.Generic;
using System.Text;
using SeteVidas.Classes;
using SeteVidas.Core;
using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Progression;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SeteVidas.UI
{
    /// <summary>
    /// O inventário (Tab, I ou Start; fecha com a mesma tecla, Esc ou B). Pausa o jogo. Spec: Docs/Design/inventario.md.
    /// Três colunas, no jeito dos metroidvanias de pixel art: à esquerda o gato animado com a classe equipada, os
    /// corações e as moedas; no meio uma grade de quadrados com ícones; à direita o nome e a descrição do slot
    /// escolhido. Abas em cima: Classes, Habilidades, Itens, Diário e Controles (Q/E ou LB/RB). Setas, WASD ou D-pad andam na grade;
    /// Enter ou A equipa a classe escolhida. Criado em código pelo HUD (não precisa de prefab).
    /// </summary>
    public class InventoryMenu : MonoBehaviour
    {
        // ------------------------------------------------------------ medidas (pixels do jogo)

        const float W = 400f, H = 214f;
        const float Margin = 8f, Top = 30f, ColumnH = 166f;
        const float LeftW = 100f, GridW = 130f, RightW = 142f, Gap = 6f;
        const float GridX = Margin + LeftW + Gap, RightX = GridX + GridW + Gap;

        [Header("Ajustes (Docs/Design/inventario.md)")]
        [SerializeField, Range(1, 3)] int previewScale = 2;
        [SerializeField, Range(4, 6)] int columns = 5;
        [SerializeField, Range(3, 7)] int rows = 6;
        [SerializeField] float slotGap = 2f;
        [Tooltip("Pulsos por segundo do cursor")]
        [SerializeField] float cursorPulse = 3f;
        [Tooltip("Tempo da entrada (s)")]
        [SerializeField] float openTime = 0.12f;

        public enum Page { Classes, Abilities, Items, Map, Diary, Controls }
        static readonly string[] PageNames = { "Classes", "Habilidades", "Itens", "Mapa", "Diário", "Controles" };
        const int PageCount = 6;

        static readonly Color Cream = new Color32(0xec, 0xdc, 0xb0, 0xff);
        static readonly Color Faint = new Color32(0x8a, 0x7a, 0x96, 0xff);
        static readonly Color Gold = new Color32(0xff, 0xe0, 0x8a, 0xff);
        static readonly Color Shade = new Color32(0x0b, 0x09, 0x10, 0xff);

        /// <summary>Uma coisa na grade.</summary>
        public class Entry
        {
            public Sprite Icon;
            public bool Locked, Equipped;
            public string Name = "", Sub = "", Body = "", Footer = "", Badge = "";
            /// <summary>Casa da grade (linha × colunas + coluna). No Diário pula as linhas de título dos capítulos.</summary>
            public int Cell;
            public Color NameColor = Cream;
            public System.Func<bool> Confirm;
            /// <summary>Botão mostrado no rodapé, antes do texto (ex.: o de confirmar, em "equipar").</summary>
            public GameAction? FooterAction;
        }

        static InventoryMenu s_instance;

        RectTransform _panel, _window;
        CanvasGroup _group;
        readonly Image[] _tabPlates = new Image[PageCount];
        readonly Text[] _tabLabels = new Text[PageCount];
        Image _keyLeft, _keyRight, _footerGlyph;
        PromptRow _help;
        Text _caption;
        Image[] _ctrlGlyphs;
        Text[] _ctrlLabels;
        Vector2 _footerPos, _footerSize;
        Image[] _slots, _slotIcons;
        Text[] _badges;
        Image _cursor;
        Image _detailIcon;
        Text _detailName, _detailSub, _detailBody, _detailFooter;
        InventoryCatPreview _cat;
        RectTransform _catRoot;
        Text _catName, _catClass, _coins, _place;
        readonly Image[] _hearts = new Image[7];
        Font _font;

        readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        readonly List<Texture2D> _textures = new List<Texture2D>();
        readonly List<Entry> _entries = new List<Entry>();
        /// <summary>Linhas da grade que são título de capítulo (Diário): linha → texto.</summary>
        readonly Dictionary<int, string> _headers = new Dictionary<int, string>();
        Text[] _headerTexts;
        Image[] _headerLines;
        readonly int[] _cursorByPage = new int[PageCount];
        Page _page;
        float _savedScale = 1f, _openedAt;
        PlayerInputReader _lockedInput;
        Vector2 _stickPrev;

        public static bool IsOpen => s_instance != null && s_instance._panel != null && s_instance._panel.gameObject.activeSelf;
        public Page CurrentPage => _page;
        public int Tab => (int)_page;
        public int Selected => _cursorByPage[(int)_page];
        public IReadOnlyList<Entry> Entries => _entries;
        public InventoryCatPreview Cat => _cat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_instance = null; // Play sem recarregar o domínio

        /// <summary>Garante o inventário na cena (o HUD chama).</summary>
        public static InventoryMenu Ensure()
        {
            if (s_instance != null) return s_instance;
            var go = new GameObject("Inventário", typeof(RectTransform));
            s_instance = go.AddComponent<InventoryMenu>();
            s_instance.Build();
            return s_instance;
        }

        void OnDestroy()
        {
            InputDevices.Changed -= OnDeviceChanged;
            if (s_instance == this) { if (IsOpen) Close(); s_instance = null; }
            foreach (var s in _sprites.Values) if (s != null) Destroy(s);
            foreach (var t in _textures) if (t != null) Destroy(t);
        }

        // ------------------------------------------------------------ abrir e fechar

        static bool TogglePressed()
        {
            var k = Keyboard.current; var p = Gamepad.current;
            return (k != null && (k.tabKey.wasPressedThisFrame || k.iKey.wasPressedThisFrame)) ||
                   (p != null && (p.startButton.wasPressedThisFrame || p.selectButton.wasPressedThisFrame));
        }

        static bool BackPressed()
        {
            var k = Keyboard.current; var p = Gamepad.current;
            return (k != null && k.escapeKey.wasPressedThisFrame) || (p != null && p.buttonEast.wasPressedThisFrame);
        }

        /// <summary>Pode abrir agora: fora de diálogo, vivo e sem outra pausa (hitstop) em andamento.</summary>
        public static bool CanOpen()
        {
            var health = PlayerHealth.Active;
            if (health == null || health.IsDead || health.IsCollapsed) return false;
            if (DialogueRunner.Active != null && DialogueRunner.Active.IsOpen) return false;
            return Time.timeScale > 0f;
        }

        public void Open()
        {
            if (IsOpen || !CanOpen()) return;
            _savedScale = Time.timeScale;
            Time.timeScale = 0f;
            var health = PlayerHealth.Active;
            _lockedInput = health != null ? health.GetComponent<PlayerInputReader>() : null;
            if (_lockedInput != null) _lockedInput.SetLock(this, true);
            EnsureCat(health);
            _panel.gameObject.SetActive(true);
            _openedAt = Time.unscaledTime;
            _mapRegionChosen = false;
            SeteVidas.Audio.Sfx.Play("ui_inv_abre");
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            _panel.gameObject.SetActive(false);
            SeteVidas.Audio.Sfx.Play("ui_inv_fecha");
            Time.timeScale = _savedScale > 0f ? _savedScale : 1f;
            if (_lockedInput != null) _lockedInput.SetLock(this, false);
            _lockedInput = null;
        }

        // ------------------------------------------------------------ controle

        void Update()
        {
            if (!IsOpen)
            {
                if (TogglePressed()) Open();
                return;
            }
            if (TogglePressed() || BackPressed()) { Close(); return; }

            var k = Keyboard.current; var p = Gamepad.current;
            int dp = 0, dx = 0, dy = 0;
            if (k != null)
            {
                if (k.qKey.wasPressedThisFrame) dp = -1;
                if (k.eKey.wasPressedThisFrame) dp = 1;
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) dx = -1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) dx = 1;
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) dy = -1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) dy = 1;
            }
            if (p != null)
            {
                if (p.leftShoulder.wasPressedThisFrame) dp = -1;
                if (p.rightShoulder.wasPressedThisFrame) dp = 1;
                if (p.dpad.left.wasPressedThisFrame) dx = -1;
                if (p.dpad.right.wasPressedThisFrame) dx = 1;
                if (p.dpad.up.wasPressedThisFrame) dy = -1;
                if (p.dpad.down.wasPressedThisFrame) dy = 1;
                // analógico: um passo por empurrão (precisa voltar ao centro para andar de novo)
                var s = p.leftStick.ReadValue();
                if (Mathf.Abs(_stickPrev.x) < 0.5f && Mathf.Abs(s.x) >= 0.5f) dx = s.x > 0 ? 1 : -1;
                if (Mathf.Abs(_stickPrev.y) < 0.5f && Mathf.Abs(s.y) >= 0.5f) dy = s.y > 0 ? -1 : 1;
                _stickPrev = s;
            }
            bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) || (p != null && p.buttonSouth.wasPressedThisFrame);

            if (dp != 0) { SetPage((Page)(((int)_page + dp + PageCount) % PageCount)); SeteVidas.Audio.Sfx.Play("ui_aba"); }
            else if (dx != 0 || dy != 0) Move(dx, dy);
            if (confirm && !Confirm() && SelectedEntry() != null && SelectedEntry().Locked) SeteVidas.Audio.Sfx.Play("ui_negado");
        }

        public void SetPage(Page page)
        {
            _page = page;
            Refresh();
        }

        public void Move(int dx, int dy)
        {
            if (_page == Page.Map) { CycleRegion(dx != 0 ? dx : dy); return; }
            if (_page == Page.Controls || _entries.Count == 0) return;   // a aba Controles só mostra a lista
            int i = (int)_page;
            var cells = new int[_entries.Count];
            for (int k = 0; k < cells.Length; k++) cells[k] = _entries[k].Cell;
            int next = InventoryGrid.MoveCells(cells, _cursorByPage[i], dx, dy, columns);
            if (next == _cursorByPage[i]) return;
            _cursorByPage[i] = next;
            ShowDetail();
            SeteVidas.Audio.Sfx.Play("ui_cursor_inv");
        }

        public void Select(int index)
        {
            _cursorByPage[(int)_page] = Mathf.Clamp(index, 0, Mathf.Max(0, _entries.Count - 1));
            ShowDetail();
        }

        /// <summary>Enter / A no slot escolhido (equipar a classe). Retorna se fez alguma coisa.</summary>
        public bool Confirm()
        {
            var e = SelectedEntry();
            if (e == null || e.Locked || e.Confirm == null) return false;
            bool done = e.Confirm();
            if (done) Refresh();
            return done;
        }

        Entry SelectedEntry()
        {
            int i = _cursorByPage[(int)_page];
            return i >= 0 && i < _entries.Count ? _entries[i] : null;
        }

        // ------------------------------------------------------------ conteúdo

        void Refresh()
        {
            _entries.Clear();
            _headers.Clear();
            switch (_page)
            {
                case Page.Classes: FillClasses(); break;
                case Page.Abilities: FillAbilities(); break;
                case Page.Items: FillItems(); break;
                case Page.Controls: break;   // a lista de comandos é desenhada à parte (RefreshControls)
                case Page.Map: break;        // o mapa é desenhado à parte (RefreshMap)
                default: FillDiary(); break;
            }
            if (_page != Page.Diary) for (int k = 0; k < _entries.Count; k++) _entries[k].Cell = k;
            int pi = (int)_page;
            _cursorByPage[pi] = Mathf.Clamp(_cursorByPage[pi], 0, Mathf.Max(0, _entries.Count - 1));

            for (int i = 0; i < PageCount; i++)
            {
                bool on = i == pi;
                _tabPlates[i].sprite = PanelSprite(on ? PanelLook.TabActive : PanelLook.Tab);
                _tabLabels[i].color = on ? Gold : Faint;
            }
            // os botões das abas e o rodapé no aparelho em uso (controle de Xbox ou teclado e mouse)
            SetGlyph(_keyLeft, InputPrompts.Glyph(GameAction.TabLeft));
            SetGlyph(_keyRight, InputPrompts.Glyph(GameAction.TabRight));
            if (_page == Page.Controls)
                _help.Set(new PromptRow.Item(GameAction.TabLeft, "abas", GameAction.TabRight), new PromptRow.Item(GameAction.Back, "fechar"));
            else if (_page == Page.Map)
                _help.Set(new PromptRow.Item(GameAction.Navigate, "região"), new PromptRow.Item(GameAction.Back, "fechar"));
            else if (_page == Page.Classes)
                _help.Set(new PromptRow.Item(GameAction.Navigate, "escolher"), new PromptRow.Item(GameAction.Confirm, "equipar"), new PromptRow.Item(GameAction.Back, "fechar"));
            else
                _help.Set(new PromptRow.Item(GameAction.Navigate, "escolher"), new PromptRow.Item(GameAction.Back, "fechar"));
            RefreshControls();
            RefreshMap();

            var byCell = new Entry[_slots.Length];
            foreach (var e in _entries) if (e.Cell >= 0 && e.Cell < byCell.Length) byCell[e.Cell] = e;
            for (int i = 0; i < _slots.Length; i++)
            {
                var e = byCell[i];
                bool header = _headers.ContainsKey(i / columns);
                _slots[i].enabled = !header && _page != Page.Controls;   // a linha do título do capítulo não tem quadrados
                _slots[i].sprite = SlotSprite(e == null ? SlotLook.Empty : e.Equipped ? SlotLook.Equipped : SlotLook.Normal);
                _slotIcons[i].enabled = !header && e != null && e.Icon != null;
                if (e != null) _slotIcons[i].sprite = e.Icon;
                _badges[i].text = !header && e != null ? e.Badge : "";
            }
            int h = 0;
            foreach (var kv in _headers)
            {
                if (h >= _headerTexts.Length) break;
                _headerTexts[h].text = kv.Value;
                _headerTexts[h].enabled = _headerLines[h].enabled = true;
                var at = _slots[kv.Key * columns].rectTransform.anchoredPosition;
                _headerTexts[h].rectTransform.anchoredPosition = new Vector2(at.x, at.y - 4f);
                _headerLines[h].rectTransform.anchoredPosition = new Vector2(at.x, at.y - 17f);
                h++;
            }
            for (; h < _headerTexts.Length; h++) _headerTexts[h].enabled = _headerLines[h].enabled = false;
            RefreshLion();
            ShowDetail();
        }

        void ShowDetail()
        {
            var e = SelectedEntry();
            _caption.text = Caption();
            _detailIcon.enabled = e != null && e.Icon != null;
            if (_page == Page.Controls)
            {
                // a página de controles: o aparelho em uso e como trocar
                bool pad = InputDevices.Current == InputDeviceKind.Gamepad;
                _detailIcon.enabled = true;
                _detailIcon.sprite = ControlsIcon();
                _detailName.text = pad ? "Controle (Xbox)" : "Teclado e mouse";
                _detailName.color = Gold;
                _detailSub.text = "o aparelho em uso agora";
                _detailBody.text = pad
                    ? "Os avisos do jogo mostram os botões do controle. Toque no teclado ou no mouse e eles passam a mostrar as teclas."
                    : "Os avisos do jogo mostram as teclas. Aperte um botão do controle e eles passam a mostrar os botões dele.";
                SetFooter("", null);
                return;
            }
            if (e == null)
            {
                _detailName.text = _detailSub.text = "";
                _detailBody.text = _page == Page.Diary ? "Nenhuma placa lida ainda.\n\nAs placas se leem com " + InteractKey() + "." : "";
                SetFooter("", null);
                return;
            }
            _detailIcon.sprite = e.Icon;
            _detailName.text = e.Name;
            _detailName.color = e.NameColor;
            _detailBody.text = InputPrompts.Format(e.Body);
            SetFooter(e.Footer, e.FooterAction);
            _detailSub.text = e.Sub;
        }

        /// <summary>Rodapé do detalhe; com <paramref name="action"/>, o botão dela aparece antes do texto ("[A] equipar").</summary>
        void SetFooter(string text, GameAction? action)
        {
            _detailFooter.text = text;
            var rt = _detailFooter.rectTransform;
            var glyph = action.HasValue && !string.IsNullOrEmpty(text) ? InputPrompts.Glyph(action.Value) : null;
            _footerGlyph.enabled = glyph != null;
            if (glyph == null)
            {
                _detailFooter.alignment = TextAnchor.LowerCenter;
                rt.anchoredPosition = _footerPos;
                rt.sizeDelta = _footerSize;
                return;
            }
            // [botão] 3 px [texto], o par centrado no rodapé
            float gw = glyph.rect.width, tw = Mathf.Ceil(_detailFooter.preferredWidth), total = gw + 3f + tw;
            float x0 = Mathf.Round(_footerPos.x + (_footerSize.x - total) * 0.5f);
            _footerGlyph.sprite = glyph;
            _footerGlyph.rectTransform.sizeDelta = new Vector2(gw, glyph.rect.height);
            _footerGlyph.rectTransform.anchoredPosition = new Vector2(x0, _footerPos.y - _footerSize.y + glyph.rect.height);
            _detailFooter.alignment = TextAnchor.LowerLeft;
            rt.anchoredPosition = new Vector2(x0 + gw + 3f, _footerPos.y);
            rt.sizeDelta = new Vector2(tw + 2f, _footerSize.y);
        }

        static void SetGlyph(Image img, Sprite glyph)
        {
            img.sprite = glyph;
            img.enabled = glyph != null;
            if (glyph != null) img.rectTransform.sizeDelta = new Vector2(glyph.rect.width, glyph.rect.height);
        }

        // ------------------------------------------------------------ controles

        /// <summary>A lista da aba Controles: o que cada botão faz, no aparelho em uso (só o que o gato já sabe fazer).</summary>
        static List<PromptRow.Item> ControlList()
        {
            var list = new List<PromptRow.Item>
            {
                new PromptRow.Item(GameAction.Move, "andar"),
                new PromptRow.Item(GameAction.Jump, "pular"),
                new PromptRow.Item(GameAction.Crouch, "agachar"),
                new PromptRow.Item(GameAction.Up, "escalar a hera"),
                new PromptRow.Item(GameAction.Attack, "atacar"),
                new PromptRow.Item(GameAction.Interact, "falar, ler, pegar"),
            };
            var data = SaveManager.Data;
            bool paladin = data != null && data.IsUnlocked(CharacterClass.Paladin);
            if (paladin)
            {
                list.Add(new PromptRow.Item(GameAction.PrevClass, "trocar de classe", GameAction.NextClass));
                list.Add(new PromptRow.Item(GameAction.Heal, "curar (segure)"));
            }
            list.Add(new PromptRow.Item(GameAction.Inventory, "inventário"));
            list.Add(new PromptRow.Item(GameAction.Back, "fechar, voltar"));
            return list;
        }

        void RefreshControls()
        {
            bool on = _page == Page.Controls;
            var list = on ? ControlList() : new List<PromptRow.Item>();
            var kind = InputDevices.Current;
            for (int i = 0; i < _ctrlGlyphs.Length; i++)
            {
                bool show = i < list.Count;
                _ctrlGlyphs[i].enabled = _ctrlLabels[i].enabled = show;
                if (!show) continue;
                var it = list[i];
                string paths = InputPrompts.PathFor(it.Action, kind);
                if (it.Also.HasValue) paths += "|" + InputPrompts.PathFor(it.Also.Value, kind);
                SetGlyph(_ctrlGlyphs[i], InputGlyphs.ForPaths(paths));
                _ctrlLabels[i].text = it.Label;
            }
            if (on) for (int i = 0; i < _slots.Length; i++) { _slotIcons[i].enabled = false; _badges[i].text = ""; }
        }

        /// <summary>Quantos comandos a aba Controles mostra agora (testes).</summary>
        public int ControlRows
        {
            get
            {
                int n = 0;
                if (_ctrlGlyphs != null) foreach (var g in _ctrlGlyphs) if (g.enabled) n++;
                return n;
            }
        }

        Sprite ControlsIcon() => InputDevices.Current == InputDeviceKind.Gamepad
            ? InputGlyphs.ForPaths("<Gamepad>/dpad") : InputGlyphs.ForPaths("<Mouse>/leftButton");

        void OnDeviceChanged() { if (IsOpen) Refresh(); }

        string Caption()
        {
            // sem "x/7": o que ainda não foi recuperado não aparece
            switch (_page)
            {
                case Page.Classes: return "Classes";
                case Page.Abilities: return "Habilidades";
                case Page.Items: return "Itens";
                case Page.Controls: return "Controles";
                case Page.Map: return "Mapa";
                default: return "Diário  " + _entries.Count + (_entries.Count == 1 ? " placa" : " placas");
            }
        }

        static readonly InventoryIcon[] ClassIcons =
        {
            InventoryIcon.Rogue, InventoryIcon.Paladin, InventoryIcon.Mage, InventoryIcon.Monk,
            InventoryIcon.Bard, InventoryIcon.Hunter, InventoryIcon.Druid,
        };

        void FillClasses()
        {
            var manager = ClassManager.Active;
            var data = SaveManager.Data;
            for (int i = 0; i < InventoryTexts.Classes.Length; i++)
            {
                var c = (CharacterClass)i;
                var t = InventoryTexts.Classes[i];
                if (data == null || !data.IsUnlocked(c)) continue;   // as que faltam ficam escondidas por enquanto
                var def = manager != null ? manager.Definition(c) : null;
                bool equipped = manager != null && manager.CurrentClass == c;
                var cls = c;
                _entries.Add(new Entry
                {
                    Icon = IconSprite(ClassIcons[i], false), Equipped = equipped,
                    Name = def != null ? def.DisplayName : t.Name,
                    NameColor = def != null ? Lighter(def.ThemeColor) : Cream,
                    Sub = t.GivenBy,
                    Body = t.Weapon + "\n\n\"" + t.Quote + "\"",
                    Footer = equipped ? "Equipada" : "equipar", FooterAction = equipped ? (GameAction?)null : GameAction.Confirm,
                    Confirm = () => manager != null && manager.TrySwitch(cls),
                });
            }
        }

        static readonly InventoryIcon[] AbilityIcons =
        {
            InventoryIcon.Climb, InventoryIcon.Sneak, InventoryIcon.Backstab, InventoryIcon.DownStab,
            InventoryIcon.DoubleJump, InventoryIcon.Dash, InventoryIcon.Anchor,
        };

        void FillAbilities()
        {
            for (int i = 0; i < InventoryTexts.Abilities.Length; i++)
            {
                var a = InventoryTexts.Abilities[i];
                if (!a.Unlocked) continue;   // as que faltam ficam escondidas por enquanto
                var icon = i < AbilityIcons.Length ? AbilityIcons[i] : InventoryIcon.TabAbilities;
                _entries.Add(new Entry { Icon = IconSprite(icon, false), Name = a.Name, Body = a.Description, Footer = "Vale em qualquer classe" });
            }

            // os pergaminhos de magia moram aqui: por enquanto, a cura do Koda
            var health = PlayerHealth.Active;
            var spell = health != null ? health.GetComponent<HealingSpell>() : null;
            if (spell != null && spell.IsUnlocked)
            {
                _entries.Add(new Entry
                {
                    Icon = ScrollSprite(spell.Charges <= 0), Name = "Pergaminho da cura", NameColor = Gold,
                    Sub = "Magia do Paladino (Koda)", Badge = spell.Charges + "/" + spell.MaxCharges, Body = InventoryTexts.HealScroll,
                    Footer = spell.Charges + " de " + spell.MaxCharges + " lacres acesos",
                });
            }
        }

        void FillItems()
        {
            // os corações já estão no painel do gato; a cura foi para Habilidades
            var data = SaveManager.Data;
            int coins = data != null ? data.coins : 0;
            _entries.Add(new Entry
            {
                Icon = IconSprite(InventoryIcon.Coin, false), Name = "Moedas", NameColor = Gold, Badge = coins.ToString(),
                Body = InventoryTexts.Coins, Footer = coins + (coins == 1 ? " moeda" : " moedas"),
            });

            // Madeira de Reconstrução: aparece a partir da primeira tábua (Floresta Silenciosa)
            int wood = SeteVidas.Progression.ReconstructionWood.Count(data);
            if (wood > 0)
            {
                bool built = SeteVidas.Progression.ReconstructionWood.BridgeBuilt(data);
                _entries.Add(new Entry
                {
                    Icon = IconSprite(InventoryIcon.Wood, false), Name = SeteVidas.Progression.ReconstructionWood.DisplayName,
                    NameColor = Gold, Badge = wood + "/" + SeteVidas.Progression.ReconstructionWood.Total,
                    Body = built ? InventoryTexts.WoodDone : InventoryTexts.Wood,
                    Footer = built ? "A ponte está de pé" : SeteVidas.Progression.ReconstructionWood.Label(data),
                });
            }

            // chaves no bolso (somem depois de abrir a porta delas)
            foreach (var key in SeteVidas.World.KeyRules.KeysInPocket(data, SeteVidas.World.KeyRules.DoorOfKey))
                _entries.Add(new Entry
                {
                    Icon = IconSprite(InventoryIcon.Key, false), Name = SeteVidas.World.KeyRules.Name(key), NameColor = Gold,
                    Body = InventoryTexts.KeyText(key), Footer = "Abre uma porta",
                });
        }

        /// <summary>
        /// Diário por capítulo: cada capítulo começa numa linha nova com o título, e as placas lidas vêm embaixo, na
        /// ordem do caminho. O capítulo atual aparece mesmo sem placas lidas.
        /// </summary>
        void FillDiary()
        {
            var byChapter = new SortedDictionary<int, List<LoreSign>>();
            int current = InventoryTexts.ChapterOf(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            byChapter[current] = new List<LoreSign>();
            foreach (var sign in LoreSign.All)
            {
                if (sign == null || !sign.IsRead) continue;
                int ch = InventoryTexts.ChapterOf(sign.gameObject.scene.name);
                if (!byChapter.TryGetValue(ch, out var list)) byChapter[ch] = list = new List<LoreSign>();
                list.Add(sign);
            }

            int row = 0;
            foreach (var kv in byChapter)
            {
                var chapter = InventoryTexts.Chapter(kv.Key);
                string title = "Capítulo " + chapter.Number + " · " + chapter.Title;
                _headers[row] = title;
                row++;
                var signs = kv.Value;
                signs.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                for (int i = 0; i < signs.Count; i++)
                {
                    var sb = new StringBuilder();
                    var def = signs[i].Text;
                    if (def != null) foreach (var line in def.Lines) sb.AppendLine(line.Text).AppendLine();
                    _entries.Add(new Entry
                    {
                        Icon = IconSprite(InventoryIcon.Page, false), Name = signs[i].Title, NameColor = Gold, Sub = title,
                        Body = sb.ToString().TrimEnd(), Cell = (row + i / columns) * columns + i % columns,
                    });
                }
                row += Mathf.Max(1, (signs.Count + columns - 1) / columns);
            }
        }

        void RefreshLion()
        {
            var data = SaveManager.Data;
            var health = PlayerHealth.Active;
            var def = ClassManager.Active != null ? ClassManager.Active.Current : null;
            _catName.text = "Lion";
            _catClass.text = def != null ? def.DisplayName : "Ladino";
            _catClass.color = def != null ? Lighter(def.ThemeColor) : Cream;
            if (_cat != null) _cat.Show(def);

            bool curseDone = data != null && data.HasFlag(PlayerHealth.CurseDoneFlag);
            var states = health != null ? HealthRules.Hearts(health.HeartsRecovered, health.CurrentHits) : new HeartState[0];
            int shown = 0;
            for (int i = 0; i < _hearts.Length; i++)
            {
                bool show = i < states.Length && states[i] != HeartState.Locked;
                _hearts[i].enabled = show;
                if (show) { _hearts[i].sprite = HeartsHud.ClassSprite(HeartsHud.HeartClass(i, curseDone), states[i]); shown++; }
            }
            // os corações ficam centralizados embaixo do gato, 12 px um do outro (2 linhas se não couber)
            const float step = 12f;
            int perRow = Mathf.Min(shown, 7);
            for (int i = 0, k = 0; i < _hearts.Length; i++)
            {
                if (!_hearts[i].enabled) continue;
                float x = LeftW * 0.5f - (perRow - 1) * step * 0.5f + k * step - 8f;
                _hearts[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(x), -112f);
                k++;
            }
            int coins = data != null ? data.coins : 0;
            _coins.text = coins.ToString();
            string sanctuary = data != null && !string.IsNullOrEmpty(data.lastSanctuaryName) ? data.lastSanctuaryName : "nenhum ainda";
            _place.text = "Cidadela do Sol Eterno\nSantuário: " + sanctuary;
        }

        static Color Lighter(Color c) => Color.Lerp(c, Color.white, 0.35f);

        static string InteractKey() => InputPrompts.Name(GameAction.Interact);

        void EnsureCat(PlayerHealth health)
        {
            if (_cat == null || _cat.HasArt || health == null) return;
            var visual = health.GetComponentInChildren<PlayerVisual>();
            if (visual != null && visual.AnimationSet != null) _cat.Build(visual.AnimationSet);
        }

        // ------------------------------------------------------------ animação da tela

        void LateUpdate()
        {
            if (!IsOpen) return;
            float t = Time.unscaledTime;
            // o gato pisca no mapa (tempo real: o jogo está pausado)
            if (_mapCat != null && _page == Page.Map) _mapCat.enabled = ((int)(t * 3f) & 1) == 0;

            // entrada: sobe 6 px e aparece
            float k = openTime > 0f ? Mathf.Clamp01((t - _openedAt) / openTime) : 1f;
            _group.alpha = k;
            _window.anchoredPosition = new Vector2(0f, Mathf.Round((1f - k) * -6f));

            // cursor: respira (1 px para fora e para dentro); o ícone escolhido sobe 1 px
            int sel = _cursorByPage[(int)_page];
            int cell = sel < _entries.Count ? Mathf.Clamp(_entries[sel].Cell, 0, _slots.Length - 1) : 0;
            bool has = sel < _entries.Count && _entries.Count > 0;
            _cursor.enabled = has;
            if (has)
            {
                var slot = _slots[cell].rectTransform;
                bool wide = Mathf.Sin(t * Mathf.PI * 2f * cursorPulse * 0.5f) > 0f;
                float size = InventoryIcons.CursorSize + (wide ? 2f : 0f);
                _cursor.rectTransform.sizeDelta = new Vector2(size, size);
                _cursor.rectTransform.anchoredPosition = slot.anchoredPosition + new Vector2(-(size - InventoryIcons.SlotSize) * 0.5f, (size - InventoryIcons.SlotSize) * 0.5f);
            }
            for (int i = 0; i < _slotIcons.Length; i++)
                _slotIcons[i].rectTransform.anchoredPosition = new Vector2(3f, has && i == cell ? -2f : -3f);
        }

        // ------------------------------------------------------------ sprites

        Sprite IconSprite(InventoryIcon icon, bool locked)
        {
            string key = "icon_" + icon + (locked ? "_locked" : "");
            if (_sprites.TryGetValue(key, out var s) && s != null) return s;
            return _sprites[key] = Make(locked ? InventoryIcons.Silhouette(icon) : InventoryIcons.Draw(icon), key, 16f, Vector4.zero);
        }

        Sprite ScrollSprite(bool faded)
        {
            string key = "scroll" + (faded ? "_faded" : "");
            if (_sprites.TryGetValue(key, out var s) && s != null) return s;
            return _sprites[key] = Make(HealScrollPixels.DrawScroll(faded), key, 16f, Vector4.zero);
        }

        Sprite SlotSprite(SlotLook look)
        {
            string key = "slot_" + look;
            if (_sprites.TryGetValue(key, out var s) && s != null) return s;
            return _sprites[key] = Make(InventoryIcons.Slot(look), key, 16f, Vector4.zero);
        }

        Sprite PanelSprite(PanelLook look)
        {
            string key = "panel_" + look;
            if (_sprites.TryGetValue(key, out var s) && s != null) return s;
            float b = look == PanelLook.Main ? InventoryIcons.PanelBorder : look == PanelLook.Inset ? InventoryIcons.InsetBorder : InventoryIcons.TabBorder;
            // 9-slice: 100 px por unidade, igual ao Canvas (1 pixel do desenho = 1 pixel do jogo)
            return _sprites[key] = Make(InventoryIcons.Panel(look), key, 100f, new Vector4(b, b, b, b));
        }

        Sprite Make(string[,] grid, string name, float ppu, Vector4 border)
        {
            int h = grid.GetLength(0), w = grid.GetLength(1);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    string hex = grid[y, x];
                    // a grade vem de cima para baixo; a textura, de baixo para cima
                    px[(h - 1 - y) * w + x] = hex != "" && ColorUtility.TryParseHtmlString(hex, out var c) ? (Color32)c : new Color32(0, 0, 0, 0);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "ui_inventario_" + name };
            tex.SetPixels32(px);
            tex.Apply(false);
            _textures.Add(tex);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border);
            sprite.name = "ui_inventario_" + name;
            return sprite;
        }

        // ------------------------------------------------------------ montagem

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95;
            canvas.pixelPerfect = true;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 270f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = new GameObject("Menu", typeof(RectTransform));
            _panel = (RectTransform)root.transform;
            _panel.SetParent(transform, false);
            Stretch(_panel);
            _group = root.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            var dim = NewImage("Escuro", _panel, null);
            dim.color = new Color(0.03f, 0.02f, 0.05f, 0.75f);
            Stretch(dim.rectTransform);

            // janela principal: moldura escura com filete dourado
            var frame = NewImage("Janela", _panel, PanelSprite(PanelLook.Main));
            frame.type = Image.Type.Sliced;
            _window = frame.rectTransform;
            _window.anchorMin = _window.anchorMax = _window.pivot = new Vector2(0.5f, 0.5f);
            _window.sizeDelta = new Vector2(W, H);

            BuildTabs();
            BuildLion();
            BuildGrid();
            BuildDetail();
            BuildMap();   // por cima da grade e do detalhe; só aparece na aba Mapa

            // rodapé: [botão] escolher   [botão] equipar   [botão] fechar, no aparelho em uso
            _help = PromptRow.Create(_window, "Ajuda", _font, 7, Faint, new Vector2(0f, -(Top + ColumnH + 2f)), W);
            InputDevices.Changed += OnDeviceChanged;
            _panel.gameObject.SetActive(false);
        }

        void BuildTabs()
        {
            const float tabW = 59f, tabH = 18f, gap = 3f;   // 6 abas (a 4ª: Mapa; a 6ª: Controles)
            float x0 = (W - (PageCount * tabW + (PageCount - 1) * gap)) * 0.5f;
            for (int i = 0; i < PageCount; i++)
            {
                var plate = NewImage("Aba " + PageNames[i], _window, PanelSprite(PanelLook.Tab));
                plate.type = Image.Type.Sliced;
                Place(plate.rectTransform, x0 + i * (tabW + gap), -7f, tabW, tabH);
                _tabPlates[i] = plate;
                var icon = NewImage("Ícone", plate.rectTransform, TabIcon(i));
                if (i == (int)Page.Controls) Place(icon.rectTransform, 6f, -3f, 11f, 11f);   // o direcional, no tamanho dele
                else Place(icon.rectTransform, 4f, -1f, 16f, 16f);
                _tabLabels[i] = NewText("Nome", plate.rectTransform, 7, TextAnchor.MiddleLeft, Faint);
                Place(_tabLabels[i].rectTransform, 22f, 0f, tabW - 24f, tabH);
                _tabLabels[i].text = PageNames[i];
            }
            // os botões das abas (Q/E ou LB/RB, em pixel art), encostados nas pontas da fileira
            _keyLeft = NewImage("Botão da aba anterior", _window, null);
            _keyLeft.rectTransform.anchorMin = _keyLeft.rectTransform.anchorMax = new Vector2(0f, 1f);
            _keyLeft.rectTransform.pivot = new Vector2(1f, 1f);
            _keyLeft.rectTransform.anchoredPosition = new Vector2(x0 - 3f, -10f);
            _keyRight = NewImage("Botão da próxima aba", _window, null);
            _keyRight.rectTransform.anchorMin = _keyRight.rectTransform.anchorMax = _keyRight.rectTransform.pivot = new Vector2(0f, 1f);
            _keyRight.rectTransform.anchoredPosition = new Vector2(x0 + PageCount * tabW + (PageCount - 1) * gap + 3f, -10f);
        }

        Sprite TabIcon(int page)
        {
            switch (page)
            {
                case (int)Page.Classes: return HeartsHud.ClassSprite(0, HeartState.Full);
                case (int)Page.Abilities: return IconSprite(InventoryIcon.TabAbilities, false);
                case (int)Page.Items: return IconSprite(InventoryIcon.TabItems, false);
                case (int)Page.Map: return IconSprite(InventoryIcon.Map, false);
                case (int)Page.Diary: return IconSprite(InventoryIcon.TabDiary, false);
                default: return InputGlyphs.ForPaths("<Gamepad>/dpad");
            }
        }

        Text KeyCap(string name, float x)
        {
            var cap = NewImage(name, _window, TutorialScroll.KeySprite());
            cap.type = Image.Type.Sliced;
            Place(cap.rectTransform, x, -9f, 16f, 13f);
            var t = NewText("Letra", cap.rectTransform, 7, TextAnchor.MiddleCenter, new Color32(0x24, 0x1c, 0x14, 0xff));
            Stretch(t.rectTransform);
            return t;
        }

        void BuildLion()
        {
            var inset = Inset("Gato", Margin, LeftW);
            _catName = NewText("Nome", inset, 9, TextAnchor.UpperCenter, Cream);
            _catName.fontStyle = FontStyle.Bold;
            Place(_catName.rectTransform, 0f, -5f, LeftW, 12f);
            _catClass = NewText("Classe", inset, 8, TextAnchor.UpperCenter, Cream);
            Place(_catClass.rectTransform, 0f, -16f, LeftW, 10f);

            // chão de pedra com sombra embaixo do gato
            var floor = NewImage("Chão", inset, FloorSprite());
            Place(floor.rectTransform, (LeftW - 64f) * 0.5f, -89f, 64f, 8f);

            var catGo = new GameObject("Gato animado", typeof(RectTransform));
            _catRoot = (RectTransform)catGo.transform;
            _catRoot.SetParent(inset, false);
            _catRoot.anchorMin = _catRoot.anchorMax = new Vector2(0f, 1f);
            _catRoot.sizeDelta = Vector2.zero;
            _catRoot.anchoredPosition = new Vector2(LeftW * 0.5f, -92f);   // pés do gato
            _catRoot.localScale = new Vector3(previewScale, previewScale, 1f);
            _cat = catGo.AddComponent<InventoryCatPreview>();

            for (int i = 0; i < _hearts.Length; i++)
            {
                _hearts[i] = NewImage("Coração " + (i + 1), inset, null);
                Place(_hearts[i].rectTransform, 0f, -112f, 16f, 16f);
            }
            var coin = NewImage("Moeda", inset, IconSprite(InventoryIcon.Coin, false));
            Place(coin.rectTransform, 30f, -129f, 16f, 16f);
            _coins = NewText("Moedas", inset, 9, TextAnchor.MiddleLeft, Gold);
            Place(_coins.rectTransform, 48f, -129f, 40f, 16f);
            _place = NewText("Onde", inset, 7, TextAnchor.UpperCenter, Faint);
            _place.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_place.rectTransform, 2f, -147f, LeftW - 4f, 18f);
        }

        void BuildGrid()
        {
            var inset = Inset("Grade", GridX, GridW);
            _caption = NewText("Título da aba", inset, 8, TextAnchor.UpperCenter, Faint);
            Place(_caption.rectTransform, 0f, -4f, GridW, 10f);

            float s = InventoryIcons.SlotSize, step = s + slotGap;
            float gx = Mathf.Round((GridW - (columns * step - slotGap)) * 0.5f), gy = -16f;
            int n = columns * rows;
            _slots = new Image[n];
            _slotIcons = new Image[n];
            _badges = new Text[n];
            for (int i = 0; i < n; i++)
            {
                var slot = NewImage("Slot " + (i + 1), inset, SlotSprite(SlotLook.Empty));
                Place(slot.rectTransform, gx + (i % columns) * step, gy - (i / columns) * step, s, s);
                _slots[i] = slot;
                _slotIcons[i] = NewImage("Ícone", slot.rectTransform, null);
                Place(_slotIcons[i].rectTransform, 3f, -3f, 16f, 16f);
                _slotIcons[i].enabled = false;
                _badges[i] = NewText("Quantidade", slot.rectTransform, 7, TextAnchor.LowerRight, Cream);
                Place(_badges[i].rectTransform, 0f, 1f, s - 1f, s);
                var o = _badges[i].gameObject.AddComponent<Outline>();
                o.effectColor = Shade;
                o.effectDistance = new Vector2(1f, -1f);
            }
            // títulos dos capítulos do Diário: ocupam uma linha da grade, com um filete embaixo
            float rowW = columns * step - slotGap;
            _headerTexts = new Text[3];
            _headerLines = new Image[3];
            for (int i = 0; i < _headerTexts.Length; i++)
            {
                _headerTexts[i] = NewText("Capítulo " + (i + 1), inset, 8, TextAnchor.UpperLeft, Gold);
                Place(_headerTexts[i].rectTransform, gx, gy, rowW, 12f);
                _headerTexts[i].enabled = false;
                _headerLines[i] = NewImage("Filete do capítulo " + (i + 1), inset, null);
                _headerLines[i].color = new Color32(0xa8, 0x84, 0x4a, 0xff);
                Place(_headerLines[i].rectTransform, gx, gy, rowW, 1f);
                _headerLines[i].enabled = false;
            }
            // aba Controles: uma linha por comando ([botão] o que faz), no lugar da grade
            _ctrlGlyphs = new Image[12];
            _ctrlLabels = new Text[12];
            for (int i = 0; i < _ctrlGlyphs.Length; i++)
            {
                float y = -18f - i * 13f;
                _ctrlGlyphs[i] = NewImage("Comando " + (i + 1), inset, null);
                _ctrlGlyphs[i].rectTransform.anchorMin = _ctrlGlyphs[i].rectTransform.anchorMax = new Vector2(0f, 1f);
                _ctrlGlyphs[i].rectTransform.pivot = new Vector2(1f, 1f);
                _ctrlGlyphs[i].rectTransform.anchoredPosition = new Vector2(52f, y);   // os botões alinhados à direita da coluna
                _ctrlGlyphs[i].enabled = false;
                _ctrlLabels[i] = NewText("O que faz", inset, 7, TextAnchor.MiddleLeft, Cream);
                Place(_ctrlLabels[i].rectTransform, 56f, y, GridW - 58f, 11f);
                _ctrlLabels[i].enabled = false;
            }
            _cursor = NewImage("Cursor", inset, Make(InventoryIcons.Cursor(), "cursor", 16f, Vector4.zero));
            Place(_cursor.rectTransform, 0f, 0f, InventoryIcons.CursorSize, InventoryIcons.CursorSize);
        }

        void BuildDetail()
        {
            var inset = Inset("Detalhe", RightX, RightW);
            // o ícone em 2×, num quadrado de destaque
            // a moldura do slot em 9-slice (esticada sem borrar)
            var big = Make(InventoryIcons.Slot(SlotLook.Normal), "slot_grande", 100f, new Vector4(4f, 4f, 4f, 4f));
            var frame = NewImage("Moldura do ícone", inset, big);
            frame.type = Image.Type.Sliced;
            Place(frame.rectTransform, (RightW - 40f) * 0.5f, -6f, 40f, 40f);
            _detailIcon = NewImage("Ícone grande", inset, null);
            Place(_detailIcon.rectTransform, (RightW - 32f) * 0.5f, -10f, 32f, 32f);
            _detailName = NewText("Nome", inset, 9, TextAnchor.UpperCenter, Cream);
            _detailName.fontStyle = FontStyle.Bold;
            Place(_detailName.rectTransform, 4f, -48f, RightW - 8f, 12f);
            _detailSub = NewText("De onde veio", inset, 7, TextAnchor.UpperCenter, Faint);
            _detailSub.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_detailSub.rectTransform, 4f, -59f, RightW - 8f, 10f);
            var line = NewImage("Divisória", inset, null);
            line.color = new Color32(0x3a, 0x30, 0x46, 0xff);
            Place(line.rectTransform, 10f, -70f, RightW - 20f, 1f);
            _detailBody = NewText("Descrição", inset, 7, TextAnchor.UpperLeft, Cream);
            _detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailBody.lineSpacing = 0.95f;
            Place(_detailBody.rectTransform, 7f, -74f, RightW - 14f, ColumnH - 74f - 14f);
            _detailFooter = NewText("Rodapé", inset, 7, TextAnchor.LowerCenter, Gold);
            Place(_detailFooter.rectTransform, 4f, -(ColumnH - 13f), RightW - 8f, 10f);
            _footerPos = _detailFooter.rectTransform.anchoredPosition;
            _footerSize = _detailFooter.rectTransform.sizeDelta;
            _footerGlyph = NewImage("Botão do rodapé", inset, null);
            Place(_footerGlyph.rectTransform, 0f, 0f, 11f, 11f);
            _footerGlyph.enabled = false;
        }

        RectTransform Inset(string name, float x, float w)
        {
            var img = NewImage(name, _window, PanelSprite(PanelLook.Inset));
            img.type = Image.Type.Sliced;
            Place(img.rectTransform, x, -Top, w, ColumnH);
            return img.rectTransform;
        }

        Sprite FloorSprite()
        {
            // elipse de sombra 64×8 com a pedra clara na borda de cima
            const int w = 64, h = 8;
            var grid = new string[h, w];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f - w * 0.5f) / (w * 0.5f), ny = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                    float d = nx * nx + ny * ny;
                    grid[y, x] = d > 1f ? "" : d > 0.75f ? (y < h / 2 ? "#3a3046" : "#140f18") : d > 0.3f ? "#100c14" : "#0b0910";
                }
            return Make(grid, "chao", 16f, Vector4.zero);
        }

        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
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

        // ------------------------------------------------------------ aba Mapa

        static readonly Color MapRoomFill = new Color32(0x2e, 0x48, 0x34, 0xff);   // salas vistas: verde da Floresta
        static readonly Color MapRoomEdge = new Color32(0x6c, 0x86, 0x60, 0xff);
        static readonly Color MapSanctuary = new Color32(0xff, 0xd3, 0x4d, 0xff);   // âmbar: lugar seguro
        static readonly Color MapSanctuaryOff = new Color32(0x8a, 0x62, 0x1e, 0xff);
        static readonly Color MapChest = new Color32(0xc4, 0x98, 0x60, 0xff);
        static readonly Color MapChestOpen = new Color32(0x52, 0x46, 0x40, 0xff);
        static readonly Color MapCat = new Color32(0xff, 0xf4, 0xdc, 0xff);

        RectTransform _mapRoot, _mapArea;
        Text _mapTitle, _mapLegend;
        Sprite _white;
        readonly List<Image> _mapPool = new List<Image>();
        int _mapUsed;
        int _mapRegion;
        Image _mapCat;

        const float MapW = GridW + Gap + RightW, MapAreaH = ColumnH - 34f;

        void BuildMap()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "ui_mapa_branco" };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false);
            _textures.Add(tex);
            _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);

            _mapRoot = Inset("Mapa", GridX, MapW);
            _mapTitle = NewText("Região", _mapRoot, 8, TextAnchor.UpperCenter, Gold);
            Place(_mapTitle.rectTransform, 0f, -4f, MapW, 11f);
            var area = new GameObject("Área", typeof(RectTransform));
            area.transform.SetParent(_mapRoot, false);
            _mapArea = (RectTransform)area.transform;
            Place(_mapArea, 4f, -17f, MapW - 8f, MapAreaH);
            _mapLegend = NewText("Legenda", _mapRoot, 7, TextAnchor.UpperCenter, Faint);
            Place(_mapLegend.rectTransform, 0f, -(ColumnH - 14f), MapW, 10f);
            _mapLegend.text = "santuário · baú · baú aberto · você";
            _mapRoot.gameObject.SetActive(false);
        }

        void CycleRegion(int dir)
        {
            var regions = SeteVidas.World.MapRules.Regions(SaveManager.Data);
            if (dir == 0 || regions.Count < 2) return;
            _mapRegion = (_mapRegion + (dir > 0 ? 1 : -1) + regions.Count) % regions.Count;
            _mapRegionChosen = true;
            SeteVidas.Audio.Sfx.Play("ui_cursor_inv");
            RefreshMap();
        }

        Image MapRect(Vector2 bottomLeft, Vector2 size, Color color)
        {
            Image img;
            if (_mapUsed < _mapPool.Count) img = _mapPool[_mapUsed];
            else
            {
                img = NewImage("Mapa " + _mapUsed, _mapArea, _white);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = Vector2.zero;
                _mapPool.Add(img);
            }
            _mapUsed++;
            img.enabled = true;
            img.color = color;
            img.rectTransform.anchoredPosition = new Vector2(Mathf.Round(bottomLeft.x), Mathf.Round(bottomLeft.y));
            img.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, Mathf.Round(size.x)), Mathf.Max(1f, Mathf.Round(size.y)));
            return img;
        }

        void RefreshMap()
        {
            if (_mapRoot == null) return;
            bool on = _page == Page.Map;
            _mapRoot.gameObject.SetActive(on);
            if (!on) return;

            _mapUsed = 0;
            _mapCat = null;
            var data = SaveManager.Data;
            var regions = SeteVidas.World.MapRules.Regions(data);
            bool hasCat = SeteVidas.World.MapRoom.TryPlayerOnMap(out string catRegion, out Vector2 catPoint);
            if (regions.Count == 0)
            {
                _mapTitle.text = "Mapa";
                foreach (var i in _mapPool) i.enabled = false;
                _mapLegend.text = "Nenhuma sala vista ainda. As salas aparecem quando você passa por elas.";
                return;
            }
            // abre na região onde o gato está
            if (_mapRegion >= regions.Count) _mapRegion = 0;
            if (!_mapRegionChosen && hasCat) { int at = regions.IndexOf(catRegion); if (at >= 0) _mapRegion = at; }
            string region = regions[_mapRegion];
            _mapTitle.text = regions.Count > 1 ? "◄  " + region + "  ►" : region;
            _mapLegend.text = "santuário · baú · baú aberto · você";

            var rooms = SeteVidas.World.MapRules.RoomsIn(data, region);
            var world = SeteVidas.World.MapRules.Bounds(rooms);
            var panel = _mapArea.sizeDelta;
            float s = SeteVidas.World.MapRules.FitScale(world, panel);
            Vector2 P(float x, float y) => SeteVidas.World.MapRules.ToPanel(new Vector2(x, y), world, panel, s);

            foreach (var r in rooms)
            {
                var a = P(r.x, r.y);
                var size = new Vector2(r.w, r.h) * s;
                MapRect(a, size, MapRoomEdge);                                            // borda clara
                MapRect(a + Vector2.one, size - 2f * Vector2.one, MapRoomFill);          // miolo
            }
            foreach (var r in rooms)
                foreach (var m in r.markers)
                {
                    var p = P(m.x, m.y);
                    if (m.kind == SeteVidas.Save.MapMarker.Sanctuary)
                        MapRect(p - new Vector2(2f, 0f), new Vector2(4f, 4f),
                            SeteVidas.World.MapRules.SanctuaryLit(data, r.scene, m.id) ? MapSanctuary : MapSanctuaryOff);
                    else if (m.kind == SeteVidas.Save.MapMarker.Chest)
                        MapRect(p - new Vector2(1.5f, 0f), new Vector2(3f, 3f),
                            SeteVidas.World.MapRules.ChestOpen(data, m.id) ? MapChestOpen : MapChest);
                }
            if (hasCat && catRegion == region) _mapCat = MapRect(P(catPoint.x, catPoint.y) - new Vector2(1.5f, 0f), new Vector2(3f, 3f), MapCat);
            for (int i = _mapUsed; i < _mapPool.Count; i++) _mapPool[i].enabled = false;
        }

        bool _mapRegionChosen; // trocou de região com ◄ ►: vale até fechar o inventário (ao abrir, volta à região do gato)

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
    }

    /// <summary>Textos do inventário (na voz do gato, irônico). [PROPOSTA] de narrativa: revisar com a skill de Narrativa.</summary>
    public static class InventoryTexts
    {
        public struct ClassText { public string Name, GivenBy, Weapon, Spells, Quote; }
        public struct AbilityText { public string Name, Description; public bool Unlocked; }

        public static readonly ClassText[] Classes =
        {
            new ClassText { Name = "Ladino", GivenBy = "A Guilda das Nove Sombras (e a rua)",
                Weapon = "Adagas. Pelas costas: 1,5×; sem ser visto: 2,5×. O 3º golpe avança. No ar, {agachar} + {atacar}: quique.",
                Spells = "nenhum", Quote = "O básico. É o que paga as contas da Luna e da Amora." },
            new ClassText { Name = "Paladino", GivenBy = "Vínculo com Koda, o Dourado",
                Weapon = "Marreta. Quebra paredes rachadas. Segure {atacar}: terremoto. No ar, {agachar} + {atacar}: queda pesada.",
                Spells = "Cura: devolve 1 coração; 3 usos, que voltam no santuário.",
                Quote = "Pesada demais para um gato. Funciona." },
            new ClassText { Name = "Mago" }, new ClassText { Name = "Monge" }, new ClassText { Name = "Bardo" },
            new ClassText { Name = "Caçador" }, new ClassText { Name = "Druida" },
        };

        public static readonly AbilityText[] Abilities =
        {
            new AbilityText { Unlocked = true, Name = "Escalar", Description = "{subir} junto da hera: sobe a parede. {pular}: pula para longe; {subir} + {pular}: sobe agarrado. A hera seca aguenta um gato. Por pouco." },
            new AbilityText { Unlocked = true, Name = "Agachar e furtividade", Description = "Agachado, o gato faz menos barulho e passa por baixo. Na sombra, ninguém vê; atrás de feno e caixotes, agachado, some de vez." },
            new AbilityText { Unlocked = true, Name = "Ataque pelas costas", Description = "Por trás: 1,5×. Por trás e sem ser visto: 2,5× (a Sentinela cai de um golpe). Esbarrar nela, nem pensar." },
            new AbilityText { Unlocked = true, Name = "Estocada para baixo", Description = "No ar, {agachar} + {atacar}: crava a adaga embaixo e quica no inimigo, no espinho ou na serra. Dá para emendar." },
            new AbilityText { Unlocked = false, Name = "Pulo duplo" },
            new AbilityText { Unlocked = false, Name = "Dash" },
            new AbilityText { Unlocked = false, Name = "Ancoragem" },
        };

        public const string Coins = "Moedas da Cidadela, com a pata cunhada. Cada uma vai para a Luna e a Amora. Quase todas.";
        public const string Wood = "Tábuas firmes, secas, de lei. Escondidas em baús pela floresta. A Tobi reconstrói a ponte com doze delas. E vinte moedas.";
        public const string WoodDone = "Viraram ponte. Menos uma, que a Tobi guardou no bolso. Taxa de material.";
        public static string KeyText(string key) => key == "cabana"
            ? "Chave de ferro com uma pena de flecha amarrada. Abre a cabana no alto da árvore. Ninguém deixa uma chave assim à toa."
            : "Uma chave. Para alguma porta por aí.";
        public const string HealScroll = "A magia do Koda: segure {curar} para devolver 1 coração. Os lacres voltam a acender no santuário.";

        public struct ChapterText { public int Number; public string Title, Scene; }

        /// <summary>Capítulos do Diário (por cena). Cena terminada em "_" vale como prefixo (Forest_A_Roots, Forest_B_Thorns...).</summary>
        public static readonly ChapterText[] Chapters =
        {
            new ChapterText { Number = 1, Title = "A Cidadela", Scene = "Fortress_MVP" },
            new ChapterText { Number = 2, Title = "A Floresta Silenciosa", Scene = "Forest_" }, // todas as cenas Forest_*
        };

        /// <summary>O capítulo de uma cena (cena desconhecida, como a área de testes: o primeiro).</summary>
        public static int ChapterOf(string scene)
        {
            if (scene == null) return Chapters[0].Number;
            foreach (var c in Chapters)
                if (c.Scene == scene || (c.Scene.EndsWith("_") && scene.StartsWith(c.Scene, System.StringComparison.Ordinal))) return c.Number;
            return Chapters[0].Number;
        }

        public static ChapterText Chapter(int number)
        {
            foreach (var c in Chapters) if (c.Number == number) return c;
            return new ChapterText { Number = number, Title = "" };
        }
    }
}
