using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LibraryApp
{
    /// <summary>Цвета, шрифты и рисование в едином стиле приложения (тёмно-синий «книжный» фон, стеклянные карточки, зелёный акцент).</summary>
    public static class Theme
    {
        public static readonly Color Ink = Color.FromArgb(30, 41, 69);
        public static readonly Color Muted = Color.FromArgb(104, 117, 140);
        public static readonly Color Line = Color.FromArgb(227, 232, 241);
        public static readonly Color Page = Color.FromArgb(244, 247, 251);
        public static readonly Color Blue = Color.FromArgb(30, 64, 140);
        public static readonly Color Navy = Color.FromArgb(23, 43, 99);
        public static readonly Color Green = Color.FromArgb(29, 190, 116);
        public static readonly Color Red = Color.FromArgb(206, 62, 62);
        public static readonly Color Bar = Color.FromArgb(84, 88, 102);
        public static readonly Color Bar2 = Color.FromArgb(29, 31, 51);

        static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();

        /// <summary>Шрифт Segoe UI (обычный или Semibold) нужного размера; шрифты кэшируются.</summary>
        public static Font F(float size, bool semibold)
        {
            string key = size.ToString(System.Globalization.CultureInfo.InvariantCulture) + (semibold ? "s" : "r");
            Font f;
            if (!fonts.TryGetValue(key, out f))
            {
                f = new Font(semibold ? "Segoe UI Semibold" : "Segoe UI", size);
                fonts[key] = f;
            }
            return f;
        }

        /// <summary>Прямоугольник со скруглёнными углами.</summary>
        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0 || d > r.Height || d > r.Width) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>Тёмно-синий фон с силуэтами книжных полок (рисуется кодом, картинки не нужны).</summary>
        public static Bitmap PaintHero(int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (LinearGradientBrush bg = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(40, 56, 114), Color.FromArgb(18, 28, 70), 90f))
                    g.FillRectangle(bg, 0, 0, w, h);

                Color[] spines = {
                    Color.FromArgb(120, 84, 66), Color.FromArgb(86, 64, 100), Color.FromArgb(70, 92, 134),
                    Color.FromArgb(138, 104, 72), Color.FromArgb(54, 74, 126), Color.FromArgb(102, 70, 70)
                };
                Random rnd = new Random(11);
                int shelfH = 150;
                for (int bottom = shelfH; bottom < h + shelfH; bottom += shelfH)
                {
                    int x = 0;
                    while (x < w)
                    {
                        int bw = rnd.Next(12, 30);
                        int bh = (int)(shelfH * (0.58 + rnd.NextDouble() * 0.34));
                        Color c = spines[rnd.Next(spines.Length)];
                        int top = bottom - 8 - bh;
                        using (SolidBrush br = new SolidBrush(Color.FromArgb(95, c))) g.FillRectangle(br, x, top, bw - 1, bh);
                        using (Pen pen = new Pen(Color.FromArgb(45, 255, 255, 255))) g.DrawLine(pen, x + 2, top + 6, x + 2, bottom - 14);
                        using (SolidBrush band = new SolidBrush(Color.FromArgb(70, 214, 178, 96))) g.FillRectangle(band, x, top + 10, bw - 1, 3);
                        x += bw;
                    }
                    using (SolidBrush plank = new SolidBrush(Color.FromArgb(160, 10, 14, 36))) g.FillRectangle(plank, 0, bottom - 8, w, 8);
                }

                // затемнение, чтобы белый текст хорошо читался
                using (LinearGradientBrush ov = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(175, 22, 36, 88), Color.FromArgb(220, 14, 24, 64), 0f))
                    g.FillRectangle(ov, 0, 0, w, h);
            }
            return bmp;
        }
    }

    /// <summary>Панель с тёмно-синим «книжным» фоном.</summary>
    public class HeroPanel : Panel
    {
        Bitmap cache;

        public HeroPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0) return;
            if (cache == null || cache.Width != Width || cache.Height != Height)
            {
                if (cache != null) cache.Dispose();
                cache = Theme.PaintHero(Width, Height);
            }
            e.Graphics.DrawImageUnscaled(cache, 0, 0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && cache != null) { cache.Dispose(); cache = null; }
            base.Dispose(disposing);
        }
    }

    /// <summary>Белая карточка со скруглёнными углами (для формы входа).</summary>
    public class RoundedCard : Panel
    {
        public RoundedCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundRect(r, 18))
            using (SolidBrush fill = new SolidBrush(Color.White))
            using (Pen border = new Pen(Color.FromArgb(222, 228, 238)))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
        }
    }

    public enum PillKind { Auto, Primary, Ghost, Danger, Glass, Light }

    /// <summary>Кнопка со скруглёнными краями. Вид выбирается по тексту (Добавить — зелёная, Удалить — красная, остальные — светлые).</summary>
    public class PillButton : Button
    {
        PillKind kind = PillKind.Auto;
        bool hover, down;

        public PillKind Kind { get { return kind; } set { kind = value; Invalidate(); } }

        public PillButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Font = Theme.F(9.75f, true);
            Cursor = Cursors.Hand;
            Size = new Size(100, 36);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size t = TextRenderer.MeasureText(Text, Font);
            return new Size(Math.Max(64, t.Width + 36), 36);
        }

        PillKind Effective()
        {
            if (kind != PillKind.Auto) return kind;
            string[] primary = { "Добавить", "Выдать", "Принять", "Создать", "Найти", "Войти", "Зарегистрироваться", "Сохранить", "Забронировать", "Выполнить", "OK", "Подтвердить" };
            string[] danger = { "Удалить", "Отменить", "Заблокировать" };
            foreach (string s in primary) if (Text.StartsWith(s)) return PillKind.Primary;
            foreach (string s in danger) if (Text.StartsWith(s)) return PillKind.Danger;
            return PillKind.Ghost;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ButtonRenderer.DrawParentBackground(g, ClientRectangle, this);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            PillKind k = Effective();
            Color fill, border, text;
            switch (k)
            {
                case PillKind.Primary:
                    fill = down ? Color.FromArgb(16, 140, 86) : hover ? Color.FromArgb(22, 166, 100) : Theme.Green;
                    border = Color.Transparent; text = Color.White; break;
                case PillKind.Danger:
                    fill = down ? Color.FromArgb(250, 228, 228) : hover ? Color.FromArgb(253, 240, 240) : Color.White;
                    border = Color.FromArgb(226, 180, 180); text = Color.FromArgb(176, 52, 52); break;
                case PillKind.Glass:
                    fill = Color.FromArgb(down ? 90 : hover ? 66 : 38, 255, 255, 255);
                    border = Color.FromArgb(95, 255, 255, 255); text = Color.White; break;
                case PillKind.Light:
                    fill = Color.White; border = Color.Transparent; text = Theme.Navy; break;
                default:
                    fill = down ? Color.FromArgb(222, 233, 250) : hover ? Color.FromArgb(238, 244, 253) : Color.White;
                    border = Color.FromArgb(206, 216, 232); text = Theme.Blue; break;
            }
            if (!Enabled)
            {
                fill = Color.FromArgb(238, 241, 246); border = Color.FromArgb(222, 226, 234); text = Color.FromArgb(150, 158, 172);
            }

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundRect(r, Math.Min(Height / 2, 20)))
            {
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, path);
                if (border.A > 0) using (Pen p = new Pen(border)) g.DrawPath(p, path);
            }
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }
    }

    public enum TileStyle { Glass, Light }

    /// <summary>Карточка-ссылка. Glass — стеклянная со стрелкой на тёмном фоне, Light — белая с цифрой и подписью.</summary>
    public class Tile : Control
    {
        public TileStyle Style;
        public string Title = "";
        public string Value;
        public Color ValueColor = Theme.Blue;
        bool hover;

        public Tile(TileStyle style)
        {
            Style = style;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { hover = true; Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { hover = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { OnClick(EventArgs.Empty); e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);

            if (Style == TileStyle.Glass)
            {
                using (GraphicsPath path = Theme.RoundRect(r, 14))
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(hover ? 52 : 26, 255, 255, 255))) g.FillPath(b, path);
                    using (Pen p = new Pen(Color.FromArgb(hover ? 120 : 70, 255, 255, 255))) g.DrawPath(p, path);
                }
                TextRenderer.DrawText(g, Title, Theme.F(11f, true), new Rectangle(22, 0, Width - 78, Height), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                int cy = Height / 2, x2 = Width - 24;
                using (Pen ap = new Pen(Color.FromArgb(230, 255, 255, 255), 1.6f))
                {
                    ap.StartCap = LineCap.Round; ap.EndCap = LineCap.Round; ap.LineJoin = LineJoin.Round;
                    g.DrawLine(ap, x2 - 18, cy, x2, cy);
                    g.DrawLines(ap, new Point[] { new Point(x2 - 7, cy - 6), new Point(x2, cy), new Point(x2 - 7, cy + 6) });
                }
            }
            else
            {
                using (GraphicsPath path = Theme.RoundRect(r, 12))
                {
                    using (SolidBrush b = new SolidBrush(hover ? Color.FromArgb(246, 249, 255) : Color.White)) g.FillPath(b, path);
                    using (Pen p = new Pen(hover ? Color.FromArgb(150, 178, 226) : Theme.Line)) g.DrawPath(p, path);
                }
                TextFormatFlags center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
                if (Value != null)
                {
                    TextRenderer.DrawText(g, Value, Theme.F(21f, true), new Rectangle(0, 8, Width, Height / 2 + 4), ValueColor, center);
                    TextRenderer.DrawText(g, Title, Theme.F(10f, true), new Rectangle(0, Height / 2 + 8, Width, Height / 2 - 12), Theme.Blue, center);
                }
                else
                {
                    TextRenderer.DrawText(g, Title, Theme.F(11f, true), new Rectangle(0, 0, Width, Height), Theme.Blue, center);
                }
            }
        }
    }

    /// <summary>Раскладывает карточки по сетке из N столбцов и сама подбирает свою высоту.</summary>
    public class CardGrid : Control
    {
        public int Columns = 3;
        public int CardHeight = 76;
        public int Gap = 14;

        public CardGrid()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public int RowsHeight()
        {
            int n = Controls.Count;
            int rows = (n + Columns - 1) / Columns;
            if (rows <= 0) return 0;
            return rows * CardHeight + (rows - 1) * Gap;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (Controls.Count == 0 || Width <= 0) return;
            int w = (Width - Gap * (Columns - 1)) / Columns;
            for (int i = 0; i < Controls.Count; i++)
            {
                int row = i / Columns, col = i % Columns;
                Controls[i].SetBounds(col * (w + Gap), row * (CardHeight + Gap), w, CardHeight);
            }
            int h = RowsHeight();
            if (Height != h) Height = h;
        }
    }

    /// <summary>Оформление выпадающего меню: на тёмной полосе — белый текст, в выпадающем списке — тёмный на белом.</summary>
    public class BarRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(e.ToolStrip.BackColor)) e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is MenuStrip) return;
            using (Pen p = new Pen(Color.FromArgb(218, 226, 237)))
                e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool top = e.Item.Owner is MenuStrip;
            Color c = top ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(232, 240, 252);
            Rectangle r = new Rectangle(2, 2, e.Item.Width - 5, e.Item.Height - 5);
            using (SolidBrush b = new SolidBrush(c))
            using (GraphicsPath p = Theme.RoundRect(r, 8))
                g.FillPath(b, p);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = (e.ToolStrip is MenuStrip) ? Color.White : Color.FromArgb(30, 40, 70);
            base.OnRenderItemText(e);
        }
    }
}
