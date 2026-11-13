using System;
using System.Collections;
using System.Collections.Generic;
using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SeteVidas.World
{
    /// <summary>
    /// Sequência roteirizada (F1 da Floresta): colapso da ponte, entrada da Vulpis, morte da Isyathy, reconstrução da
    /// ponte. Monte com os métodos encadeados e toque com <see cref="Play"/>: o gato fica sem controle e sem dano até
    /// o fim. Com <see cref="Skippable"/>, depois de vista uma vez (flag), dá para pular com Esc/B/Start.
    /// <code>
    /// new Cutscene("cena_ponte").Fade(1f, 0.3f).Do(() => ponte.Quebrar()).Wait(0.5f).Say(fala).Fade(0f, 0.4f).Play();
    /// </code>
    /// </summary>
    public class Cutscene
    {
        readonly List<Func<IEnumerator>> _steps = new List<Func<IEnumerator>>();
        readonly List<Action> _onSkip = new List<Action>();

        /// <summary>Id da cena: vira a flag "cena_vista_&lt;id&gt;" quando termina.</summary>
        public string Id { get; }
        /// <summary>Pode pular depois de vista uma vez.</summary>
        public bool Skippable { get; private set; }

        public Cutscene(string id) { Id = id ?? ""; }

        public const string SeenPrefix = "cena_vista_";
        public string SeenFlag => SeenPrefix + Id;

        /// <summary>Número de passos (para testes).</summary>
        public int StepCount => _steps.Count;

        public Cutscene AllowSkipAfterFirstTime() { Skippable = true; return this; }

        /// <summary>Espera (tempo do jogo).</summary>
        public Cutscene Wait(float seconds) { _steps.Add(() => WaitRoutine(seconds)); return this; }

        /// <summary>Executa algo na hora (mudar sprite, tocar som, ligar objeto).</summary>
        public Cutscene Do(Action action) { _steps.Add(() => Once(action)); return this; }

        /// <summary>Executa algo que leva tempo (uma corrotina sua).</summary>
        public Cutscene Run(Func<IEnumerator> routine) { if (routine != null) _steps.Add(routine); return this; }

        /// <summary>Escurece ou clareia a tela.</summary>
        public Cutscene Fade(float alpha, float seconds) { _steps.Add(() => FadeRoutine(alpha, seconds)); return this; }

        /// <summary>Toca um diálogo e espera ele fechar.</summary>
        public Cutscene Say(DialogueDefinition dialogue) { _steps.Add(() => SayRoutine(dialogue)); return this; }

        /// <summary>Balão curto sobre alguém, sem parar a cena.</summary>
        public Cutscene Bark(Transform who, string text, float seconds = 2f) { _steps.Add(() => Once(() => SpeechBubble.Show(who, text, seconds))); return this; }

        /// <summary>Anda um objeto até <paramref name="to"/> (mundo) em <paramref name="speed"/> tiles/s.</summary>
        public Cutscene MoveTo(Transform who, Vector2 to, float speed) { _steps.Add(() => MoveRoutine(who, to, speed)); return this; }

        /// <summary>Som (Sfx).</summary>
        public Cutscene Sfx(string id, float pitch = 1f) { _steps.Add(() => Once(() => SeteVidas.Audio.Sfx.Play(id, null, 1f, pitch))); return this; }

        /// <summary>O que fazer se o jogador pular (deixar o mundo no estado final da cena).</summary>
        public Cutscene OnSkip(Action action) { if (action != null) _onSkip.Add(action); return this; }

        /// <summary>Toca a cena. <paramref name="onDone"/> roda no fim (também se pulada).</summary>
        public void Play(Action onDone = null) => CutsceneRunner.Play(this, onDone);

        internal IReadOnlyList<Func<IEnumerator>> Steps => _steps;
        internal IReadOnlyList<Action> SkipActions => _onSkip;

        static IEnumerator Once(Action a) { a?.Invoke(); yield break; }

        static IEnumerator WaitRoutine(float s) { if (s > 0f) yield return new WaitForSeconds(s); }

        static IEnumerator FadeRoutine(float alpha, float s)
        {
            var f = ScreenFader.Active;
            if (f != null) yield return f.FadeTo(alpha, s);
            else if (s > 0f) yield return new WaitForSeconds(s);
        }

        static IEnumerator SayRoutine(DialogueDefinition d)
        {
            var runner = DialogueRunner.Active;
            if (d == null || runner == null) yield break;
            bool done = false;
            var input = PlayerHealth.Active != null ? PlayerHealth.Active.GetComponent<PlayerInputReader>() : null;
            if (!runner.Play(d, input, () => done = true)) yield break;
            while (!done && runner.IsOpen) yield return null;
        }

        static IEnumerator MoveRoutine(Transform who, Vector2 to, float speed)
        {
            if (who == null) yield break;
            while (who != null && Vector2.Distance(who.position, to) > 0.01f)
            {
                who.position = Vector2.MoveTowards(who.position, to, Mathf.Max(0.1f, speed) * Time.deltaTime);
                yield return null;
            }
        }
    }

    /// <summary>Toca as <see cref="Cutscene"/>: trava o gato, roda os passos, deixa pular e devolve o controle.</summary>
    public class CutsceneRunner : MonoBehaviour
    {
        static CutsceneRunner s_instance;
        static Cutscene s_current;

        /// <summary>Uma cena está tocando.</summary>
        public static bool IsPlaying => s_current != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_instance = null; s_current = null; } // Play sem recarregar o domínio

        bool _skip;

        public static void Play(Cutscene cutscene, Action onDone)
        {
            if (cutscene == null || s_current != null) return;
            if (s_instance == null)
            {
                var go = new GameObject("[Cutscene]");
                s_instance = go.AddComponent<CutsceneRunner>(); // fica na cena: troca de cena interrompe a cena
            }
            s_current = cutscene;
            s_instance.StartCoroutine(s_instance.Run(cutscene, onDone));
        }

        IEnumerator Run(Cutscene c, Action onDone)
        {
            _skip = false;
            var health = PlayerHealth.Active;
            var input = health != null ? health.GetComponent<PlayerInputReader>() : null;
            if (input != null) input.SetLock(this, true);
            if (health != null) health.Shielded = true;
            bool canSkip = c.Skippable && SeteVidas.Save.SaveManager.Data != null && SeteVidas.Save.SaveManager.Data.HasFlag(c.SeenFlag);

            foreach (var step in c.Steps)
            {
                if (_skip) break;
                var routine = step();
                while (routine != null)
                {
                    if (canSkip && SkipPressed()) { _skip = true; break; }
                    if (!MoveNext(routine)) break;
                    yield return routine.Current;
                }
            }

            if (_skip)
            {
                foreach (var a in c.SkipActions) a?.Invoke();
                var f = ScreenFader.Active;
                if (f != null) f.SetAlpha(0f);
            }

            if (SeteVidas.Save.SaveManager.Data != null) SeteVidas.Save.SaveManager.Data.SetFlag(c.SeenFlag);
            if (input != null) input.SetLock(this, false);
            if (health != null) health.Shielded = false;
            s_current = null;
            onDone?.Invoke();
        }

        static bool MoveNext(IEnumerator e)
        {
            try { return e.MoveNext(); }
            catch (Exception ex) { Debug.LogException(ex); return false; }
        }

        static bool SkipPressed()
        {
            var k = Keyboard.current; var p = Gamepad.current;
            return (k != null && k.escapeKey.wasPressedThisFrame) ||
                   (p != null && (p.buttonEast.wasPressedThisFrame || p.startButton.wasPressedThisFrame));
        }

        void OnDestroy()
        {
            if (s_instance == this) { s_instance = null; s_current = null; }
        }
    }
}
