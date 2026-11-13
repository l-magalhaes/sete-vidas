using NUnit.Framework;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>A barra de vida do Koda em pixel art (pedido do usuário, 12/11/2026).</summary>
    public class BossBarArtTests
    {
        static Color32[] Draw(float health, float trail, bool furious = false, bool flash = false)
        {
            var px = new Color32[BossBarArt.Width * BossBarArt.Height];
            BossBarArt.Draw(px, new BossBarArt.State
            {
                Health = health, Trail = trail, PhaseAt = 0.6f, Furious = furious, Flash = flash, Glint = -1, Name = "Koda, o Dourado",
            });
            return px;
        }

        static Color32 Inner(Color32[] px, float at) =>
            BossBarArt.Get(px, BossBarArt.FillLeft + Mathf.RoundToInt(at * (BossBarArt.FillLength - 1)), BossBarArt.InnerTop + 2);

        [Test]
        public void Columns_CoverTheWholeBar()
        {
            Assert.AreEqual(0, BossBarArt.Columns(0f));
            Assert.AreEqual(BossBarArt.FillLength, BossBarArt.Columns(1f));
            Assert.AreEqual(1, BossBarArt.Columns(0.001f), "um fio de vida ainda aparece");
        }

        [Test]
        public void HalfHealth_GoldOnTheLeft_DarkOnTheRight()
        {
            var px = Draw(0.5f, 0.5f);
            var left = Inner(px, 0.2f);
            var right = Inner(px, 0.8f);
            Assert.Greater(left.r + left.g, 300, "ouro à esquerda");
            Assert.Less(right.r + right.g, 120, "fundo escuro à direita");
        }

        [Test]
        public void RecentDamage_IsPale_AndFuryTurnsToEmber()
        {
            var px = Draw(0.4f, 0.7f);
            var pale = Inner(px, 0.55f);
            Assert.Greater(pale.b, 150, "a parte clara do dano recente");
            var gold = Inner(Draw(0.9f, 0.9f), 0.2f);
            var ember = Inner(Draw(0.4f, 0.4f, furious: true), 0.2f);
            Assert.Greater(gold.g, ember.g, "na fúria o ouro vira brasa");
        }

        [Test]
        public void Hit_FlashesWhite()
        {
            var c = Inner(Draw(0.5f, 0.5f, flash: true), 0.2f);
            Assert.AreEqual(new Color32(255, 255, 255, 255), c);
        }
    }
}
