using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SeteVidas.Audio
{
    /// <summary>Como um som toca: volume, variação de altura, alcance no mundo e quantas cópias ao mesmo tempo.</summary>
    public readonly struct SfxSettings
    {
        public readonly float Volume;
        /// <summary>Variação aleatória da altura (0,05 = ±5%).</summary>
        public readonly float PitchJitter;
        /// <summary>Distância (tiles) em que o som some. 0 = não espacial (gato, interface, voz, cenas).</summary>
        public readonly float Range;
        public readonly int MaxInstances;
        /// <summary>Tempo mínimo entre dois disparos do mesmo som (s).</summary>
        public readonly float Cooldown;
        /// <summary>Quanto a música abaixa enquanto este som toca (1 = nada; 0,5 = metade). Ver <see cref="AudioMix"/>.</summary>
        public readonly float Duck;

        public SfxSettings(float volume, float range, int maxInstances = 3, float jitter = 0.05f, float cooldown = 0.03f, float duck = 1f)
        {
            Volume = volume;
            Range = range;
            MaxInstances = maxInstances;
            PitchJitter = jitter;
            Cooldown = cooldown;
            Duck = duck;
        }
    }

    /// <summary>
    /// A mixagem dos SFX (Docs/Audio/plano-sfx.md §13). Os arquivos saem do gerador com o mesmo volume percebido
    /// (normalizados pela intensidade), então o volume daqui é a hierarquia da ação:
    /// <list type="bullet">
    /// <item>0,6–0,9: o que pede reação ou pesa na história (alerta "!", dano no gato, telégrafos do chefe, impactos fortes, furtivo);</item>
    /// <item>0,35–0,6: o combate e os avisos (golpes, acertos, mira, armadilhas armando, objetos barulhentos);</item>
    /// <item>0,1–0,3: o corpo e o mundo de fundo (passos, escalada, serra, interface);</item>
    /// <item>abaixo de 0,1: o que deve ser quase mudo (andar agachado).</item>
    /// </list>
    /// Os sons dos inimigos e do mundo são espaciais: somem com a distância da câmera, e o alcance dos sons de
    /// ruído acompanha o raio da regra de furtividade.
    /// </summary>
    public static class SfxCatalog
    {
        static readonly Dictionary<string, SfxSettings> s_table = new Dictionary<string, SfxSettings>
        {
            // ---------------- gato: corpo (fundo; sempre no centro da tela, não espacial)
            { "gato_passo", new SfxSettings(0.07f, 0f, 2, 0.06f) },               // correndo (foley gravado; era 0,13: pedido do usuário)
            { "gato_passo_lento", new SfxSettings(0.05f, 0f, 2, 0.06f) },          // andando devagar (foley)
            { "gato_passo_agachado", new SfxSettings(0.02f, 0f, 2, 0.08f) },   // agachar = ruído 0: quase mudo
            { "gato_pulo", new SfxSettings(0.12f, 0f, 1) },
            { "gato_aterrissa", new SfxSettings(0.16f, 0f, 1) },
            { "gato_aterrissa_pesada", new SfxSettings(0.4f, 0f, 1) },           // faz barulho na regra: tem que se ouvir
            { "gato_borda", new SfxSettings(0.18f, 0f, 1) },
            { "gato_sobe_borda", new SfxSettings(0.12f, 0f, 1) },
            { "gato_hera", new SfxSettings(0.15f, 0f, 1) },
            { "gato_escalada", new SfxSettings(0.08f, 0f, 2, 0.1f) },
            { "gato_pulo_parede", new SfxSettings(0.18f, 0f, 1) },
            { "gato_splash_grande", new SfxSettings(0.7f, 0f, 3, 0f, 0.08f, duck: 0.5f) }, // o salto de fé (pulos seguidos: várias cópias)
            { "gato_splash", new SfxSettings(0.25f, 0f, 2) },
            { "gato_esconder", new SfxSettings(0.1f, 0f, 1, 0.05f, 0.4f) },

            // ---------------- gato: combate
            { "adaga_golpe", new SfxSettings(0.22f, 0f, 2) },
            { "adaga_golpe2", new SfxSettings(0.26f, 0f, 2) },
            { "acerto", new SfxSettings(0.42f, 0f, 3) },
            { "acerto_costas", new SfxSettings(0.48f, 0f, 3) },
            { "acerto_furtivo", new SfxSettings(0.8f, 0f, 2, 0.02f, duck: 0.5f) },   // a recompensa da furtividade
            { "adaga_tink", new SfxSettings(0.3f, 0f, 2) },
            { "estocada", new SfxSettings(0.24f, 0f, 1) },
            { "quique", new SfxSettings(0.3f, 0f, 1) },
            { "marreta_ergue", new SfxSettings(0.26f, 0f, 1) },
            { "marreta_golpe", new SfxSettings(0.34f, 0f, 1) },
            { "marreta_acerto", new SfxSettings(0.62f, 0f, 2, duck: 0.7f) },
            { "marreta_chao", new SfxSettings(0.8f, 0f, 1, duck: 0.45f) },          // terremoto

            // ---------------- gato: classes, cura, vida
            { "classe_ladino", new SfxSettings(0.3f, 0f, 1, 0f) },
            { "classe_paladino", new SfxSettings(0.32f, 0f, 1, 0f) },
            { "classe_desbloqueada", new SfxSettings(0.45f, 0f, 1, 0f, duck: 0.5f) },
            { "cura_concentra", new SfxSettings(0.26f, 0f, 1, 0f) },
            { "cura_efeito", new SfxSettings(0.38f, 0f, 1, 0f) },
            { "cura_interrompida", new SfxSettings(0.34f, 0f, 1, 0f) },
            { "cura_negada", new SfxSettings(0.15f, 0f, 1) },
            { "gato_dano", new SfxSettings(0.7f, 0f, 1, 0.03f, duck: 0.55f) },
            { "gato_morte", new SfxSettings(0.6f, 0f, 1, 0f, duck: 0.3f) },
            { "gato_renasce", new SfxSettings(0.35f, 0f, 1, 0f) },
            { "gato_batimento_loop", new SfxSettings(0.32f, 0f, 1, 0f) },
            { "espinhos", new SfxSettings(0.45f, 0f, 1) },
            { "chao_seguro", new SfxSettings(0.18f, 0f, 1) },
            { "coracao_cai", new SfxSettings(0.28f, 14f, 2) },
            { "coracao_pega", new SfxSettings(0.38f, 0f, 1, 0f) },

            // ---------------- templários: os vocais contam o estado (o "!" é o som mais legível do jogo)
            { "guarda_passo", new SfxSettings(0.15f, 12f, 4, 0.08f) },
            { "guarda_ocioso", new SfxSettings(0.16f, 10f, 1, 0.05f, 2f) },
            { "guarda_suspeito", new SfxSettings(0.55f, 18f, 2, duck: 0.8f) },
            { "guarda_investiga", new SfxSettings(0.32f, 14f, 2) },
            { "guarda_alerta", new SfxSettings(0.8f, 20f, 2, duck: 0.6f) },
            { "guarda_caca", new SfxSettings(0.42f, 16f, 2) },
            { "guarda_desiste", new SfxSettings(0.32f, 14f, 2) },
            { "lanca_prepara", new SfxSettings(0.55f, 14f, 3, 0.03f) },              // telégrafo
            { "lanca_golpe", new SfxSettings(0.4f, 14f, 3) },
            { "guarda_dano", new SfxSettings(0.4f, 16f, 3) },
            { "guarda_morte", new SfxSettings(0.45f, 18f, 2) },
            // ---------------- templário zumbi (depois do tomo): o arrasto avisa que ele está perto, mesmo fora da tela
            { "zumbi_passo", new SfxSettings(0.2f, 12f, 3, 0.08f, 0.1f) },
            { "zumbi_ocioso", new SfxSettings(0.22f, 11f, 2, 0.06f, 2.5f) },
            { "zumbi_sente", new SfxSettings(0.62f, 18f, 2, 0.05f, 0.5f, duck: 0.8f) },  // o "!" do zumbi
            { "zumbi_caca", new SfxSettings(0.36f, 14f, 2, 0.06f, 1f) },
            { "zumbi_dano", new SfxSettings(0.4f, 16f, 3) },
            { "zumbi_morte", new SfxSettings(0.45f, 18f, 2) },
            // ---------------- veneno (são do gato: não espaciais, médios-agudos)
            { "zumbi_toque", new SfxSettings(0.6f, 0f, 1, 0.04f, 0.25f) },
            { "veneno_tique", new SfxSettings(0.45f, 0f, 1, 0.03f, 0.2f) },
            { "veneno_passa", new SfxSettings(0.3f, 0f, 1, 0f) },
            { "besta_mira", new SfxSettings(0.38f, 18f, 3, 0f) },                    // telégrafo
            { "besta_trava", new SfxSettings(0.65f, 18f, 3, 0.02f) },                // hora de sair do lugar
            { "besta_disparo", new SfxSettings(0.45f, 18f, 3) },
            { "virote_crava", new SfxSettings(0.28f, 14f, 3) },
            { "besta_recarga", new SfxSettings(0.22f, 14f, 3, 0.03f) },
            { "seteira_disparo", new SfxSettings(0.24f, 12f, 2) },

            // ---------------- chefe
            { "koda_passo", new SfxSettings(0.32f, 24f, 2, 0.06f) },
            { "koda_ergue", new SfxSettings(0.5f, 24f, 1) },                         // telégrafo
            { "koda_impacto", new SfxSettings(0.72f, 24f, 2, duck: 0.6f) },
            { "koda_salto_agacha", new SfxSettings(0.52f, 24f, 1) },                 // telégrafo
            { "koda_salto_ar", new SfxSettings(0.32f, 24f, 1) },
            { "koda_salto_queda", new SfxSettings(0.88f, 24f, 1, duck: 0.4f) },
            { "koda_cura_loop", new SfxSettings(0.3f, 24f, 1, 0f) },
            { "koda_cura_quebra", new SfxSettings(0.55f, 24f, 1) },
            { "koda_atordoado", new SfxSettings(0.28f, 24f, 1) },
            { "koda_raio_ergue", new SfxSettings(0.62f, 24f, 1, 0f, duck: 0.6f) },  // telégrafo
            { "raio_aviso", new SfxSettings(0.32f, 24f, 3) },
            { "raio", new SfxSettings(0.62f, 24f, 3, duck: 0.6f) },
            { "escudo_liga", new SfxSettings(0.32f, 24f, 1, 0f) },
            { "escudo_dong", new SfxSettings(0.42f, 24f, 2, 0.04f, 0.12f) },
            { "escudo_desliga", new SfxSettings(0.28f, 24f, 1, 0f) },
            { "koda_furioso", new SfxSettings(0.78f, 24f, 1, 0f, duck: 0.45f) },
            { "koda_dano", new SfxSettings(0.32f, 24f, 2) },
            { "koda_derrota", new SfxSettings(0.65f, 24f, 1, 0f, duck: 0.5f) },
            { "grade_fecha", new SfxSettings(0.55f, 24f, 2, 0.03f, duck: 0.7f) },
            { "grade_abre", new SfxSettings(0.38f, 24f, 2, 0.03f) },
            { "selo_golpe", new SfxSettings(0.88f, 0f, 1, 0f, duck: 0.3f) },

            // ---------------- o tomo e a praga (cena: não espacial, exceto o zumbido)
            { "tomo_zumbido_loop", new SfxSettings(0.2f, 10f, 1, 0f) },
            { "tomo_pega", new SfxSettings(0.62f, 0f, 1, 0f, duck: 0.5f) },
            { "tomo_paginas_loop", new SfxSettings(0.25f, 0f, 1, 0f) },
            { "coracao_arrancado", new SfxSettings(0.55f, 0f, 2, 0f) },
            { "tomo_fecha", new SfxSettings(0.55f, 0f, 1, 0f) },
            { "capivara_surge", new SfxSettings(0.6f, 0f, 1, 0f, duck: 0.6f) },
            { "capivara_some", new SfxSettings(0.5f, 0f, 1, 0f) },
            { "moeda_pesada", new SfxSettings(0.5f, 0f, 1, 0f) },

            // ---------------- mundo (o alcance dos barulhentos segue o raio da regra: cacos 10, sininhos 11, folhas 3, balde 4)
            { "cacos", new SfxSettings(0.5f, 20f, 2, 0.08f) },
            { "folhas", new SfxSettings(0.2f, 10f, 2, 0.08f) },
            { "sininhos", new SfxSettings(0.45f, 22f, 2, 0.03f) },
            { "balde", new SfxSettings(0.4f, 12f, 1) },
            { "moeda", new SfxSettings(0.26f, 0f, 3, 0.03f) },
            { "moeda5", new SfxSettings(0.32f, 0f, 2, 0.02f) },
            { "moeda100", new SfxSettings(0.48f, 0f, 1, 0f) },
            { "placa", new SfxSettings(0.22f, 0f, 1) },
            { "santuario_descansa", new SfxSettings(0.42f, 0f, 1, 0f, duck: 0.5f) },
            { "parede_racha", new SfxSettings(0.48f, 18f, 2) },
            { "parede_quebra", new SfxSettings(0.75f, 22f, 1, duck: 0.5f) },

            // ---------------- armadilhas: o som de armar é mais alto que o do golpe (o aviso é o que importa)
            { "fio", new SfxSettings(0.55f, 18f, 2, duck: 0.75f) },
            { "placa_clic", new SfxSettings(0.5f, 16f, 2, duck: 0.75f) },
            { "flecha_aviso", new SfxSettings(0.36f, 12f, 2) },
            { "flecha", new SfxSettings(0.24f, 12f, 3, 0.08f) },
            { "flecha_crava", new SfxSettings(0.18f, 10f, 3, 0.08f) },
            { "lanca_aviso", new SfxSettings(0.42f, 12f, 2) },
            { "lanca_sobe", new SfxSettings(0.36f, 12f, 2) },
            { "chama_fagulha", new SfxSettings(0.16f, 10f, 2) },
            { "chama", new SfxSettings(0.24f, 10f, 2) },
            { "serra_loop", new SfxSettings(0.13f, 9f, 8, 0f) },
            { "plataforma_treme", new SfxSettings(0.34f, 12f, 2) },
            { "plataforma_cai", new SfxSettings(0.32f, 14f, 2) },
            { "plataforma_volta", new SfxSettings(0.14f, 12f, 2) },

            // ---------------- interface (discreta: não compete com o jogo)
            { "ui_cursor", new SfxSettings(0.14f, 0f, 2, 0.02f) },
            { "ui_cursor_inv", new SfxSettings(0.14f, 0f, 2, 0.02f) },
            { "ui_avanca", new SfxSettings(0.12f, 0f, 1, 0.02f) },
            { "ui_aba", new SfxSettings(0.18f, 0f, 1, 0.02f) },
            { "ui_centelha", new SfxSettings(0.16f, 0f, 1, 0.02f) },
            { "ui_barra_chefe", new SfxSettings(0.5f, 0f, 1, 0f, duck: 0.5f) },
            { "ui_aviso", new SfxSettings(0.3f, 0f, 1, 0f) },
            { "ui_apaga", new SfxSettings(0.32f, 0f, 1, 0f) },
        };

        public static SfxSettings For(string id)
        {
            if (s_table.TryGetValue(id, out var s)) return s;
            if (id.StartsWith("ui_")) return new SfxSettings(0.24f, 0f, 2, 0.02f);
            if (id.StartsWith("vox_")) return new SfxSettings(0.18f, 0f, 1, 0f, 0f);   // vozes baixas (pedido do usuário)
            return new SfxSettings(0.4f, 16f);
        }
    }

    /// <summary>
    /// Toca os SFX do jogo (Docs/Audio/plano-sfx.md). Os clipes ficam em <c>Resources/Audio/SFX</c> (gerados por
    /// <c>Tools/Audio/gerar_sfx.ps1</c>); o id é o nome do arquivo sem "sfx_" e sem o "_NN" das variações
    /// (ex.: <c>sfx_gato_passo_03.wav</c> → "gato_passo"). Volume geral em PlayerPrefs ("volume_efeitos").
    /// </summary>
    public static class Sfx
    {
        public const string VolumeKey = "volume_efeitos";

        /// <summary>Toca um som. <paramref name="at"/> = posição no mundo (sons espaciais somem com a distância).</summary>
        public static void Play(string id, Vector2? at = null, float volume = 1f, float pitch = 1f) =>
            SfxPlayer.Get()?.Play(id, at, volume, pitch, -1);

        /// <summary>Toca uma variação específica (ex.: o 3º coração arrancado).</summary>
        public static void PlayVariant(string id, int variant, Vector2? at = null, float volume = 1f) =>
            SfxPlayer.Get()?.Play(id, at, volume, 1f, variant);

        /// <summary>Som em loop que segue <paramref name="follow"/> (serra, zumbido do tomo, batimento). Pare com <see cref="SfxLoop.Stop"/>.</summary>
        public static SfxLoop Loop(string id, Transform follow, float volume = 1f) => SfxPlayer.Get()?.StartLoop(id, follow, volume);

        /// <summary>Existe clipe para este id?</summary>
        public static bool Has(string id) => SfxPlayer.Get()?.HasClip(id) ?? false;

        /// <summary>Volume geral dos efeitos (0 a 1).</summary>
        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set => PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        }

        /// <summary>Atenuação (0 a 1) de um som a <paramref name="distance"/> tiles, com alcance <paramref name="range"/>. Regra pura.</summary>
        public static float Attenuation(float distance, float range)
        {
            if (range <= 0f) return 1f;
            if (distance >= range) return 0f;
            float near = range * 0.35f;
            if (distance <= near) return 1f;
            float k = 1f - (distance - near) / (range - near);
            return k * k;
        }

        /// <summary>O id de um arquivo: sem "sfx_" e sem o "_NN" final. Regra pura.</summary>
        public static string IdOf(string clipName)
        {
            string id = Regex.Replace(clipName, @"_\d+$", "");
            return id.StartsWith("sfx_") ? id.Substring(4) : id;
        }
    }

    /// <summary>A fonte de som de um loop (segue um objeto e atenua pela distância). Criado por <see cref="Sfx.Loop"/>.</summary>
    public class SfxLoop : MonoBehaviour
    {
        AudioSource _source;
        Transform _follow;
        bool _hadFollow;
        SfxSettings _settings;
        float _volume = 1f, _fade, _fadeTarget = 1f, _fadeSpeed = 4f;

        public float Volume { get => _volume; set => _volume = Mathf.Max(0f, value); }
        public float Pitch { get => _source != null ? _source.pitch : 1f; set { if (_source != null) _source.pitch = value; } }

        internal void Begin(AudioClip clip, Transform follow, SfxSettings settings, float volume)
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.clip = clip;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.time = Random.Range(0f, clip.length * 0.9f); // loops iguais lado a lado não andam juntos
            _follow = follow;
            _hadFollow = follow != null;
            _settings = settings;
            _volume = volume;
            _fade = 0f;
            Apply();
            _source.Play();
        }

        /// <summary>Some em <paramref name="fade"/> s e se destrói.</summary>
        public void Stop(float fade = 0.25f)
        {
            if (this == null) return;
            _fadeTarget = 0f;
            _fadeSpeed = fade > 0f ? 1f / fade : 1000f;
        }

        void Update()
        {
            if (_hadFollow && _follow == null) Stop(0.1f);
            _fade = Mathf.MoveTowards(_fade, _fadeTarget, _fadeSpeed * Time.unscaledDeltaTime);
            if (_fadeTarget <= 0f && _fade <= 0f) { Destroy(gameObject); return; }
            Apply();
        }

        void Apply()
        {
            if (_source == null) return;
            float att = 1f, pan = 0f;
            if (_follow != null && _settings.Range > 0f) SfxPlayer.Spatial(_follow.position, _settings.Range, out att, out pan);
            _source.panStereo = pan;
            _source.volume = _settings.Volume * _volume * _fade * att * Sfx.MasterVolume;
        }
    }

    /// <summary>O tocador: carrega os clipes e mantém um conjunto de fontes de som (criado no primeiro uso).</summary>
    public class SfxPlayer : MonoBehaviour
    {
        const int PoolSize = 24;
        const string ResourcePath = "Audio/SFX";

        static SfxPlayer s_instance;
        static readonly HashSet<string> s_warned = new HashSet<string>();

        readonly Dictionary<string, AudioClip[]> _clips = new Dictionary<string, AudioClip[]>();
        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        readonly Dictionary<string, int> _lastVariant = new Dictionary<string, int>();
        AudioSource[] _pool;
        string[] _poolIds;
        float[] _poolStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() // Play sem recarregar o domínio
        {
            s_instance = null;
            s_warned.Clear();
        }

        internal static SfxPlayer Get()
        {
            if (s_instance != null) return s_instance;
            if (!Application.isPlaying) return null;
            var go = new GameObject("[Sfx]");
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<SfxPlayer>();
            return s_instance;
        }

        void Awake()
        {
            foreach (var clip in Resources.LoadAll<AudioClip>(ResourcePath))
            {
                string id = Sfx.IdOf(clip.name);
                var list = _clips.TryGetValue(id, out var have) ? new List<AudioClip>(have) : new List<AudioClip>();
                list.Add(clip);
                list.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); // variações em ordem (o coração 1 é o _01)
                _clips[id] = list.ToArray();
            }
            _pool = new AudioSource[PoolSize];
            _poolIds = new string[PoolSize];
            _poolStarted = new float[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _pool[i] = s;
            }
        }

        void OnDestroy() { if (s_instance == this) s_instance = null; }

        internal bool HasClip(string id) => _clips.ContainsKey(id);

        /// <summary>Volume (0 a 1) e pan de um ponto do mundo em relação à câmera.</summary>
        internal static void Spatial(Vector2 at, float range, out float attenuation, out float pan)
        {
            var cam = Camera.main;
            Vector2 listener = cam != null ? (Vector2)cam.transform.position : at;
            float d = Vector2.Distance(at, listener);
            attenuation = Sfx.Attenuation(d, range);
            pan = Mathf.Clamp((at.x - listener.x) / 14f, -0.7f, 0.7f);
        }

        AudioClip Pick(string id, int variant)
        {
            if (!_clips.TryGetValue(id, out var clips) || clips.Length == 0)
            {
                if (s_warned.Add(id)) Debug.LogWarning($"[Sete Vidas] Som \"{id}\" não encontrado em Resources/{ResourcePath} (rode Tools/Audio/gerar_sfx.ps1).");
                return null;
            }
            if (variant >= 0) return clips[Mathf.Clamp(variant, 0, clips.Length - 1)];
            int last = _lastVariant.TryGetValue(id, out var l) ? l : -1;
            int pick = Random.Range(0, clips.Length);
            if (clips.Length > 1 && pick == last) pick = (pick + 1) % clips.Length; // nunca o mesmo arquivo duas vezes seguidas
            _lastVariant[id] = pick;
            return clips[pick];
        }

        internal void Play(string id, Vector2? at, float volume, float pitch, int variant)
        {
            if (string.IsNullOrEmpty(id)) return;
            var settings = SfxCatalog.For(id);
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(id, out float last) && now - last < settings.Cooldown) return;

            float att = 1f, pan = 0f;
            if (at.HasValue && settings.Range > 0f)
            {
                Spatial(at.Value, settings.Range, out att, out pan);
                if (att <= 0.001f) return; // longe demais: nem ocupa uma fonte
            }

            var clip = Pick(id, variant);
            if (clip == null) return;

            bool voice = id.StartsWith("vox_");
            int count = 0;
            for (int i = 0; i < PoolSize; i++)
            {
                if (!_pool[i].isPlaying) continue;
                // uma voz nova corta a anterior (a fala seguinte começa)
                if (voice && _poolIds[i] != null && _poolIds[i].StartsWith("vox_")) { _pool[i].Stop(); continue; }
                if (_poolIds[i] == id) count++;
            }
            if (count >= settings.MaxInstances) return;

            int slot = -1;
            float oldest = float.MaxValue;
            for (int i = 0; i < PoolSize; i++)
            {
                if (!_pool[i].isPlaying) { slot = i; break; }
                if (_poolStarted[i] < oldest) { oldest = _poolStarted[i]; slot = i; }
            }
            var src = _pool[slot];
            src.Stop();
            src.clip = clip;
            src.volume = settings.Volume * volume * att * Sfx.MasterVolume;
            src.pitch = pitch * (1f + Random.Range(-settings.PitchJitter, settings.PitchJitter));
            src.panStereo = pan;
            src.Play();
            _poolIds[slot] = id;
            _poolStarted[slot] = now;
            _lastPlayed[id] = now;
            // momentos fortes abaixam a trilha um instante (quanto mais perto, mais abaixa)
            if (settings.Duck < 1f) AudioMix.Duck(Mathf.Lerp(1f, settings.Duck, Mathf.Clamp01(att * volume)), Mathf.Clamp(clip.length * 0.5f, 0.15f, 1.2f));
        }

        internal SfxLoop StartLoop(string id, Transform follow, float volume)
        {
            var clip = Pick(id, 0);
            if (clip == null) return null;
            var go = new GameObject("[Sfx loop] " + id);
            go.transform.SetParent(transform, false);
            var loop = go.AddComponent<SfxLoop>();
            loop.Begin(clip, follow, SfxCatalog.For(id), volume);
            return loop;
        }
    }
}
