using System.Collections;
using SeteVidas.Combat;
using SeteVidas.Core;
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

    /// <summary>Como o gato sai da cena e como ele chega na outra (regras puras, EditMode).</summary>
    public static class TransitionRules
    {
        /// <summary>
        /// Direção da saída pela forma da passagem: mais alta que larga = passagem lateral (o gato sai andando para o
        /// lado dela); mais larga que alta = buraco ou topo (cai ou sobe). <paramref name="facing"/> desempata.
        /// </summary>
        public static Vector2 ExitDirection(Rect passage, Vector2 catFeet, int facing)
        {
            if (passage.height >= passage.width)
            {
                float dx = passage.center.x - catFeet.x;
                int side = Mathf.Abs(dx) < 0.05f ? (facing >= 0 ? 1 : -1) : (dx > 0f ? 1 : -1);
                return new Vector2(side, 0f);
            }
            return new Vector2(0f, passage.center.y < catFeet.y + 0.5f ? -1f : 1f);
        }

        /// <summary>Quanto o gato cai ao chegar caindo: até <paramref name="wanted"/>, sem bater no teto (margem de 2).</summary>
        public static float FallHeight(float wanted, float ceilingDistance)
        {
            if (ceilingDistance < 0f) return wanted;          // nada acima
            return Mathf.Clamp(ceilingDistance - 2f, 0f, wanted);
        }
    }

    /// <summary>
    /// Troca de sala/cena: escurece, carrega a cena, põe o gato na entrada (ou no santuário) e clareia. A vida atual
    /// atravessa a porta; morrer ou viajar por santuário chega de vida cheia. Na mesma cena, só teleporta.
    /// Melhorias 14/11/2026: pela passagem lateral o gato sai andando enquanto escurece e entra andando enquanto
    /// clareia; por um buraco, continua caindo (e tropeça no ar); chegando por uma entrada "de queda" (ex.: A1_queda,
    /// depois da ponte), ele despenca do alto, bate no chão, fica deitado um instante e levanta.
    /// </summary>
    public static class SceneTravel
    {
        public const float FadeOut = 0.3f;
        public const float FadeIn = 0.35f;
        /// <summary>Saindo andando: o tempo em que ele anda para fora enquanto a tela escurece.</summary>
        public const float WalkOutTime = 0.4f;
        /// <summary>Caindo por um buraco: escurece mais devagar (dá para ver a queda).</summary>
        public const float FallOutTime = 0.6f;
        /// <summary>Entrando andando: tempo andando sozinho enquanto clareia.</summary>
        public const float WalkInTime = 0.3f;
        /// <summary>Chegada de queda: altura de onde ele despenca (tiles), deitado depois (s).</summary>
        public const float FallInHeight = 9f, LieTime = 1.1f, GetUpTime = 0.7f;

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

        /// <summary>
        /// Passa pela porta: vai para a entrada <paramref name="entranceId"/> da cena <paramref name="scene"/>, com a vida
        /// atual. <paramref name="exitDirection"/>: (±1, 0) sai andando para o lado; (0, −1) sai caindo; zero = parado.
        /// </summary>
        public static void ToEntrance(string scene, string entranceId, PlayerHealth from, Vector2 exitDirection = default)
        {
            int hits = from != null ? from.CurrentHits : -1;
            Go(scene, new Arrival(ArrivalKind.Entrance, entranceId, hits), from, alreadyBlack: false, exitDirection);
        }

        /// <summary>Viagem rápida ou renascer: chega de pé no santuário, de vida cheia.</summary>
        public static void ToSanctuary(string scene, string sanctuaryId, PlayerHealth from, bool alreadyBlack = false)
        {
            Go(scene, new Arrival(ArrivalKind.Sanctuary, sanctuaryId, -1), from, alreadyBlack, Vector2.zero);
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

        static void Go(string scene, Arrival arrival, PlayerHealth from, bool alreadyBlack, Vector2 exitDirection)
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
            s_runner.StartCoroutine(Run(scene, same, arrival, from, alreadyBlack, exitDirection));
        }

        static IEnumerator Run(string scene, bool same, Arrival arrival, PlayerHealth from, bool alreadyBlack, Vector2 exitDir)
        {
            var input = from != null ? from.GetComponent<PlayerInputReader>() : null;
            object lockOwner = s_runner;
            if (input != null) input.SetLock(lockOwner, true);
            if (from != null) from.Shielded = true; // sem dano no escuro

            // ----- saída: anda para fora da tela, ou continua caindo (tropeçando no ar)
            bool walkOut = exitDir.x != 0f && !alreadyBlack;
            bool fallOut = exitDir.y < 0f && !alreadyBlack;
            var oldVisual = from != null ? from.GetComponentInChildren<PlayerVisual>() : null;
            if (walkOut && input != null) input.SetScriptedMove(Mathf.Sign(exitDir.x));
            if (fallOut && oldVisual != null) oldVisual.PlayAction("hurt", hold: true);

            var fader = ScreenFader.Active;
            float fadeOut = walkOut ? WalkOutTime : fallOut ? FallOutTime : FadeOut;
            if (!alreadyBlack && fader != null) yield return fader.FadeTo(1f, fadeOut);
            if (input != null) input.ClearScriptedMove();
            if (fallOut && oldVisual != null) oldVisual.StopAction();

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

            // ----- chegada (o gato da cena nova também fica sem controle e sem dano até clarear)
            var health = same ? from : PlayerHealth.Active;
            var newInput = health != null ? health.GetComponent<PlayerInputReader>() : null;
            if (newInput != null) newInput.SetLock(lockOwner, true);
            if (health != null) health.Shielded = true;

            yield return new WaitForFixedUpdate(); // o chão embaixo do gato já foi conferido
            fader = ScreenFader.Active;

            var entrance = arrival.Kind == ArrivalKind.Entrance ? RoomEntrance.Find(arrival.Id) : null;
            if (entrance != null && entrance.FallsIn && health != null)
                yield return FallIn(health, fader);
            else
            {
                var player = health != null ? health.GetComponent<PlayerController>() : null;
                bool walkIn = walkOut && entrance != null && player != null && player.IsGrounded && newInput != null;
                if (walkIn) newInput.SetScriptedMove(entrance.FacingDirection);
                if (fader != null)
                {
                    if (walkIn)
                    {
                        // anda sozinho no começo do clarão; o resto do clarão já é com o jogador parado
                        float t = 0f;
                        while (t < FadeIn)
                        {
                            t += Time.unscaledDeltaTime;
                            fader.SetAlpha(1f - Mathf.Clamp01(t / FadeIn));
                            if (t >= WalkInTime) newInput.ClearScriptedMove();
                            yield return null;
                        }
                        fader.SetAlpha(0f);
                    }
                    else yield return fader.FadeTo(0f, FadeIn);
                }
                else if (walkIn) yield return new WaitForSeconds(WalkInTime);
                if (newInput != null) newInput.ClearScriptedMove();
            }

            if (newInput != null) newInput.SetLock(lockOwner, false);
            if (health != null) health.Shielded = false;
            if (input != null && input != newInput) input.SetLock(lockOwner, false);
            s_busy = false;
        }

        /// <summary>
        /// Chegada de queda (a ponte caiu): o gato aparece no alto, despenca tropeçando enquanto a tela clareia, bate
        /// no chão (poeira, tremor), fica deitado um instante e levanta.
        /// </summary>
        static IEnumerator FallIn(PlayerHealth health, ScreenFader fader)
        {
            var player = health.GetComponent<PlayerController>();
            var visual = health.GetComponentInChildren<PlayerVisual>();
            if (player == null)
            {
                if (fader != null) yield return fader.FadeTo(0f, FadeIn);
                yield break;
            }
            Vector2 feet = player.Body.position;
            var hit = Physics2D.Raycast(feet + Vector2.up * 1.6f, Vector2.up, FallInHeight + 4f,
                LayerMask.GetMask(GameConstants.Layers.Ground));
            float h = TransitionRules.FallHeight(FallInHeight, hit.collider != null ? hit.distance + 1.6f : -1f);
            player.Teleport(feet + Vector2.up * h);
            player.Body.linearVelocity = new Vector2(0f, -4f);
            if (visual != null) visual.PlayAction("hurt", hold: true);
            if (fader != null) s_runner.StartCoroutine(fader.FadeTo(0f, 0.3f));

            yield return new WaitForFixedUpdate();
            float t = 0f;
            while (t < 3f && !player.IsGrounded)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (visual != null) visual.StopAction();
            // o baque: poeira e pedrinhas, a câmera treme (o som de pouso pesado vem do PlayerAudio)
            QuakeFx.Spawn(player.Body.position, 1.2f, 0.15f, 0.35f);
            CameraShake.Shake(3f, 0.2f);
            health.SetCollapsed(true);
            yield return new WaitForSeconds(LieTime);
            health.SetCollapsed(false, graceAfter: 0.5f); // o visual toca "get_up"
            yield return new WaitForSeconds(GetUpTime);
        }
    }

    /// <summary>Dono das corrotinas da troca de cena (sobrevive ao carregamento).</summary>
    public class SceneTravelRunner : MonoBehaviour { }
}
