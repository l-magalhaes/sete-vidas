using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.Abilities
{
    /// <summary>
    /// Regras puras do Caçador (F4b, Docs/Design/classe-cacador.md): dash, flecha com corda (gancho) e arco.
    /// Dash e gancho valem em qualquer classe depois do vínculo com a Vulpis (decisão do usuário, 14/11/2026);
    /// o arco, só com o Caçador. Números [PROPOSTA], a balancear no playtest.
    /// </summary>
    public static class HunterRules
    {
        /// <summary>Flag do vínculo com a Vulpis (dá a classe, o coração, o dash e o gancho).</summary>
        public const string BondFlag = "vinculo_vulpis";

        /// <summary>Dash e gancho liberados (vínculo feito ou a classe já está no save).</summary>
        public static bool MovesUnlocked(SaveData data) =>
            data != null && (data.HasFlag(BondFlag) || data.IsUnlocked(SeteVidas.Classes.CharacterClass.Hunter));

        // ---------------------------------------------------------------- dash

        /// <summary>Distância (tiles) e duração (s) do dash: generoso, como pediu o usuário.</summary>
        public const float DashDistance = 6f, DashTime = 0.2f;
        /// <summary>Intervalo mínimo entre dois dashes no chão (s).</summary>
        public const float DashCooldown = 0.3f;
        /// <summary>Velocidade que sobra no fim do dash (fração): o gato não para seco no ar.</summary>
        public const float DashCarry = 0.45f;

        public static float DashSpeed => DashDistance / DashTime;

        /// <summary>
        /// Pode começar um dash agora? No chão, só depois da recarga; no ar, só se ainda houver o dash do ar
        /// (<paramref name="airDashReady"/>), que volta ao tocar o chão, agarrar parede, chegar num gancho ou quicar.
        /// </summary>
        public static bool CanDash(bool grounded, bool airDashReady, float sinceLastDash) =>
            sinceLastDash >= DashCooldown && (grounded || airDashReady);

        /// <summary>Direção do dash: o lado apertado agora; sem direção, para onde o gato olha.</summary>
        public static int DashDirection(float moveX, int facing) =>
            moveX > 0.01f ? 1 : moveX < -0.01f ? -1 : (facing >= 0 ? 1 : -1);

        // ---------------------------------------------------------------- flecha com corda (gancho)

        /// <summary>Alcance da corda (tiles) e meia abertura do cone da mira automática (graus).</summary>
        public const float GrappleRange = 9f, GrappleCone = 30f;
        /// <summary>Velocidade da flecha da corda e do puxão (tiles/s).</summary>
        public const float GrappleShotSpeed = 40f, GrapplePull = 20f;
        /// <summary>Quanto o gato fica pendurado no gancho ao chegar (s), podendo pular ou dar dash.</summary>
        public const float GrappleHang = 0.4f;
        /// <summary>A que distância do gancho o puxão termina (tiles).</summary>
        public const float GrappleArrive = 0.6f;

        /// <summary>
        /// Direção da mira: o direcional em 8 direções; sem direção, para a frente. No chão, apontar para baixo vira
        /// para a frente (a corda não puxa para dentro do chão).
        /// </summary>
        public static Vector2 AimDirection(float moveX, float moveY, int facing, bool grounded)
        {
            int x = moveX > 0.3f ? 1 : moveX < -0.3f ? -1 : 0;
            int y = moveY > 0.3f ? 1 : moveY < -0.3f ? -1 : 0;
            if (grounded && y < 0) y = 0;
            if (x == 0 && y == 0) x = facing >= 0 ? 1 : -1;
            return new Vector2(x, y).normalized;
        }

        /// <summary>
        /// Nota de um gancho para a mira automática (menor = melhor; negativa = fora). Precisa estar no alcance e no
        /// cone; entre os válidos, ganha o mais alinhado com a mira, desempatado pela distância.
        /// </summary>
        public static float AnchorScore(Vector2 from, Vector2 aim, Vector2 anchor)
        {
            Vector2 d = anchor - from;
            float dist = d.magnitude;
            if (dist < 0.01f || dist > GrappleRange) return -1f;
            float angle = Vector2.Angle(aim, d);
            if (angle > GrappleCone) return -1f;
            return angle / GrappleCone + dist / GrappleRange * 0.5f;
        }

        /// <summary>Velocidade do puxão até o gancho (para quando chega).</summary>
        public static Vector2 PullVelocity(Vector2 from, Vector2 anchor)
        {
            Vector2 d = anchor - from;
            return d.magnitude <= GrappleArrive ? Vector2.zero : d.normalized * GrapplePull;
        }


        /// <summary>
        /// Velocidade (tiles/s), alcance (tiles) e intervalo (s) das flechas. Melhorias 3 (14/11/2026): atira mais
        /// rápido (0,35 → 0,2 s) e até 3 flechas no ar (antes, uma de cada vez).
        /// </summary>
        public const float ArrowSpeed = 22f, ArrowRange = 12f, ArrowCooldown = 0.35f; // melhorias 5: 0,2 → 0,35 (estava forte demais)
        public const int MaxArrowsInFlight = 2;
        /// <summary>Dano da flecha sem o arco montado (a adaga base 2, metade).</summary>
        public const int ArrowDamage = 1;
        /// <summary>
        /// Pedido de 14/11/2026: cada flecha dá 40% de um golpe de adaga (já com o bônus dos corações), no mínimo 1.
        /// O dano é inteiro: a fração que sobra passa para a próxima flecha (adaga 3 → flechas 1, 1, 1, 1, 2…).
        /// </summary>
        public const float ArrowDamageFraction = 0.4f; // melhorias 5: 0,5 → 0,4 (ainda forte)

        /// <summary>Dano desta flecha: 40% de <paramref name="daggerHit"/> (mínimo 1), guardando o resto em <paramref name="carry"/>.</summary>
        public static int ArrowHit(int daggerHit, ref float carry)
        {
            float total = Mathf.Max(0, daggerHit) * ArrowDamageFraction + carry;
            int hit = Mathf.Max(1, Mathf.FloorToInt(total + 1e-4f));
            carry = Mathf.Max(0f, total - hit);
            return hit;
        }
        /// <summary>Barulho do disparo (0..1): baixo, o arco é a arma silenciosa.</summary>
        public const float ArrowNoise = 0.15f;

        /// <summary>Pode atirar: passou o intervalo e ainda cabe flecha no ar.</summary>
        public static bool CanShoot(float sinceLastShot, int arrowsInFlight) =>
            sinceLastShot >= ArrowCooldown && arrowsInFlight < MaxArrowsInFlight;

        /// <summary>Direção da flecha: para cima com {subir}; senão para a frente (sem mira).</summary>
        public static Vector2 ArrowDirection(bool up, int facing) => up ? Vector2.up : new Vector2(facing >= 0 ? 1 : -1, 0f);

        // ---------------------------------------------------------------- mira do arco (melhorias 3)

        /// <summary>A mira anda de 15 em 15 graus (como a besta dos atiradores), de 45° para baixo até reto para cima.</summary>
        public const float AimStep = 15f, AimMaxDown = 45f, AimDeadZone = 0.3f;
        /// <summary>
        /// Mira assistida: sem apontar, pega o inimigo mais perto na frente do gato dentro de ±<see cref="AssistConeFree"/>°;
        /// apontando, só corrige até ±<see cref="AssistConeAimed"/>° da mira. Não trava: cada flecha decide sozinha.
        /// </summary>
        public const float AssistConeFree = 40f, AssistConeAimed = 20f;

        /// <summary>
        /// Ângulo da mira em graus, relativo à frente (0 = reto, 90 = para cima, negativo = para baixo), já de 15 em 15°.
        /// <paramref name="side"/> = para que lado sai (o lado do direcional; sem lado, para onde o gato olha).
        /// Sem direção: reto para a frente.
        /// </summary>
        public static float AimAngle(Vector2 stick, int facing, out int side)
        {
            side = stick.x > 0.2f ? 1 : stick.x < -0.2f ? -1 : (facing >= 0 ? 1 : -1);
            if (stick.magnitude < AimDeadZone) return 0f;
            float a = Mathf.Atan2(stick.y, Mathf.Abs(stick.x)) * Mathf.Rad2Deg;
            a = Mathf.Clamp(a, -AimMaxDown, 90f);
            return Mathf.Round(a / AimStep) * AimStep;
        }

        /// <summary>Direção da flecha pela mira (direcional ou analógico).</summary>
        public static Vector2 BowAim(Vector2 stick, int facing)
        {
            int side;
            float r = AimAngle(stick, facing, out side) * Mathf.Deg2Rad;
            var d = new Vector2(side * Mathf.Cos(r), Mathf.Sin(r));
            if (Mathf.Abs(d.x) < 1e-4f) d.x = 0f;
            return d.normalized;
        }

        /// <summary>
        /// Mira assistida: entre os alvos <paramref name="targets"/> (centro de cada inimigo), o mais perto que está na
        /// frente do gato (lado <paramref name="side"/>), no alcance e dentro do cone em volta da mira. Achou: a flecha
        /// vai nele; senão, vale a mira. <paramref name="aimed"/> = o jogador apontou (cone menor).
        /// </summary>
        public static Vector2 AssistAim(Vector2 from, Vector2 aim, int side, bool aimed, System.Collections.Generic.IList<Vector2> targets)
        {
            float cone = aimed ? AssistConeAimed : AssistConeFree;
            float best = float.MaxValue;
            Vector2 result = aim;
            if (targets == null) return result;
            for (int i = 0; i < targets.Count; i++)
            {
                Vector2 d = targets[i] - from;
                float dist = d.magnitude;
                if (dist < 0.3f || dist > ArrowRange) continue;
                if (d.x * side < -0.1f) continue;                       // atrás do gato: não
                if (Vector2.Angle(aim, d) > cone) continue;
                if (dist < best) { best = dist; result = d / dist; }
            }
            return result;
        }
    }
}
