using NUnit.Framework;
using SeteVidas.UI;

namespace SeteVidas.Tests
{
    /// <summary>Comandos no aparelho em uso: controle de Xbox ou teclado e mouse (pedido do usuário, 12/11/2026).</summary>
    public class InputPromptsTests
    {
        const InputDeviceKind Pad = InputDeviceKind.Gamepad, Keys = InputDeviceKind.KeyboardMouse;

        [Test]
        public void Gamepad_UsesTheXboxButtons()
        {
            Assert.AreEqual("A", InputPrompts.Name(GameAction.Jump, Pad));
            Assert.AreEqual("X", InputPrompts.Name(GameAction.Attack, Pad));
            Assert.AreEqual("B", InputPrompts.Name(GameAction.Interact, Pad));
            Assert.AreEqual("LT", InputPrompts.Name(GameAction.Heal, Pad));
            Assert.AreEqual("RB", InputPrompts.Name(GameAction.NextClass, Pad));
            Assert.AreEqual("Menu", InputPrompts.Name(GameAction.Inventory, Pad));
        }

        [Test]
        public void Keyboard_UsesTheKeysAndTheMouse()
        {
            Assert.AreEqual("Espaço", InputPrompts.Name(GameAction.Jump, Keys));
            Assert.AreEqual("clique", InputPrompts.Name(GameAction.Attack, Keys));
            Assert.AreEqual("E", InputPrompts.Name(GameAction.Interact, Keys));
            Assert.AreEqual("F", InputPrompts.Name(GameAction.Heal, Keys));
            Assert.AreEqual("A/D", InputPrompts.Name(GameAction.Move, Keys));
            Assert.AreEqual("Tab", InputPrompts.Name(GameAction.Inventory, Keys));
        }

        [Test]
        public void Texts_SwapTheirMarksForTheDevice()
        {
            Assert.AreEqual("Aperte LB/RB para trocar de classe.", InputPrompts.Format("Aperte {trocar_classe} para trocar de classe.", Pad));
            Assert.AreEqual("Aperte 2 (ou Q/R) para trocar de classe.", InputPrompts.Format("Aperte {trocar_classe} para trocar de classe.", Keys));
            Assert.AreEqual("No ar, S + clique", InputPrompts.Format("No ar, {agachar} + {atacar}", Keys));
            Assert.AreEqual("Aperte LT para curar", InputPrompts.Format("Aperte {curar} para curar", Pad));
            Assert.AreEqual("sem marca", InputPrompts.Format("sem marca", Pad));
        }

        [Test]
        public void ControlNames_InPortuguese()
        {
            Assert.AreEqual("A", InputPrompts.ControlName("<Gamepad>/buttonSouth"));
            Assert.AreEqual("Y", InputPrompts.ControlName("<Gamepad>/buttonNorth"));
            Assert.AreEqual("clique dir.", InputPrompts.ControlName("<Mouse>/rightButton"));
            Assert.AreEqual("Shift", InputPrompts.ControlName("<Keyboard>/leftShift"));
            Assert.AreEqual("↓", InputPrompts.ControlName("<Keyboard>/downArrow"));
            Assert.AreEqual("ESPAÇO", InputPrompts.KeyLabel("space", true));
        }

        [Test]
        public void Glyphs_AreElevenPixelsTall_AndKeysGrowWithTheirName()
        {
            var a = InputPrompts.Glyph(GameAction.Jump, Pad);
            var space = InputPrompts.Glyph(GameAction.Jump, Keys);
            var e = InputPrompts.Glyph(GameAction.Interact, Keys);
            var move = InputPrompts.Glyph(GameAction.Move, Keys);
            Assert.IsNotNull(a);
            Assert.AreEqual(InputGlyphs.Height, (int)a.rect.height);
            Assert.AreEqual(InputGlyphs.Height, (int)space.rect.height);
            Assert.Greater(space.rect.width, e.rect.width, "ESPAÇO é mais larga que E");
            Assert.Greater(move.rect.width, e.rect.width * 2f - 1f, "A/D: duas teclas lado a lado");
            Assert.AreSame(a, InputPrompts.Glyph(GameAction.Jump, Pad), "fica em cache");
        }
    }
}
