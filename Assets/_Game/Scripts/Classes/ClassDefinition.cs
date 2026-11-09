using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Classes
{
    /// <summary>
    /// Dados de uma classe (GDD §9.4): nome, ícone, cor, arma e a camada visual sobre o gato.
    /// O gato continua o gato: a classe só troca arma, postura e efeitos (GDD §14).
    /// </summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Class Definition", fileName = "ClassDefinition")]
    public class ClassDefinition : ScriptableObject
    {
        public CharacterClass Class;
        public string DisplayName = "Ladino";
        [Tooltip("Ícone 16×16 ao lado dos corações")]
        public Sprite Icon;
        [Tooltip("Cor da classe (ícone, efeito da troca)")]
        public Color ThemeColor = Color.white;

        [Header("Arma")]
        [Tooltip("Dano, tempos, hitbox e tipo de dano. A marreta é Heavy (quebra parede rachada)")]
        public MeleeAttackSettings Weapon;

        [Header("Camada visual sobre o gato (vazio = nada)")]
        [Tooltip("Arma carregada (parado, andando, pulando)")]
        public Sprite OverlayIdle;
        [Tooltip("Arma na preparação do golpe")]
        public Sprite OverlayWindup;
        [Tooltip("Arma no golpe e na recuperação")]
        public Sprite OverlayStrike;
        [Tooltip("Efeito do golpe, um por golpe do combo (vazio = corte padrão)")]
        public Sprite[] SlashSprites;
    }
}
