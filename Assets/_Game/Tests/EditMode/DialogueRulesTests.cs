using NUnit.Framework;
using SeteVidas.Dialogue;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>Diálogo: texto letra por letra, regras de tamanho da narrativa e retratos por expressão.</summary>
    public class DialogueRulesTests
    {
        [Test]
        public void Typewriter_RevealsOverTime_AndCompletesOnDemand()
        {
            var t = new TypewriterText();
            t.Start("Olá, gato.");
            Assert.AreEqual(0, t.Visible);
            Assert.AreEqual(5, t.Tick(0.1f, 50f), "50 letras/s × 0,1 s = 5 letras");
            Assert.AreEqual("Olá, ", t.VisibleText);
            Assert.IsFalse(t.IsComplete);
            t.Complete();
            Assert.IsTrue(t.IsComplete);
            Assert.AreEqual("Olá, gato.", t.VisibleText);
            Assert.AreEqual(0, t.Tick(1f, 50f), "Completo: não aparece mais nada");
        }

        [Test]
        public void Typewriter_NullText_IsEmptyAndComplete()
        {
            var t = new TypewriterText();
            t.Start(null);
            Assert.IsTrue(t.IsComplete);
            Assert.AreEqual("", t.VisibleText);
        }

        [Test]
        public void Validate_CatchesLongBoxesAndTooManyBoxes()
        {
            var d = ScriptableObject.CreateInstance<DialogueDefinition>();
            try
            {
                d.ScriptId = "D-TESTE";
                d.Lines.Add(new DialogueLine { Text = new string('a', DialogueDefinition.MaxCharsPerLine + 1) });
                for (int i = 0; i < DialogueDefinition.MaxLinesRequired; i++) d.Lines.Add(new DialogueLine { Text = "ok" });
                var problems = d.Validate();
                Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
                d.Required = false;
                Assert.AreEqual(1, d.Validate().Count, "Conversa opcional pode ter mais caixas");
            }
            finally
            {
                Object.DestroyImmediate(d);
            }
        }

        [Test]
        public void AllDialoguesInProject_RespectNarrativeLimits()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(DialogueDefinition), new[] { "Assets/_Game" });
            foreach (var guid in guids)
            {
                var d = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var problems = d.Validate();
                Assert.IsEmpty(problems, string.Join("\n", problems));
            }
        }

        [Test]
        public void Speaker_UsesExpressionPortrait_OrFallsBackToDefault()
        {
            var s = ScriptableObject.CreateInstance<SpeakerProfile>();
            var tex = new Texture2D(2, 2);
            var neutral = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.zero);
            var ironic = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
            try
            {
                s.Portrait = neutral;
                s.Expressions.Add(new SpeakerProfile.Expression { Key = "ironico", Portrait = ironic });
                Assert.AreSame(ironic, s.PortraitFor("ironico"));
                Assert.AreSame(neutral, s.PortraitFor(""));
                Assert.AreSame(neutral, s.PortraitFor("bravo"), "Expressão que não existe usa o retrato padrão");
            }
            finally
            {
                Object.DestroyImmediate(neutral);
                Object.DestroyImmediate(ironic);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(s);
            }
        }
    }
}
