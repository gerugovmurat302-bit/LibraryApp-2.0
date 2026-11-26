using System;
using System.Data;
using System.Windows.Forms;
using System.Drawing;

namespace LibraryApp
{
    /// <summary>Вспомогательные методы интерфейса.</summary>
    public static class Ui
    {
        public const string Title = "Библиотека вуза";

        public static void Info(string text) { MessageBox.Show(text, Title, MessageBoxButtons.OK, MessageBoxIcon.Information); }
        public static void Warn(string text) { MessageBox.Show(text, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        public static void Error(string text) { MessageBox.Show(text, Title, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        public static bool Ask(string text)
        {
            return MessageBox.Show(text, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public static DataGridView NewGrid()
        {
            DataGridView g = new DataGridView();
            g.Dock = DockStyle.Fill;
            g.ReadOnly = true;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.RowHeadersVisible = false;
            g.MultiSelect = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Color.FromArgb(232, 237, 244);
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersDefaultCellStyle.BackColor = Theme.Navy;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Navy;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = Theme.F(9.5f, true);
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 42;
            g.RowTemplate.Height = 36;
            g.DefaultCellStyle.Font = Theme.F(9.5f, false);
            g.DefaultCellStyle.ForeColor = Color.FromArgb(48, 60, 78);
            g.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(222, 235, 252);
            g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(24, 39, 64);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 254);
            return g;
        }

        public static Label NewLabel(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Anchor = AnchorStyles.Left;
            l.ForeColor = Theme.Muted;
            return l;
        }

        /// <summary>Панель кнопок и поиска над таблицей: сама подбирает высоту, переносит элементы, выравнивает по вертикали.</summary>
        public static FlowLayoutPanel NewToolbar()
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.Dock = DockStyle.Top;
            return f;
        }

        public static void ApplyTheme(Form form, bool addBack)
        {
            form.Font = new Font("Segoe UI", 9.5f);
            form.BackColor = Theme.Page;
            form.ForeColor = Theme.Ink;
            if (addBack) form.MinimumSize = new Size(820, 520);
            form.KeyPreview = true;
            form.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { form.Close(); e.Handled = true; }
            };
            StyleControls(form.Controls, Theme.Ink);
            if (addBack) AddBand(form);
        }

        /// <summary>Тёмно-синяя шапка раздела: «Главная / раздел», крупный заголовок и кнопка возврата.</summary>
        static void AddBand(Form form)
        {
            HeroPanel band = new HeroPanel();
            band.Dock = DockStyle.Top;
            band.Height = 104;

            Label crumb = new Label();
            crumb.Text = "Главная  /  " + form.Text;
            crumb.AutoSize = true;
            crumb.BackColor = Color.Transparent;
            crumb.ForeColor = Color.FromArgb(190, 201, 232);
            crumb.Font = Theme.F(9.5f, false);
            crumb.Location = new Point(28, 14);

            Label title = new Label();
            title.Text = form.Text;
            title.AutoSize = true;
            title.BackColor = Color.Transparent;
            title.ForeColor = Color.White;
            title.Font = Theme.F(22f, true);
            title.Location = new Point(26, 38);

            PillButton back = new PillButton();
            back.Text = "←  На главную";
            back.Kind = PillKind.Glass;
            back.Size = back.GetPreferredSize(Size.Empty);
            back.Click += delegate { form.Close(); };
            band.Resize += delegate { back.Location = new Point(band.ClientSize.Width - back.Width - 28, 34); };
            back.Location = new Point(band.Width - back.Width - 28, 34);

            band.Controls.Add(crumb);
            band.Controls.Add(title);
            band.Controls.Add(back);
            form.Controls.Add(band);   // добавлена последней — значит, окажется самой верхней
        }

        static void StyleToolbar(FlowLayoutPanel f)
        {
            f.AutoSize = true;
            f.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            f.WrapContents = true;
            f.BackColor = Color.White;
            f.Padding = new Padding(16, 12, 16, 8);
            foreach (Control c in f.Controls)
            {
                c.Anchor = AnchorStyles.Left;          // без Top/Bottom — по вертикали выравнивается по центру строки
                c.Margin = new Padding(0, 0, 12, 4);
                TextBox t = c as TextBox;
                if (t != null) t.Font = Theme.F(10f, false);
            }
            f.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.Line)) e.Graphics.DrawLine(p, 0, f.Height - 1, f.Width, f.Height - 1);
            };
        }

        static void StyleControls(Control.ControlCollection controls, Color ink)
        {
            foreach (Control c in controls)
            {
                if (c is PillButton)
                {
                    // кнопки-таблетки красят себя сами
                }
                else
                {
                    Button b = c as Button;
                    if (b != null)
                    {
                        b.FlatStyle = FlatStyle.Flat;
                        b.FlatAppearance.BorderColor = Color.FromArgb(218, 226, 237);
                        b.BackColor = Color.White;
                        b.ForeColor = ink;
                        b.Cursor = Cursors.Hand;
                        b.Padding = new Padding(7, 3, 7, 3);
                        b.Height = Math.Max(b.Height, 30);
                    }
                }
                TextBox t = c as TextBox;
                if (t != null) { t.BorderStyle = BorderStyle.FixedSingle; t.BackColor = Color.White; t.ForeColor = ink; }
                ComboBox cb = c as ComboBox;
                if (cb != null) { cb.FlatStyle = FlatStyle.Flat; cb.BackColor = Color.White; }
                TabControl tabs = c as TabControl;
                if (tabs != null) tabs.Font = Theme.F(9.5f, true);
                DataGridView g = c as DataGridView;
                if (g != null) { g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None; g.RowHeadersVisible = false; }
                FlowLayoutPanel flow = c as FlowLayoutPanel;
                if (flow != null && flow.Dock == DockStyle.Top) StyleToolbar(flow);
                Label lb = c as Label;
                if (lb != null && lb.Dock == DockStyle.Bottom)
                {
                    lb.AutoSize = false;
                    lb.Height = 34;
                    lb.BackColor = Color.White;
                    lb.ForeColor = Theme.Muted;
                    lb.Padding = new Padding(16, 9, 0, 0);
                }
                StyleControls(c.Controls, ink);
            }
        }

        /// <summary>Подписи столбцов, скрытие служебных столбцов, формат дат.</summary>
        public static void Format(DataGridView grid, System.Collections.Generic.Dictionary<string, string> captions, params string[] hidden)
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                string name = col.DataPropertyName;
                if (captions != null && captions.ContainsKey(name)) col.HeaderText = captions[name];
                if (Array.IndexOf(hidden, name) >= 0) col.Visible = false;
                if (col.ValueType == typeof(DateTime)) col.DefaultCellStyle.Format = "dd.MM.yyyy";
                if (col.ValueType == typeof(decimal)) col.DefaultCellStyle.Format = "N2";
            }
        }

        /// <summary>Загружает выпадающий список из запроса вида SELECT id, name ...</summary>
        public static void Lookup(ComboBox cb, string sql, params object[] args)
        {
            DataTable t = Db.Table(sql, args);
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.DataSource = null;
            cb.DisplayMember = t.Columns[1].ColumnName;
            cb.ValueMember = t.Columns[0].ColumnName;
            cb.DataSource = t;
        }

        /// <summary>Экранирование символов % и _ в пользовательском шаблоне LIKE.</summary>
        public static string Like(string s)
        {
            if (s == null) s = "";
            return "%" + s.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        }
    }

    /// <summary>Базовая форма с проверкой роли: форму можно открыть только разрешённым ролям.</summary>
    public class SecureForm : Form
    {
        readonly string[] roles;

        protected SecureForm(params string[] roles)
        {
            this.roles = roles;
            this.Font = new System.Drawing.Font("Segoe UI", 9.5f);
            this.StartPosition = FormStartPosition.CenterParent;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (roles != null && roles.Length > 0 && !Session.InRole(roles))
            {
                Ui.Warn("Недостаточно прав для открытия этой формы.");
                BeginInvoke(new Action(Close));
                return;
            }
            base.OnLoad(e);
            Ui.ApplyTheme(this, true);
            WindowState = FormWindowState.Maximized;
        }
    }

    /// <summary>Небольшое окно ввода одной строки (например, нового пароля).</summary>
    public static class Prompt
    {
        public static string Show(string title, string label, bool password)
        {
            Form f = new Form();
            f.Text = title;
            f.Font = new System.Drawing.Font("Segoe UI", 9.5f);
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.StartPosition = FormStartPosition.CenterParent;
            f.MinimizeBox = false;
            f.MaximizeBox = false;
            f.ClientSize = new System.Drawing.Size(360, 120);
            Label l = new Label();
            l.Text = label;
            l.SetBounds(12, 12, 336, 20);
            TextBox t = new TextBox();
            t.SetBounds(12, 36, 336, 24);
            t.UseSystemPasswordChar = password;
            Button ok = new PillButton();
            ok.Text = "OK";
            ok.DialogResult = DialogResult.OK;
            ok.SetBounds(180, 76, 80, 28);
            Button cancel = new PillButton();
            cancel.Text = "Отмена";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(268, 76, 80, 28);
            f.Controls.AddRange(new Control[] { l, t, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;
            string result = f.ShowDialog() == DialogResult.OK ? t.Text : null;
            f.Dispose();
            return result;
        }
    }
}
