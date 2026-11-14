using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Regras puras dos inimigos da Floresta (EditMode). Base: Docs/Design/floresta-bestiario.md, com os ajustes do
    /// usuário de 13/11/2026: o Galho Seco fica parado e é alto (bloqueia a passagem); o Broto solta morcegos; a Planta
    /// estica o pescoço e pega longe; o Morcego persegue.
    /// <para>F4 (14/11/2026): variante <b>infectada</b> de cada um (parâmetro <c>infected</c>; o componente
    /// <see cref="Infected"/> liga a variante no objeto). Mesma IA, números e paleta diferentes.</para>
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

        // infectado (bestiário): aviso mais curto, menos tempo preso, um pouco mais de vida, nuvem tóxica ao morrer
        public const float BranchWindupInfected = 0.4f;
        public const float BranchStuckInfected = 0.7f;
        public const int BranchHealthInfected = 7;
        /// <summary>Nuvem tóxica do Galho Seco infectado ao morrer: 2 × 2 tiles por 2 s.</summary>
        public const float BranchCloudSize = 2f, BranchCloudTime = 2f;

        public static float BranchWindupFor(bool infected) => infected ? BranchWindupInfected : BranchWindup;
        public static float BranchStuckFor(bool infected) => infected ? BranchStuckInfected : BranchStuck;
        public static int BranchHealthFor(bool infected) => infected ? BranchHealthInfected : BranchHealth;

        /// <summary>
        /// O Galho Seco infectado disfarçado pisca os olhos verdes de vez em quando (dá para notar): acesos por
        /// <see cref="BranchBlinkOn"/> s a cada <see cref="BranchBlinkEvery"/> s.
        /// </summary>
        public const float BranchBlinkEvery = 2.5f, BranchBlinkOn = 0.25f;
        public static bool BranchEyesLit(float time) => Mathf.Repeat(time, BranchBlinkEvery) < BranchBlinkOn;

        /// <summary>
        /// Próximo estado do Galho Seco. <paramref name="playerDist"/> = distância horizontal até o gato na mesma altura
        /// (negativa = longe ou em outra altura). <paramref name="timer"/> é o tempo que falta no estado.
        /// </summary>
        public static BranchState StepBranch(BranchState s, ref float timer, float dt, float playerDist, bool playerHidden, bool infected = false)
        {
            bool near = playerDist >= 0f;
            switch (s)
            {
                case BranchState.Disguised:
                    // finge ser tronco até o gato chegar perto sem estar escondido
                    return near && playerDist <= BranchWakeRange && !playerHidden ? BranchState.Awake : BranchState.Disguised;
                case BranchState.Awake:
                    if (near && playerDist <= BranchAttackRange) { timer = BranchWindupFor(infected); return BranchState.Windup; }
                    return BranchState.Awake;
                case BranchState.Windup:
                    timer -= dt;
                    if (timer > 0f) return BranchState.Windup;
                    timer = BranchStuckFor(infected); // bateu: o braço fica preso no chão
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

        // infectada (bestiário): alcance maior, aviso e recuo mais curtos, esporos venenosos ao ser atingida
        public const float PlantReachInfected = 7f;
        public const float PlantWindupInfected = 0.45f;
        public const float PlantRetractInfected = 0.5f;
        public const int PlantHealthInfected = 5;
        /// <summary>Esporos: nuvem de 1,5 tile (de lado) por 1,5 s; uma nuvem de cada vez.</summary>
        public const float PlantSporeSize = 1.5f, PlantSporeTime = 1.5f;

        public static float PlantReachFor(bool infected) => infected ? PlantReachInfected : PlantReach;
        public static float PlantWindupFor(bool infected) => infected ? PlantWindupInfected : PlantWindup;
        public static float PlantRetractFor(bool infected) => infected ? PlantRetractInfected : PlantRetract;
        public static int PlantHealthFor(bool infected) => infected ? PlantHealthInfected : PlantHealth;

        /// <summary>Próximo estado da Planta (a mordida acontece durante o bote e a espera lá na ponta).</summary>
        public static PlantState StepPlant(PlantState s, ref float timer, float dt, bool playerInRange, bool infected = false)
        {
            switch (s)
            {
                case PlantState.Idle:
                    if (!playerInRange) return PlantState.Idle;
                    timer = PlantWindupFor(infected);
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
                    timer = PlantRetractFor(infected);
                    return PlantState.Retract;
                default:
                    timer -= dt;
                    return timer > 0f ? PlantState.Retract : PlantState.Idle;
            }
        }

        /// <summary>Quanto do pescoço está esticado (0 = cabeça na base, 1 = lá na ponta).</summary>
        public static float PlantExtension(PlantState s, float timer, bool infected = false)
        {
            switch (s)
            {
                case PlantState.Lunge: return 1f - Mathf.Clamp01(timer / PlantLunge);
                case PlantState.Hold: return 1f;
                case PlantState.Retract: return Mathf.Clamp01(timer / PlantRetractFor(infected));
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

        // infectada (bestiário): vê de mais longe, fica menos tempo esticada e no buraco; a mordida envenena
        public const float SnakeTriggerInfected = 4f;
        public const float SnakeStretchedInfected = 0.4f;
        public const float SnakeRetreatInfected = 1f;
        public const int SnakeHealthInfected = 4;

        public static float SnakeTriggerFor(bool infected) => infected ? SnakeTriggerInfected : SnakeTrigger;
        public static float SnakeStretchedFor(bool infected) => infected ? SnakeStretchedInfected : SnakeStretched;
        public static float SnakeRetreatFor(bool infected) => infected ? SnakeRetreatInfected : SnakeRetreat;
        public static int SnakeHealthFor(bool infected) => infected ? SnakeHealthInfected : SnakeHealth;

        /// <summary>
        /// Próximo estado da Serpente (Docs/Design/floresta-bestiario.md): escondida até o gato chegar a 3 tiles sem estar
        /// escondido na folhagem; chocalho (0,4 s), bote de 3 tiles, fica esticada 0,6 s (pogo e ponto fraco) e volta para
        /// o buraco por 1,5 s (invulnerável). <paramref name="playerDist"/> negativo = longe ou em outra altura.
        /// </summary>
        public static SnakeState StepSnake(SnakeState s, ref float timer, float dt, float playerDist, bool playerHidden, bool infected = false)
        {
            switch (s)
            {
                case SnakeState.Hidden:
                    if (playerDist < 0f || playerDist > SnakeTriggerFor(infected) || playerHidden) return SnakeState.Hidden;
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
                    timer = SnakeStretchedFor(infected);
                    return SnakeState.Stretched;
                case SnakeState.Stretched:
                    timer -= dt;
                    if (timer > 0f) return SnakeState.Stretched;
                    timer = SnakeRetreatFor(infected);
                    return SnakeState.Retreat;
                default:
                    timer -= dt;
                    return timer > 0f ? SnakeState.Retreat : SnakeState.Hidden;
            }
        }

        /// <summary>Quanto o corpo saiu do buraco (0 = escondida, 1 = bote inteiro, 3 tiles).</summary>
        public static float SnakeExtension(SnakeState s, float timer, bool infected = false)
        {
            switch (s)
            {
                case SnakeState.Strike: return 1f - Mathf.Clamp01(timer / SnakeStrike);
                case SnakeState.Stretched: return 1f;
                // volta deslizando para o buraco (antes sumia de uma vez e reaparecia no buraco)
                case SnakeState.Retreat: return 1f - Mathf.Clamp01((SnakeRetreatFor(infected) - timer) / SnakeRetract);
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

        // infectado (bestiário): mais vida, acorda com barulho menor e de mais longe, voa mais rápido em zigue-zague e
        // volta logo depois de encostar (as "investidas seguidas")
        public const int BatHealthInfected = 3;
        public const float BatNoiseToWake = 0.3f, BatNoiseToWakeInfected = 0.2f;
        public const float BatWakeRangeInfected = 9f;
        public const float BatSpeedInfected = 5.5f;
        public const float BatRecoilInfected = 0.35f;
        /// <summary>Zigue-zague do infectado: tiles/s de lado, trocando de lado <see cref="BatZigzagRate"/> vezes por segundo.</summary>
        public const float BatZigzag = 3.5f, BatZigzagRate = 2.5f;

        public static int BatHealthFor(bool infected) => infected ? BatHealthInfected : BatHealth;
        public static float BatNoiseFor(bool infected) => infected ? BatNoiseToWakeInfected : BatNoiseToWake;
        /// <summary>Alcance de acordar: o infectado vê 9/7 mais longe que o normal daquele lugar.</summary>
        public static float BatWakeRangeFor(bool infected, float normal = BatWakeRange) =>
            infected ? normal * (BatWakeRangeInfected / BatWakeRange) : normal;
        public static float BatSpeedFor(bool infected) => infected ? BatSpeedInfected : BatSpeed;
        public static float BatRecoilFor(bool infected) => infected ? BatRecoilInfected : BatRecoil;

        /// <summary>Lado do zigue-zague (+1/-1) no instante <paramref name="time"/>: troca em degraus, não em seno.</summary>
        public static int BatZigzagSide(float time) => ((int)Mathf.Floor(time * BatZigzagRate) & 1) == 0 ? 1 : -1;

        /// <summary>Acorda com o gato perto (agachado, só bem perto) ou com um barulho.</summary>
        public static bool BatWakes(float dist, bool crouched, bool heardNoise, float range = BatWakeRange) =>
            heardNoise || dist <= (crouched ? range * (BatWakeRangeCrouched / BatWakeRange) : range);

        /// <summary>Próximo estado do Morcego. <paramref name="hitPlayer"/> = encostou no gato neste passo.</summary>
        public static BatState StepBat(BatState s, ref float timer, float dt, float dist, bool crouched, bool heardNoise, bool hitPlayer, bool atPerch, float wakeRange = BatWakeRange, bool infected = false)
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
                    if (hitPlayer) { timer = BatRecoilFor(infected); return BatState.Recoil; }
                    return dist > BatGiveUpRange ? BatState.Return : BatState.Chase;
                case BatState.Recoil:
                    timer -= dt;
                    return timer > 0f ? BatState.Recoil : BatState.Chase;
                default:
                    if (dist <= BatWakeRange) return BatState.Chase;
                    return atPerch ? BatState.Sleep : BatState.Return;
            }
        }

        // ---------------------------------------------------------------- Cacto Flutuante infectado

        /// <summary>Ciclo dos disparos (s): incha nos últimos <see cref="CactusSwell"/> s e solta 4 espinhos em cruz.</summary>
        public const float CactusShotEvery = 3f, CactusSwell = 0.5f;
        public const float CactusSpikeRange = 5f, CactusSpikeSpeed = 8f;
        /// <summary>Tremida da trajetória (tiles).</summary>
        public const float CactusJitter = 0.5f;

        /// <summary>Inchando (aviso do disparo) com <paramref name="sinceShot"/> s desde o último disparo.</summary>
        public static bool CactusSwelling(float sinceShot) => sinceShot >= CactusShotEvery - CactusSwell && sinceShot < CactusShotEvery;

        /// <summary>Hora de disparar: passou o ciclo inteiro desde o último disparo.</summary>
        public static bool CactusFires(float sinceShot) => sinceShot >= CactusShotEvery;

        /// <summary>As 4 direções do disparo em cruz.</summary>
        public static Vector2 CactusSpikeDir(int i)
        {
            switch (i & 3)
            {
                case 0: return Vector2.up;
                case 1: return Vector2.right;
                case 2: return Vector2.down;
                default: return Vector2.left;
            }
        }

        /// <summary>
        /// Tremida do cacto infectado (até <see cref="CactusJitter"/> tile em cada eixo), suave: ruído de Perlin.
        /// <paramref name="seed"/> separa um cacto do outro.
        /// </summary>
        public static Vector2 CactusJitterOffset(float time, float seed)
        {
            float x = Mathf.PerlinNoise(seed, time * 1.3f) * 2f - 1f;
            float y = Mathf.PerlinNoise(time * 1.3f, seed + 17.3f) * 2f - 1f;
            return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f)) * CactusJitter;
        }

        // ---------------------------------------------------------------- nuvem tóxica (Galho Seco e Planta infectados)

        /// <summary>
        /// Cada nuvem envenena uma vez só (não reinicia o veneno nem o deixa letal sozinha: consequência sem punição
        /// excessiva). O gato está dentro se o centro dele cai no retângulo.
        /// </summary>
        public static bool CloudPoisons(bool alreadyPoisoned, Rect cloud, Vector2 catCenter) => !alreadyPoisoned && cloud.Contains(catCenter);
    }
}
