using System.Collections.Generic;
using System.Text;
using SeteVidas.Classes;
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
    /// Melhoria 10: o inventário (Tab, I ou Start; fecha com a mesma tecla ou Esc). Pausa o jogo e mostra quatro abas num
    /// pergaminho grande: Lion (retrato, corações, moedas, onde está e a opção das dicas), Classes (uma carta por classe:
    /// quem deu, a arma e os feitiços; as não recuperadas como "?"), Habilidades (as de locomoção, que valem em qualquer
    /// classe) e Diário (as placas de lore já lidas, para reler). Q/E ou LB/RB trocam de aba; ↑/↓ escolhem.
    /// Criado em código pelo HUD (não precisa de prefab).
    /// </summary>
    public class InventoryMenu : MonoBehaviour
    {
        const float W = 400f, H = 214f;
        static readonly string[] Tabs = { "Lion", "Classes", "Habilidades", "Diário" };
        static readonly Color Ink = new Color32(0x3a, 0x2a, 0x18, 0xff);
        static readonly Color Faint = new Color32(0x8a, 0x76, 0x58, 0xff);

        static InventoryMenu s_instance;

        RectTransform _panel;
        readonly Text[] _tabLabels = new Text[4];
        Text _title, _body, _side;
        Image _portrait;
        readonly Image[] _hearts = new Image[7];
        int _tab, _row;
        float _savedScale = 1f;
        PlayerInputReader _lockedInput;

        public static bool IsOpen => s_instance != null && s_instance._panel != null && s_instance._panel.gameObject.activeSelf;
        public int Tab => _tab;

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

        void OnDestroy() { if (s_instance == this) { if (IsOpen) Close(); s_instance = null; } }

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
            _panel.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            _panel.gameObject.SetActive(false);
            Time.timeScale = _savedScale > 0f ? _savedScale : 1f;
            if (_lockedInput != null) _lockedInput.SetLock(this, false);
            _lockedInput = null;
        }

        void Update()
        {
            if (!IsOpen)
            {
                if (TogglePressed()) Open();
                return;
            }
            if (TogglePressed() || BackPressed()) { Close(); return; }

            var k = Keyboard.current; var p = Gamepad.current;
            int dt = 0, dr = 0;
            if (k != null)
            {
                if (k.qKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) dt = -1;
                if (k.eKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) dt = 1;
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) dr = -1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) dr = 1;
            }
            if (p != null)
            {
                if (p.leftShoulder.wasPressedThisFrame || p.dpad.left.wasPressedThisFrame) dt = -1;
                if (p.rightShoulder.wasPressedThisFrame || p.dpad.right.wasPressedThisFrame) dt = 1;
                if (p.dpad.up.wasPressedThisFrame) dr = -1;
                if (p.dpad.down.wasPressedThisFrame) dr = 1;
            }
            bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) || (p != null && p.buttonSouth.wasPressedThisFrame);

            if (dt != 0) { _tab = (_tab + dt + Tabs.Length) % Tabs.Length; _row = 0; }
            if (dr != 0) _row = Mathf.Max(0, _row + dr);
            if (confirm && _tab == 0) ToggleHints();
            if (dt != 0 || dr != 0 || confirm) Refresh();
        }

        static void ToggleHints() => TutorialScroll.HintsDisabled = !TutorialScroll.HintsDisabled;

        // ------------------------------------------------------------ conteúdo

        void Refresh()
        {
            for (int i = 0; i < _tabLabels.Length; i++)
            {
                _tabLabels[i].text = i == _tab ? "[ " + Tabs[i] + " ]" : Tabs[i];
                _tabLabels[i].color = i == _tab ? Ink : Faint;
            }
            _portrait.gameObject.SetActive(_tab == 0);
            foreach (var h in _hearts) h.gameObject.SetActive(false);
            _side.text = "";
            switch (_tab)
            {
                case 0: FillLion(); break;
                case 1: FillClasses(); break;
                case 2: FillAbilities(); break;
                default: FillDiary(); break;
            }
        }

        void FillLion()
        {
            var data = SaveManager.Data;
            var health = PlayerHealth.Active;
            _title.text = "Lionel \"Lion\" — Ladino da Guilda das Nove Sombras";
            if (_portrait.sprite == null) _portrait.sprite = CatPortrait();

            if (health != null)
            {
                var states = HealthRules.Hearts(health.HeartsRecovered, health.CurrentHits);
                bool curseDone = data != null && data.HasFlag(PlayerHealth.CurseDoneFlag);
                for (int i = 0; i < _hearts.Length && i < states.Length; i++)
                {
                    bool show = states[i] != HeartState.Locked;
                    _hearts[i].gameObject.SetActive(show);
                    if (show) _hearts[i].sprite = HeartsHud.ClassSprite(HeartsHud.HeartClass(i, curseDone), states[i]);
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Moedas: " + (data != null ? data.coins : 0));
            string sanctuary = data != null && !string.IsNullOrEmpty(data.lastSanctuaryName) ? data.lastSanctuaryName : "nenhum ainda";
            sb.AppendLine("Onde: Cidadela do Sol Eterno · último santuário: " + sanctuary);
            sb.AppendLine();
            bool hintsOff = TutorialScroll.HintsDisabled;
            sb.AppendLine("Dicas do tutorial: " + (hintsOff ? "desligadas" : "ligadas") + "   (Enter / A: trocar)");
            _body.text = sb.ToString();
        }

        void FillClasses()
        {
            var manager = ClassManager.Active;
            var data = SaveManager.Data;
            int count = InventoryTexts.Classes.Length;
            _row = Mathf.Min(_row, count - 1);
            var list = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var c = (CharacterClass)i;
                bool unlocked = data != null && data.IsUnlocked(c);
                string label = unlocked ? (manager != null && manager.Definition(c) != null ? manager.Definition(c).DisplayName : InventoryTexts.Classes[i].Name) : "???";
                list.AppendLine((i == _row ? "> " : "   ") + label);
            }
            _side.text = list.ToString();
            var sel = (CharacterClass)_row;
            bool open = data != null && data.IsUnlocked(sel);
            var t = InventoryTexts.Classes[_row];
            _title.text = open ? t.Name : "???";
            _body.text = open
                ? "Quem deu: " + t.GivenBy + "\n\nArma: " + t.Weapon + "\n\nFeitiços: " + t.Spells + "\n\n\"" + t.Quote + "\""
                : "Um coração ainda apagado.\nOutro vínculo, outro caminho.";
        }

        void FillAbilities()
        {
            int count = InventoryTexts.Abilities.Length;
            _row = Mathf.Min(_row, count - 1);
            var list = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var a = InventoryTexts.Abilities[i];
                list.AppendLine((i == _row ? "> " : "   ") + (a.Unlocked ? a.Name : "???"));
            }
            _side.text = list.ToString();
            var s = InventoryTexts.Abilities[_row];
            _title.text = s.Unlocked ? s.Name : "???";
            _body.text = s.Unlocked ? s.Description : "Ainda não. Talvez um dia.";
        }

        void FillDiary()
        {
            var read = new List<LoreSign>();
            foreach (var sign in LoreSign.All) if (sign != null && sign.IsRead) read.Add(sign);
            read.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            _title.text = "Diário";
            if (read.Count == 0)
            {
                _side.text = "";
                _body.text = "Nenhuma placa lida ainda.\nAs placas se leem com " + InteractKey() + ".";
                return;
            }
            _row = Mathf.Min(_row, read.Count - 1);
            var list = new StringBuilder();
            for (int i = 0; i < read.Count; i++) list.AppendLine((i == _row ? "> " : "   ") + read[i].Title);
            _side.text = list.ToString();
            var def = read[_row].Text;
            var sb = new StringBuilder();
            if (def != null) foreach (var line in def.Lines) sb.AppendLine(line.Text).AppendLine();
            _title.text = read[_row].Title;
            _body.text = sb.ToString();
        }

        static string InteractKey() => Gamepad.current != null ? "E (ou X)" : "E";

        static Sprite CatPortrait()
        {
            foreach (var p in Resources.FindObjectsOfTypeAll<SpeakerProfile>())
                if (p != null && p.Portrait != null && (p.DisplayName.Contains("Lion") || p.DisplayName.Contains("Gato"))) return p.Portrait;
            return null;
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
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // fundo escurecido + pergaminho grande
            var root = new GameObject("Menu", typeof(RectTransform));
            _panel = (RectTransform)root.transform;
            _panel.SetParent(transform, false);
            _panel.anchorMin = Vector2.zero; _panel.anchorMax = Vector2.one; _panel.offsetMin = _panel.offsetMax = Vector2.zero;
            var dim = NewImage("Escuro", _panel, null);
            dim.color = new Color(0.043f, 0.039f, 0.063f, 0.7f);
            Stretch(dim.rectTransform);
            var paper = NewImage("Pergaminho", _panel, TutorialScroll.PaperSprite());
            paper.type = Image.Type.Sliced;
            var pr = paper.rectTransform;
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(W, H);

            for (int i = 0; i < Tabs.Length; i++)
            {
                _tabLabels[i] = NewText("Aba " + Tabs[i], pr, font, 9, TextAnchor.MiddleCenter);
                Place(_tabLabels[i].rectTransform, 24f + i * 92f, -10f, 86f, 14f);
            }
            var hint = NewText("Ajuda", pr, font, 7, TextAnchor.MiddleRight);
            hint.color = Faint;
            hint.text = "Q/E: abas · ↑/↓: escolher · Tab ou Esc: fechar";
            Place(hint.rectTransform, W - 214f, -H + 16f, 200f, 10f);

            _portrait = NewImage("Retrato", pr, null);
            _portrait.preserveAspect = true;
            Place(_portrait.rectTransform, 22f, -34f, 48f, 48f);
            for (int i = 0; i < _hearts.Length; i++)
            {
                _hearts[i] = NewImage("Coração " + (i + 1), pr, null);
                Place(_hearts[i].rectTransform, 80f + i * 18f, -50f, 16f, 16f);
            }

            _side = NewText("Lista", pr, font, 8, TextAnchor.UpperLeft);
            Place(_side.rectTransform, 22f, -34f, 110f, H - 60f);
            _title = NewText("Título", pr, font, 9, TextAnchor.UpperLeft);
            _title.fontStyle = FontStyle.Bold;
            Place(_title.rectTransform, 140f, -32f, W - 160f, 14f);
            _body = NewText("Texto", pr, font, 8, TextAnchor.UpperLeft);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_body.rectTransform, 140f, -50f, W - 160f, H - 76f);
            _panel.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            // na aba Lion, o texto fica ao lado do retrato e embaixo dos corações; nas outras, ao lado da lista
            if (!IsOpen) return;
            float x = _tab == 0 ? 80f : 140f;
            _title.rectTransform.anchoredPosition = new Vector2(x, _tab == 0 ? -32f : -32f);
            _body.rectTransform.anchoredPosition = new Vector2(x, _tab == 0 ? -64f : -50f);
            _body.rectTransform.sizeDelta = new Vector2(W - x - 20f, H - 76f);
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

        static Text NewText(string name, RectTransform parent, Font font, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = Ink;
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
            new ClassText { Name = "Ladino", GivenBy = "a Guilda das Nove Sombras (e anos de rua)",
                Weapon = "Adagas. Pelas costas dói 1,5×; sem ser visto, 2,5×. Três golpes seguidos: o último avança. No ar, ↓ + ataque: quique.",
                Spells = "nenhum", Quote = "O básico. É o que paga as contas da Luna e da Amora." },
            new ClassText { Name = "Paladino", GivenBy = "Koda, o Dourado, no vínculo",
                Weapon = "Marreta. Quebra paredes rachadas. Segure o ataque: terremoto. No ar, ↓ + ataque: queda pesada.",
                Spells = "Cura: devolve 1 coração; 3 usos, que voltam ao descansar no santuário.",
                Quote = "Pesada demais para um gato. Funciona." },
            new ClassText { Name = "Mago" }, new ClassText { Name = "Monge" }, new ClassText { Name = "Bardo" },
            new ClassText { Name = "Caçador" }, new ClassText { Name = "Druida" },
        };

        public static readonly AbilityText[] Abilities =
        {
            new AbilityText { Unlocked = true, Name = "Escalar", Description = "↑ junto da hera: sobe a parede. Espaço: pula para longe; ↑ + Espaço: sobe agarrado. A hera seca aguenta um gato. Por pouco." },
            new AbilityText { Unlocked = true, Name = "Agachar e furtividade", Description = "Agachado, o gato faz menos barulho e passa por baixo. Na sombra, ninguém vê; atrás de feno e caixotes, agachado, some de vez." },
            new AbilityText { Unlocked = true, Name = "Ataque pelas costas", Description = "Por trás: 1,5×. Por trás e sem ser visto: 2,5× (a Sentinela cai de um golpe). Esbarrar nela, nem pensar." },
            new AbilityText { Unlocked = true, Name = "Estocada para baixo", Description = "No ar, ↓ + ataque: crava a adaga embaixo e quica no inimigo, no espinho ou na serra. Dá para emendar." },
            new AbilityText { Unlocked = false, Name = "Pulo duplo" },
            new AbilityText { Unlocked = false, Name = "Dash" },
            new AbilityText { Unlocked = false, Name = "Ancoragem" },
        };
    }
}
