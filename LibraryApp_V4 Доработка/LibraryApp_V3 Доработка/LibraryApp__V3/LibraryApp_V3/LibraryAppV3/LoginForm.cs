using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp
{
    /// <summary>Форма авторизации.</summary>
    public class LoginForm : Form
    {
        TextBox txtLogin, txtPass;
        Label lblMsg;
        PillButton btnLogin, btnReg, btnExit;
        int failed;
        DateTime lockedUntil = DateTime.MinValue;

        public LoginForm()
        {
            Text = Ui.Title + " — вход";
            Font = new Font("Segoe UI", 10f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            const int W = 820, H = 600;
            ClientSize = new Size(W, H);

            HeroPanel hero = new HeroPanel();
            hero.Dock = DockStyle.Fill;

            // всё по центру окна, чтобы слева и справа не оставалось пустых полос
            Label bigTitle = new Label();
            bigTitle.Text = "Информационная система\n«Библиотека вуза»";
            bigTitle.BackColor = Color.Transparent;
            bigTitle.ForeColor = Color.White;
            bigTitle.Font = Theme.F(26f, true);
            bigTitle.TextAlign = ContentAlignment.MiddleCenter;
            bigTitle.SetBounds(0, 30, W, 100);

            RoundedCard card = new RoundedCard();
            card.SetBounds((W - 400) / 2, 150, 400, 384);

            Label head = new Label();
            head.Text = "Вход в систему";
            head.BackColor = Color.White;
            head.ForeColor = Theme.Navy;
            head.Font = Theme.F(17f, true);
            head.SetBounds(36, 22, 328, 36);

            Label l1 = new Label(); l1.Text = "Логин"; l1.BackColor = Color.White; l1.ForeColor = Theme.Muted; l1.SetBounds(36, 76, 328, 20);
            txtLogin = new TextBox(); txtLogin.SetBounds(36, 98, 328, 30); txtLogin.MaxLength = 50;
            Label l2 = new Label(); l2.Text = "Пароль"; l2.BackColor = Color.White; l2.ForeColor = Theme.Muted; l2.SetBounds(36, 142, 328, 20);
            txtPass = new TextBox(); txtPass.SetBounds(36, 164, 328, 30); txtPass.UseSystemPasswordChar = true; txtPass.MaxLength = 100;
            foreach (TextBox t in new TextBox[] { txtLogin, txtPass })
            {
                t.Font = Theme.F(11f, false);
                t.BorderStyle = BorderStyle.FixedSingle;
            }

            lblMsg = new Label();
            lblMsg.BackColor = Color.White;
            lblMsg.ForeColor = Theme.Red;
            lblMsg.SetBounds(36, 200, 328, 40);

            btnLogin = new PillButton(); btnLogin.Text = "Войти"; btnLogin.Kind = PillKind.Primary; btnLogin.SetBounds(36, 250, 328, 46);
            btnReg = new PillButton(); btnReg.Text = "Регистрация"; btnReg.Kind = PillKind.Ghost; btnReg.SetBounds(36, 308, 158, 42);
            btnExit = new PillButton(); btnExit.Text = "Выход"; btnExit.Kind = PillKind.Ghost; btnExit.SetBounds(206, 308, 158, 42);

            Label footer = new Label();
            footer.Text = "Каталог изданий   ·   Выдача и возврат   ·   Бронирование   ·   Штрафы";
            footer.BackColor = Color.Transparent;
            footer.ForeColor = Color.FromArgb(190, 201, 232);
            footer.Font = Theme.F(10f, false);
            footer.TextAlign = ContentAlignment.MiddleCenter;
            footer.SetBounds(0, 556, W, 28);

            card.Controls.AddRange(new Control[] { head, l1, txtLogin, l2, txtPass, lblMsg, btnLogin, btnReg, btnExit });
            hero.Controls.AddRange(new Control[] { bigTitle, card, footer });
            Controls.Add(hero);
            AcceptButton = btnLogin;
            CancelButton = btnExit;

            btnLogin.Click += delegate { DoLogin(); };
            btnReg.Click += delegate
            {
                using (RegisterForm f = new RegisterForm())
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        txtLogin.Text = f.CreatedLogin;
                        txtPass.Clear();
                        txtPass.Focus();
                    }
                }
            };
            btnExit.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
        }

        void DoLogin()
        {
            if (DateTime.Now < lockedUntil)
            {
                lblMsg.Text = "Слишком много попыток. Повторите через " + (int)(lockedUntil - DateTime.Now).TotalSeconds + " с.";
                return;
            }
            string login = txtLogin.Text.Trim();
            string pass = txtPass.Text;
            if (login.Length == 0 || pass.Length == 0)
            {
                lblMsg.Text = "Введите логин и пароль.";
                return;
            }
            try
            {
                DataTable t = Db.Table(
                    "SELECT u.user_id, u.password_hash, u.salt, r.role_name, u.is_active, u.reader_id, u.employee_id " +
                    "FROM users u JOIN roles r ON r.role_id = u.role_id WHERE u.login = @login",
                    "@login", login);
                bool ok = t.Rows.Count == 1 &&
                          Security.Verify(pass, (string)t.Rows[0]["salt"], (string)t.Rows[0]["password_hash"]);
                if (!ok)
                {
                    failed++;
                    if (failed >= 5)
                    {
                        lockedUntil = DateTime.Now.AddSeconds(30);
                        failed = 0;
                        lblMsg.Text = "Превышено число попыток. Вход заблокирован на 30 секунд.";
                    }
                    else lblMsg.Text = "Неверный логин или пароль.";
                    txtPass.Clear();
                    return;
                }
                DataRow row = t.Rows[0];
                if (Convert.ToInt32(row["is_active"]) == 0)
                {
                    lblMsg.Text = "Учётная запись заблокирована администратором.";
                    return;
                }
                Session.Set(Convert.ToInt32(row["user_id"]), login, (string)row["role_name"],
                    Db.IsNull(row["reader_id"]) ? (int?)null : Convert.ToInt32(row["reader_id"]),
                    Db.IsNull(row["employee_id"]) ? (int?)null : Convert.ToInt32(row["employee_id"]));
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblMsg.Text = Db.Explain(ex);
            }
        }
    }
}
