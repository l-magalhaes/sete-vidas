// O botão que desliga as armadilhas (TrapSwitch; pedido do usuário, 17/11/2026: "um asset melhor para esse botão, que
// se adeque com o lugar e o jogo"). Uma placa de pressão antiga, de pedra, embutida no chão da floresta: a moldura gasta
// com musgo nas quinas, a placa levantada com uma pata gravada que brilha em azul-claro (o azul continua sendo a cor do
// botão). Apertada, a placa afunda até a moldura e a pata apaga. 32 × 12, o meio do desenho no meio (como o de antes):
// no jogo (y 0,25 acima do chão) as 2 linhas de baixo ficam enterradas. Luz da lua no alto à esquerda. C# 5.
//   botao           a placa levantada, a pata acesa
//   botao_apertado  a placa afundada, a pata apagada
using System;
using System.IO;

public static class TrapButtonGen
{
    public static void Run(string projectRoot, string metaTemplatePath)
    {
        string template = File.ReadAllText(metaTemplatePath);
        string dir = Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Placeholder");
        PixelKit.Pal.Clear();
        PixelKit.P('k', "#15131c");   // contorno
        PixelKit.P('a', "#2c2a36"); PixelKit.P('b', "#45424f"); PixelKit.P('c', "#625e6c"); PixelKit.P('d', "#827d8a"); PixelKit.P('e', "#a39eaa"); // pedra
        PixelKit.P('n', "#1f1a1a");   // terra no vão
        PixelKit.P('m', "#2f4a33"); PixelKit.P('M', "#4c7046"); PixelKit.P('L', "#6e9a5a"); // musgo
        PixelKit.P('h', "#2b5f78"); PixelKit.P('g', "#58c8ee"); PixelKit.P('G', "#c8f2ff"); // o brilho da pata
        PixelKit.P('r', "#36404a");   // a pata apagada
        PixelKit.WriteSprite(Button(false), Path.Combine(dir, "botao.png"), "flo_prop/botao", template, false);
        PixelKit.WriteSprite(Button(true), Path.Combine(dir, "botao_apertado.png"), "flo_prop/botao_apertado", template, false);
    }

    static double N(int x, int y, int s)
    {
        unchecked { uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)s * 982451653u; h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) % 1000u) / 1000.0; }
    }

    public static PixelCanvas Button(bool pressed)
    {
        var c = new PixelCanvas(32, 12);
        // a moldura: uma laje gasta, um pouco mais larga embaixo, com as quinas lascadas
        for (int y = 6; y < 12; y++)
            for (int x = 0; x < 32; x++)
            {
                int inset = y == 6 ? 2 : y == 7 ? 1 : 0;
                if (x < inset || x > 31 - inset) continue;
                char ch = y == 6 ? 'd' : y == 7 ? 'c' : y < 10 ? 'b' : 'a';
                if (x < 3 && y > 7) ch = 'c';                                 // a luz na quina esquerda
                if (y >= 8 && N(x, y, 3) < 0.12) ch = 'a';                     // pedra gasta
                if (y >= 8 && (x == 11 || x == 21) && y < 11) ch = 'a';        // as juntas das pedras
                c.Force(x, y, ch);
            }
        // o vão da placa (escuro) e a placa
        for (int x = 6; x <= 25; x++) c.Force(x, 6, 'n');
        int top = pressed ? 5 : 2;                                             // apertada: afunda até a moldura
        for (int y = top; y <= 6; y++)
            for (int x = 7; x <= 24; x++)
            {
                char ch = y == top ? 'e' : y == top + 1 ? 'd' : 'c';
                if (x == 7 || x == 24) ch = y == top ? 'd' : 'b';
                c.Force(x, y, ch);
            }
        // a pata gravada na frente da placa: a almofada e os 3 dedos
        if (!pressed)
        {
            int cx = 15;
            foreach (var p in new[] { new[] { cx, 5 }, new[] { cx + 1, 5 }, new[] { cx - 1, 5 }, new[] { cx, 4 }, new[] { cx + 1, 4 } }) c.Force(p[0], p[1], 'g');
            c.Force(cx - 2, 3, 'g'); c.Force(cx, 3, 'G'); c.Force(cx + 2, 3, 'g');   // os dedos (o do meio mais aceso)
            // o halo azul na pedra em volta (em degraus, sem pontilhado)
            foreach (var p in new[] { new[] { cx - 3, 4 }, new[] { cx + 3, 4 }, new[] { cx - 2, 5 }, new[] { cx + 2, 5 }, new[] { cx - 3, 3 }, new[] { cx + 3, 3 } })
                if (c.Get(p[0], p[1]) != 'g' && c.Get(p[0], p[1]) != 'G') c.Force(p[0], p[1], 'h');
            c.Force(cx, 6, 'h'); c.Force(cx + 1, 6, 'h');                       // a luz escorrendo no vão
        }
        else
        {
            c.Force(15, 5, 'r'); c.Force(16, 5, 'r'); c.Force(14, 5, 'r');      // a pata, apagada, no topo da placa
        }
        // musgo nas quinas da moldura e um tufo caindo
        foreach (var p in new[] { new[] { 2, 5 }, new[] { 3, 5 }, new[] { 1, 6 }, new[] { 4, 6 }, new[] { 2, 6 }, new[] { 3, 6 }, new[] { 28, 5 }, new[] { 29, 6 }, new[] { 28, 6 }, new[] { 27, 6 }, new[] { 30, 7 } })
            c.Force(p[0], p[1], N(p[0], p[1], 7) < 0.4 ? 'L' : 'M');
        c.Force(1, 7, 'm'); c.Force(30, 8, 'm'); c.Force(2, 4, 'M'); c.Force(29, 4, 'L');
        PixelKit.Outline(c, 'k');
        return c;
    }
}
