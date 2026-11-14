using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Regras puras dos inimigos da Floresta (EditMode). Base: Docs/Design/floresta-bestiario.md, com os ajustes do
    /// usuário de 13/11/2026: o Galho Seco fica parado e é alto (bloqueia a passagem); o Broto solta morcegos; a Planta
    /// estica o pescoço e pega longe; o Morcego persegue.
    /// </summary>
    public static class ForestEnemyRules
    {
        // ---------------------------------------------------------------- Galho Seco (parado e alto)

        public enum BranchState { Disguised, Awake, Windup, Stuck }

        public const float BranchWakeRange = 3.5f;
        public const float BranchAttackRange = 2.5f;
        public const float BranchWindup = 0.6f;
        public const float BranchStuck = 1f;
        public const int BranchHealth = 6;
        /// <summary>Altura (tiles): mais que o pulo (4) e que agarrar a borda (5): não dá para passar por cima.</summary>
        public const float BranchHeight = 5.5f;

        /// <summary>
        /// Próximo estado do Galho Seco. <paramref name="playerDist"/> = distância horizontal até o gato na mesma altura
        /// (negativa = longe ou em outra altura). <paramref name="timer"/> é o tempo que falta no estado.
        /// </summary>
        public static BranchState StepBranch(BranchState s, ref float timer, float dt, float playerDist, bool playerHidden)
        {
            bool near = playerDist >= 0f;
            switch (s)
            {
                case BranchState.Disguised:
                    // finge ser tronco até o gato chegar perto sem estar escondido
                    return near && playerDist <= BranchWakeRange && !playerHidden ? BranchState.Awake : BranchState.Disguised;
                case BranchState.Awake:
                    if (near && playerDist <= BranchAttackRange) { timer = BranchWindup; return BranchState.Windup; }
                    return BranchState.Awake;
                case BranchState.Windup:
                    timer -= dt;
                    if (timer > 0f) return BranchState.Windup;
                    timer = BranchStuck;              // bateu: o braço fica preso no chão
                    return BranchState.Stuck;
                default:
                    timer -= dt;
                    return timer > 0f ? BranchState.Stuck : BranchState.Awake;
            }
        }

        /// <summary>Só aceita pogo com o braço preso no chão; e as costas levam o dobro.</summary>
        public static bool BranchVulnerable(BranchState s) => s == BranchState.Stuck;

        // ---------------------------------------------------------------- Broto (Galho Seco que solta morcegos)

        public const float SproutInterval = 4f;
        public const int SproutMaxBats = 2;
        public const float SproutRange = 10f;

        /// <summary>O Broto solta um morcego das folhas agora?</summary>
        public static bool SproutSpawns(float sinceLast, int batsAlive, bool playerNear) =>
            playerNear && batsAlive < SproutMaxBats && sinceLast >= SproutInterval;

        // ---------------------------------------------------------------- Planta Carnívora (bote de pescoço comprido)

        public enum PlantState { Idle, Windup, Lunge, Hold, Retract }

        /// <summary>Até onde a cabeça vai (tiles): "bem mais longe", o pescoço estica.</summary>
        public const float PlantReach = 6f;
        public const float PlantWindup = 0.6f;
        public const float PlantLunge = 0.18f;
        public const float PlantHold = 0.25f;
        public const float PlantRetract = 0.7f;
        public const int PlantHealth = 4;
        /// <summary>Compatibilidade: alcance antigo da mordida curta (o setup usa <see cref="PlantReach"/>).</summary>
        public const float PlantRange = PlantReach;

        /// <summary>Próximo estado da Planta (a mordida acontece durante o bote e a espera lá na ponta).</summary>
        public static PlantState StepPlant(PlantState s, ref float timer, float dt, bool playerInRange)
        {
            switch (s)
            {
                case PlantState.Idle:
                    if (!playerInRange) return PlantState.Idle;
                    timer = PlantWindup;
                    return PlantState.Windup;
                case PlantState.Windup:
                    timer -= dt;
                    if (timer > 0f) return PlantState.Windup;
                    timer = PlantLunge;
                    return PlantState.Lunge;
                case PlantState.Lunge:
                    timer -= dt;
                    if (timer > 0f) return PlantState.Lunge;
                    timer = PlantHold;
                    return PlantState.Hold;
                case PlantState.Hold:
                    timer -= dt;
                    if (timer > 0f) return PlantState.Hold;
                    timer = PlantRetract;
                    return PlantState.Retract;
                default:
                    timer -= dt;
                    return timer > 0f ? PlantState.Retract : PlantState.Idle;
            }
        }

        /// <summary>Quanto do pescoço está esticado (0 = cabeça na base, 1 = lá na ponta).</summary>
        public static float PlantExtension(PlantState s, float timer)
        {
            switch (s)
            {
                case PlantState.Lunge: return 1f - Mathf.Clamp01(timer / PlantLunge);
                case PlantState.Hold: return 1f;
                case PlantState.Retract: return Mathf.Clamp01(timer / PlantRetract);
                default: return 0f;
            }
        }

        /// <summary>A cabeça machuca no bote e lá na ponta.</summary>
        public static bool PlantBites(PlantState s) => s == PlantState.Lunge || s == PlantState.Hold;

        /// <summary>Pogo na cabeça só enquanto o pescoço volta (a cabeça cansada, flor turquesa).</summary>
        public static bool PlantVulnerable(PlantState s) => s == PlantState.Retract;

        /// <summary>
        /// Para onde a cabeça vai: na direção do gato, sem passar de 80° da frente da planta (não morde para trás da parede).
        /// Devolve o vetor unitário.
        /// </summary>
        public static Vector2 PlantAim(Vector2 forward, Vector2 toPlayer)
        {
            if (toPlayer.sqrMagnitude < 0.0001f) return forward.normalized;
            float a = Vector2.SignedAngle(forward, toPlayer);
            a = Mathf.Clamp(a, -80f, 80f);
            return (Vector2)(Quaternion.Euler(0f, 0f, a) * forward.normalized);
        }

        // ---------------------------------------------------------------- Serpente da Floresta (bote do buraco)

        public enum SnakeState { Hidden, Warn, Strike, Stretched, Retreat }

        /// <summary>Distância (tiles) em que o gato em pé, na frente, provoca o bote.</summary>
        public const float SnakeTrigger = 3f;
        public const float SnakeWarn = 0.4f;
        public const float SnakeStrike = 0.12f;
        public const float SnakeStretched = 0.6f;
        public const float SnakeRetreat = 1.5f;
        /// <summary>Quanto dura o corpo voltando para o buraco, no começo do recuo (s).</summary>
        public const float SnakeRetract = 0.35f;
        public const float SnakeLunge = 3f;
        public const int SnakeHealth = 3;
        /// <summary>Velocidade da serpente que rasteja (tiles/s) entre os botes.</summary>
        public const float SnakeCrawlSpeed = 1.6f;

        /// <summary>
        /// Próximo estado da Serpente (Docs/Design/floresta-bestiario.md): escondida até o gato chegar a 3 tiles sem estar
        /// escondido na folhagem; chocalho (0,4 s), bote de 3 tiles, fica esticada 0,6 s (pogo e ponto fraco) e volta para
        /// o buraco por 1,5 s (invulnerável). <paramref name="playerDist"/> negativo = longe ou em outra altura.
        /// </summary>
        public static SnakeState StepSnake(SnakeState s, ref float timer, float dt, float playerDist, bool playerHidden)
        {
            switch (s)
            {
                case SnakeState.Hidden:
                    if (playerDist < 0f || playerDist > SnakeTrigger || playerHidden) return SnakeState.Hidden;
                    timer = SnakeWarn;
                    return SnakeState.Warn;
                case SnakeState.Warn:
                    timer -= dt;
                    if (timer > 0f) return SnakeState.Warn;
                    timer = SnakeStrike;
                    return SnakeState.Strike;
                case SnakeState.Strike:
                    timer -= dt;
                    if (timer > 0f) return SnakeState.Strike;
                    timer = SnakeStretched;
                    return SnakeState.Stretched;
                case SnakeState.Stretched:
                    timer -= dt;
                    if (timer > 0f) return SnakeState.Stretched;
                    timer = SnakeRetreat;
                    return SnakeState.Retreat;
                default:
                    timer -= dt;
                    return timer > 0f ? SnakeState.Retreat : SnakeState.Hidden;
            }
        }

        /// <summary>Quanto o corpo saiu do buraco (0 = escondida, 1 = bote inteiro, 3 tiles).</summary>
        public static float SnakeExtension(SnakeState s, float timer)
        {
            switch (s)
            {
                case SnakeState.Strike: return 1f - Mathf.Clamp01(timer / SnakeStrike);
                case SnakeState.Stretched: return 1f;
                // volta deslizando para o buraco (antes sumia de uma vez e reaparecia no buraco)
                case SnakeState.Retreat: return 1f - Mathf.Clamp01((SnakeRetreat - timer) / SnakeRetract);
                default: return 0f;
            }
        }

        /// <summary>Morde durante o bote e esticada; aceita pogo e leva dano só esticada.</summary>
        public static bool SnakeBites(SnakeState s) => s == SnakeState.Strike || s == SnakeState.Stretched;
        public static bool SnakeVulnerable(SnakeState s) => s == SnakeState.Stretched;
        /// <summary>No buraco (escondida ou voltando) não leva dano; a que rasteja leva dano fora do bote também.</summary>
        public static bool SnakeInvulnerable(SnakeState s, bool crawls) =>
            s == SnakeState.Retreat || (!crawls && s == SnakeState.Hidden);

        // ---------------------------------------------------------------- Morcego Noturno (persegue)

        public enum BatState { Sleep, Wake, Chase, Recoil, Return }

        public const float BatWakeRange = 7f;            // detecta de mais longe (pedido do usuário)
        public const float BatWakeRangeCrouched = 3.5f;
        public const float BatWake = 0.4f;
        public const float BatSpeed = 4.5f;
        public const float BatRecoil = 0.7f;
        public const float BatGiveUpRange = 18f;
        public const int BatHealth = 2;

        /// <summary>Acorda com o gato perto (agachado, só bem perto) ou com um barulho.</summary>
        public static bool BatWakes(float dist, bool crouched, bool heardNoise, float range = BatWakeRange) =>
            heardNoise || dist <= (crouched ? range * (BatWakeRangeCrouched / BatWakeRange) : range);

        /// <summary>Próximo estado do Morcego. <paramref name="hitPlayer"/> = encostou no gato neste passo.</summary>
        public static BatState StepBat(BatState s, ref float timer, float dt, float dist, bool crouched, bool heardNoise, bool hitPlayer, bool atPerch, float wakeRange = BatWakeRange)
        {
            switch (s)
            {
                case BatState.Sleep:
                    if (!BatWakes(dist, crouched, heardNoise, wakeRange)) return BatState.Sleep;
                    timer = BatWake;
                    return BatState.Wake;
                case BatState.Wake:
                    timer -= dt;
                    return timer > 0f ? BatState.Wake : BatState.Chase;
                case BatState.Chase:
                    if (hitPlayer) { timer = BatRecoil; return BatState.Recoil; }
                    return dist > BatGiveUpRange ? BatState.Return : BatState.Chase;
                case BatState.Recoil:
                    timer -= dt;
                    return timer > 0f ? BatState.Recoil : BatState.Chase;
                default:
                    if (dist <= BatWakeRange) return BatState.Chase;
                    return atPerch ? BatState.Sleep : BatState.Return;
            }
        }
    }
}
