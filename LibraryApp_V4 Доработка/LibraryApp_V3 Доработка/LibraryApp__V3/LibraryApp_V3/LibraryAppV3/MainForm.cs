using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp
{
    /// <summary>Главное окно: меню по ролям, карточки быстрых действий и сводка по фонду.</summary>
    public class MainForm : Form
    {
        public bool LogoutRequested;

        MenuStrip menu;
        Label lblUser, lblStatus, hello, statsTitle;
        Panel page, light;
        HeroPanel hero;
        CardGrid actions, stats;

        public MainForm()
        {
            Text = Ui.Title;
            Font = Theme.F(10f, false);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(960, 640);
            Size = new Size(1240, 800);
            BackColor = Color.White;

            // ---------- верхняя серая полоса: «логотип» и меню ----------
            Panel bar1 = new Panel();
            bar1.Dock = DockStyle.Top;
            bar1.Height = 54;
            bar1.BackColor = Theme.Bar;

            PillButton logo = new PillButton();
            logo.Text = "БИБЛИОТЕКА ВУЗА";
            logo.Kind = PillKind.Light;
            logo.TabStop = false;
            logo.Cursor = Cursors.Default;
            logo.Font = Theme.F(10.5f, true);
            logo.Size = new Size(190, 36);
            logo.Location = new Point(24, 9);

            menu = new MenuStrip();
            menu.Dock = DockStyle.None;
            menu.AutoSize = true;
            menu.GripStyle = ToolStripGripStyle.Hidden;
            menu.BackColor = Theme.Bar;
            menu.Font = Theme.F(10f, false);
            menu.Renderer = new BarRenderer();
            menu.Location = new Point(228, 9);
            MainMenuStrip = menu;
            AddMenu(menu);
            StyleMenu(menu);

            lblUser = new Label();
            lblUser.AutoSize = true;
            lblUser.ForeColor = Color.FromArgb(226, 230, 242);
            lblUser.Font = Theme.F(9.5f, false);
            lblUser.Text = Session.Login + "   ·   " + Session.RoleTitle;
            bar1.Controls.Add(logo);
            bar1.Controls.Add(menu);
            bar1.Controls.Add(lblUser);
            bar1.Resize += delegate { lblUser.Location = new Point(bar1.ClientSize.Width - lblUser.Width - 28, 17); };
            lblUser.Location = new Point(1000, 17);

            // ---------- тёмная полоса с названием и зелёной кнопкой ----------
            Panel bar2 = new Panel();
            bar2.Dock = DockStyle.Top;
            bar2.Height = 66;
            bar2.BackColor = Theme.Bar2;

            Label barTitle = new Label();
            barTitle.Text = "БИБЛИОТЕКА ВУЗА   ·   ИНФОРМАЦИОННАЯ СИСТЕМА";
            barTitle.ForeColor = Color.White;
            barTitle.Font = Theme.F(12.5f, true);
            barTitle.TextAlign = ContentAlignment.MiddleCenter;
            barTitle.Dock = DockStyle.Fill;

            PillButton switchUser = new PillButton();
            switchUser.Text = "Сменить пользователя";
            switchUser.Kind = PillKind.Primary;
            switchUser.Size = switchUser.GetPreferredSize(Size.Empty);
            switchUser.Click += delegate { LogoutRequested = true; Close(); };
            bar2.Controls.Add(switchUser);
            bar2.Controls.Add(barTitle);
            bar2.Resize += delegate { switchUser.Location = new Point(bar2.ClientSize.Width - switchUser.Width - 28, 15); };
            switchUser.Location = new Point(1000, 15);
            switchUser.BringToFront();

            // ---------- нижняя строка ----------
            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 46;
            footer.BackColor = Color.White;
            footer.Paint += delegate(object s, PaintEventArgs e) { using (Pen p = new Pen(Theme.Line)) e.Graphics.DrawLine(p, 0, 0, footer.Width, 0); };

            lblStatus = new Label();
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Theme.Muted;
            lblStatus.Text = "●  Подключено   |   " + Session.Login + "   |   " + Session.RoleTitle;
            lblStatus.Location = new Point(24, 14);

            PillButton refresh = new PillButton();
            refresh.Text = "↻  Обновить данные";
            refresh.Kind = PillKind.Ghost;
            refresh.Size = refresh.GetPreferredSize(Size.Empty);
            refresh.Click += delegate { ShowSummary(); };
            footer.Controls.Add(lblStatus);
            footer.Controls.Add(refresh);
            footer.Resize += delegate { refresh.Location = new Point(footer.ClientSize.Width - refresh.Width - 24, 5); };
            refresh.Location = new Point(1000, 5);

            // ---------- страница с прокруткой: «герой» + белый блок сводки ----------
            page = new Panel();
            page.Dock = DockStyle.Fill;
            page.AutoScroll = true;
            page.BackColor = Color.White;

            hero = new HeroPanel();
            hero.Dock = DockStyle.Top;
            actions = new CardGrid { Columns = 3, CardHeight = 76, Gap = 14, Dock = DockStyle.Top };
            hello = MakeLabel("", 11.5f, false, Color.FromArgb(205, 214, 238), 38);
            Label heading = MakeLabel("Информационная система\n«Библиотека вуза»", 32f, true, Color.White, 112);
            Label crumb = MakeLabel("Главная  /  Панель управления", 9.5f, false, Color.FromArgb(190, 201, 232), 30);
            // порядок добавления: последний добавленный окажется выше всех
            hero.Controls.Add(actions);
            hero.Controls.Add(hello);
            hero.Controls.Add(heading);
            hero.Controls.Add(crumb);

            light = new Panel();
            light.Dock = DockStyle.Top;
            light.BackColor = Color.White;
            stats = new CardGrid { Columns = 4, CardHeight = 92, Gap = 14, Dock = DockStyle.Top };
            statsTitle = new Label();
            statsTitle.Dock = DockStyle.Top;
            statsTitle.Height = 44;
            statsTitle.Font = Theme.F(14f, true);
            statsTitle.ForeColor = Theme.Navy;
            statsTitle.BackColor = Color.White;
            statsTitle.TextAlign = ContentAlignment.TopLeft;
            light.Controls.Add(stats);
            light.Controls.Add(statsTitle);

            page.Controls.Add(light);
            page.Controls.Add(hero);

            Controls.Add(page);
            Controls.Add(footer);
            Controls.Add(bar2);
            Controls.Add(bar1);

            page.Resize += delegate { LayoutPage(); };
            Load += delegate { ShowSummary(); };
        }

        static Label MakeLabel(string text, float size, bool semibold, Color color, int height)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.F(size, semibold);
            l.ForeColor = color;
            l.BackColor = Color.Transparent;
            l.AutoSize = false;
            l.Dock = DockStyle.Top;
            l.Height = height;
            return l;
        }

        void LayoutPage()
        {
            int w = page.ClientSize.Width;
            int pad = Math.Max(28, (w - 1120) / 2);
            hero.Padding = new Padding(pad, 20, pad, 44);
            light.Padding = new Padding(pad, 34, pad, 44);
            hero.Height = hero.Padding.Top + 30 + 112 + 38 + actions.RowsHeight() + hero.Padding.Bottom;
            light.Height = light.Padding.Top + 44 + stats.RowsHeight() + light.Padding.Bottom;
            actions.PerformLayout();
            stats.PerformLayout();
        }

        // ---------- меню ----------

        void AddMenu(MenuStrip m)
        {
            var catalog = new ToolStripMenuItem("Каталог");
            catalog.DropDownItems.Add(Item("Поиск изданий", delegate { Open(new CatalogForm()); })); m.Items.Add(catalog);
            if (Session.InRole(Session.Reader))
            {
                var r = new ToolStripMenuItem("Читателю");
                r.DropDownItems.Add(Item("Мой кабинет", delegate { Open(new MyCabinetForm()); })); m.Items.Add(r);
            }
            if (Session.InRole(Session.Librarian, Session.Admin))
            {
                var s = new ToolStripMenuItem("Обслуживание");
                s.DropDownItems.Add(Item("Читатели", delegate { Open(Forms.Readers()); }));
                s.DropDownItems.Add(Item("Выдача и возврат", delegate { Open(new LoansForm()); }));
                s.DropDownItems.Add(Item("Бронирования", delegate { Open(new ReservationsForm()); }));
                s.DropDownItems.Add(Item("Штрафы", delegate { Open(new FinesForm()); })); m.Items.Add(s);
            }
            if (Session.InRole(Session.Admin))
            {
                var a = new ToolStripMenuItem("Администрирование");
                a.DropDownItems.Add(Item("Издания", delegate { Open(Forms.Editions()); }));
                a.DropDownItems.Add(Item("Экземпляры", delegate { Open(Forms.Copies()); }));
                a.DropDownItems.Add(Item("Сотрудники", delegate { Open(Forms.Employees()); }));
                a.DropDownItems.Add(Item("Пользователи системы", delegate { Open(new UsersForm()); }));
                a.DropDownItems.Add(Item("Просмотр таблиц БД", delegate { Open(new DbTablesForm()); })); m.Items.Add(a);
            }
            var session = new ToolStripMenuItem("Сеанс");
            session.DropDownItems.Add(Item("Сменить пользователя", delegate { LogoutRequested = true; Close(); }));
            session.DropDownItems.Add(Item("Выход", delegate { LogoutRequested = false; Close(); })); m.Items.Add(session);
        }

        static void StyleMenu(MenuStrip m)
        {
            foreach (ToolStripItem it in m.Items)
            {
                it.ForeColor = Color.White;
                it.Padding = new Padding(10, 6, 10, 6);
                ToolStripMenuItem mi = it as ToolStripMenuItem;
                if (mi == null) continue;
                mi.DropDown.BackColor = Color.White;
                ToolStripDropDownMenu dd = mi.DropDown as ToolStripDropDownMenu;
                if (dd != null) dd.ShowImageMargin = false;
                foreach (ToolStripItem sub in mi.DropDownItems)
                {
                    sub.ForeColor = Color.FromArgb(30, 40, 70);
                    sub.BackColor = Color.White;
                    sub.Padding = new Padding(6, 6, 6, 6);
                }
            }
        }

        static ToolStripMenuItem Item(string text, EventHandler handler) { var i = new ToolStripMenuItem(text); i.Click += handler; return i; }

        void Open(Form f) { try { f.ShowDialog(this); } finally { f.Dispose(); } ShowSummary(); }

        // ---------- карточки ----------

        void AddAction(string title, EventHandler click)
        {
            Tile t = new Tile(TileStyle.Glass);
            t.Title = title;
            t.Click += click;
            actions.Controls.Add(t);
        }

        void AddStat(string title, string value, Color color, EventHandler click)
        {
            Tile t = new Tile(TileStyle.Light);
            t.Title = title;
            t.Value = value;
            t.ValueColor = color;
            if (click != null) t.Click += click;
            else t.Cursor = Cursors.Default;
            stats.Controls.Add(t);
        }

        void ShowSummary()
        {
            hello.Text = "Здравствуйте, " + (Session.Login ?? "пользователь") + "!   Роль: " + Session.RoleTitle + "  —  " + Session.Rights;
            actions.Controls.Clear();
            stats.Controls.Clear();
            statsTitle.Font = Theme.F(14f, true);
            statsTitle.ForeColor = Theme.Navy;
            Color blue = Theme.Blue, green = Color.FromArgb(22, 150, 92), red = Theme.Red;

            if (Session.InRole(Session.Reader))
            {
                AddAction("Каталог изданий", delegate { Open(new CatalogForm()); });
                AddAction("Забронировать издание", delegate { Open(new CatalogForm()); });
                AddAction("Мои выдачи", delegate { Open(new MyCabinetForm(0)); });
                AddAction("Мои бронирования", delegate { Open(new MyCabinetForm(1)); });
                AddAction("Мои штрафы", delegate { Open(new MyCabinetForm(2)); });
            }
            else
            {
                AddAction("Каталог изданий", delegate { Open(new CatalogForm()); });
                AddAction("Читатели", delegate { Open(Forms.Readers()); });
                AddAction("Выдача и возврат", delegate { Open(new LoansForm()); });
                AddAction("Бронирования", delegate { Open(new ReservationsForm()); });
                AddAction("Штрафы", delegate { Open(new FinesForm()); });
                if (Session.InRole(Session.Admin))
                {
                    AddAction("Издания", delegate { Open(Forms.Editions()); });
                    AddAction("Экземпляры", delegate { Open(Forms.Copies()); });
                    AddAction("Сотрудники", delegate { Open(Forms.Employees()); });
                    AddAction("Пользователи системы", delegate { Open(new UsersForm()); });
                }
            }

            try
            {
                if (Session.InRole(Session.Reader))
                {
                    statsTitle.Text = "Моя сводка";
                    stats.Columns = 4;
                    DataTable t = Db.Table(
                        "SELECT r.ticket_number, s.status_name, " +
                        "(SELECT COUNT(*) FROM loans l WHERE l.reader_id = r.reader_id AND l.return_date IS NULL) AS open_loans, " +
                        "(SELECT COUNT(*) FROM reservations v WHERE v.reader_id = r.reader_id AND v.status_id = 1) AS active_res, " +
                        "(SELECT COUNT(*) FROM fines f JOIN loans l2 ON l2.loan_id = f.loan_id WHERE l2.reader_id = r.reader_id AND f.paid_on IS NULL) AS unpaid " +
                        "FROM readers r JOIN reader_statuses s ON s.status_id = r.status_id WHERE r.reader_id = @id",
                        "@id", Session.ReaderId);
                    if (t.Rows.Count > 0)
                    {
                        DataRow r = t.Rows[0];
                        int unpaid = Convert.ToInt32(r["unpaid"]);
                        AddStat("Книг на руках", Convert.ToString(r["open_loans"]), blue, delegate { Open(new MyCabinetForm(0)); });
                        AddStat("Активных броней", Convert.ToString(r["active_res"]), green, delegate { Open(new MyCabinetForm(1)); });
                        AddStat("Неоплаченных штрафов", Convert.ToString(unpaid), unpaid > 0 ? red : green, delegate { Open(new MyCabinetForm(2)); });
                        AddStat("Читательский билет № " + Convert.ToString(r["ticket_number"]), Convert.ToString(r["status_name"]), blue, null);
                    }
                }
                else
                {
                    statsTitle.Text = "Сводка по фонду";
                    stats.Columns = 5;
                    DataTable t = Db.Table(
                        "SELECT (SELECT COUNT(*) FROM editions) editions, " +
                        "(SELECT COUNT(*) FROM copies WHERE status_id = 1) free_copies, " +
                        "(SELECT COUNT(*) FROM loans WHERE return_date IS NULL) open_loans, " +
                        "(SELECT COUNT(*) FROM loans WHERE return_date IS NULL AND due_date < CURRENT_DATE) overdue, " +
                        "(SELECT COUNT(*) FROM reservations WHERE status_id = 1) active_res");
                    DataRow r = t.Rows[0];
                    int overdue = Convert.ToInt32(r["overdue"]);
                    AddStat("Изданий в каталоге", Convert.ToString(r["editions"]), blue, delegate { Open(new CatalogForm()); });
                    AddStat("Свободных экземпляров", Convert.ToString(r["free_copies"]), green, delegate { Open(new CatalogForm(true)); });
                    AddStat("Книг на руках", Convert.ToString(r["open_loans"]), blue, delegate { Open(new LoansForm()); });
                    AddStat("Просроченных выдач", Convert.ToString(overdue), overdue > 0 ? red : green, delegate { Open(new LoansForm(true)); });
                    AddStat("Активных броней", Convert.ToString(r["active_res"]), blue, delegate { Open(new ReservationsForm()); });
                }
            }
            catch (Exception ex)
            {
                statsTitle.Text = "Не удалось загрузить данные: " + Db.Explain(ex);
                statsTitle.ForeColor = Theme.Red;
                statsTitle.Font = Theme.F(10f, false);
            }
            LayoutPage();
        }
    }
}
