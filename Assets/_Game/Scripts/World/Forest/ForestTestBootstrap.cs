using SeteVidas.Classes;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Só nas cenas de teste da Floresta (Forest_Test_*): deixa o save como o de quem acabou de sair da Fortaleza,
    /// para testar sem jogar a fase 1. Paladino liberado, maldição feita e 2 corações (1 da maldição + o do vínculo).
    /// Não mexe em nada se o save já passou disso.
    /// </summary>
    public class ForestTestBootstrap : MonoBehaviour
    {
        [SerializeField, Min(1)] int hearts = 2;

        void Start()
        {
            // nunca no save do jogo (o graybox da F2 também tem este preparador; ver ForestGrayboxSave)
            if (SaveManager.ActiveSlot == SaveManager.MainSlot) return;
            var data = SaveManager.Data;
            var health = PlayerHealth.Active;
            if (data == null || health == null) return;

            var classes = health.GetComponent<ClassManager>();
            if (classes != null) classes.Unlock(CharacterClass.Paladin);
            else if (!data.IsUnlocked(CharacterClass.Paladin)) data.unlockedClasses.Add(CharacterClass.Paladin.ToString());

            if (!data.HasFlag(PlayerHealth.CurseDoneFlag))
            {
                data.SetFlag(PlayerHealth.CurseDoneFlag);
                health.SetHeartsRecovered(hearts, save: true);
                health.RestoreFull();
                Debug.Log($"[Sete Vidas] Cena de teste da Floresta: Paladino liberado, maldição feita, {hearts} corações.");
            }
        }
    }
}
