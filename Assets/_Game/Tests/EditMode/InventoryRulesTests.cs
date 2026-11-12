using NUnit.Framework;
using SeteVidas.UI;

/// <summary>Inventário (Docs/Design/inventario.md): navegação na grade e a arte dos ícones.</summary>
public class InventoryRulesTests
{
    [TestCase(0, 1, 0, 1)]     // anda para a direita
    [TestCase(4, 1, 0, 4)]     // última coluna: para, não dá a volta
    [TestCase(5, -1, 0, 5)]    // primeira coluna: para
    [TestCase(0, 0, -1, 0)]    // linha de cima: para
    [TestCase(1, 0, 1, 6)]     // desce na mesma coluna
    [TestCase(4, 0, 1, 6)]     // embaixo não tem nada nessa coluna: vai para o último da linha de baixo
    [TestCase(6, 0, 1, 6)]     // não existe linha de baixo: fica
    public void Move_StopsAtTheEdges(int from, int dx, int dy, int expected)
    {
        Assert.AreEqual(expected, InventoryGrid.Move(from, dx, dy, 5, 7));
    }

    // Diário: linha 0 = título do capítulo 1; placas nas casas 5..7; linha 2 = título do capítulo 2; placa na casa 15
    static readonly int[] DiaryCells = { 5, 6, 7, 15 };

    [TestCase(0, 0, 1, 3)]     // desce pulando a linha do título
    [TestCase(3, 0, -1, 0)]    // sobe pulando o título: a coluna mais perto
    [TestCase(0, 0, -1, 0)]    // em cima só tem título: fica
    [TestCase(2, 1, 0, 2)]     // à direita não tem placa: fica
    [TestCase(1, -1, 0, 0)]
    public void MoveCells_SkipsChapterTitles(int from, int dx, int dy, int expected)
    {
        Assert.AreEqual(expected, InventoryGrid.MoveCells(DiaryCells, from, dx, dy, 5));
    }

    [Test]
    public void Move_EmptyGrid_StaysAtZero()
    {
        Assert.AreEqual(0, InventoryGrid.Move(0, 1, 0, 5, 0));
    }

    [Test]
    public void EveryIcon_Is16x16_WithOutlineAndColor()
    {
        foreach (InventoryIcon icon in System.Enum.GetValues(typeof(InventoryIcon)))
        {
            var px = InventoryIcons.Draw(icon);
            Assert.AreEqual(16, px.GetLength(0), icon.ToString());
            Assert.AreEqual(16, px.GetLength(1), icon.ToString());
            int outline = 0, color = 0;
            foreach (var c in px) { if (c == InventoryIcons.Outline) outline++; else if (c != "") color++; }
            Assert.Greater(outline, 8, icon + ": tem contorno");
            Assert.Greater(color, 12, icon + ": tem desenho");
            // a borda de fora fica livre para o contorno não ser cortado
            for (int i = 0; i < 16; i++)
                Assert.IsTrue(px[0, i] == "" || px[0, i] == InventoryIcons.Outline, icon + ": linha de cima");
        }
    }

    [Test]
    public void Silhouette_KeepsTheShape_ButNotTheColors()
    {
        var full = InventoryIcons.Draw(InventoryIcon.Mage);
        var dark = InventoryIcons.Silhouette(InventoryIcon.Mage);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                Assert.AreEqual(full[y, x] == "", dark[y, x] == "", "mesma forma");
                if (dark[y, x] != "") Assert.IsFalse(dark[y, x] == "#f08a3a", "sem o laranja do fogo");
            }
    }

    [Test]
    public void Slot_Is22_WithRoundCorners_AndEquippedIsGold()
    {
        var normal = InventoryIcons.Slot(SlotLook.Normal);
        var gold = InventoryIcons.Slot(SlotLook.Equipped);
        Assert.AreEqual(InventoryIcons.SlotSize, normal.GetLength(0));
        Assert.AreEqual("", normal[0, 0], "canto arredondado");
        Assert.AreEqual(InventoryIcons.Outline, normal[0, 5]);
        Assert.AreNotEqual(normal[1, 5], gold[1, 5], "a borda da equipada é dourada");
    }

    [Test]
    public void Cursor_HasFourGoldCorners_AndAnEmptyMiddle()
    {
        var c = InventoryIcons.Cursor();
        int n = InventoryIcons.CursorSize;
        Assert.AreEqual(n, c.GetLength(0));
        Assert.AreNotEqual("", c[1, 1]);
        Assert.AreNotEqual("", c[n - 2, n - 2]);
        Assert.AreEqual("", c[n / 2, n / 2], "o meio é vazado (o ícone aparece)");
        Assert.AreEqual("", c[1, n / 2], "entre os cantos não tem borda");
    }

    [Test]
    public void Panels_HaveTheSizeOfTheirSlice()
    {
        Assert.AreEqual(InventoryIcons.PanelSize, InventoryIcons.Panel(PanelLook.Main).GetLength(0));
        Assert.AreEqual(InventoryIcons.InsetSize, InventoryIcons.Panel(PanelLook.Inset).GetLength(0));
        Assert.AreEqual(InventoryIcons.TabSize, InventoryIcons.Panel(PanelLook.TabActive).GetLength(0));
        Assert.Less(2 * InventoryIcons.PanelBorder, InventoryIcons.PanelSize);
    }
}
