using System.Collections;
using SeteVidas.Player;
using SeteVidas.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeteVidas.World
{
    /// <summary>Onde o gato aparece depois de uma troca de cena.</summary>
    public enum ArrivalKind { Entrance, Sanctuary }

    /// <summary>Chegada pendente: lida pelo <see cref="PlayerHealth"/> da cena nova no Start.</summary>
    public readonly struct Arrival
    {
        public readonly ArrivalKind Kind;
        public readonly string Id;
        /// <summary>Vida ao chegar (hits). Negativo = vida cheia.</summary>
        public readonly int Hits;

        public Arrival(ArrivalKind kind, string id, int hits)
        {
            Kind = kind;
            Id = id;
            Hits = hits;
        }
    }

    /// <summary>
    /// Troca de sala/cena da Floresta (F1): escurece, carrega a cena, põe o gato na entrada (ou no santuário) e clareia.
    /// A vida atual atravessa a porta; morrer ou viajar por santuário chega de vida cheia. Na mesma cena, só teleporta.
    /// </summary>
    public static class SceneTravel
    {
        public const float FadeOut = 0.3f;
        public const float FadeIn = 0.35f;

        static bool s_hasPending;
        static Arrival s_pending;
        static bool s_busy;
        static SceneTravelRunner s_runner;

        /// <summary>Uma troca está acontecendo (portas não disparam de novo, viagens esperam).</summary>
        public static bool IsTraveling => s_busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() // Play sem recarregar o domínio
        {
            s_hasPending = false;
            s_pending = default;
            s_busy = false;
            s_runner = null;
        }

        /// <summary>Passa pela porta: vai para a entrada <paramref name="entranceId"/> da cena <paramref name="scene"/>, com a vida atual.</summary>
        public static void ToEntrance(string scene, string entranceId, PlayerHealth from)
        {
            int hits = from != null ? from.CurrentHits : -1;
            Go(scene, new Arrival(ArrivalKind.Entrance, entranceId, hits), from, alreadyBlack: false);
        }

        /// <summary>Viagem rápida ou renascer: chega de pé no santuário, de vida cheia.</summary>
        public static void ToSanctuary(string scene, string sanctuaryId, PlayerHealth from, bool alreadyBlack = false)
        {
            Go(scene, new Arrival(ArrivalKind.Sanctuary, sanctuaryId, -1), from, alreadyBlack);
        }

        /// <summary>A cena nova pega a chegada pendente (só uma vez).</summary>
        public static bool TryConsume(out Arrival arrival)
        {
            arrival = s_pending;
            if (!s_hasPending) return false;
            s_hasPending = false;
            return true;
        }

        /// <summary>Lugar e direção de chegada já carregados na cena atual.</summary>
        public static bool TryResolve(in Arrival arrival, out Vector2 feet, out int facing)
        {
            feet = default;
            facing = 0;
            if (arrival.Kind == ArrivalKind.Entrance)
            {
                var e = RoomEntrance.Find(arrival.Id);
                if (e == null) return false;
                feet = e.SpawnPoint;
                facing = e.FacingDirection;
                return true;
            }
            var s = Sanctuary.Find(arrival.Id);
            if (s == null) return false;
            feet = s.SpawnPoint;
            return true;
        }

        static void Go(string scene, Arrival arrival, PlayerHealth from, bool alreadyBlack)
        {
            if (s_busy) return;
            if (string.IsNullOrEmpty(scene)) scene = SceneManager.GetActiveScene().name;
            bool same = scene == SceneManager.GetActiveScene().name;
            if (!same && !Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning($"[Sete Vidas] Cena \"{scene}\" não está no Build Settings: a porta não leva a lugar nenhum.");
                return;
            }
            if (s_runner == null)
            {
                var go = new GameObject("[SceneTravel]");
                Object.DontDestroyOnLoad(go);
                s_runner = go.AddComponent<SceneTravelRunner>();
            }
            s_busy = true;
            s_runner.StartCoroutine(Run(scene, same, arrival, from, alreadyBlack));
        }

        static IEnumerator Run(string scene, bool same, Arrival arrival, PlayerHealth from, bool alreadyBlack)
        {
            var input = from != null ? from.GetComponent<PlayerInputReader>() : null;
            object lockOwner = s_runner;
            if (input != null) input.SetLock(lockOwner, true);
            if (from != null) from.Shielded = true; // sem dano no escuro

            var fader = ScreenFader.Active;
            if (!alreadyBlack && fader != null) yield return fader.FadeTo(1f, FadeOut);

            if (same)
            {
                if (from != null) from.ArriveHere(arrival);
            }
            else
            {
                s_pending = arrival;
                s_hasPending = true;
                var op = SceneManager.LoadSceneAsync(scene);
                while (op != null && !op.isDone) yield return null;
                yield return null; // o Start da cena nova (PlayerHealth pega a chegada)
                fader = ScreenFader.Active;
                if (fader != null) fader.SetAlpha(1f);
                s_hasPending = false; // cena sem jogador: descarta
            }

            yield return null;
            fader = ScreenFader.Active;
            if (fader != null) yield return fader.FadeTo(0f, FadeIn);

            if (same)
            {
                if (input != null) input.SetLock(lockOwner, false);
                if (from != null) from.Shielded = false;
            }
            s_busy = false;
        }
    }

    /// <summary>Dono das corrotinas da troca de cena (sobrevive ao carregamento).</summary>
    public class SceneTravelRunner : MonoBehaviour { }
}
