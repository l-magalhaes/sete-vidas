using System;
using System.Collections;
using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Progression;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A Tobi, toupeira engenheira, na cabeceira leste da Ponte do Último Marco (F9; Docs/Narrativa/floresta-roteiro.md).
    /// <list type="bullet">
    /// <item>Ao chegar na cabeceira leste (vindo da G3), o gato vê a aurora (D-SAIDA-01, uma vez).</item>
    /// <item>Chegando perto dela, o encontro (D-TOBI-01) acontece sozinho; depois, E para falar.</item>
    /// <item>Faltam tábuas (D-TOBI-02, com a conta) ou moedas (D-TOBI-03). Com as 12 tábuas e 20 moedas: a cutscene da
    /// reconstrução (D-TOBI-04): a chave inglesa maior do que ela, a pilha de tábuas, ela cava, sai do outro lado do
    /// abismo puxando a corda, a nuvem de obra atravessa montando a ponte (a câmera vai junto), a taxa de material, o gato
    /// pisa, a ponte range... e aguenta.</item>
    /// <item>Depois, falas em rodízio (D-TOBI-05).</item>
    /// </list>
    /// A ponte nova (<see cref="RebuiltBridge"/>) libera a volta para a Fortaleza e a corda para as Raízes.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TobiNpc : MonoBehaviour
    {
        [Header("Conversas")]
        [SerializeField] DialogueDefinition meet;
        [SerializeField] DialogueDefinition meetMore;
        [Tooltip("D-TOBI-02: \"#N#\" vira a conta das tábuas que faltam")]
        [SerializeField] DialogueDefinition missingWood;
        [SerializeField] DialogueDefinition missingCoins;
        [Tooltip("D-TOBI-04, em partes: o começo, \"segurei!\", a taxa e a garantia")]
        [SerializeField] DialogueDefinition buildStart, buildCarry, buildTax, buildEnd;
        [SerializeField] DialogueDefinition[] after = new DialogueDefinition[0];

        [Header("Lugar")]
        [Tooltip("A cabeceira leste (mundo): ao entrar, a fala da aurora")]
        [SerializeField] Rect auroraArea;
        [Tooltip("O encontro acontece sozinho com o gato a esta distância (tiles)")]
        [SerializeField] float meetRadius = 8f;
        [SerializeField] TextMesh prompt;
        [SerializeField] VulpisVisual visual;

        PlayerHealth _near;
        int _afterIndex;
        bool _busy;
        Vector3 _home;
        static Sprite s_board;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_board = null; }   // Play sem recarregar o domínio

        static readonly string[] AuroraLines = { "Do outro lado. Finalmente.", "E dessa vez eu subi. Ninguém me jogou de lugar nenhum." };

        public void Configure(DialogueDefinition[] talks, DialogueDefinition[] afterTalks, Rect aurora, TextMesh promptText, VulpisVisual look)
        {
            meet = talks[0]; meetMore = talks[1]; missingWood = talks[2]; missingCoins = talks[3];
            buildStart = talks[4]; buildCarry = talks[5]; buildTax = talks[6]; buildEnd = talks[7];
            after = afterTalks; auroraArea = aurora; prompt = promptText; visual = look;
        }

        void Start()
        {
            _home = transform.position;
            SeteVidas.UI.WorldPrompt.Upgrade(prompt);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h != null) _near = h;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h != null && h == _near) _near = null;
        }

        void Update()
        {
            var data = SaveManager.Data;
            var cat = PlayerHealth.Active;
            var runner = DialogueRunner.Active;
            bool talking = runner != null && runner.IsOpen;
            if (data == null || cat == null || cat.IsDead) { if (prompt != null) prompt.gameObject.SetActive(false); return; }
            Vector2 c = cat.transform.position;
            // a aurora: chegou do outro lado
            if (!data.HasFlag(TobiRules.AuroraFlag) && auroraArea.Contains(c))
            {
                data.SetFlag(TobiRules.AuroraFlag);
                StartCoroutine(Aurora(cat.transform));
            }
            if (_busy) { if (prompt != null) prompt.gameObject.SetActive(false); return; }
            // olha para o gato
            if (visual != null && Mathf.Abs(c.x - transform.position.x) > 0.5f) visual.FaceLeft = c.x < transform.position.x;
            // o encontro acontece sozinho (ela está no caminho)
            if (!data.HasFlag(TobiRules.MetFlag) && !talking && Vector2.Distance(c, transform.position) < meetRadius)
            {
                Talk(cat);
                return;
            }
            bool near = _near != null && !_near.IsDead;
            if (prompt != null) prompt.gameObject.SetActive(near && !talking);
            if (!near || talking) return;
            var input = _near.GetComponent<PlayerInputReader>();
            if (input != null && input.isActiveAndEnabled && input.InteractPressedThisFrame) Talk(_near);
        }

        IEnumerator Aurora(Transform cat)
        {
            yield return new WaitForSeconds(0.8f);
            foreach (var line in AuroraLines)
            {
                if (cat == null) yield break;
                SpeechBubble.Show(cat, line, 2.4f);
                yield return new WaitForSeconds(2.5f);
            }
        }

        /// <summary>E perto dela (ou o encontro): a conversa certa para agora.</summary>
        public void Talk(PlayerHealth cat)
        {
            var runner = DialogueRunner.Active;
            var data = SaveManager.Data;
            if (runner == null || runner.IsOpen || data == null || _busy) return;
            var input = cat.GetComponent<PlayerInputReader>();
            switch (TobiRules.Next(data))
            {
                case TobiTalk.Meet:
                    _busy = true;
                    runner.Play(meet, input, () => runner.Play(meetMore, input, () =>
                    {
                        data.SetFlag(TobiRules.MetFlag);
                        SaveManager.Save();
                        _busy = false;
                    }));
                    break;
                case TobiTalk.MissingWood:
                    runner.Play(WithCount(missingWood, ReconstructionWood.Missing(data)), input);
                    break;
                case TobiTalk.MissingCoins:
                    runner.Play(missingCoins, input);
                    break;
                case TobiTalk.Build:
                    StartCoroutine(BuildScene(cat));
                    break;
                default:
                    if (after.Length == 0) return;
                    runner.Play(after[_afterIndex % after.Length], input);
                    _afterIndex++;
                    break;
            }
        }

        /// <summary>Uma cópia da conversa com a conta no lugar de <see cref="TobiRules.CountToken"/>.</summary>
        static DialogueDefinition WithCount(DialogueDefinition src, int missing)
        {
            if (src == null) return null;
            var copy = ScriptableObject.CreateInstance<DialogueDefinition>();
            copy.ScriptId = src.ScriptId;
            copy.Required = false;
            copy.hideFlags = HideFlags.DontSave;
            foreach (var l in src.Lines)
                copy.Lines.Add(new DialogueLine { Speaker = l.Speaker, Expression = l.Expression, Text = TobiRules.FillCount(l.Text, missing) });
            return copy;
        }

        // ------------------------------------------------------------ a reconstrução (cutscene 2)

        IEnumerator Say(DialogueDefinition d, PlayerInputReader input)
        {
            var runner = DialogueRunner.Active;
            if (runner == null || d == null) yield break;
            bool done = false;
            if (!runner.Play(d, input, () => done = true)) yield break;
            while (!done) yield return null;
        }

        void Anim(string clip, string then = null) { if (visual != null) visual.Play(clip, then); }
        float Dur(string clip) => visual != null ? visual.Duration(clip) : 0.5f;

        void ShowTobi(bool on)
        {
            foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = on;
        }

        IEnumerator BuildScene(PlayerHealth cat)
        {
            var data = SaveManager.Data;
            if (!CoinPurse.TrySpend(ReconstructionWood.BridgePrice)) yield break;
            _busy = true;
            data.SetFlag(ReconstructionWood.BridgeFlag);   // gravada já: se a cena for interrompida, a ponte fica pronta
            SaveManager.Save();
            var input = cat.GetComponent<PlayerInputReader>();
            if (input != null) input.SetLock(this, true);
            cat.Shielded = true;
            var bridge = RebuiltBridge.Active;
            if (bridge != null) bridge.BeginBuild();
            try
            {
                // "Doze tábuas e vinte moedas! Agora sim, fala a minha língua!"
                Anim("greedy");
                yield return Say(buildStart, input);
                // a chave inglesa maior do que ela
                Anim("wrench");
                yield return new WaitForSeconds(Dur("wrench") + 0.5f);
                // as 12 tábuas voam do gato e empilham nos braços dela, que cambaleia
                Anim("carry");
                for (int i = 0; i < ReconstructionWood.Total; i++)
                {
                    StartCoroutine(FlyBoard(cat.transform.position + Vector3.up, transform.position + Vector3.up * (1.2f + i * 0.08f)));
                    yield return new WaitForSeconds(0.07f);
                }
                yield return new WaitForSeconds(0.9f);
                yield return Say(buildCarry, input);   // "Segura... segura... segurei!"
                // cava e some
                Anim("dig");
                yield return new WaitForSeconds(Dur("dig"));
                ShowTobi(false);
                if (bridge != null) yield return AcrossTheGap(bridge);
                // volta pulando do chão ao lado do gato
                transform.position = _home;
                ShowTobi(true);
                Anim("emerge", "proud");
                HitSparks.Burst((Vector2)_home + Vector2.up * 0.2f, 12, new Color32(0x7a, 0x64, 0x50, 0xff), new Color32(0x4e, 0x3e, 0x30, 0xff), 2f, 5f, 0.5f, 20f, 0.6f);
                yield return new WaitForSeconds(Dur("emerge") + 0.4f);
                Anim("greedy");
                yield return Say(buildTax, input);    // "Essa aqui é a taxa de material."
                // o gato pisa na ponte nova: range alto... silêncio... e aguenta
                if (bridge != null) yield return StepOn(cat, input, bridge);
                Anim("proud");
                yield return Say(buildEnd, input);
            }
            finally
            {
                if (bridge != null) bridge.Finish();
                CinematicCamera.End();
                if (input != null) { input.ClearScriptedMove(); input.SetLock(this, false); }
                if (cat != null) cat.Shielded = false;
                transform.position = _home;
                ShowTobi(true);
                if (visual != null) visual.Release();
                _busy = false;
            }
        }

        /// <summary>
        /// A câmera atravessa o abismo: a Tobi sai do chão do lado da Fortaleza puxando a corda, e a nuvem de obra volta
        /// para o leste montando a ponte (martelo, broca, estrelinhas), com a câmera indo junto.
        /// </summary>
        IEnumerator AcrossTheGap(RebuiltBridge bridge)
        {
            if (!CinematicCamera.Begin()) yield break;
            Vector2 home = CinematicCamera.Home;
            float y = bridge.DeckY;
            Vector2 west = new Vector2(bridge.StartX - 2.5f, y);
            // vai até o outro lado
            yield return Pan(home, west + Vector2.up * 2f, 1.6f);
            transform.position = west;
            ShowTobi(true);
            if (visual != null) visual.FaceLeft = false;
            Anim("emerge", "pull");
            HitSparks.Burst(west + Vector2.up * 0.2f, 12, new Color32(0x7a, 0x64, 0x50, 0xff), new Color32(0x4e, 0x3e, 0x30, 0xff), 2f, 5f, 0.5f, 20f, 0.6f);
            yield return new WaitForSeconds(Dur("emerge") + 0.9f);
            // a nuvem de obra atravessa montando a ponte
            float x = bridge.StartX, t = 0f;
            while (x < bridge.EndX)
            {
                t += Time.deltaTime;
                x = Mathf.Min(bridge.EndX, x + TobiRules.BuildSpeed * Time.deltaTime);
                bridge.SetFront(x);
                transform.position = new Vector2(x - 0.6f, y);
                Anim(((int)(t * 2.5f)) % 2 == 0 ? "drill" : "hammer");
                if (UnityEngine.Random.value < Time.deltaTime * 40f)
                    HitSparks.One(new Vector2(x + UnityEngine.Random.Range(-1.4f, 0.6f), y + UnityEngine.Random.Range(0.2f, 2.2f)),
                        new Vector2(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(0.5f, 2.5f)), new Color(0.78f, 0.72f, 0.64f), 0.6f, 0f);
                if (UnityEngine.Random.value < Time.deltaTime * 8f)   // "plink": estrelinhas
                    HitSparks.Burst(new Vector2(x + UnityEngine.Random.Range(-1f, 1f), y + UnityEngine.Random.Range(0.8f, 2f)), 4, Color.white, new Color(1f, 0.95f, 0.6f), 2f, 4f, 0.25f, 0f, 0.3f);
                if (UnityEngine.Random.value < Time.deltaTime * 5f) CameraShake.Shake(0.8f, 0.1f);
                CinematicCamera.MoveTo(new Vector2(x, y + 2f));
                yield return null;
            }
            bridge.Finish();
            ShowTobi(false);
            // a câmera volta para o gato
            yield return Pan(CinematicCamera.Position, home, 0.9f);
            CinematicCamera.End();
        }

        static IEnumerator Pan(Vector2 from, Vector2 to, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                k = k * k * (3f - 2f * k);
                CinematicCamera.MoveTo(Vector2.Lerp(from, to, k));
                yield return null;
            }
        }

        /// <summary>O gato anda até a ponte nova e pisa: ela range alto, todo mundo congela... e aguenta.</summary>
        IEnumerator StepOn(PlayerHealth cat, PlayerInputReader input, RebuiltBridge bridge)
        {
            if (input == null) yield break;
            float target = bridge.EndX - 1.5f, t = 0f;
            float dir = Mathf.Sign(target - cat.transform.position.x);
            input.SetScriptedMove(dir * 0.6f);
            while (t < 4f && Mathf.Abs(cat.transform.position.x - target) > 0.4f && Mathf.Sign(target - cat.transform.position.x) == dir)
            {
                t += Time.deltaTime;
                yield return null;
            }
            input.ClearScriptedMove();
            bridge.Creak(1.4f);
            CameraShake.Shake(1.2f, 0.5f);
            SeteVidas.Audio.Sfx.Play("parede_racha", cat.transform.position, 0.8f, 0.6f);   // o rangido (som próprio na F10)
            yield return new WaitForSeconds(1.6f);                                        // silêncio
        }

        /// <summary>Uma tábua voando em arco (do gato para os braços da Tobi).</summary>
        IEnumerator FlyBoard(Vector2 from, Vector2 to)
        {
            var go = new GameObject("tábua voando");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Board();
            sr.sortingOrder = 6;
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float k = t / 0.45f;
                go.transform.position = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 2f;
                go.transform.rotation = Quaternion.Euler(0f, 0f, k * 360f);
                yield return null;
            }
            Destroy(go);
        }

        static Sprite Board()
        {
            if (s_board != null) return s_board;
            const int w = 10, h = 3;
            var px = new Color32[w * h];
            for (int x = 0; x < w; x++) { px[x] = new Color32(0x7a, 0x4e, 0x26, 0xff); px[w + x] = new Color32(0xb0, 0x7a, 0x44, 0xff); px[2 * w + x] = new Color32(0xd2, 0x9a, 0x5c, 0xff); }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "tabua_voando", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_board = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
            s_board.hideFlags = HideFlags.DontSave;
            return s_board;
        }
    }
}
