using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Audio
{
    /// <summary>
    /// Área retangular com faixa própria no lugar da música da fase (Floresta: ruínas, trecho corrompido, ninho).
    /// Combate, furtividade, chefe e santuário continuam vencendo (ver <see cref="MusicRules.Pick"/>). A cena precisa
    /// ter uma faixa para o momento escolhido no <see cref="MusicDirector"/>; sem faixa, segue a que já toca.
    /// </summary>
    public class MusicArea : MonoBehaviour
    {
        static readonly List<MusicArea> s_all = new List<MusicArea>();

        [SerializeField] MusicCue cue = MusicCue.Ruins;
        [Tooltip("Tamanho da área (tiles), centrada neste objeto")]
        [SerializeField] Vector2 size = new Vector2(30f, 17f);

        public MusicCue Cue => cue;
        public Rect Area => new Rect((Vector2)transform.position - size * 0.5f, size);

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }
        void OnDisable() => s_all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_all.Clear(); // Play sem recarregar o domínio

        /// <summary>Para o setup do editor.</summary>
        public void Configure(MusicCue c, Vector2 s) { cue = c; size = s; }

        /// <summary>A faixa da área que contém <paramref name="point"/> (a última criada vence). None = nenhuma.</summary>
        public static MusicCue CueAt(Vector2 point)
        {
            for (int i = s_all.Count - 1; i >= 0; i--)
                if (s_all[i] != null && s_all[i].Area.Contains(point)) return s_all[i].cue;
            return MusicCue.None;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 0.6f, 1f, 0.35f);
            Gizmos.DrawWireCube(transform.position, size);
        }
#endif
    }
}
