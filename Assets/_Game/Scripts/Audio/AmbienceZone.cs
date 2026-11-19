using System.Collections.Generic;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Audio
{
    /// <summary>
    /// A ambiência de uma sala (F10 da Floresta, Docs/Audio/floresta-audio.md §Ambiências): um loop
    /// (<c>amb_floresta_viva</c>, <c>amb_floresta_ruinas</c>, <c>amb_floresta_corrompida</c>, <c>amb_ninho</c>,
    /// <c>amb_estrada</c>) e, de vez em quando, um som pontual (coruja, galho caindo, sapo, a máquina dos Primatas). Vale a
    /// zona onde o gato está (a menor, se houver mais de uma); a troca é um crossfade de 2 s. O pilar da fase: quanto mais
    /// perto do Coração, menos som (o volume de cada zona do Ninho cai).
    /// </summary>
    public class AmbienceZone : MonoBehaviour
    {
        static readonly List<AmbienceZone> s_all = new List<AmbienceZone>();
        static AmbiencePlayer s_player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_all.Clear(); s_player = null; }   // Play sem recarregar o domínio

        [SerializeField] string loop = "amb_floresta_viva";
        [SerializeField, Range(0f, 2f)] float volume = 1f;
        [Tooltip("Área (mundo)")]
        [SerializeField] Rect area;
        [Tooltip("Sons pontuais desta sala (ids do Sfx), um de cada vez")]
        [SerializeField] string[] spots = new string[0];
        [Tooltip("Intervalo entre os sons pontuais (s)")]
        [SerializeField] Vector2 spotGap = new Vector2(9f, 20f);

        public string Loop => loop;
        public float Volume => volume;
        public Rect Area => area;
        public string[] Spots => spots;
        public Vector2 SpotGap => spotGap;

        public void Configure(string loopId, float vol, Rect worldArea, string[] spotIds)
        {
            loop = loopId; volume = vol; area = worldArea; spots = spotIds ?? new string[0];
        }

        void OnEnable()
        {
            if (!s_all.Contains(this)) s_all.Add(this);
            if (s_player == null && Application.isPlaying) s_player = new GameObject("[Ambiência]").AddComponent<AmbiencePlayer>();
        }

        void OnDisable() => s_all.Remove(this);

        /// <summary>A zona que vale num ponto: a menor que o contém. Regra pura sobre a lista dada.</summary>
        public static int Pick(IList<Rect> areas, Vector2 p)
        {
            int best = -1;
            float size = float.MaxValue;
            for (int i = 0; i < areas.Count; i++)
            {
                if (!areas[i].Contains(p)) continue;
                float s = areas[i].width * areas[i].height;
                if (s < size) { size = s; best = i; }
            }
            return best;
        }

        internal static AmbienceZone At(Vector2 p)
        {
            var rects = new List<Rect>(s_all.Count);
            foreach (var z in s_all) rects.Add(z != null ? z.area : Rect.zero);
            int i = Pick(rects, p);
            return i >= 0 ? s_all[i] : null;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.9f, 0.6f, 0.4f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
#endif
    }

    /// <summary>Toca a ambiência da zona do gato (um por cena, criado pela primeira <see cref="AmbienceZone"/>).</summary>
    public class AmbiencePlayer : MonoBehaviour
    {
        /// <summary>Tempo da troca de uma ambiência para outra (s).</summary>
        public const float Crossfade = 2f;

        SfxLoop _current;
        string _currentId;
        float _currentVolume;
        float _nextSpot;

        void Update()
        {
            var cat = PlayerHealth.Active;
            Vector2 p = cat != null ? (Vector2)cat.transform.position : (Camera.main != null ? (Vector2)Camera.main.transform.position : Vector2.zero);
            var zone = AmbienceZone.At(p);
            string want = zone != null ? zone.Loop : null;
            float vol = zone != null ? zone.Volume : 0f;
            if (want != _currentId)
            {
                if (_current != null) _current.Stop(Crossfade);
                _current = string.IsNullOrEmpty(want) ? null : Sfx.Loop(want, null, vol);
                _currentId = want;
                _currentVolume = vol;
            }
            else if (_current != null && !Mathf.Approximately(_currentVolume, vol))
            {
                _currentVolume = Mathf.MoveTowards(_currentVolume, vol, Time.unscaledDeltaTime * 0.5f);   // o Ninho vai ficando quieto
                _current.Volume = _currentVolume;
            }

            // os pontuais: de longe, de um lado da tela
            if (zone == null || zone.Spots.Length == 0) return;
            if (Time.time < _nextSpot) return;
            if (_nextSpot > 0f)
            {
                string id = zone.Spots[Random.Range(0, zone.Spots.Length)];
                Vector2 at = p + new Vector2(Random.value < 0.5f ? -12f : 12f, Random.Range(1f, 6f));
                Sfx.Play(id, at);
            }
            _nextSpot = Time.time + Random.Range(zone.SpotGap.x, zone.SpotGap.y);
        }

        void OnDestroy()
        {
            if (_current != null) _current.Stop(0.2f);
        }
    }
}
