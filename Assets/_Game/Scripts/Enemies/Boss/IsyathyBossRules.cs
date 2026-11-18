using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>Os golpes da Isyathy (refeitos em 17/11/2026: ela anda dentro da terra pelas quatro bordas da arena).</summary>
    public enum IsyathyMove
    {
        /// <summary>Sai do chão (num vão sem plataforma), mira e dá o bote no gato (meio coração). Fica com o queixo no chão.</summary>
        Lunge,
        /// <summary>Sai da parede na altura do gato e avança reto, atravessando meia arena (meio coração). Fica caída.</summary>
        WallStrike,
        /// <summary>Desce do teto em cima do gato (a sombra no chão avisa) e morde até o chão (um coração). Fica pendurada.</summary>
        CeilingDrop,
        /// <summary>Fase II: a cabeça sai de um vão e o rabo de outro (o corpo em arco embaixo do chão). Dá para bater nos dois lados.</summary>
        Arch,
        /// <summary>Fase II: sai da parede e cospe três bolas da praga em arco; cada uma vira uma poça de veneno no chão.</summary>
        Spit,
    }

    /// <summary>Os quatro lados da arena, na ordem em que ela anda por dentro da terra (sentido anti-horário).</summary>
    public enum ArenaSide { Floor, Right, Ceiling, Left }

    /// <summary>
    /// Regras da luta com a Isyathy, a Devoradora Silenciosa (F8; funções puras, testadas em Tests/EditMode/IsyathyBossTests).
    /// Refeita em 17/11/2026 (pedido do usuário, inspirado nos chefes-serpente que andam pelas bordas da sala):
    /// <list type="number">
    /// <item><b>Anda</b> dentro da terra pelas quatro bordas da arena (chão, parede, teto, parede): um montinho de terra corre
    /// pela superfície, rachando, a uma velocidade que dá para acompanhar (<see cref="TravelSpeed"/>). No chão, encostar
    /// no montinho fere.</item>
    /// <item><b>Avisa</b>: para num ponto, a superfície racha e brilha (<see cref="Warning"/>).</item>
    /// <item><b>Sai e ataca</b>, sempre com parte do corpo dentro da terra (nunca sai inteira). Às vezes a cabeça sai de um
    /// lado e o rabo de outro (<see cref="IsyathyMove.Arch"/>).</item>
    /// <item><b>Espera</b> um pouco, cansada (<see cref="Exposed"/>): é a hora de bater na cabeça (e no rabo, se estiver
    /// fora). Depois volta para a terra e anda de novo.</item>
    /// </list>
    /// Vida <see cref="MaxHealth"/>; fases em <see cref="PhaseTwoAt"/> e <see cref="PhaseThreeAt"/>. Ao cruzar cada uma, a
    /// praga a cobre (<see cref="ShellTime"/>, imune) e a Vulpis quebra a casca com a flecha forte (<see cref="StunTime"/>).
    /// "Quanto maior o aviso, maior o dano": meio coração (1) nos golpes de aviso curto, um coração (2) na descida do teto.
    /// </summary>
    public static class IsyathyBossRules
    {
        // 17/11/2026 (pedido do usuário: "triplique a vida, está muito fácil"): era 54
        // 18/11/2026 (pedido do usuário: "dobre a vida"): era 162 (e antes 54)
        public const int MaxHealth = 324;
        public const float PhaseTwoAt = 0.65f, PhaseThreeAt = 0.30f;

        public static int Phase(int health, int max)
        {
            float f = max <= 0 ? 0f : health / (float)max;
            return f > PhaseTwoAt ? 1 : f > PhaseThreeAt ? 2 : 3;
        }

        /// <summary>A vida cruzou um limiar da Vulpis (65% ou 30%) com este golpe.</summary>
        public static bool CrossedVulpisMark(int before, int after, int max)
        {
            foreach (float mark in new[] { PhaseTwoAt, PhaseThreeAt })
            {
                float m = mark * max;
                if (before > m && after <= m) return true;
            }
            return false;
        }

        // ------------------------------------------------------------ o que ela faz

        static readonly IsyathyMove[] PhaseOne = { IsyathyMove.Lunge, IsyathyMove.WallStrike, IsyathyMove.CeilingDrop };
        static readonly IsyathyMove[] PhaseTwo = { IsyathyMove.Lunge, IsyathyMove.WallStrike, IsyathyMove.CeilingDrop, IsyathyMove.Arch, IsyathyMove.Spit };

        /// <summary>O próximo golpe: sorteado entre os da fase (<paramref name="roll"/> 0..1), nunca o mesmo duas vezes seguidas.</summary>
        public static IsyathyMove Pick(int phase, IsyathyMove last, float roll)
        {
            var pool = phase <= 1 ? PhaseOne : PhaseTwo;
            int n = pool.Length;
            int i = Mathf.Clamp((int)(roll * n), 0, n - 1);
            for (int k = 0; k < n; k++)
            {
                var m = pool[(i + k) % n];
                if (m != last) return m;
            }
            return pool[0];
        }

        /// <summary>Na fase III ela emenda dois golpes (sai, ataca, volta e já sai de novo perto, sem andar a volta toda).</summary>
        // 18/11/2026 ("mais agressiva"): emenda em todas as fases: 2 golpes seguidos na fase I, 3 na II, 4 na III
        public static bool Chains(int phase, int chained) => chained < phase + 1;   // 18/11/2026 ("mais difícil"): 3 / 4 / 5 seguidos

        /// <summary>De que lado da arena sai o golpe (o Bote e o Arco, do chão; a Varredura e o Cuspe, da parede).</summary>
        public static ArenaSide SideOf(IsyathyMove m, float catX, Rect inside)
        {
            switch (m)
            {
                case IsyathyMove.WallStrike: return catX < inside.center.x ? ArenaSide.Left : ArenaSide.Right;      // a parede perto do gato
                case IsyathyMove.Spit: return catX < inside.center.x ? ArenaSide.Right : ArenaSide.Left;            // a de longe: o arco se lê
                case IsyathyMove.CeilingDrop: return ArenaSide.Ceiling;
                default: return ArenaSide.Floor;
            }
        }

        // ------------------------------------------------------------ dano no gato

        /// <summary>Dano do golpe no gato (1 = meio coração, 2 = um coração).</summary>
        public static int Damage(IsyathyMove m, bool finisher = false)
        {
            // 18/11/2026 ("ainda está muito fácil"): era 1 (meio coração) no bote, 2 na descida e no fim da combinação
            if (finisher) return 3;   // o fim da combinação da fase III: um coração e meio
            return m == IsyathyMove.Spit ? 1 : 2;   // a mordida: um coração
        }

        /// <summary>A onda da descida do teto (fase III) e a poça de veneno: meio coração.</summary>
        public const int ShockDamage = 1, PuddleDamage = 1;
        /// <summary>
        /// O montinho de terra (ela andando embaixo do chão) não fere: é só o aviso de onde ela está (pedido do usuário,
        /// 18/11/2026: "estamos tomando dano do monte de terra quando ela se move").
        /// </summary>
        public const bool MoundHurts = false;

        // ------------------------------------------------------------ tempos (todos legíveis: dá para prever)

        /// <summary>A velocidade (tiles/s) do montinho de terra andando pelas bordas: dá para acompanhar com os olhos.</summary>
        // 17/11/2026, 2ª versão ("está demorando para sair da terra e atacar"): ela vai direto ao lugar do golpe, mais
        // rápida quando está longe (<see cref="FarRun"/>); antes, andava 26–56 tiles a 9 tiles/s antes de escolher.
        public static float TravelSpeed(int phase) => phase <= 1 ? 13f : phase == 2 ? 14.5f : 16f;

        /// <summary>Longe do lugar do golpe (mais de tantos tiles), ela corre: <see cref="FarBoost"/>× a velocidade.</summary>
        public const float FarRun = 14f, FarBoost = 2f;

        /// <summary>A velocidade com <paramref name="left"/> tiles até o lugar do golpe: corre de longe, chega devagar (dá para ler onde vai sair).</summary>
        public static float TravelSpeed(int phase, float left) => TravelSpeed(phase) * (left > FarRun ? FarBoost : 1f);


        /// <summary>O aviso antes de sair (s): a superfície racha e brilha. Mais longo no golpe mais forte; a fase III encurta.</summary>
        public static float Warning(IsyathyMove m, int phase)
        {
            float w;
            switch (m)
            {
                case IsyathyMove.CeilingDrop: w = 0.9f; break;
                case IsyathyMove.WallStrike: w = 0.75f; break;
                case IsyathyMove.Arch: w = 0.75f; break;
                case IsyathyMove.Spit: w = 0.75f; break;
                default: w = 0.72f; break;
            }
            return phase >= 3 ? w * 0.85f : phase == 2 ? w * 0.92f : w;
        }

        /// <summary>Fora da terra, armada: a mira antes do bote (s). A cabeça recua e os olhos piscam.</summary>
        public static float AimTime(int phase) => phase <= 1 ? 0.5f : phase == 2 ? 0.45f : 0.4f;

        /// <summary>O avanço do bote (s): o momento rápido, mas dá para ver.</summary>
        public const float StrikeTime = 0.22f, RiseTime = 0.36f, RetractTime = 0.55f;

        /// <summary>Quanto tempo ela fica esperando, cansada, depois do golpe (s): é quando a cabeça leva dano.</summary>
        public static float Exposed(IsyathyMove m, int phase = 1)
        {
            float e;
            switch (m)
            {
                case IsyathyMove.Lunge: e = 1.05f; break;   // 18/11/2026 ("mais difícil"): as esperas cansadas encurtaram
                case IsyathyMove.WallStrike: e = 1f; break;
                case IsyathyMove.CeilingDrop: e = 1.25f; break;
                case IsyathyMove.Arch: e = 2.5f; break;      // a cabeça e o rabo para fora o tempo todo
                case IsyathyMove.Spit: e = 1f; break;
                default: e = 1.5f; break;
            }
            return phase >= 3 ? e * 0.85f : e;
        }

        /// <summary>O respiro depois de voltar para a terra, antes de começar a andar (s).</summary>
        public static float Rest(int phase) => phase <= 1 ? 0.12f : phase == 2 ? 0.08f : 0.05f;   // 18/11/2026: era 0,25 / 0,15 / 0,1

        // ------------------------------------------------------------ a Vulpis e o dano na cabeça

        /// <summary>A flecha da Vulpis: atordoada por este tempo, levando <see cref="StunMultiplier"/>× o dano.</summary>
        // 18/11/2026 (pedido do usuário: "mais tempo de stun quando a Vulpis quebra o efeito"): era 3,2 s
        public const float StunTime = 5.5f, StunMultiplier = 1.5f;

        /// <summary>
        /// A Casca da Praga (pedido do usuário, 17/11/2026): ao cruzar 65% e 30%, a praga cobre a Isyathy e ela fica imune
        /// por este tempo (s), lutando normalmente. Depois, a Vulpis pula e quebra a casca com a flecha forte.
        /// </summary>
        public const float ShellTime = 4.5f;

        /// <summary>A Vulpis só atira quando a casca já durou <see cref="ShellTime"/> e a Isyathy está entre golpes.</summary>
        public static bool VulpisBreaksShell(bool shell, float shellFor) => shell && shellFor >= ShellTime;

        /// <summary>
        /// O golpe no corpo (18/11/2026, pedido do usuário: "o corpo todo recebe dano"): fora da terra, qualquer pedaço dela
        /// leva o dano do golpe; atordoada pela flecha da Vulpis, leva mais. Só a Casca da Praga bloqueia.
        /// </summary>
        public static int BodyDamage(int amount, bool exposed, bool stunned, bool shell = false)
        {
            if (shell) return 0;
            return stunned ? Mathf.CeilToInt(amount * StunMultiplier) : amount;
        }

        /// <summary>Cansada (na espera depois do golpe), a cabeça leva um pouco mais: o ponto fraco.</summary>
        public const float TiredHeadMultiplier = 1.25f;

        /// <summary>O dano na cabeça: o do corpo, e mais na espera (<see cref="TiredHeadMultiplier"/>) e atordoada.</summary>
        public static int HeadDamage(int amount, bool exposed, bool stunned, bool shell = false)
        {
            if (shell) return 0;   // a casca da praga: nada passa
            if (stunned) return Mathf.CeilToInt(amount * StunMultiplier);
            return exposed ? Mathf.CeilToInt(amount * TiredHeadMultiplier) : amount;
        }

        // ------------------------------------------------------------ geometria: as bordas da arena

        /// <summary>A cabeça encosta no gato (o focinho a até este raio, tiles).</summary>
        public const float BiteRadius = 1.8f;   // 18/11/2026: era 1,5 ("ela ainda erra muito")
        /// <summary>Alcance do bote do chão e do avanço da parede (tiles).</summary>
        // 17/11/2026 (pedido: "ela ataca o ar como se não estivéssemos lá"): era 7 e 11. A parede alcança o meio da arena.
        public const float LungeReach = 11f, WallReach = 16f;
        /// <summary>
        /// A mira atrasada (18/11/2026, pedido do usuário: "ela mira em nós e vai onde nós estávamos 1 segundo atrás, dando
        /// tempo de darmos o dash"): o golpe vai onde o gato estava há <see cref="AimDelay"/> s, não onde ele está nem
        /// para onde vai. A cabeça continua encarando o gato (parece viva); o alvo é o rastro dele.
        /// </summary>
        public static float AimDelay(int phase) => phase >= 3 ? 0.27f : phase == 2 ? 0.32f : 0.38f;   // 18/11/2026: era 1 / 0,85 / 0,7, depois 0,75 / 0,65 / 0,55, depois 0,5 / 0,42 / 0,35 ("ainda está muito fácil")

        /// <summary>
        /// Só a boca fere (18/11/2026): o corpo não machuca, mas é sólido: o gato não atravessa a cabeça nem o corpo, nem
        /// com o dash (para no corpo dela, como numa parede). O dash não dá invencibilidade: se a boca encostar, fere.
        /// </summary>
        public const bool MouthOnly = true, BodyIsSolid = true, DashDodges = false;

        /// <summary>
        /// O corpo da Isyathy da luta (tiles): mais comprido que o do Ninho (22), para descer do teto até o chão
        /// (a arena tem 26 de altura por dentro) sem o rabo sair da terra.
        /// </summary>
        public const float BodyLength = 32f;
        /// <summary>O pedaço do rabo que fica sempre dentro da terra (tiles; pedido do usuário, 18/11/2026).</summary>
        public const float MinBuried = 3.5f;
        /// <summary>Quanto do corpo pode sair de um buraco (tiles).</summary>
        public const float MaxOut = BodyLength - MinBuried;
        /// <summary>Até onde a cabeça vai no trilho, com o buraco no ponto <paramref name="holeS"/> dele.</summary>
        public static float MaxHead(float holeS) => holeS + MaxOut;

        /// <summary>Quantas bolas o Cuspe solta (e o tempo de voo, s).</summary>
        public const int SpitShots = 5;
        public const float SpitFlight = 0.8f;
        /// <summary>Quanto a cabeça sai da superfície ao se armar (tiles).</summary>
        public const float RearOut = 3.4f;

        /// <summary>O perímetro da arena (tiles): o caminho dela dentro da terra.</summary>
        public static float Perimeter(Rect inside) => 2f * (inside.width + inside.height);

        /// <summary>
        /// O ponto da borda na posição <paramref name="u"/> do perímetro (a partir do canto de baixo à esquerda, no sentido
        /// anti-horário: chão → parede da direita → teto → parede da esquerda), com a normal para dentro da arena.
        /// </summary>
        public static Vector2 SurfaceAt(Rect r, float u, out Vector2 normal, out ArenaSide side)
        {
            float p = Perimeter(r);
            u = Mathf.Repeat(u, p);
            if (u < r.width) { normal = Vector2.up; side = ArenaSide.Floor; return new Vector2(r.xMin + u, r.yMin); }
            u -= r.width;
            if (u < r.height) { normal = Vector2.left; side = ArenaSide.Right; return new Vector2(r.xMax, r.yMin + u); }
            u -= r.height;
            if (u < r.width) { normal = Vector2.down; side = ArenaSide.Ceiling; return new Vector2(r.xMax - u, r.yMax); }
            u -= r.width;
            normal = Vector2.right; side = ArenaSide.Left;
            return new Vector2(r.xMin, r.yMax - u);
        }

        /// <summary>A posição no perímetro do ponto da borda mais perto de <paramref name="p"/>.</summary>
        public static float ParamOf(Rect r, Vector2 p)
        {
            float dFloor = Mathf.Abs(p.y - r.yMin), dCeil = Mathf.Abs(p.y - r.yMax), dLeft = Mathf.Abs(p.x - r.xMin), dRight = Mathf.Abs(p.x - r.xMax);
            float m = Mathf.Min(Mathf.Min(dFloor, dCeil), Mathf.Min(dLeft, dRight));
            float x = Mathf.Clamp(p.x, r.xMin, r.xMax), y = Mathf.Clamp(p.y, r.yMin, r.yMax);
            if (m == dFloor) return x - r.xMin;
            if (m == dRight) return r.width + (y - r.yMin);
            if (m == dCeil) return r.width + r.height + (r.xMax - x);
            return 2f * r.width + r.height + (r.yMax - y);
        }

        /// <summary>Quanto andar (tiles, sempre ≥ 0) de <paramref name="from"/> até <paramref name="to"/> no sentido <paramref name="dir"/> (±1).</summary>
        public static float Distance(Rect r, float from, float to, int dir)
        {
            float p = Perimeter(r);
            return Mathf.Repeat((to - from) * (dir >= 0 ? 1f : -1f), p);
        }

        /// <summary>O sentido mais curto (±1) de <paramref name="from"/> até <paramref name="to"/>.</summary>
        public static int ShortestDir(Rect r, float from, float to) => Distance(r, from, to, 1) <= Distance(r, from, to, -1) ? 1 : -1;

        /// <summary>A altura (mundo) de onde ela sai da parede: a do gato, longe do chão e do teto.</summary>
        public static float WallHeight(float catY, Rect inside) => Mathf.Clamp(catY, inside.yMin + 1.2f, inside.yMax - 4f);

        /// <summary>O ângulo (graus) que gira a cratera na borda com esta normal (0 = no chão).</summary>
        public static float CraterAngle(Vector2 normal) => Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;

        /// <summary>O vão (dos <paramref name="holesX"/>, colunas sem plataforma) mais longe do gato.</summary>
        public static int FarHole(float[] holesX, float catX)
        {
            int best = 0; float far = -1f;
            for (int i = 0; i < holesX.Length; i++)
            {
                float d = Mathf.Abs(holesX[i] - catX);
                if (d > far) { far = d; best = i; }
            }
            return best;
        }

        /// <summary>O vão mais perto do gato, mas a pelo menos <paramref name="minDist"/> dele (não sai embaixo dele).</summary>
        public static int NearHole(float[] holesX, float catX, float minDist)
        {
            int best = -1; float near = float.MaxValue;
            for (int i = 0; i < holesX.Length; i++)
            {
                float d = Mathf.Abs(holesX[i] - catX);
                if (d >= minDist && d < near) { near = d; best = i; }
            }
            return best >= 0 ? best : FarHole(holesX, catX);
        }

        /// <summary>O vão mais perto do gato, mesmo em cima dele (a descida do teto).</summary>
        public static int ClosestHole(float[] holesX, float catX)
        {
            int best = 0; float near = float.MaxValue;
            for (int i = 0; i < holesX.Length; i++)
            {
                float d = Mathf.Abs(holesX[i] - catX);
                if (d < near) { near = d; best = i; }
            }
            return best;
        }

        /// <summary>
        /// O Arco: a cabeça sai do vão perto do gato (sem ser embaixo dele) e o rabo do vão vizinho, do outro lado. Devolve
        /// os índices (cabeça, rabo); com menos de dois vãos, o rabo é −1.
        /// </summary>
        public static Vector2Int ArchHoles(float[] holesX, float catX)
        {
            if (holesX.Length < 2) return new Vector2Int(0, -1);
            int head = NearHole(holesX, catX, 3f);
            int away = holesX[head] >= catX ? head + 1 : head - 1;      // o rabo do lado de longe do gato
            if (away < 0 || away >= holesX.Length) away = holesX[head] >= catX ? head - 1 : head + 1;
            return new Vector2Int(head, Mathf.Clamp(away, 0, holesX.Length - 1));
        }

        /// <summary>
        /// Quanto o rabo sai da terra no Arco (tiles): o que sobra do corpo depois da cabeça, do mergulho embaixo do chão
        /// (<paramref name="underground"/> tiles de trilho) e da subida da cabeça (<paramref name="headOut"/>).
        /// </summary>
        public static float TailOut(float bodyLength, float underground, float headOut) => bodyLength - underground - headOut;
    }
}
