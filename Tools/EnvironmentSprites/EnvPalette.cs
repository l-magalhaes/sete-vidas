// Paleta mestre do cenário (plano de assets, Etapa 1). FONTE DE VERDADE das cores de cenário:
// os geradores de sprites usam estas cores, e gerar.ps1 exporta .gpl (Aseprite/GIMP), .hex, uma prévia PNG e
// Docs/Arte/paleta.md a partir daqui. C# 5 (compilado pelo PowerShell).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

public class EnvSwatch
{
    public string Group, Name, Hex, Use;
    public EnvSwatch(string group, string name, string hex, string use) { Group = group; Name = name; Hex = hex; Use = use; }
    public Color Color { get { return ColorTranslator.FromHtml(Hex); } }
}

public static class EnvPalette
{
    /// <summary>Grupos na ordem em que aparecem na paleta e na documentação.</summary>
    public static readonly string[] Groups =
    {
        "Reservadas (jogo)", "Objetos barulhentos", "Céu do pôr do sol", "Silhuetas de fundo", "Rocha natural (quente)", "Pedra trabalhada (fria)",
        "Madeira", "Ordem da Presa Dourada", "Vegetação", "Água", "Fogo e luz", "Primatas", "Tinta",
    };

    public static List<EnvSwatch> All()
    {
        var p = new List<EnvSwatch>();
        string g;

        g = "Reservadas (jogo)";
        S(p, g, "Armadilha", "#c83a3a", "Só armadilhas (dano). Nenhum enfeite pode usar.");
        S(p, g, "Rachadura (brilho)", "#ffd34d", "Parede quebrável pelo Paladino. Rachadura decorativa é escura, sem brilho.");
        S(p, g, "Hera escalável (ramo)", "#4f5a2e", "Só parede escalável.");
        S(p, g, "Hera escalável (folha)", "#8a9a4e", "Só parede escalável.");
        S(p, g, "Praga (roxo)", "#5b2a6e", "Só a praga e o Tomo. O que identifica a praga é o PAR roxo + verde-ácido: o céu do pôr do sol pode ter roxo (o validador não checa o roxo nos fundos).");
        S(p, g, "Praga (verde-ácido)", "#8fd14f", "Só a praga e o Tomo.");
        S(p, g, "Matinho (esconderijo)", "#456633", "Só a moita jogável 32×16.");

        g = "Objetos barulhentos";
        S(p, g, "Cacos de vidro (brilho)", "#e8f0ff", "Brilho de 1 px sobre o chão. Não é exclusivo (a espuma da água é parecida), mas no chão do plano do jogo só os cacos brilham assim.");
        S(p, g, "Folhas secas", "#a0582e", "Tapete de folhas no exterior e no pátio.");
        S(p, g, "Folhas secas (sombra)", "#6e3a22", "Sombra das folhas.");
        S(p, g, "Sininhos (latão)", "#b08a3a", "Sininhos pendurados na altura da cabeça.");

        g = "Céu do pôr do sol";
        string[] sky = { "#1f1533", "#2e1a42", "#4a2150", "#6e2a5a", "#9a3a5a", "#c4504f", "#e0703f", "#f09a45", "#f7c063" };
        for (int i = 0; i < sky.Length; i++) S(p, g, "Céu " + (i + 1), sky[i], i == 0 ? "Topo" : i == sky.Length - 1 ? "Horizonte" : "Faixa do gradiente");
        S(p, g, "Sol", "#ffe9a8", "Disco do sol");
        S(p, g, "Sol (borda)", "#ffd070", "Borda do sol, janelas acesas de longe");
        S(p, g, "Nuvem", "#e88a7a", "Nuvem na sombra");
        S(p, g, "Nuvem (luz)", "#f2b0a0", "Nuvem na luz");

        g = "Silhuetas de fundo";
        S(p, g, "Montanha longe", "#3a2247", "bg_longe");
        S(p, g, "Montanha longe (luz)", "#6a3a6a", "Borda voltada para o sol");
        S(p, g, "Montanha longe (névoa)", "#8a5a7a", "Faixa de névoa");
        S(p, g, "Cidadela longe", "#2a1838", "Silhueta da Cidadela");
        S(p, g, "Morro", "#24162f", "bg_medio");
        S(p, g, "Morro (luz)", "#33203f", "Borda do morro");
        S(p, g, "Pinheiro longe", "#1a1024", "Pinheiros do bg_medio");

        g = "Rocha natural (quente)";
        S(p, g, "Rocha escura", "#3b3346", "Sombra, base");
        S(p, g, "Rocha", "#574c62", "Corpo");
        S(p, g, "Rocha clara", "#7b6d80", "Face iluminada");
        S(p, g, "Rocha (luz)", "#a08f98", "Quinas e estratos");

        g = "Pedra trabalhada (fria)";
        S(p, g, "Fundação", "#4a455c", "Blocos de Golden (ano 423)");
        S(p, g, "Fundação (luz)", "#5e5872", "Face dos blocos grandes");
        S(p, g, "Soluki", "#7a748e", "Tijolo militar");
        S(p, g, "Reforma", "#9a94aa", "Pedra nova, mais clara");
        S(p, g, "Argamassa nova", "#c8c2b8", "Juntas de reforma");
        S(p, g, "Placeholder chão", "#5a556e", "Tile de chão provisório");
        S(p, g, "Placeholder borda", "#3a364a", "Contorno do tile provisório");
        S(p, g, "Placeholder luz", "#77728c", "Topo de rampa, ranhuras");

        g = "Madeira";
        S(p, g, "Madeira velha", "#6b4a2b", "Estruturas antigas");
        S(p, g, "Madeira velha (luz)", "#9a7044", "Topo das tábuas");
        S(p, g, "Madeira nova", "#b08a5a", "Reformas (tábuas novas da arquibancada)");

        g = "Ordem da Presa Dourada";
        S(p, g, "Carmesim", "#8f2a2a", "Tecido da Ordem (não é o vermelho das armadilhas)");
        S(p, g, "Carmesim escuro", "#5a161c", "Sombra do tecido");
        S(p, g, "Dourado", "#d9a441", "Pata dourada, filetes");
        S(p, g, "Dourado escuro", "#8a621e", "Sombra do dourado");
        S(p, g, "Vitral azul", "#2a4a8f", "Vidro");
        S(p, g, "Vitral azul (luz)", "#4a7ac8", "Vidro na luz");
        S(p, g, "Pergaminho", "#e9dcb8", "Placas, alvos");

        g = "Vegetação";
        S(p, g, "Folha escura", "#2b3d2c", "Copa na sombra");
        S(p, g, "Folha", "#3f5a34", "Copa");
        S(p, g, "Folha clara", "#6f8a45", "Copa na luz");
        S(p, g, "Tronco escuro", "#3d2a22", "Tronco na sombra");
        S(p, g, "Tronco", "#5e4030", "Tronco");
        S(p, g, "Tronco claro", "#7d5a3e", "Casca na luz");
        S(p, g, "Hera decorativa", "#36524a", "Hera de fundo, azulada (nunca no plano do jogo)");
        S(p, g, "Musgo", "#36524a", "Juntas úmidas");
        S(p, g, "Luz de borda do pôr do sol", "#e0a060", "Rim light do lado do sol");
        S(p, g, "Vaga-lume", "#e6f2c8", "Partícula de 1 px (não amarelo: não parecer moeda)");

        g = "Água";
        S(p, g, "Água", "#9fd4f0", "Corpo");
        S(p, g, "Água média", "#6fb0e0", "Meio-tom");
        S(p, g, "Espuma", "#e8f8ff", "Espuma, brilho");
        S(p, g, "Água funda", "#4a86c0", "Sombra");

        g = "Fogo e luz";
        S(p, g, "Chama (miolo)", "#fff2b0", "Centro da chama");
        S(p, g, "Chama", "#ffa83a", "Corpo da chama");
        S(p, g, "Chama (ponta)", "#e0702a", "Ponta da chama de tocha (laranja, longe do vermelho das armadilhas)");

        g = "Primatas";
        S(p, g, "Estrada negra", "#15171c", "Pedra lisa sem juntas");
        S(p, g, "Estrada negra (luz)", "#2a2e36", "Face da placa");
        S(p, g, "Cristal de Éter", "#7fd8e0", "Brilho frio dos Vestígios");

        g = "Tinta";
        S(p, g, "Tinta", "#1c1a24", "Contorno e sombra mais escura do cenário");
        return p;
    }

    static void S(List<EnvSwatch> p, string group, string name, string hex, string use) { p.Add(new EnvSwatch(group, name, hex, use)); }

    /// <summary>
    /// Cores que enfeites (função decorativo, narrativo ou luz) não podem usar. Nos fundos (categoria plx)
    /// o roxo da praga é liberado: o céu do pôr do sol é roxo, e a praga se reconhece pelo par com o verde-ácido.
    /// </summary>
    public static List<EnvSwatch> Reserved(bool background)
    {
        var r = new List<EnvSwatch>();
        foreach (var s in All())
        {
            if (s.Group != Groups[0]) continue;
            if (background && s.Hex == "#5b2a6e") continue;
            r.Add(s);
        }
        return r;
    }

    // ---------------------------------------------------------------- exportação

    public static void Export(string projectRoot, string docsRefs)
    {
        var all = All();
        string arte = Path.Combine(projectRoot, "Docs/Arte");
        Directory.CreateDirectory(arte);

        // GIMP/Aseprite (.gpl)
        var gpl = new StringBuilder();
        gpl.Append("GIMP Palette\nName: Sete Vidas - Fortaleza\nColumns: 8\n#\n");
        var seen = new HashSet<string>();
        foreach (var s in all)
        {
            if (!seen.Add(s.Hex.ToLowerInvariant())) continue; // cor repetida (ex.: hera decorativa = musgo)
            var c = s.Color;
            gpl.AppendFormat("{0,3} {1,3} {2,3}\t{3} / {4}\n", c.R, c.G, c.B, s.Group, s.Name);
        }
        File.WriteAllText(Path.Combine(arte, "paleta_fortaleza.gpl"), gpl.ToString(), new UTF8Encoding(false));

        // .hex (Lospec)
        var hex = new StringBuilder();
        seen.Clear();
        foreach (var s in all) if (seen.Add(s.Hex.ToLowerInvariant())) hex.Append(s.Hex.TrimStart('#').ToLowerInvariant()).Append('\n');
        File.WriteAllText(Path.Combine(arte, "paleta_fortaleza.hex"), hex.ToString(), new UTF8Encoding(false));

        // prévia: uma linha por grupo, quadrados de 16 px
        int sq = 16, labelW = 180, rows = Groups.Length, cols = 0;
        foreach (var gname in Groups) cols = Math.Max(cols, all.FindAll(x => x.Group == gname).Count);
        using (var bmp = new Bitmap(labelW + cols * (sq + 2) + 4, rows * (sq + 4) + 4))
        using (var gfx = Graphics.FromImage(bmp))
        using (var font = new Font("Consolas", 8))
        {
            gfx.Clear(ColorTranslator.FromHtml("#14121a"));
            for (int r = 0; r < rows; r++)
            {
                int y = 4 + r * (sq + 4);
                gfx.DrawString(Groups[r], font, Brushes.Gainsboro, 4, y + 2);
                var row = all.FindAll(x => x.Group == Groups[r]);
                for (int i = 0; i < row.Count; i++)
                    using (var b = new SolidBrush(row[i].Color)) gfx.FillRectangle(b, labelW + i * (sq + 2), y, sq, sq);
            }
            Directory.CreateDirectory(docsRefs);
            bmp.Save(Path.Combine(docsRefs, "paleta_fortaleza.png"), ImageFormat.Png);
        }

        // Docs/Arte/paleta.md
        var md = new StringBuilder();
        md.Append("# Paleta mestre do cenário\n\n");
        md.Append("> Arquivo gerado por `Tools/EnvironmentSprites/gerar.ps1` a partir de `Tools/EnvironmentSprites/EnvPalette.cs`. **Não edite aqui:** mude o `.cs` e rode o script.\n\n");
        md.Append("Arquivos: `Docs/Arte/paleta_fortaleza.gpl` (abre no Aseprite e no GIMP), `Docs/Arte/paleta_fortaleza.hex` e a prévia `SeteVidasDocs/Referencias/paleta_fortaleza.png`.\n\n");
        md.Append("**Regras:** as cores reservadas nunca aparecem em enfeites (o validador do `gerar.ps1` confere). O fundo fica abaixo de 25% de brilho nas áreas noturnas e internas. Rocha natural é quente, pedra trabalhada é fria (plano de assets, 4.2).\n\n");
        foreach (var gname in Groups)
        {
            md.Append("## ").Append(gname).Append("\n\n| Cor | Hex | Uso |\n|---|---|---|\n");
            foreach (var s in all.FindAll(x => x.Group == gname))
                md.Append("| ").Append(s.Name).Append(" | `").Append(s.Hex).Append("` | ").Append(s.Use).Append(" |\n");
            md.Append('\n');
        }
        File.WriteAllText(Path.Combine(arte, "paleta.md"), md.ToString(), new UTF8Encoding(false));
    }
}

/// <summary>Procura cores reservadas nos PNGs dos enfeites.</summary>
public static class EnvValidator
{
    /// <summary>Distância RGB abaixo da qual uma cor "parece" a reservada.</summary>
    public const int Tolerance = 28;

    /// <summary>Devolve "hex do pixel ~ nome reservado" para cada cor reservada (ou quase) encontrada no PNG.</summary>
    public static List<string> FindReserved(string pngPath, bool background)
    {
        var hits = new List<string>();
        var reserved = EnvPalette.Reserved(background);
        var found = new HashSet<string>();
        using (var bmp = new Bitmap(pngPath))
            for (int x = 0; x < bmp.Width; x++)
                for (int y = 0; y < bmp.Height; y++)
                {
                    var c = bmp.GetPixel(x, y);
                    if (c.A < 128) continue;
                    foreach (var r in reserved)
                    {
                        var k = r.Color;
                        int dr = c.R - k.R, dg = c.G - k.G, db = c.B - k.B;
                        if (dr * dr + dg * dg + db * db > Tolerance * Tolerance) continue;
                        string key = string.Format("#{0:x2}{1:x2}{2:x2} ~ {3} ({4})", c.R, c.G, c.B, r.Name, r.Hex);
                        if (found.Add(key)) hits.Add(key);
                    }
                }
        return hits;
    }
}
