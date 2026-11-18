using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Regras da Isyathy embaixo da terra no Ninho (pedido do usuário, 17/11/2026: "em alguns momentos a tela vai tremer
    /// e ela vai sair do chão para nos atacar, como a cobra menor dando o bote, e voltar para a terra"). Funções puras:
    /// testadas em Tests/EditMode/NestBurrowTests.
    /// <list type="bullet">
    /// <item>De <see cref="CooldownMin"/> a <see cref="CooldownMax"/> s entre um bote e outro (o 1º, <see cref="FirstDelay"/>
    /// s depois do despertar), só com o gato no chão de uma das salas E1–E3 e longe de santuário.</item>
    /// <item>O aviso (<see cref="WarnTime"/> s): a tela treme e o chão racha num ponto a <see cref="SpotOffset"/> tiles do
    /// gato (de preferência para onde ele vai). Dá tempo de recuar, pular ou passar com dash.</item>
    /// <item>O bote: sobe do buraco e se arma inclinada para trás (<see cref="RisePath"/>), mira e avança no ponto onde o
    /// gato estava (alcance <see cref="Reach"/>), fecha a boca e volta para a terra. A mordida tira
    /// <see cref="Damage"/> (um coração).</item>
    /// </list>
    /// O corpo segue a cabeça pelo mesmo caminho (as fatias ficam atrás dela no trilho): sobe, curva, volta.
    /// </summary>
    public static class BurrowRules
    {
        // ------------------------------------------------------------ o corpo

        /// <summary>Comprimento do corpo (tiles, do pescoço à ponta do rabo).</summary>
        public const float BodyLength = 22f;
        /// <summary>Distância entre as fatias (tiles): 3 px, o tubo fica liso.</summary>
        public const float DiscSpacing = 3f / 16f;

        /// <summary>O raio (px) a <paramref name="d"/> tiles do pescoço: grosso logo atrás da cabeça, afinando até o rabo.</summary>
        public static float Radius(float d) => Radius(d, BodyLength);

        /// <summary>O mesmo, num corpo de <paramref name="length"/> tiles (a Isyathy da luta é mais comprida que a do Ninho).</summary>
        public static float Radius(float d, float length)
        {
            if (d < 0f) d = 0f;
            if (d < 6f) return 12f + 3f * d / 6f;
            float t = Mathf.Clamp01((d - 6f) / (length - 6f));
            return Mathf.Max(2.5f, 15f - 12f * Mathf.Pow(t, 1.4f));
        }

        /// <summary>A onda que corre pelo corpo (tiles de lado a lado), presa no pescoço.</summary>
        public static float Wave(float d, float amplitude, float phase)
        {
            float pin = Mathf.Clamp01(d / 2.5f);
            return amplitude * pin * Mathf.Sin(2f * Mathf.PI * d / 5f - phase);
        }

        // ------------------------------------------------------------ o tempo

        // 17/11/2026 (pedido do usuário: "dobre o número de aparições"): era 4 s e 6–10 s
        // 17/11/2026, de novo ("ela aparece só 2 vezes no caminho; faça 4 a 5"): 2,5–4 s, contados desde que ela entra na terra
        // 17/11/2026, o bote mais lento (+2 s por ciclo): 2–3,2 s, para manter os 4 a 5 botes por caminho
        public const float FirstDelay = 2f, CooldownMin = 2f, CooldownMax = 3.2f;
        /// <summary>Com o gato pendurado ou no ar, ela sai do chão até esta distância (tiles) embaixo dele.</summary>
        public const float MaxBelow = 7f;
        // 17/11/2026 (pedido do usuário: "o bote está muito rápido"): era 1,1 / 0,32 / 0,45 / 0,17 / 0,3 / 0,6 s. Agora dá
        // para ver a subida, a mira e o avanço; o bote ainda é o momento rápido, mas legível.
        public const float WarnTime = 1.3f, RiseTime = 0.6f, AimTime = 0.9f, LungeTime = 0.3f, BiteHold = 0.55f, RetractTime = 0.9f;
        public const float CraterFade = 1.6f;

        // ------------------------------------------------------------ o lugar e o bote

        public const float SpotOffset = 3.5f, SpotFar = 5f;
        public const float RearHeight = 3.2f, RearLean = 0.9f, Reach = 6.5f, Overshoot = 0.7f, MinAbove = 0.6f;
        /// <summary>Quão perto (tiles) do meio do gato a ponta do focinho precisa chegar para morder.</summary>
        public const float HitRadius = 1f;
        public const int Damage = 2;
        /// <summary>Perto de um santuário (tiles), ela não ataca: o gato salva em paz.</summary>
        public const float SafeRadius = 4.5f;
        /// <summary>Meia largura (tiles) da boca do buraco: abaixo do chão, ali dentro, o corpo some.</summary>
        public const float HoleHalfWidth = 1.7f;
        /// <summary>A profundidade (tiles) de onde ela vem, embaixo do buraco.</summary>
        public const float Depth = 10f;

        /// <summary>Os pontos que ela tenta, em ordem: na frente do gato (para onde ele vai), atrás, e mais longe.</summary>
        public static float[] Offsets(int dir)
        {
            int s = dir >= 0 ? 1 : -1;
            return new[] { s * SpotOffset, -s * SpotOffset, s * SpotFar, -s * SpotFar, s * 2.5f, -s * 2.5f, s * 6.5f, -s * 6.5f };
        }

        public static bool InSafeZone(Vector2 cat, Vector2[] safe)
        {
            if (safe == null) return false;
            foreach (var p in safe) if (Vector2.Distance(p, cat) < SafeRadius) return true;
            return false;
        }

        /// <summary>
        /// O caminho da subida: lá do fundo, a boca do buraco, e a cabeça armada inclinada para longe do gato
        /// (<paramref name="side"/>: 1 = o gato à direita do buraco).
        /// </summary>
        public static Vector2[] RisePath(Vector2 hole, int side) => RisePath(hole, side, Vector2.zero);

        /// <summary>
        /// A subida em S (o pescoço sai do buraco para o lado do gato, dobra e se arma inclinado para trás), com a cabeça
        /// deslocada de <paramref name="headOffset"/> (ela acompanha o gato e respira enquanto mira).
        /// </summary>
        public static Vector2[] RisePath(Vector2 hole, int side, Vector2 headOffset)
        {
            int s = side >= 0 ? 1 : -1;
            return new[]
            {
                new Vector2(hole.x, hole.y - Depth), new Vector2(hole.x, hole.y - 2f), hole,
                new Vector2(hole.x + s * 0.45f, hole.y + RearHeight * 0.4f),
                new Vector2(hole.x - s * RearLean * 0.5f, hole.y + RearHeight * 0.75f) + headOffset * 0.4f,
                new Vector2(hole.x - s * RearLean, hole.y + RearHeight) + headOffset,
            };
        }

        /// <summary>O bote: um arco (passa um pouco por cima) da cabeça armada até o alvo.</summary>
        public static Vector2[] LungeArc(Vector2 rear, Vector2 target, int steps = 6)
        {
            Vector2 mid = (rear + target) * 0.5f + Vector2.up * Mathf.Min(1.2f, Vector2.Distance(rear, target) * 0.15f);
            var pts = new Vector2[steps];
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                pts[i - 1] = (1 - t) * (1 - t) * rear + 2 * (1 - t) * t * mid + t * t * target;
            }
            return pts;
        }

        // ------------------------------------------------------------ curvas (nada de quina no trilho)

        /// <summary>Quantos pedaços cada trecho do trilho vira ao alisar.</summary>
        public const int SmoothSteps = 6;

        /// <summary>
        /// O trilho alisado (Catmull-Rom): passa por todos os pontos, sem quinas. O ponto <c>i</c> do original fica no
        /// índice <c>i * SmoothSteps</c> do alisado.
        /// </summary>
        public static Vector2[] Smooth(Vector2[] p)
        {
            if (p == null || p.Length < 3) return p;
            var list = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < p.Length - 1; i++)
            {
                Vector2 p0 = i == 0 ? p[0] - (p[1] - p[0]) : p[i - 1], p1 = p[i], p2 = p[i + 1];
                Vector2 p3 = i + 2 < p.Length ? p[i + 2] : p[i + 1] + (p[i + 1] - p[i]);
                for (int k = 0; k < SmoothSteps; k++) list.Add(CatmullRom(p0, p1, p2, p3, k / (float)SmoothSteps));
            }
            list.Add(p[p.Length - 1]);
            return list.ToArray();
        }

        public static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>A onda do corpo anda com a velocidade dela (rastejando de verdade): fase por segundo.</summary>
        public static float WaveSpeed(float headSpeed) => 1.5f + Mathf.Abs(headSpeed) * 1.6f;

        /// <summary>E fica maior quando ela se move rápido (o corpo se contorce), até um teto.</summary>
        public static float WaveAmplitude(float baseAmp, float headSpeed) => Mathf.Min(0.32f, baseAmp + Mathf.Abs(headSpeed) * 0.012f);

        /// <summary>Ease que passa um pouco do ponto e volta (a cabeça levantando, o bote).</summary>
        public static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float EaseInOut(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        /// <summary>
        /// Onde o bote termina: o meio do gato no fim da mira, até <see cref="Reach"/> da cabeça armada, nunca abaixo de
        /// <see cref="MinAbove"/> do chão, e um pouco além (o bote passa do ponto).
        /// </summary>
        public static Vector2 LungeTarget(Vector2 rear, Vector2 cat, float surfaceY)
        {
            Vector2 d = cat - rear;
            if (d.sqrMagnitude < 0.01f) d = Vector2.right;
            if (d.magnitude > Reach) d = d.normalized * Reach;
            Vector2 t = rear + d + d.normalized * Overshoot;
            t.y = Mathf.Max(t.y, surfaceY + MinAbove);
            return t;
        }

        public static bool Bites(Vector2 snout, Vector2 cat)
        {
            return Vector2.Distance(snout, cat) <= HitRadius;
        }

        public static float EaseOut(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
        public static float EaseIn(float t) { t = Mathf.Clamp01(t); return t * t; }

        // ------------------------------------------------------------ o trilho (polilinha)

        public static float Length(Vector2[] path)
        {
            float l = 0f;
            for (int i = 1; i < path.Length; i++) l += Vector2.Distance(path[i - 1], path[i]);
            return l;
        }

        /// <summary>O ponto a <paramref name="s"/> tiles do começo; fora das pontas, continua reto (o fundo da terra).</summary>
        public static Vector2 PointAt(Vector2[] path, float s)
        {
            if (path == null || path.Length == 0) return Vector2.zero;
            if (path.Length == 1) return path[0];
            if (s <= 0f) return path[0] + (path[0] - path[1]).normalized * -s;
            for (int i = 1; i < path.Length; i++)
            {
                float seg = Vector2.Distance(path[i - 1], path[i]);
                if (s <= seg) return seg < 1e-5f ? path[i] : Vector2.Lerp(path[i - 1], path[i], s / seg);
                s -= seg;
            }
            int n = path.Length;
            return path[n - 1] + (path[n - 1] - path[n - 2]).normalized * s;
        }

        /// <summary>A direção do trilho (para a frente) a <paramref name="s"/> tiles do começo.</summary>
        public static Vector2 DirectionAt(Vector2[] path, float s)
        {
            if (path == null || path.Length < 2) return Vector2.right;
            if (s <= 0f) return (path[1] - path[0]).normalized;
            for (int i = 1; i < path.Length; i++)
            {
                float seg = Vector2.Distance(path[i - 1], path[i]);
                if (s <= seg && seg > 1e-5f) return (path[i] - path[i - 1]) / seg;
                s -= seg;
            }
            int n = path.Length;
            return (path[n - 1] - path[n - 2]).normalized;
        }

        /// <summary>A distância no trilho até o ponto <paramref name="index"/> (o fim do trecho que termina nele).</summary>
        public static float LengthTo(Vector2[] path, int index)
        {
            float l = 0f;
            for (int i = 1; i <= index && i < path.Length; i++) l += Vector2.Distance(path[i - 1], path[i]);
            return l;
        }
    }
}
