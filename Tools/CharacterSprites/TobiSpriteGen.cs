// A Tobi: toupeira engenheira (feminina), da cabeceira leste (F9). Sprites, animações, retratos e a ponte de madeira.
// Roteiro (Docs/Narrativa/floresta-roteiro.md): pequena, óculos de solda na testa, mochila de ferramentas maior do que
// ela; exagerada, orgulhosa, apaixonada por madeira, não trabalha fiado, enxerga mal. Retratos: animada, ofendida,
// gananciosa (olhos de moeda) e orgulhosa.
// Visual: pelo de veludo cinza-arroxeado, focinho comprido rosa, mãos enormes rosadas com garras claras (de cavar), olhos
// miudinhos com cílios, lenço vermelho no pescoço, avental de couro com ferramentas, a mochila de lona com serrote,
// martelo e régua saindo por cima.
// Canvas 48×48, pivô no meio da borda de baixo, olhando para a direita (o jogo espelha).
// C# 5. Compilado junto com PixelKit.cs por Tools/CharacterSprites/gerar.ps1 (ou gerar_tobi.ps1).
using System;
using System.Collections.Generic;
using System.Drawing;

public static class TobiSpriteGen
{
    public const int W = 48, H = 48;
    const int Ground = 46;

    public static void InitPalette()
    {
        PixelKit.Pal.Clear();
        PixelKit.P('o', "#1a1418");                                                    // contorno
        PixelKit.P('F', "#5e5262"); PixelKit.P('f', "#3e3444"); PixelKit.P('l', "#7e7084"); // pelo de veludo
        PixelKit.P('B', "#8c7c88");                                                    // barriga
        PixelKit.P('N', "#f08c9c"); PixelKit.P('n', "#c8586e"); PixelKit.P('h', "#ffc0c8"); // focinho, ponta, brilho
        PixelKit.P('P', "#f2a0ac"); PixelKit.P('p', "#c87884"); PixelKit.P('c', "#fff0e0"); // mãos, garras
        PixelKit.P('E', "#14101a"); PixelKit.P('x', "#ffffff");                         // olhos
        PixelKit.P('Y', "#d8a640"); PixelKit.P('y', "#8a6420"); PixelKit.P('T', "#5ad0c8"); PixelKit.P('t', "#2a8a86"); // óculos
        PixelKit.P('R', "#d83c3c"); PixelKit.P('r', "#962430");                         // lenço
        PixelKit.P('A', "#8a5a32"); PixelKit.P('a', "#5e3a1e");                         // avental de couro
        PixelKit.P('K', "#8a7448"); PixelKit.P('k', "#5e4c2c"); PixelKit.P('j', "#a89060"); // mochila de lona
        PixelKit.P('I', "#9aa0ac"); PixelKit.P('i', "#5c6270"); PixelKit.P('v', "#d8dce4"); // ferro (ferramentas)
        PixelKit.P('W', "#b07a44"); PixelKit.P('w', "#7a4e26"); PixelKit.P('q', "#d29a5c"); // madeira
        PixelKit.P('G', "#e8c040"); PixelKit.P('g', "#a8801c");                         // ouro (olhos de moeda)
        PixelKit.P('D', "#7a6450"); PixelKit.P('d', "#4e3e30");                         // terra
        PixelKit.P('S', "#c8b08a");                                                    // corda
        // a ponte nova (18/11/2026, a referência do usuário): vigas e postes escuros, tábuas avermelhadas
        PixelKit.P('1', "#3a241c"); PixelKit.P('2', "#583829"); PixelKit.P('6', "#4a2c21"); PixelKit.P('7', "#6e4636");
        PixelKit.P('3', "#74412c"); PixelKit.P('4', "#8e5536"); PixelKit.P('5', "#55301f");
    }

    public class Pose
    {
        public int Dy;                 // respiração
        public int Lean;               // o tronco inclina (x)
        public int Nose;               // o focinho mexe (0, 1)
        public string Face = "n";      // n, talk, blink, greedy, offended, proud, wow, squint
        public string Arms = "down";   // down, talk, rub, cross, hips, up, carry, hammer, pull, dig
        public int Hand;               // variação do gesto
        public string Prop = "";       // wrench, planks, hammer, drill, rope
        public int Stagger;            // cambaleando com as tábuas
        public float Sink;             // 0..1 entrando na terra (cavando)
        public bool Mound;             // o montinho de terra em cima do buraco
        public int Spin;               // a broca girando a Tobi (0..3)
        public int Pack;               // a mochila balançando (-1..1)
        public Pose Clone() { return (Pose)MemberwiseClone(); }
    }

    static Pose With(Pose b, Action<Pose> e) { var p = b.Clone(); e(p); return p; }

    // ------------------------------------------------------------- o corpo

    static PixelCanvas Body(Pose p)
    {
        var cv = new PixelCanvas(W, H);
        int dy = p.Dy, lx = p.Lean + p.Stagger;
        Backpack(cv, p, lx, dy);
        Feet(cv, p);
        // o corpo: gordinho, de veludo, a barriga clara
        PixelKit.Ellipse(cv, 23.5f + lx * 0.5f, 38.5f + dy, 6.5f, 7f, (a, b) => a < -0.55f ? 'f' : (b < -0.6f && a > -0.2f ? 'l' : 'F'));
        PixelKit.Ellipse(cv, 25f + lx * 0.5f, 40f + dy, 3.6f, 4.6f, (a, b) => 'B');
        Apron(cv, lx, dy);
        Head(cv, p, lx, dy);
        Arms(cv, p, lx, dy);
        PropOver(cv, p, lx, dy);
        return cv;
    }

    static void Feet(PixelCanvas cv, Pose p)
    {
        int s = p.Stagger;
        // pés rosados, largos, de garras claras
        for (int x = 19; x <= 22; x++) cv.Set(x + s, Ground, x == 22 ? 'c' : 'P');
        for (int x = 25; x <= 29; x++) cv.Set(x - s, Ground, x == 29 ? 'c' : 'P');
        cv.Set(20 + s, Ground - 1, 'p'); cv.Set(26 - s, Ground - 1, 'p'); cv.Set(27 - s, Ground - 1, 'P');
    }

    /// <summary>A mochila de lona, maior do que ela, com o serrote, o martelo e a régua saindo por cima.</summary>
    static void Backpack(PixelCanvas cv, Pose p, int lx, int dy)
    {
        int ox = 10 + lx / 2 + p.Pack, oy = 21 + dy;
        for (int y = 0; y <= 21; y++)
            for (int x = 0; x <= 9; x++)
            {
                bool corner = (y == 0 || y == 21) && (x == 0 || x == 9);
                if (corner) continue;
                char c = x <= 1 ? 'k' : (x >= 8 ? 'j' : 'K');
                if (y == 6 || y == 7) c = x % 3 == 0 ? 'y' : 'k';                    // a tira com fivela
                if (y >= 12 && y <= 17 && x >= 3 && x <= 7) c = (y == 12 || x == 3 || x == 7) ? 'k' : 'K';   // o bolso
                if (y == 14 && x == 5) c = 'Y';
                cv.Set(ox + x, oy + y, c);
            }
        // ferramentas saindo por cima: o serrote (dentes), o cabo do martelo e a régua amarela
        for (int y = -8; y <= 0; y++) { cv.Set(ox + 2, oy + y, 'I'); cv.Set(ox + 3, oy + y, y % 2 == 0 ? 'i' : 'I'); }
        cv.Set(ox + 2, oy - 9, 'w'); cv.Set(ox + 3, oy - 9, 'W');
        for (int y = -6; y <= 0; y++) cv.Set(ox + 5, oy + y, 'W');
        for (int x = 4; x <= 7; x++) { cv.Set(ox + x, oy - 7, 'i'); cv.Set(ox + x, oy - 8, 'I'); }
        for (int y = -5; y <= 0; y++) cv.Set(ox + 8, oy + y, y % 2 == 0 ? 'G' : 'g');
    }

    static void Apron(PixelCanvas cv, int lx, int dy)
    {
        int x0 = 23 + lx / 2;
        for (int y = 35; y <= 44; y++)
            for (int x = x0; x <= x0 + 5; x++)
            {
                if (y > 42 && (x == x0 || x == x0 + 5)) continue;
                cv.Force(x, y + dy, x == x0 || y == 44 ? 'a' : 'A');
            }
        // a chave pequena e o lápis no bolso do avental
        cv.Force(x0 + 2, 39 + dy, 'I'); cv.Force(x0 + 2, 38 + dy, 'v'); cv.Force(x0 + 4, 38 + dy, 'Y'); cv.Force(x0 + 4, 39 + dy, 'W');
        for (int x = x0 + 1; x <= x0 + 4; x++) cv.Force(x, 40 + dy, 'a');
    }

    /// <summary>A cabeça: redonda, os óculos de solda na testa, o focinho comprido rosa e os olhos miudinhos.</summary>
    static void Head(PixelCanvas cv, Pose p, int lx, int dy)
    {
        float cx = 25.5f + lx, cy = 27f + dy;
        if (p.Face == "proud") cy -= 1;                              // o queixo para cima
        int hy = (int)cy;
        PixelKit.Ellipse(cv, cx, cy, 5.8f, 5.2f, (a, b) => a < -0.6f ? 'f' : (b < -0.55f ? 'l' : 'F'));
        // o lenço vermelho no pescoço, com o nó do lado
        for (int x = (int)cx - 4; x <= (int)cx + 3; x++) cv.Force(x, hy + 5, x % 2 == 0 ? 'R' : 'r');
        cv.Force((int)cx - 5, hy + 5, 'R'); cv.Force((int)cx - 6, hy + 6, 'R'); cv.Force((int)cx - 5, hy + 6, 'r');
        // o focinho: comprido, rosa, para a direita (mexe)
        int nx = (int)cx + 4, ny = hy + 1 - (p.Face == "proud" ? 1 : 0) - p.Nose;
        for (int x = 0; x <= 5; x++)
        {
            cv.Force(nx + x, ny, x >= 5 ? 'n' : 'N');
            cv.Force(nx + x, ny + 1, x >= 5 ? 'n' : (x >= 3 ? 'N' : 'N'));
            if (x <= 3) cv.Force(nx + x, ny + 2, 'p');
        }
        cv.Force(nx + 1, ny, 'h'); cv.Force(nx + 6, ny, 'n'); cv.Force(nx + 6, ny + 1, 'n');
        // bigodes
        cv.Set(nx + 2, ny - 1, 'c'); cv.Set(nx + 4, ny + 3, 'c');
        // a boca
        switch (p.Face)
        {
            case "talk": case "wow": cv.Force(nx + 1, ny + 3, 'E'); cv.Force(nx + 2, ny + 3, 'n'); cv.Force(nx + 1, ny + 4, 'n'); break;
            case "greedy": case "proud": cv.Force(nx, ny + 3, 'E'); cv.Force(nx + 1, ny + 3, 'E'); cv.Force(nx + 2, ny + 2, 'E'); break;
            case "offended": cv.Force(nx, ny + 3, 'E'); cv.Force(nx + 1, ny + 3, 'E'); break;
            default: cv.Force(nx + 1, ny + 3, 'E'); break;
        }
        // os olhos miudinhos (ela enxerga mal) com cílios
        int ex = (int)cx + 2, ey = hy - 1;
        switch (p.Face)
        {
            case "blink": case "proud": cv.Force(ex, ey, 'E'); cv.Force(ex + 1, ey, 'E'); cv.Force(ex + 1, ey - 1, 'E'); break;
            case "greedy":   // olhos de moeda
                cv.Force(ex, ey, 'G'); cv.Force(ex + 1, ey, 'G'); cv.Force(ex, ey - 1, 'G'); cv.Force(ex + 1, ey - 1, 'g'); cv.Force(ex + 1, ey + 1, 'g');
                break;
            case "offended": cv.Force(ex, ey, 'E'); cv.Force(ex - 1, ey - 1, 'E'); cv.Force(ex + 1, ey - 1, 'E'); break;   // a sobrancelha franzida
            case "squint": cv.Force(ex, ey, 'E'); cv.Force(ex + 1, ey, 'E'); break;
            case "wow": cv.Force(ex, ey, 'E'); cv.Force(ex, ey - 1, 'E'); cv.Force(ex + 1, ey - 1, 'x'); cv.Force(ex + 1, ey, 'E'); break;
            default: cv.Force(ex, ey, 'E'); cv.Force(ex + 1, ey - 1, 'E'); break;   // o olho e o cílio
        }
        // os óculos de solda na testa: a tira escura e as duas lentes verdes de aro dourado
        int gy = hy - 4;
        for (int x = (int)cx - 5; x <= (int)cx + 4; x++) cv.Force(x, gy + 1, 'f');
        Lens(cv, (int)cx - 1, gy, p.Face == "wow");
        Lens(cv, (int)cx + 3, gy, p.Face == "wow");
    }

    static void Lens(PixelCanvas cv, int x, int y, bool shine)
    {
        cv.Force(x - 1, y, 'Y'); cv.Force(x + 1, y, 'Y'); cv.Force(x, y - 1, 'Y'); cv.Force(x, y + 1, 'y');
        cv.Force(x - 1, y - 1, 'y'); cv.Force(x + 1, y - 1, 'Y'); cv.Force(x - 1, y + 1, 'y'); cv.Force(x + 1, y + 1, 'y');
        cv.Force(x, y, shine ? 'x' : 'T');
    }

    /// <summary>As mãos enormes, rosadas, de garras claras (mãos de cavar).</summary>
    static void Paw(PixelCanvas cv, int x, int y, bool clawsUp)
    {
        PixelKit.Ellipse(cv, x + 0.5f, y + 0.5f, 1.9f, 1.6f, (a, b) => b > 0.3f ? 'p' : 'P');
        int cyc = clawsUp ? y - 2 : y + 2;
        cv.Force(x - 1, cyc, 'c'); cv.Force(x + 1, cyc, 'c'); cv.Force(x, cyc, 'c');
    }

    static void Arm(PixelCanvas cv, int sx, int sy, int hx, int hy)
    {
        PixelKit.Thick(cv, sx, sy, hx, hy, 2, (t, j) => j == 0 ? 'F' : 'f');
    }

    static void Arms(PixelCanvas cv, Pose p, int lx, int dy)
    {
        int sx = 27 + lx / 2, sy = 34 + dy;          // ombro da frente
        int bx = 21 + lx / 2;                        // ombro de trás
        switch (p.Arms)
        {
            case "talk":   // gesticula como na ópera
                Arm(cv, sx, sy, sx + 4, sy - 3 - p.Hand * 2); Paw(cv, sx + 5, sy - 4 - p.Hand * 2, true);
                Arm(cv, bx, sy, bx - 1, sy + 4); Paw(cv, bx - 1, sy + 5, false);
                break;
            case "rub":    // esfregando as mãos (gananciosa)
                Arm(cv, sx, sy, sx + 2, sy + 2); Arm(cv, bx, sy, sx + 1 - p.Hand, sy + 3);
                Paw(cv, sx + 3 - p.Hand, sy + 2, true); Paw(cv, sx + 1 + p.Hand, sy + 3, true);
                break;
            case "cross":  // braços cruzados (ofendida)
                Arm(cv, sx, sy, bx + 1, sy + 3); Arm(cv, bx, sy, sx + 1, sy + 2);
                Paw(cv, bx, sy + 3, false); Paw(cv, sx + 2, sy + 2, false);
                break;
            case "hips":   // mãos na cintura (orgulhosa)
                Arm(cv, sx, sy, sx + 3, sy + 3); Paw(cv, sx + 3, sy + 4, false);
                Arm(cv, bx, sy, bx - 3, sy + 3); Paw(cv, bx - 3, sy + 4, false);
                break;
            case "up":     // segurando algo grande acima da cabeça
                Arm(cv, sx, sy, sx + 2, sy - 9); Paw(cv, sx + 2, sy - 10, true);
                Arm(cv, bx, sy, bx + 2, sy - 9); Paw(cv, bx + 2, sy - 10, true);
                break;
            case "carry":  // abraçando a pilha de tábuas
                Arm(cv, sx, sy, sx + 4, sy - 1); Paw(cv, sx + 5, sy - 1, true);
                Arm(cv, bx, sy, sx + 3, sy + 2); Paw(cv, sx + 4, sy + 2, true);
                break;
            case "hammer": // o martelo sobe e desce
                Arm(cv, sx, sy, sx + 4, p.Hand == 0 ? sy - 6 : sy + 1); Paw(cv, sx + 5, p.Hand == 0 ? sy - 7 : sy + 1, true);
                Arm(cv, bx, sy, bx - 1, sy + 4); Paw(cv, bx - 1, sy + 5, false);
                break;
            case "pull":   // puxando a corda
                Arm(cv, sx, sy, sx + 5, sy - 1 + p.Hand); Paw(cv, sx + 6, sy - 1 + p.Hand, true);
                Arm(cv, bx, sy, sx + 2, sy + p.Hand); Paw(cv, sx + 3, sy + p.Hand, true);
                break;
            case "dig":    // as garras para a frente, cavando
                Arm(cv, sx, sy, sx + 5, sy + 4 + p.Hand); Paw(cv, sx + 6, sy + 5 + p.Hand, false);
                Arm(cv, bx, sy, sx + 3, sy + 5 - p.Hand); Paw(cv, sx + 4, sy + 6 - p.Hand, false);
                break;
            default:       // soltos, as mãos grandes ao lado do corpo
                Arm(cv, sx, sy, sx + 2, sy + 5); Paw(cv, sx + 2, sy + 6, false);
                Arm(cv, bx, sy, bx - 1, sy + 5); Paw(cv, bx - 1, sy + 6, false);
                break;
        }
    }

    /// <summary>A ferramenta (ou as tábuas) na mão, por cima de tudo.</summary>
    static void PropOver(PixelCanvas cv, Pose p, int lx, int dy)
    {
        int sx = 27 + lx / 2, sy = 34 + dy, bx = 21 + lx / 2;
        switch (p.Prop)
        {
            case "wrench":   // a chave inglesa maior do que ela, erguida
            {
                int hx = (sx + bx) / 2 + 3, top = sy - 10 - 14;
                for (int y = top + 5; y <= sy - 9; y++) { cv.Force(hx, y, 'I'); cv.Force(hx + 1, y, 'i'); }
                // a boca da chave (em cima)
                for (int x = hx - 3; x <= hx + 4; x++) { cv.Force(x, top + 4, 'I'); cv.Force(x, top + 3, 'v'); }
                for (int y = top; y <= top + 3; y++) { cv.Force(hx - 3, y, 'I'); cv.Force(hx - 2, y, 'i'); cv.Force(hx + 3, y, 'I'); cv.Force(hx + 4, y, 'i'); }
                cv.Force(hx - 3, top - 1, 'v'); cv.Force(hx + 3, top - 1, 'v');
                // o parafuso de ajuste
                cv.Force(hx, top + 7, 'Y'); cv.Force(hx + 1, top + 7, 'y');
                break;
            }
            case "planks":   // a pilha de tábuas, alta e torta
                for (int i = 0; i < 6; i++)
                {
                    int y = sy - 1 - i * 2, off = (i % 2 == 0 ? 0 : 1) * (p.Stagger != 0 ? p.Stagger : 1) + (i >= 4 ? p.Stagger : 0);
                    for (int x = sx - 2; x <= sx + 9; x++) { cv.Force(x + off, y, i % 2 == 0 ? 'W' : 'q'); cv.Force(x + off, y + 1, 'w'); }
                    cv.Force(sx - 2 + off, y, 'w'); cv.Force(sx + 9 + off, y, 'w');
                }
                break;
            case "hammer":
            {
                int hx = sx + 5, hy = p.Hand == 0 ? sy - 8 : sy + 1;
                for (int y = hy - 3; y <= hy; y++) cv.Force(hx, y, 'W');
                for (int x = hx - 1; x <= hx + 2; x++) { cv.Force(x, hy - 4, 'I'); cv.Force(x, hy - 5, 'i'); }
                break;
            }
            case "drill":    // a broca girando (as linhas de giro em volta)
            {
                int hx = sx + 6, hy = sy + 2;
                for (int x = hx; x <= hx + 5; x++) cv.Force(x, hy, x % 2 == p.Spin % 2 ? 'v' : 'I');
                cv.Force(hx + 6, hy, 'v');
                for (int y = hy - 1; y <= hy + 1; y++) { cv.Force(hx - 1, y, 'Y'); cv.Force(hx - 2, y, 'y'); }
                break;
            }
            case "rope":     // a ponta da corda saindo da mão para trás
                for (int x = sx + 6; x >= 2; x--) cv.Force(x, sy - 1 + p.Hand + ((x / 3) % 2), 'S');
                break;
        }
    }

    /// <summary>Corpo com contorno; cavando, o que está abaixo da linha da terra some e aparece o montinho.</summary>
    static PixelCanvas Render(Pose p)
    {
        var body = Body(p);
        if (p.Spin > 0) body = SpinBody(body, p.Spin);
        PixelKit.Outline(body, 'o', "");
        if (p.Sink <= 0f && !p.Mound) return body;
        // entrando na terra: desce o desenho e corta embaixo do chão
        int down = (int)Math.Round(p.Sink * 28f);
        var cv = new PixelCanvas(W, H);
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                int sy = y - down;
                if (sy < 0) continue;
                char c = body.Px[x, sy];
                if (c != '.' && y <= Ground) cv.Px[x, y] = c;
            }
        // o buraco e a terra voando
        if (p.Sink > 0f || p.Mound)
            PixelKit.Ellipse(cv, 26f, Ground + 0.5f, 7f, p.Mound ? 3.5f : 2.2f, (a, b) => b < -0.2f ? (PixelKit.Noise((int)(a * 9), (int)(b * 9), 3) > 0.5f ? 'D' : 'd') : 'd');
        if (p.Sink > 0f && p.Sink < 1f)
            for (int i = 0; i < 6; i++)
            {
                int x = 18 + (int)(PixelKit.Noise(i, down, 7) * 16f), y = Ground - 4 - (int)(PixelKit.Noise(i, down, 9) * 8f);
                cv.Force(x, y, i % 2 == 0 ? 'D' : 'd');
            }
        PixelKit.Outline(cv, 'o', "");
        return cv;
    }

    /// <summary>A broca girando a Tobi: o desenho espelhado e deitado aos poucos.</summary>
    static PixelCanvas SpinBody(PixelCanvas src, int spin)
    {
        var turned = spin % 2 == 1 ? src.Mirror() : src;
        return PixelKit.Rotate(turned, spin * 30f, 24f, 34f, 0f, 0f);
    }

    static readonly List<PixelKit.Clip> Clips = new List<PixelKit.Clip>();

    static void Add(string name, float t, bool loop, params Pose[] poses)
    {
        var c = new PixelKit.Clip { Name = name, FrameTime = t, Loop = loop };
        foreach (var p in poses) c.Frames.Add(Render(p));
        Clips.Add(c);
    }

    static void BuildClips()
    {
        Clips.Clear();
        var std = new Pose();

        // parada: respira, o focinho fareja, a mochila balança; pisca
        Add("idle", 0.22f, true,
            std,
            With(std, p => { p.Nose = 1; }),
            With(std, p => { p.Dy = 1; p.Pack = 1; }),
            With(std, p => { p.Dy = 1; p.Pack = 1; p.Nose = 1; }),
            With(std, p => { p.Face = "blink"; }),
            With(std, p => { p.Pack = -1; p.Face = "squint"; }));
        // falando como na ópera
        Add("talk", 0.13f, true,
            With(std, p => { p.Face = "talk"; p.Arms = "talk"; p.Hand = 1; }),
            With(std, p => { p.Arms = "talk"; p.Hand = 0; p.Nose = 1; }),
            With(std, p => { p.Face = "talk"; p.Arms = "talk"; p.Hand = 1; p.Dy = 1; }),
            With(std, p => { p.Arms = "talk"; p.Hand = 0; p.Dy = 1; }));
        // gananciosa: olhos de moeda, esfregando as mãos
        Add("greedy", 0.12f, true,
            With(std, p => { p.Face = "greedy"; p.Arms = "rub"; p.Hand = 0; }),
            With(std, p => { p.Face = "greedy"; p.Arms = "rub"; p.Hand = 1; p.Nose = 1; }),
            With(std, p => { p.Face = "greedy"; p.Arms = "rub"; p.Hand = 0; p.Dy = 1; }));
        // ofendida: braços cruzados, o focinho para cima
        Add("offended", 0.3f, true,
            With(std, p => { p.Face = "offended"; p.Arms = "cross"; p.Lean = -1; }),
            With(std, p => { p.Face = "offended"; p.Arms = "cross"; p.Lean = -1; p.Nose = 1; p.Dy = 1; }));
        // orgulhosa: mãos na cintura, o queixo para cima
        Add("proud", 0.3f, true,
            With(std, p => { p.Face = "proud"; p.Arms = "hips"; p.Lean = 1; }),
            With(std, p => { p.Face = "proud"; p.Arms = "hips"; p.Lean = 1; p.Dy = 1; p.Pack = 1; }));
        // tira da mochila a chave inglesa maior do que ela
        Add("wrench", 0.12f, false,
            With(std, p => { p.Arms = "up"; p.Face = "wow"; p.Dy = 1; }),
            With(std, p => { p.Arms = "up"; p.Prop = "wrench"; p.Face = "wow"; }),
            With(std, p => { p.Arms = "up"; p.Prop = "wrench"; p.Face = "proud"; p.Dy = -1; }),
            With(std, p => { p.Arms = "up"; p.Prop = "wrench"; p.Face = "proud"; }));
        // a pilha de tábuas nos braços, cambaleando como uma torre de pratos
        Add("carry", 0.14f, true,
            With(std, p => { p.Arms = "carry"; p.Prop = "planks"; p.Face = "wow"; p.Stagger = -1; }),
            With(std, p => { p.Arms = "carry"; p.Prop = "planks"; p.Face = "squint"; p.Stagger = 0; p.Dy = 1; }),
            With(std, p => { p.Arms = "carry"; p.Prop = "planks"; p.Face = "wow"; p.Stagger = 1; }),
            With(std, p => { p.Arms = "carry"; p.Prop = "planks"; p.Face = "squint"; p.Stagger = 0; p.Dy = 1; }));
        // cava e some na terra (é uma toupeira!)
        Add("dig", 0.09f, false,
            With(std, p => { p.Arms = "dig"; p.Face = "squint"; p.Hand = 0; }),
            With(std, p => { p.Arms = "dig"; p.Face = "squint"; p.Hand = 1; p.Sink = 0.2f; }),
            With(std, p => { p.Arms = "dig"; p.Face = "squint"; p.Hand = 0; p.Sink = 0.45f; }),
            With(std, p => { p.Arms = "dig"; p.Face = "squint"; p.Hand = 1; p.Sink = 0.7f; }),
            With(std, p => { p.Arms = "dig"; p.Sink = 0.95f; }),
            With(std, p => { p.Sink = 1f; p.Mound = true; }));
        // sai da terra (do outro lado), puxando a corda
        Add("emerge", 0.09f, false,
            With(std, p => { p.Sink = 1f; p.Mound = true; }),
            With(std, p => { p.Arms = "up"; p.Face = "wow"; p.Sink = 0.7f; }),
            With(std, p => { p.Arms = "up"; p.Face = "wow"; p.Sink = 0.4f; }),
            With(std, p => { p.Arms = "pull"; p.Prop = "rope"; p.Face = "talk"; p.Sink = 0.1f; }),
            With(std, p => { p.Arms = "pull"; p.Prop = "rope"; p.Face = "proud"; }));
        // puxando a corda (a primeira viga)
        Add("pull", 0.12f, true,
            With(std, p => { p.Arms = "pull"; p.Prop = "rope"; p.Lean = -2; p.Hand = 0; p.Face = "squint"; }),
            With(std, p => { p.Arms = "pull"; p.Prop = "rope"; p.Lean = -3; p.Hand = 1; p.Face = "squint"; p.Dy = 1; }));
        // martelando
        Add("hammer", 0.08f, true,
            With(std, p => { p.Arms = "hammer"; p.Prop = "hammer"; p.Hand = 0; p.Face = "squint"; }),
            With(std, p => { p.Arms = "hammer"; p.Prop = "hammer"; p.Hand = 1; p.Face = "wow"; p.Dy = 1; }));
        // a broca que gira a própria Tobi
        Add("drill", 0.06f, true,
            With(std, p => { p.Arms = "dig"; p.Prop = "drill"; p.Spin = 0; p.Face = "wow"; }),
            With(std, p => { p.Arms = "dig"; p.Prop = "drill"; p.Spin = 1; p.Face = "wow"; }),
            With(std, p => { p.Arms = "dig"; p.Prop = "drill"; p.Spin = 2; p.Face = "wow"; }),
            With(std, p => { p.Arms = "dig"; p.Prop = "drill"; p.Spin = 3; p.Face = "wow"; }));
    }

    // ------------------------------------------------------------- retratos

    // 48×48, olhando para a direita (espelhado depois: a Tobi fala do lado direito da tela).
    static PixelCanvas Portrait(string expr)
    {
        var cv = new PixelCanvas(48, 48);
        // a mochila atrás do ombro, com o serrote e o martelo
        for (int y = 26; y < 48; y++) for (int x = 0; x <= 9; x++) cv.Force(x, y, x <= 1 ? 'k' : (x >= 8 ? 'j' : 'K'));
        for (int y = 16; y < 26; y++) { cv.Force(3, y, 'I'); cv.Force(4, y, y % 2 == 0 ? 'i' : 'I'); }
        for (int y = 19; y < 26; y++) cv.Force(7, y, 'W');
        for (int x = 6; x <= 9; x++) { cv.Force(x, 18, 'I'); cv.Force(x, 17, 'i'); }
        // os ombros e o lenço
        PixelKit.Ellipse(cv, 24f, 52f, 18f, 12f, (a, b) => a < -0.5f ? 'f' : 'F');
        for (int x = 12; x <= 36; x++) { int y = 41 + (Math.Abs(x - 24) > 9 ? 1 : 0); cv.Force(x, y, x % 2 == 0 ? 'R' : 'r'); cv.Force(x, y + 1, 'r'); }
        cv.Force(10, 42, 'R'); cv.Force(9, 43, 'R'); cv.Force(10, 43, 'r');
        // a cabeça redonda
        bool proud = expr == "orgulhosa";
        float cy = proud ? 24f : 25f;
        PixelKit.Ellipse(cv, 22f, cy, 14f, 13f, (a, b) => a < -0.65f ? 'f' : (b < -0.6f ? 'l' : 'F'));
        // o focinho comprido rosa
        int ny = (int)cy + 2 - (proud ? 2 : 0);
        for (int y = ny - 3; y <= ny + 4; y++)
            for (int x = 30; x <= 46; x++)
            {
                float k = (x - 30) / 16f;
                int top = ny - 3 + (int)(k * 2f), bot = ny + 4 - (int)(k * 3f);
                if (y < top || y > bot) continue;
                cv.Force(x, y, x >= 43 ? 'n' : (y == top ? 'h' : (y >= bot - 1 ? 'p' : 'N')));
            }
        cv.Force(46, ny - 1, 'n'); cv.Force(46, ny, 'n');
        // os bigodes
        // dois fios para cima e dois para baixo, saindo do meio do focinho
        foreach (var q in PixelKit.Line(38, ny - 4, 42, ny - 8)) cv.Force(q.X, q.Y, 'c');
        foreach (var q in PixelKit.Line(40, ny - 4, 45, ny - 7)) cv.Force(q.X, q.Y, 'c');
        foreach (var q in PixelKit.Line(38, ny + 4, 42, ny + 8)) cv.Force(q.X, q.Y, 'c');
        foreach (var q in PixelKit.Line(40, ny + 3, 45, ny + 6)) cv.Force(q.X, q.Y, 'c');
        // a boca
        switch (expr)
        {
            case "animada": for (int x = 33; x <= 37; x++) cv.Force(x, ny + 4, 'E'); cv.Force(34, ny + 5, 'n'); cv.Force(35, ny + 5, 'n'); cv.Force(36, ny + 5, 'n'); break;
            case "gananciosa": for (int x = 32; x <= 38; x++) cv.Force(x, ny + 4 - (x > 36 ? 1 : 0), 'E'); break;
            case "ofendida": for (int x = 33; x <= 36; x++) cv.Force(x, ny + 5, 'E'); break;
            default: for (int x = 32; x <= 37; x++) cv.Force(x, ny + 4 - (x > 35 ? 1 : 0), 'E'); break;
        }
        // os olhos miudinhos
        int ex = 27, ey = (int)cy - 3;
        switch (expr)
        {
            case "gananciosa":
                PixelKit.Ellipse(cv, ex + 1f, ey + 0.5f, 2.2f, 2.2f, (a, b) => a * a + b * b > 0.5f ? 'g' : 'G'); cv.Force(ex, ey - 1, 'x'); cv.Force(ex + 1, ey, 'g');
                break;
            case "ofendida":
                cv.Force(ex, ey + 1, 'E'); cv.Force(ex + 1, ey + 1, 'E');
                for (int x = ex - 2; x <= ex + 2; x++) cv.Force(x, ey - 1 + (x > ex ? 1 : 0), 'f');   // a sobrancelha franzida
                break;
            case "orgulhosa":
                cv.Force(ex - 1, ey + 1, 'E'); cv.Force(ex, ey, 'E'); cv.Force(ex + 1, ey, 'E'); cv.Force(ex + 2, ey + 1, 'E');   // fechados, contente
                break;
            default:
                cv.Force(ex, ey, 'E'); cv.Force(ex + 1, ey, 'E'); cv.Force(ex, ey + 1, 'E'); cv.Force(ex + 1, ey + 1, 'E'); cv.Force(ex, ey, 'x');
                cv.Force(ex + 2, ey - 1, 'E'); cv.Force(ex + 3, ey - 2, 'E');   // os cílios
                break;
        }
        // as bochechas e os óculos de solda na testa
        cv.Force(25, ny + 3, 'N'); cv.Force(26, ny + 3, 'N');
        int gy = (int)cy - 9;
        for (int x = 9; x <= 34; x++) cv.Force(x, gy + 2, 'f');
        BigLens(cv, 20, gy, expr == "animada");
        BigLens(cv, 29, gy, expr == "animada");
        PixelKit.Outline(cv, 'o', "");
        if (expr == "gananciosa")
        {
            // uma moeda brilhando na mão levantada
            PixelKit.Ellipse(cv, 41.5f, 41f, 3f, 3f, (a, b) => a * a + b * b > 0.55f ? 'g' : 'G');
            cv.Force(40, 40, 'x');
            for (int y = 44; y < 48; y++) for (int x = 38; x <= 44; x++) cv.Force(x, y, x == 38 ? 'p' : 'P');
        }
        return cv;
    }

    static void BigLens(PixelCanvas cv, int x, int y, bool shine)
    {
        PixelKit.Ellipse(cv, x + 0.5f, y + 0.5f, 3.6f, 3.2f, (a, b) =>
        {
            float d = a * a + b * b;
            if (d > 0.55f) return b < 0 ? 'Y' : 'y';
            return (a < -0.1f && b < -0.1f) || shine ? 'T' : 't';
        });
        cv.Force(x - 1, y - 1, 'x');
    }

    static readonly string[][] PortraitList = {
        new[] { "portrait_tobi", "animada" },
        new[] { "portrait_tobi_offended", "ofendida" },
        new[] { "portrait_tobi_greedy", "gananciosa" },
        new[] { "portrait_tobi_proud", "orgulhosa" },
    };

    // ------------------------------------------------------------- a ponte de madeira (F9)

    /// <summary>
    /// Os blocos da ponte nova (32 × 24, 2 tiles de largo; o topo do tabuleiro na linha 8). 18/11/2026 (pedido do usuário,
    /// com uma imagem de referência): uma faixa grossa de madeira escura vista de lado. Em cima e embaixo, a viga escura com
    /// as cabeças dos postes saindo para fora; no meio, as tábuas em pé, avermelhadas, com veios. Sem o corrimão de corda e
    /// sem as vigas em X. Variações (a posição dos postes, a cor e o veio de cada tábua) para não repetir.
    /// </summary>
    static PixelCanvas Plank(int v)
    {
        var cv = new PixelCanvas(32, 24);
        const int beamTop = 8, plankTop = 11, plankBottom = 17, beamBottom = 18, end = 20;
        // as tábuas em pé (2 ou 3 px cada, cores e veios diferentes)
        int x0 = 0, board = 0;
        while (x0 < 32)
        {
            int w = PixelKit.Noise(board, v, 13) < 0.5f ? 2 : 3;
            float n = PixelKit.Noise(board, v, 31);
            char c = n < 0.4f ? '3' : (n < 0.8f ? '4' : '3');
            for (int x = x0; x < Math.Min(32, x0 + w); x++)
                for (int y = plankTop; y <= plankBottom; y++)
                {
                    char px = x == x0 + w - 1 ? '5' : c;                                      // a fresta entre as tábuas
                    if (w == 3 && x == x0 + 1 && y == plankTop + 2 + board % 3 && PixelKit.Noise(board, v, 23) > 0.7f) px = '5';   // um nó, de vez em quando
                    cv.Force(x, y, px);
                }
            x0 += w; board++;
        }
        // as vigas (em cima e embaixo): escuras, com a luz na borda de cima
        for (int x = 0; x < 32; x++)
        {
            cv.Force(x, beamTop, '2');
            cv.Force(x, beamTop + 1, PixelKit.Noise(x, v, 3) > 0.8f ? '2' : '1');
            cv.Force(x, beamTop + 2, '1');
            cv.Force(x, beamBottom, '2');
            cv.Force(x, beamBottom + 1, '1');
            cv.Force(x, end, PixelKit.Noise(x, v, 9) > 0.85f ? '2' : '1');
        }
        // os postes: atravessam as vigas e saem com a cabeça arredondada em cima e embaixo
        int first = 3 + (v % 2) * 4;
        foreach (int px0 in new[] { first, first + 16 })
        {
            if (px0 + 3 >= 32) continue;
            for (int y = beamTop - 2; y <= end + 2; y++)
                for (int x = px0; x <= px0 + 3; x++)
                {
                    bool cap = y < beamTop || y > end;
                    if (cap && (x == px0 || x == px0 + 3) && (y == beamTop - 2 || y == end + 2)) continue;   // a cabeça redonda
                    if (!cap && y >= plankTop && y <= plankBottom) continue;                                 // no meio, as tábuas passam na frente
                    cv.Force(x, y, x == px0 + 1 && y <= beamTop ? '7' : '6');
                }
        }
        return cv;
    }

    /// <summary>A corda pendurada no começo da ponte, descendo para a floresta (8 × 48, o pivô no alto).</summary>
    static PixelCanvas Rope()
    {
        var cv = new PixelCanvas(8, 48);
        for (int y = 0; y < 48; y++) { int x = 3 + (int)Math.Round(Math.Sin(y / 9.0) * 1.2); cv.Force(x, y, y % 3 == 0 ? 'W' : 'S'); }
        for (int y = 0; y < 3; y++) for (int x = 1; x <= 6; x++) cv.Force(x, y, 'w');   // o nó no galho
        for (int y = 40; y < 48; y += 3) { cv.Force(2, y, 'S'); cv.Force(5, y, 'S'); }   // os nós de apoio
        return cv;
    }

    public const string AssetGuid = "c5e2a8f14b7d4e93b1a6d0f8e3c2b745";
    public const int PlankVariants = 4;

    public static string Run(string projectRoot, string metaTemplatePath, string previewDir)
    {
        InitPalette();
        BuildClips();
        string template = System.IO.File.ReadAllText(metaTemplatePath);
        string result = PixelKit.WriteAnimationSet(Clips, System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/Characters/Tobi"), "chr_tobi",
            System.IO.Path.Combine(projectRoot, "Assets/_Game/ScriptableObjects/Characters/TobiAnimationSet.asset"), "TobiAnimationSet", AssetGuid, template);
        string pdir = System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/UI/Portraits");
        foreach (var p in PortraitList)
            PixelKit.WriteSprite(Portrait(p[1]).Mirror(), System.IO.Path.Combine(pdir, p[0] + ".png"), "portrait/" + p[0], template, false);
        string edir = System.IO.Path.Combine(projectRoot, "Assets/_Game/Art/Environments/Forest/Placeholder");
        for (int v = 0; v < PlankVariants; v++)
        {
            var plank = Plank(v);
            PixelKit.Outline(plank, 'o', "S");
            PixelKit.WriteSprite(plank, System.IO.Path.Combine(edir, "ponte_madeira_" + v + ".png"), "env/ponte_madeira_" + v, template, false);
        }
        PixelKit.WriteSprite(Rope(), System.IO.Path.Combine(edir, "corda_ponte.png"), "env/corda_ponte", template, false);
        if (!string.IsNullOrEmpty(previewDir)) Preview(previewDir);
        return "Tobi: " + result + ", retratos: " + PortraitList.Length + ", ponte: " + PlankVariants + " blocos";
    }

    public static void Preview(string dir)
    {
        InitPalette();
        BuildClips();
        PixelKit.PreviewClips(Clips, System.IO.Path.Combine(dir, "tobi_animacoes.png"), 6, new Rectangle(0, 0, 48, 48), "#26222e");
        var portraits = new List<PixelCanvas>(); var names = new List<string>();
        foreach (var p in PortraitList) { portraits.Add(Portrait(p[1]).Mirror()); names.Add(p[1]); }
        PixelKit.PreviewPortraits(portraits, names, System.IO.Path.Combine(dir, "tobi_retratos.png"), 6);
        var planks = new List<PixelKit.Clip>();
        var c = new PixelKit.Clip { Name = "ponte de madeira", FrameTime = 1f, Loop = true };
        for (int v = 0; v < PlankVariants; v++) { var pl = Plank(v); PixelKit.Outline(pl, 'o', "S"); var big = new PixelCanvas(48, 48); for (int x = 0; x < 32; x++) for (int y = 0; y < 24; y++) big.Px[x + 8, y + 12] = pl.Px[x, y]; c.Frames.Add(big); }
        planks.Add(c);
        PixelKit.PreviewClips(planks, System.IO.Path.Combine(dir, "tobi_ponte.png"), 6, new Rectangle(0, 0, 48, 48), "#26222e");
    }
}
