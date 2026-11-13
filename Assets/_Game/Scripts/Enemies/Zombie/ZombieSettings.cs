using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Enemies.Zombie
{
    /// <summary>Números do Templário Zumbi (spec: Docs/Design/zumbi-templario.md). Sem asset, valem estes padrões.</summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Zombie Settings", fileName = "ZombieSettings")]
    public class ZombieSettings : ScriptableObject
    {
        [Header("Vida e dano")]
        [Min(1)] public int MaxHealth = 6;
        [Tooltip("Golpe pelas costas: nunca é pego de surpresa, mas leva este multiplicador (pedido do usuário: dobrado)")]
        [Min(1f)] public float BackMultiplier = 2f;

        [Header("Movimento")]
        [Tooltip("Vagando: anda sempre para a frente, devagar (tiles/s)")]
        [Min(0f)] public float WanderSpeed = 0.8f;
        [Tooltip("Perseguindo (tiles/s). Era 5,5; o usuário achou difícil demais (12/11/2026): agora abaixo do templário (4,5)")]
        [Min(0f)] public float ChaseSpeed = 3.2f;
        [Tooltip("Para um instante ao virar na parede ou na beira (s)")]
        [Min(0f)] public float TurnPause = 0.5f;
        [Min(0f)] public float GravityScale = 4f;
        public Vector2 BodySize = new Vector2(0.75f, 1.6f);

        [Header("Sentidos")]
        [Tooltip("Vê o gato à frente até esta distância (tiles); sem cone de suspeita, sem \"?\"")]
        [Min(0f)] public float SightRange = 6f;
        [Tooltip("Sente o gato de qualquer lado bem perto (o faro); escondido, metade")]
        [Min(0f)] public float CloseRange = 2f;
        [Tooltip("Só sente no mesmo andar")]
        [Min(0f)] public float SenseHeight = 2f;
        [Tooltip("Ouve os barulhos da furtividade (NoiseSystem) com este multiplicador do raio")]
        [Min(0f)] public float HearingMultiplier = 1f;
        [Tooltip("Desiste depois de tanto tempo sem sentir o gato (s)")]
        [Min(0f)] public float LoseTime = 3f;
        [Tooltip("Tranco da cabeça ao sentir o gato, antes de correr (s)")]
        [Min(0f)] public float AlertTime = 0.5f;

        [Header("Toque que envenena")]
        [Tooltip("Área do toque (tiles), centrada no corpo")]
        public Vector2 ContactSize = new Vector2(0.85f, 1.5f);
        [Tooltip("Tempo entre dois toques seguidos (s); cada toque envenena ou reinicia o veneno")]
        [Min(0.05f)] public float ContactCooldown = 0.5f;

        [Header("Reação e morte")]
        public HitReactionTable Reactions = new HitReactionTable();
        [Tooltip("Tempo deitado depois da animação de morte, antes de sumir (s)")]
        [Min(0f)] public float CorpseTime = 0.2f;
    }
}
