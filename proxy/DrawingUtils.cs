using Win32;
using Win32.Console;

namespace VRChatProxy;

public static class DrawingUtils
{
    public static void Line(this AnsiRenderer renderer, Coord a, Coord b, AnsiColor color)
    {
        a.Y *= 2;
        b.Y *= 2;

        int dx = b.X - a.X;
        int dy = b.Y - a.Y;

        int sx = Math.Sign(dx);
        int sy = Math.Sign(dy);

        dx = Math.Abs(dx);
        dy = Math.Abs(dy);
        int d = Math.Max(dx, dy);

        float r = d / 2f;

        int x = a.X;
        int y = a.Y;

        if (dx > dy)
        {
            for (int i = 0; i < d; i++)
            {
                if (x < 0 || x >= renderer.Width || y < 0 || y / 2 >= renderer.Height) continue;

                char c;
                if ((y & 1) == 0)
                {
                    c = '▀';
                }
                else
                {
                    c = '▄';
                }

                renderer.Set(x, y / 2, new AnsiChar(c, (byte)color));

                x += sx;
                r += dy;

                if (r >= dx)
                {
                    y += sy;
                    r -= dx;
                }
            }
        }
        else
        {
            int _y = -1;
            int _x = -1;
            for (int i = 0; i < d; i++)
            {
                if (x < 0 || x >= renderer.Width || y < 0 || y / 2 >= renderer.Height) continue;

                char c;
                if (_y == y && _x == x)
                {
                    c = '█';
                }
                else if ((y & 1) == 0)
                {
                    c = '▀';
                    if (sy > 0) _y = y + sy;
                    else _y = -1;
                }
                else
                {
                    c = '▄';
                    if (sy < 0) _y = y + sy;
                    else _y = -1;
                }

                renderer.Set(x, y / 2, new AnsiChar(c, (byte)color));
                _x = x;

                y += sy;
                r += dx;

                if (r >= dy)
                {
                    x += sx;
                    r -= dy;
                }
            }
        }
    }

    public static void FillCircle(this AnsiRenderer renderer, Coord c, int r, AnsiColor color)
    {
        int hr = r / 2 + 1;

        Coord min = new(Math.Max(0, c.X - hr), Math.Max(0, c.Y - hr));
        Coord max = new(Math.Min(renderer.Width - 1, c.X + hr), Math.Min(renderer.Height - 1, c.Y + hr));

        min *= 2;
        max *= 4;

        c.X *= 2;
        c.Y *= 4;

        const string Barille = "⠀⠁⠂⠃⠄⠅⠆⠇⠈⠉⠊⠋⠌⠍⠎⠏⠐⠑⠒⠓⠔⠕⠖⠗⠘⠙⠚⠛⠜⠝⠞⠟⠠⠡⠢⠣⠤⠥⠦⠧⠨⠩⠪⠫⠬⠭⠮⠯⠰⠱⠲⠳⠴⠵⠶⠷⠸⠹⠺⠻⠼⠽⠾⠿⡀⡁⡂⡃⡄⡅⡆⡇⡈⡉⡊⡋⡌⡍⡎⡏⡐⡑⡒⡓⡔⡕⡖⡗⡘⡙⡚⡛⡜⡝⡞⡟⡠⡡⡢⡣⡤⡥⡦⡧⡨⡩⡪⡫⡬⡭⡮⡯⡰⡱⡲⡳⡴⡵⡶⡷⡸⡹⡺⡻⡼⡽⡾⡿⢀⢁⢂⢃⢄⢅⢆⢇⢈⢉⢊⢋⢌⢍⢎⢏⢐⢑⢒⢓⢔⢕⢖⢗⢘⢙⢚⢛⢜⢝⢞⢟⢠⢡⢢⢣⢤⢥⢦⢧⢨⢩⢪⢫⢬⢭⢮⢯⢰⢱⢲⢳⢴⢵⢶⢷⢸⢹⢺⢻⢼⢽⢾⢿⣀⣁⣂⣃⣄⣅⣆⣇⣈⣉⣊⣋⣌⣍⣎⣏⣐⣑⣒⣓⣔⣕⣖⣗⣘⣙⣚⣛⣜⣝⣞⣟⣠⣡⣢⣣⣤⣥⣦⣧⣨⣩⣪⣫⣬⣭⣮⣯⣰⣱⣲⣳⣴⣵⣶⣷⣸⣹⣺⣻⣼⣽⣾⣿";

        for (int y = min.Y; y <= max.Y; y++)
        {
            for (int x = min.X; x <= max.X; x++)
            {
                int dx = x - c.X;
                int dy = y - c.Y;
                if (dx * dx + dy * dy < hr * hr)
                {
                    char chr = (x % 2, y % 4) switch
                    {
                        (0, 0) => '⠁',
                        (0, 1) => '⠂',
                        (0, 2) => '⠄',
                        (0, 3) => '⡀',
                        (1, 0) => '⠈',
                        (1, 1) => '⠐',
                        (1, 2) => '⠠',
                        (1, 3) => '⢀',
                        _ => '⠀',
                    };
                    if (renderer[x / 2, y / 4].Char >= Barille[0] &&
                        renderer[x / 2, y / 4].Char <= Barille[^1])
                    {
                        renderer[x / 2, y / 4].Char |= chr;
                    }
                    else
                    {
                        renderer[x / 2, y / 4] = new AnsiChar(chr, (byte)color);
                    }
                }
            }
        }
    }
}
