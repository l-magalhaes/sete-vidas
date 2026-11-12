using System.Collections.Generic;
using NUnit.Framework;
using SeteVidas.UI;

namespace SeteVidas.Tests
{
    /// <summary>
    /// Melhoria 7 (revisada): corações vermelhos vestidos com o acessório da classe; antes da maldição os 7 são do
    /// Ladino, depois cada coração recuperado é o da sua classe.
    /// </summary>
    public class ClassHeartTests
    {
        static int Count(string[,] px, string hex)
        {
            int n = 0;
            foreach (var c in px) if (c == hex) n++;
            return n;
        }

        static string Key(string[,] px)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in px) sb.Append(c).Append(',');
            return sb.ToString();
        }

        [Test]
        public void EveryHeart_IsRed_WithItsOwnAccessory()
        {
            var looks = new HashSet<string>();
            for (int k = 0; k < ClassHeartPixels.Classes; k++)
            {
                var full = ClassHeartPixels.Draw(k, HeartLook.Full);
                Assert.Greater(Count(full, ClassHeartPixels.Red), 40, $"Classe {k}: o coração é vermelho");
                Assert.Greater(ClassHeartPixels.AccessoryPixels(k), 8, $"Classe {k}: tem acessório");
                Assert.IsTrue(looks.Add(Key(full)), $"Classe {k}: igual a outra classe");
            }
        }

        [Test]
        public void Half_IsRedOnlyOnTheLeft()
        {
            var half = ClassHeartPixels.Draw(1, HeartLook.Half);
            int right = 0;
            for (int y = 0; y < ClassHeartPixels.Size; y++)
                for (int x = 8; x < ClassHeartPixels.Size; x++)
                    if (half[y, x] == ClassHeartPixels.Red) right++;
            Assert.AreEqual(0, right, "A metade direita fica vazia (1 hit tomado)");
            Assert.Greater(Count(half, ClassHeartPixels.Red), 15);
        }

        [Test]
        public void Empty_HasNoRed_ButKeepsTheAccessory()
        {
            var empty = ClassHeartPixels.Draw(0, HeartLook.Empty);
            Assert.AreEqual(0, Count(empty, ClassHeartPixels.Red));
            Assert.Greater(Count(empty, ClassHeartPixels.Hollow), 20);
            Assert.AreNotEqual(Key(empty), Key(ClassHeartPixels.Draw(1, HeartLook.Empty)), "O capuz continua lá");
        }

        [Test]
        public void Locked_IsJustStone()
        {
            Assert.AreEqual(Key(ClassHeartPixels.Draw(0, HeartLook.Locked)), Key(ClassHeartPixels.Draw(6, HeartLook.Locked)),
                "Travado: pedra rachada, sem acessório");
            Assert.AreEqual(0, Count(ClassHeartPixels.Draw(3, HeartLook.Locked), ClassHeartPixels.Red));
        }

        [Test]
        public void BeforeTheCurse_AllSevenAreRogue_AfterIt_EachClass()
        {
            for (int i = 0; i < 7; i++) Assert.AreEqual(0, HeartsHud.HeartClass(i, curseDone: false), "Antes da maldição: Ladino");
            Assert.AreEqual(0, HeartsHud.HeartClass(0, curseDone: true), "O 1º é sempre o do Ladino");
            Assert.AreEqual(1, HeartsHud.HeartClass(1, curseDone: true), "O 2º recuperado é o do Paladino");
            Assert.AreEqual(6, HeartsHud.HeartClass(6, curseDone: true), "O 7º é o do Druida");
        }
    }
}
