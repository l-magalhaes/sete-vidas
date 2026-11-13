using System.Collections.Generic;
using SeteVidas.Enemies;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
using SeteVidas.UI;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Audio
{
    /// <summary>Qual música deve tocar (Docs/Audio/plano-musicas.md).</summary>
    /// <summary>Momentos da trilha. Só acrescente no fim (o número vai para as cenas). Floresta: Docs/Audio/floresta-audio.md.</summary>
    public enum MusicCue
    {
        None, Menu, Mountain, Explore, Stealth, Combat, Boss, Curse, Sanctuary,
        Road, Ruins, Blight, Nest, Vulpis, Tobi, Spirit,
    }

    /// <summary>Regras puras (EditMode): qual música vence quando várias coisas acontecem juntas.</summary>
    public static class MusicRules
    {
        public struct Situation
        {
            public bool CurseScene, BossFighting, BossOutro, AnyCombat, InSanctuary, OnMountain;
            /// <summary>O gato está obscurecido na sombra (a silhueta escura): é só aí que toca a música de furtividade.</summary>
            public bool Obscured;
            /// <summary>Faixa da área onde o gato está (ruínas, praga, ninho; ver <see cref="MusicArea"/>). None = a da fase.</summary>
            public MusicCue AreaCue;
        }

        /// <summary>Prioridade: cena do tomo &gt; chefe &gt; combate &gt; furtividade &gt; santuário &gt; montanha &gt; fase.</summary>
        public static MusicCue Pick(Situation s)
        {
            if (s.CurseScene) return MusicCue.Curse;
            if (s.BossFighting) return MusicCue.Boss;
            if (s.BossOutro) return MusicCue.None;   // o desfecho do chefe segue tocando sozinho, depois silêncio
            if (s.AnyCombat) return MusicCue.Combat;
            if (s.Obscured) return MusicCue.Stealth;
            if (s.InSanctuary) return MusicCue.Sanctuary;
            if (s.OnMountain) return MusicCue.Mountain;
            if (s.AreaCue != MusicCue.None) return s.AreaCue;
            return MusicCue.Explore;
        }

        /// <summary>Tempo da troca (s): o perigo entra rápido e sai devagar.</summary>
        public static float FadeTime(MusicCue from, MusicCue to)
        {
            if (to == MusicCue.Curse) return 0.3f;
            if (to == MusicCue.Combat) return 1.2f;    // entra sem susto
            if (from == MusicCue.Combat) return 1.2f;  // e sai logo
            if (to == MusicCue.Boss) return 1f;
            if (from == MusicCue.Stealth || to == MusicCue.Stealth) return 1.5f;
            return 2f;
        }
    }

    /// <summary>
    /// O diretor de música da fase: escolhe a faixa pelo que está acontecendo (estado das Sentinelas, gato escondido,
    /// chefe, cena do tomo, santuário, alto da montanha) e troca com crossfade entre duas fontes. A fase, a montanha
    /// e a furtividade voltam de onde pararam; combate, chefe e cena começam do início. No inventário a música fica
    /// abafada. Volume em PlayerPrefs ("volume_musica"). Montado pelo menu 13 (FortressSetup).
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        [System.Serializable]
        public class Track
        {
            public MusicCue Cue;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume = 0.8f;
            [Tooltip("Continua de onde parou ao voltar (fase, montanha, furtividade)")]
            public bool Resume;
            [Tooltip("Fim do trecho em loop (s). 0 = a faixa toda. O que vem depois é o desfecho (chefe)")]
            public float LoopEnd;
        }

        public const string VolumeKey = "volume_musica";

        [SerializeField] Track[] tracks = new Track[0];
        [Header("Lugares")]
        [Tooltip("Alto da montanha (mundo): a música contemplativa até o pulo")]
        [SerializeField] Rect mountainArea = new Rect(-14f, 59f, 27f, 40f);
        [Tooltip("Distância (tiles) do santuário em que toca o tema dele")]
        [SerializeField] float sanctuaryRadius = 5f;
        [Header("Furtividade e combate")]
        [Tooltip("Segura o combate por este tempo depois que o último inimigo sai dele (s)")]
        [SerializeField] float combatHold = 1.5f;
        [Tooltip("Segura a música furtiva por este tempo depois que o gato sai da sombra (s)")]
        [SerializeField] float stealthHold = 1.5f;
        [Header("Chefe")]
        [Tooltip("Silêncio antes do tema do chefe (s)")]
        [SerializeField] float bossSilence = 2f;
        [Header("Inventário")]
        [SerializeField] float pausedCutoff = 900f;
        [SerializeField, Range(0f, 1f)] float pausedVolume = 0.7f;

        public static MusicDirector Active { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active = null; // Play sem recarregar o domínio

        AudioSource[] _src = new AudioSource[2];
        AudioLowPassFilter[] _lp = new AudioLowPassFilter[2];
        readonly float[] _target = new float[2];
        readonly float[] _fadeSpeed = new float[2];
        readonly float[] _level = new float[2];   // o fade de cada fonte (sem a mixagem)
        float _mixGain = AudioMix.MusicLevel;
        readonly MusicCue[] _srcCue = new MusicCue[2];
        int _front;
        readonly Dictionary<MusicCue, float> _resumeAt = new Dictionary<MusicCue, float>();

        MusicCue _current = MusicCue.None;
        float _combatUntil = -99f, _stealthUntil = -99f, _pendingAt = -1f;
        MusicCue _pending = MusicCue.None;
        bool _bossWasFighting, _bossOutro;
        EnemyAI[] _enemies = new EnemyAI[0];
        float _nextScan;
        CursedTome _tome;
        ShadowSilhouette _silhouette;
        float _userVolume = 1f;

        public MusicCue Current => _current;
        /// <summary>A música que está soando agora (testes).</summary>
        public AudioClip CurrentClip => _src[_front] != null && _src[_front].isPlaying ? _src[_front].clip : null;

        void Awake()
        {
            Active = this;
            _userVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Fonte " + (i + 1));
                go.transform.SetParent(transform, false);
                _src[i] = go.AddComponent<AudioSource>();
                _src[i].playOnAwake = false;
                _src[i].loop = true;
                _src[i].spatialBlend = 0f;
                _src[i].ignoreListenerPause = true;
                _src[i].volume = 0f;
                _lp[i] = go.AddComponent<AudioLowPassFilter>();
                _lp[i].cutoffFrequency = 22000f;
            }
        }

        void OnDestroy() { if (Active == this) Active = null; }

        public void SetVolume(float v)
        {
            _userVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(VolumeKey, _userVolume);
        }

        Track Find(MusicCue cue)
        {
            foreach (var t in tracks) if (t != null && t.Cue == cue && t.Clip != null) return t;
            return null;
        }

        // ------------------------------------------------------------ o que está acontecendo

        MusicRules.Situation Read()
        {
            var s = new MusicRules.Situation();
            float now = Time.unscaledTime;
            if (now >= _nextScan) { _enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None); _tome = FindAnyObjectByType<CursedTome>(); _nextScan = now + 2f; }

            var health = PlayerHealth.Active;
            Vector2 cat = health != null ? (Vector2)health.transform.position : Vector2.zero;
            // furtividade: só com o gato obscurecido na sombra (o mesmo sinal da silhueta escura)
            if (health != null && _silhouette == null) _silhouette = health.GetComponent<ShadowSilhouette>();
            if (_silhouette != null && _silhouette.IsSilhouette) _stealthUntil = now + stealthHold;
            s.Obscured = now < _stealthUntil;

            bool combat = false;
            foreach (var e in _enemies)
                if (e != null && !e.IsDead && e.State == AlertState.Combat) { combat = true; break; }
            if (combat) _combatUntil = now + combatHold;
            s.AnyCombat = now < _combatUntil;

            var boss = GoldenBoss.Active;
            bool fighting = boss != null && boss.IsFighting;
            if (_bossWasFighting && !fighting && boss != null && boss.IsDefeated) _bossOutro = true;   // ajoelhou: toca o desfecho
            _bossWasFighting = fighting;
            s.BossFighting = fighting;
            s.BossOutro = _bossOutro;
            s.CurseScene = _tome != null && _tome.IsRunning;
            if (s.CurseScene) _bossOutro = false;   // a cena do tomo encerra o silêncio do pós-chefe

            if (health != null)
            {
                s.OnMountain = mountainArea.Contains(cat);
                s.AreaCue = MusicArea.CueAt(cat);
                foreach (var sa in Sanctuary.All)
                    if (sa != null && Vector2.Distance(cat, sa.transform.position) <= sanctuaryRadius) { s.InSanctuary = true; break; }
            }
            return s;
        }

        // ------------------------------------------------------------ tocar

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var situation = Read();
            // momentos sem faixa não contam: a escolha cai para o próximo da fila (ex.: sombra sem música de furtividade)
            if (Find(MusicCue.Stealth) == null) situation.Obscured = false;
            if (Find(MusicCue.Combat) == null) situation.AnyCombat = false;
            var want = MusicRules.Pick(situation);
            if (want != MusicCue.None && Find(want) == null) want = _pending != MusicCue.None ? _pending : _current;   // momento sem faixa (ex.: combate): segue a que já toca
            if (want != _current && want != _pending) Request(want);

            if (_pending != MusicCue.None && Time.unscaledTime >= _pendingAt) { var p = _pending; _pending = MusicCue.None; Begin(p); }

            bool paused = InventoryMenu.IsOpen;
            // a trilha é fundo: nível baixo, abaixa nos momentos fortes e no diálogo (AudioMix), sem estalo
            _mixGain = Mathf.MoveTowards(_mixGain, AudioMix.MusicGain, 2.5f * dt);
            for (int i = 0; i < 2; i++)
            {
                var src = _src[i];
                if (src == null) continue;
                var t = Find(_srcCue[i]);
                float baseVol = t != null ? t.Volume : 0f;
                float goal = _target[i] * baseVol * _userVolume * (paused ? pausedVolume : 1f);
                _level[i] = Mathf.MoveTowards(_level[i], goal, _fadeSpeed[i] * dt);
                src.volume = _level[i] * _mixGain;
                if (_level[i] <= 0.0001f && _target[i] <= 0f && src.isPlaying)
                {
                    if (t != null && t.Resume) _resumeAt[_srcCue[i]] = src.time;
                    src.Stop();
                }
                _lp[i].cutoffFrequency = Mathf.MoveTowards(_lp[i].cutoffFrequency, paused ? pausedCutoff : 22000f, 40000f * dt);

                // loop do trecho (chefe): volta ao começo antes do desfecho
                if (t != null && t.LoopEnd > 0f && src.loop && src.time >= t.LoopEnd) src.time = 0f;
            }
        }

        void Request(MusicCue cue)
        {
            float fade = MusicRules.FadeTime(_current, cue);
            if (cue == MusicCue.None && _bossOutro && _current == MusicCue.Boss)
            {
                PlayOutro();
                _current = MusicCue.None;
                return;
            }
            FadeOut(_front, fade);
            if (cue == MusicCue.Boss)
            {
                // silêncio antes do chefe
                _pending = cue;
                _pendingAt = Time.unscaledTime + fade + bossSilence;
                _current = cue;
                return;
            }
            _pending = MusicCue.None;
            _current = cue;
            if (cue != MusicCue.None) Begin(cue, fade);
        }

        void Begin(MusicCue cue, float fade = 1f)
        {
            var t = Find(cue);
            if (t == null) return;
            int back = 1 - _front;
            var src = _src[back];
            if (src.isPlaying && _srcCue[back] != MusicCue.None)
            {
                var old = Find(_srcCue[back]);
                if (old != null && old.Resume) _resumeAt[_srcCue[back]] = src.time;
            }
            src.Stop();
            src.clip = t.Clip;
            src.loop = true;
            float at = 0f;
            if (t.Resume && _resumeAt.TryGetValue(cue, out var saved)) at = saved;
            src.time = Mathf.Clamp(at, 0f, Mathf.Max(0f, t.Clip.length - 0.1f));
            src.volume = 0f;
            _level[back] = 0f;
            src.Play();
            _srcCue[back] = cue;
            _target[back] = 1f;
            _fadeSpeed[back] = 1f / Mathf.Max(0.05f, fade);
            _front = back;
        }

        void FadeOut(int i, float fade)
        {
            _target[i] = 0f;
            _fadeSpeed[i] = 1f / Mathf.Max(0.05f, fade);
        }

        /// <summary>O chefe ajoelhou: pula para o desfecho da faixa e deixa terminar.</summary>
        void PlayOutro()
        {
            var src = _src[_front];
            var t = Find(MusicCue.Boss);
            if (src == null || t == null || t.LoopEnd <= 0f || !src.isPlaying) { FadeOut(_front, 2f); return; }
            src.loop = false;
            if (src.time < t.LoopEnd) src.time = t.LoopEnd;
        }

        /// <summary>Monta as faixas (o menu 13 chama no editor).</summary>
        public void Configure(Track[] list, Rect mountain)
        {
            tracks = list;
            mountainArea = mountain;
        }
    }
}
