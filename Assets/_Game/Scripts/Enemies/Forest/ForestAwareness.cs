using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>Como um bicho da Floresta percebe o gato (alcance, cone, velocidade da suspeita, audição).</summary>
    public struct ForestSenseProfile
    {
        /// <summary>Alcance da visão com o gato totalmente visível (tiles).</summary>
        public float Range;
        /// <summary>Meia abertura do cone (graus). 180 = vê em volta (o morcego, pelo eco).</summary>
        public float HalfAngle;
        /// <summary>Suspeita por segundo vendo o gato (como o SightFillRate da Sentinela).</summary>
        public float FillRate;
        /// <summary>Multiplica o raio dos ruídos que ele ouve.</summary>
        public float Hearing;
        /// <summary>Multiplica o quanto um ruído enche a suspeita.</summary>
        public float HearGain;
    }

    /// <summary>
    /// Pedido do usuário (14/11/2026): os inimigos da Floresta ganham o sistema de alerta e detecção dos outros
    /// inimigos (medidor de suspeita, "?" e "!", visão em cone bloqueada por parede e pela furtividade, audição).
    /// O morcego vê muito mais rápido e em volta; os outros usam o padrão da Sentinela. Regras puras (EditMode).
    /// </summary>
    public static class ForestSenseRules
    {
        /// <summary>O padrão dos outros inimigos (Sentinela): 8 tiles, cone de 35°, 0,8 por segundo.</summary>
        public static ForestSenseProfile Default => new ForestSenseProfile
        {
            Range = 8f, HalfAngle = 35f, FillRate = 0.8f, Hearing = 1f, HearGain = 1f,
        };

        /// <summary>Morcego: vê em volta (eco), enche a suspeita 5× mais rápido e ouve muito bem.</summary>
        public static ForestSenseProfile Bat(float wakeRange) => new ForestSenseProfile
        {
            Range = wakeRange, HalfAngle = 180f, FillRate = 4f, Hearing = 1.5f, HearGain = 3f,
        };

        /// <summary>Serpente no buraco: sente pela vibração em volta, mas perto (só sai para morder).</summary>
        public static ForestSenseProfile Snake => new ForestSenseProfile
        {
            Range = 6f, HalfAngle = 180f, FillRate = 0.8f, Hearing = 1f, HearGain = 1f,
        };

        /// <summary>Visibilidade mínima do gato para quem já está em alerta (não some de vista no escuro).</summary>
        public const float CombatVisibilityFloor = 0.6f;

        /// <summary>Medidor dos bichos parados: perde o gato de vista em 2 s e esquece rápido (não saem procurando).</summary>
        public static AlertTuning Tuning
        {
            get
            {
                var t = AlertTuning.Default;
                t.SearchTime = 1.5f;
                t.HuntSearchTime = 2.5f;
                return t;
            }
        }

        /// <summary>O gato está no cone em volta de <paramref name="forward"/> e no alcance?</summary>
        public static bool InCone(Vector2 eye, Vector2 forward, Vector2 target, float range, float halfAngle)
        {
            Vector2 d = target - eye;
            float sqr = d.sqrMagnitude;
            if (sqr > range * range) return false;
            if (sqr < 1e-4f || halfAngle >= 180f) return true;
            return Vector2.Angle(forward, d) <= halfAngle;
        }

        /// <summary>Suspeita por segundo vendo o gato a <paramref name="distance"/> com esta visibilidade (0 = fora).</summary>
        public static float SightFill(in ForestSenseProfile p, float distance, float visibility, bool inCombat)
        {
            float vis = inCombat ? Mathf.Max(visibility, CombatVisibilityFloor) : visibility;
            float effective = PerceptionMath.EffectiveRange(p.Range, vis);
            return PerceptionMath.SightFillRate(distance, effective, p.FillRate);
        }
    }

    /// <summary>
    /// Olhos, ouvidos e medidor de um bicho da Floresta (sem MonoBehaviour: o <see cref="ForestEnemy"/> é o dono).
    /// Alimenta um <see cref="AlertBrain"/> igual ao da Sentinela e mostra o <see cref="AlertIcon"/> em cima da cabeça.
    /// </summary>
    public sealed class ForestAwareness
    {
        readonly AlertBrain _brain = new AlertBrain();
        readonly GameObject _owner;
        AlertIcon _icon;
        static int s_occluders = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_occluders = -1; // Play sem recarregar o domínio

        public ForestSenseProfile Profile;
        /// <summary>Não mostra o "?"/"!" (o Galho Seco disfarçado não se entrega).</summary>
        public bool IconHidden;

        public ForestAwareness(GameObject owner, ForestSenseProfile profile, Vector3 iconLocalPos)
        {
            _owner = owner;
            Profile = profile;
            // o ícone fica sempre de pé e acima (a planta na parede é girada)
            _icon = AlertIcon.Create(owner.transform, owner.transform.InverseTransformDirection(iconLocalPos));
            _icon.transform.rotation = Quaternion.identity;
        }

        public AlertState State => _brain.State;
        public float Suspicion => _brain.Suspicion;
        /// <summary>Detectou o gato ("!"): o bicho ataca.</summary>
        public bool Aware => _brain.State == AlertState.Combat;
        /// <summary>Estranhou algo ("?"): olha para lá, ainda não ataca.</summary>
        public bool Suspicious => _brain.State == AlertState.Suspicious || _brain.State == AlertState.Investigating;
        public Vector2 LastKnown => _brain.LastKnownPosition;
        /// <summary>Viu o gato no último passo.</summary>
        public bool SeesPlayer { get; private set; }

        /// <summary>Olha uma vez e atualiza o medidor. <paramref name="forward"/>: para onde o bicho olha.</summary>
        public void Step(Vector2 eye, Vector2 forward, float dt)
        {
            SeesPlayer = false;
            var stealth = StealthController.Active;
            if (stealth != null && stealth.isActiveAndEnabled && stealth.IsTargetable)
            {
                Vector2 target = stealth.ChestPosition;
                float dist = Vector2.Distance(eye, target);
                if (ForestSenseRules.InCone(eye, forward, target, Profile.Range, Profile.HalfAngle))
                {
                    if (s_occluders < 0)
                        s_occluders = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
                    // nos dois sentidos: um olho encostado (ou dentro) do bloco não enxerga através dele (o morcego no teto)
                    if (!Blocked(eye, target))
                    {
                        float fill = ForestSenseRules.SightFill(Profile, dist, stealth.Visibility, Aware);
                        if (fill > 0f)
                        {
                            _brain.See(target, fill, dt);
                            SeesPlayer = true;
                        }
                    }
                }
            }
            _brain.Tick(dt, ForestSenseRules.Tuning);
            // bicho parado: chegou à "última posição" na hora (olha de onde está)
            if (_brain.State == AlertState.Investigating) _brain.ArrivedAtLastKnownPosition();
            UpdateIcon();
        }

        /// <summary>Quanto sobra de um barulho do outro lado de uma parede: nada (o morcego no teto ainda ficava com "?" pelos pousos lá em cima).</summary>
        public const float MuffledByWall = 0f;

        /// <summary>Tem bloco entre <paramref name="a"/> e <paramref name="b"/> (testado nos dois sentidos).</summary>
        public static bool Blocked(Vector2 a, Vector2 b)
        {
            if (s_occluders < 0)
                s_occluders = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Climbable, GameConstants.Layers.Breakable);
            // olho (ou alvo) dentro do bloco também conta como tapado
            if (Physics2D.OverlapPoint(a, s_occluders) != null || Physics2D.OverlapPoint(b, s_occluders) != null) return true;
            return Physics2D.Linecast(a, b, s_occluders).collider != null || Physics2D.Linecast(b, a, s_occluders).collider != null;
        }

        /// <summary>Um ruído do <see cref="NoiseSystem"/>.</summary>
        public void Hear(in NoiseEvent n, Vector2 at)
        {
            if (n.Source == _owner) return;
            float radius = n.Radius * Profile.Hearing;
            if ((n.Position - at).sqrMagnitude > radius * radius) return;
            // a parede bloqueia o som: do outro lado do bloco, o bicho não ouve
            // o ruído dos passos nasce no pé (encostado no chão): testa um pouco acima dele
            float k = Blocked(at, n.Position + Vector2.up * 0.3f) ? MuffledByWall : 1f;
            if (k <= 0f) return;
            _brain.Hear(n.Position, Mathf.Clamp01(n.Loudness * Profile.HearGain * k));
        }

        /// <summary>Apanhou: alerta na hora.</summary>
        public void Damaged(Vector2 from) => _brain.OnDamaged(from);

        /// <summary>Já nasce sabendo (os morcegos que saem do Broto).</summary>
        public void ForceAware(Vector2 at) => _brain.OnDamaged(at);

        public void Kill()
        {
            _brain.Kill();
            if (_icon != null) _icon.Show(AlertIcon.Mark.None, false, 0f);
        }

        public void Reset()
        {
            _brain.Reset();
            if (_icon != null) _icon.Show(AlertIcon.Mark.None, false, 0f);
        }

        void UpdateIcon()
        {
            if (_icon == null) return;
            if (IconHidden) { _icon.Show(AlertIcon.Mark.None, false, 0f); return; }
            switch (_brain.State)
            {
                case AlertState.Suspicious:
                    float threshold = Mathf.Max(0.01f, ForestSenseRules.Tuning.InvestigateThreshold);
                    _icon.Show(AlertIcon.Mark.Question, false, _brain.Suspicion / threshold);
                    break;
                case AlertState.Investigating: _icon.Show(AlertIcon.Mark.Question, _brain.IsHunting, 1f); break;
                case AlertState.Combat: _icon.Show(AlertIcon.Mark.Exclamation, false, 1f); break;
                default: _icon.Show(AlertIcon.Mark.None, false, 0f); break;
            }
        }
    }
}
