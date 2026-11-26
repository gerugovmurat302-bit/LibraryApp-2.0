using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LibraryApp
{
    /// <summary>
    /// Самостоятельная регистрация. Ученик получает читательский билет и роль «Читатель»;
    /// сотрудник и администратор — запись в таблице сотрудников и свою роль (для них нужен код доступа).
    /// </summary>
    public class RegisterForm : Form
    {
        const string RoleStudent = "Ученик";
        const string RoleStaff = "Сотрудник";
        const string RoleAdmin = "Администратор";

        class Row
        {
            public Label Cap;
            public Control Input;
            public bool Show = true;
            public int W = 400;            // ширина поля
            public bool SameLine;           // true — поле стоит справа от предыдущего в той же строке
        }

        readonly List<Row> rows = new List<Row>();
        TextBox txtLast, txtFirst, txtMiddle, txtPhone, txtEmail, txtLogin, txtPass, txtPass2, txtCode;
        ComboBox cbRole;
        Label lblHint;
        Row rowCode, rowPhone, rowEmail, rowRole;
        PillButton btnOk, btnCancel;
        HeroPanel band;
        public string CreatedLogin;

        public RegisterForm()
        {
            Text = "Регистрация";
            Font = new Font("Segoe UI", 10f);
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(456, 500);

            // тёмно-синяя шапка
            band = new HeroPanel();
            band.SetBounds(0, 0, 456, 78);
            Label title = new Label();
            title.Text = "Регистрация";
            title.AutoSize = true;
            title.BackColor = Color.Transparent;
            title.ForeColor = Color.White;
            title.Font = Theme.F(20f, true);
            title.Location = new Point(26, 8);
            Label sub = new Label();
            sub.Text = "Создайте учётную запись для работы в библиотеке";
            sub.AutoSize = true;
            sub.BackColor = Color.Transparent;
            sub.ForeColor = Color.FromArgb(200, 210, 236);
            sub.Font = Theme.F(9.5f, false);
            sub.Location = new Point(28, 48);
            band.Controls.Add(title);
            band.Controls.Add(sub);
            Controls.Add(band);

            txtLast = AddText("Фамилия *", 50, true, false);
            txtFirst = AddText("Имя *", 50, true, true);
            txtMiddle = AddText("Отчество", 50, false, false);

            cbRole = new ComboBox();
            cbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cbRole.Items.AddRange(new object[] { RoleStudent, RoleStaff, RoleAdmin });
            cbRole.SelectedIndex = 0;
            cbRole.Font = Theme.F(10f, false);
            rowRole = AddRow("Кто вы? *", cbRole);

            lblHint = new Label();
            lblHint.ForeColor = Theme.Muted;
            lblHint.Font = Theme.F(9f, false);
            lblHint.BackColor = Color.White;
            Controls.Add(lblHint);

            txtCode = AddText("Код доступа * (выдаёт администратор библиотеки)", 40, false, false);
            rowCode = rows[rows.Count - 1];
            txtCode.UseSystemPasswordChar = true;

            txtPhone = AddText("Телефон", 20, true, false);
            rowPhone = rows[rows.Count - 1];
            txtEmail = AddText("E-mail", 100, true, true);
            rowEmail = rows[rows.Count - 1];
            txtLogin = AddText("Логин * (латиница, цифры, _ ; 4–30 символов)", 30, false, false);
            txtPass = AddText("Пароль * (8+ символов, A-Z, a-z, 0-9)", 100, true, false);
            txtPass.UseSystemPasswordChar = true;
            txtPass2 = AddText("Повтор пароля *", 100, true, true);
            txtPass2.UseSystemPasswordChar = true;

            btnOk = new PillButton(); btnOk.Text = "Зарегистрироваться"; btnOk.Kind = PillKind.Primary; btnOk.Size = new Size(200, 42);
            btnCancel = new PillButton(); btnCancel.Text = "Отмена"; btnCancel.Kind = PillKind.Ghost; btnCancel.Size = new Size(120, 42);
            Controls.Add(btnOk); Controls.Add(btnCancel);
            AcceptButton = btnOk; CancelButton = btnCancel;
            btnOk.Click += delegate { Register(); };
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            cbRole.SelectedIndexChanged += delegate { Relayout(); };
            Relayout();
        }

        TextBox AddText(string caption, int maxLen, bool half, bool sameLine)
        {
            TextBox t = new TextBox();
            t.MaxLength = maxLen;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.Font = Theme.F(10f, false);
            Row r = AddRow(caption, t);
            if (half) r.W = 194;
            r.SameLine = sameLine;
            return t;
        }

        Row AddRow(string caption, Control input)
        {
            Label l = new Label();
            l.Text = caption;
            l.ForeColor = Theme.Muted;
            l.BackColor = Color.White;
            l.AutoSize = false;
            Row r = new Row { Cap = l, Input = input };
            rows.Add(r);
            Controls.Add(l);
            Controls.Add(input);
            return r;
        }

        /// <summary>Расставляет поля друг под другом; для сотрудника и администратора показывает код доступа и скрывает телефон/e-mail.</summary>
        void Relayout()
        {
            string role = Convert.ToString(cbRole.SelectedItem);
            bool student = role == RoleStudent;
            rowCode.Show = !student;
            rowPhone.Show = student;
            rowEmail.Show = student;

            if (role == RoleStudent) lblHint.Text = "Ученик: каталог, бронирование, личный кабинет (выдачи, брони, штрафы).";
            else if (role == RoleStaff) lblHint.Text = "Сотрудник: читатели, выдача и возврат, бронирования, штрафы.";
            else lblHint.Text = "Администратор: всё, что у сотрудника, плюс фонд, сотрудники, пользователи, удаление и списание штрафов.";

            SuspendLayout();
            int y = band.Bottom + 12;      // верх очередной строки полей
            int lineTop = y;
            foreach (Row r in rows)
            {
                r.Cap.Visible = r.Show;
                r.Input.Visible = r.Show;
                if (!r.Show) continue;
                if (!r.SameLine) { lineTop = y; y += 50; }
                int x = r.SameLine ? 28 + 206 : 28;
                r.Cap.SetBounds(x, lineTop, r.W, 18);
                r.Input.SetBounds(x, lineTop + 20, r.W, 26);
                if (r == rowRole)
                {
                    lblHint.SetBounds(28, lineTop + 48, 400, 32);
                    y += 34;
                }
            }
            y += 6;
            btnOk.Location = new Point(28, y);
            btnCancel.Location = new Point(28 + 200 + 12, y);
            ClientSize = new Size(456, y + 42 + 24);   // снизу оставляем свободное место под кнопками
            ResumeLayout();
        }

        void Register()
        {
            string last = txtLast.Text.Trim(), first = txtFirst.Text.Trim(), middle = txtMiddle.Text.Trim();
            string phone = txtPhone.Text.Trim(), email = txtEmail.Text.Trim(), login = txtLogin.Text.Trim();
            string role = Convert.ToString(cbRole.SelectedItem);
            bool student = role == RoleStudent;
            string name = @"^[A-Za-zА-Яа-яЁё\- ]+$";
            string err = null;
            if (last.Length == 0 || first.Length == 0) err = "Заполните фамилию и имя.";
            else if (!Regex.IsMatch(last, name) || !Regex.IsMatch(first, name) || (middle.Length > 0 && !Regex.IsMatch(middle, name)))
                err = "ФИО может содержать только буквы, пробел и дефис.";
            else if (!student && txtCode.Text != (role == RoleAdmin ? Security.AdminCode : Security.StaffCode))
                err = "Неверный код доступа для роли «" + role + "».";
            else if (student && phone.Length > 0 && !Regex.IsMatch(phone, @"^\+?[0-9\s\-\(\)]{5,20}$")) err = "Неверный формат телефона.";
            else if (student && email.Length > 0 && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) err = "Неверный формат e-mail.";
            else if (!Regex.IsMatch(login, @"^[A-Za-z0-9_]{4,30}$")) err = "Логин: 4–30 символов, латинские буквы, цифры и «_».";
            else if (txtPass.Text != txtPass2.Text) err = "Пароли не совпадают.";
            else err = Security.CheckPasswordPolicy(txtPass.Text);
            if (err != null) { Ui.Warn(err); return; }

            try
            {
                if (Db.Scalar("SELECT COUNT(*) FROM users WHERE login = @l", "@l", login).ToString() != "0")
                {
                    Ui.Warn("Такой логин уже занят.");
                    return;
                }
                // проверка, что скрипт базы выполнился полностью и справочники заполнены
                if (Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM roles")) < 3 ||
                    Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM reader_statuses")) < 1 ||
                    Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM reader_categories")) < 1)
                {
                    Ui.Error("В базе данных пустые справочники (роли, статусы, категории читателей).\nСкрипт library_db.sql выполнился не полностью. Запустите его целиком заново и дождитесь окончания без ошибок.");
                    return;
                }
                string salt = Security.NewSalt();
                string hash = Security.Hash(txtPass.Text, salt);
                string message;

                if (student)
                {
                    string ticket = Db.InTransaction<string>(delegate (MySql.Data.MySqlClient.MySqlConnection conn, MySql.Data.MySqlClient.MySqlTransaction tx)
                    {
                        object cat = Db.Scalar(conn, tx, "SELECT COALESCE((SELECT category_id FROM reader_categories WHERE category_name = 'Студент' LIMIT 1), (SELECT MIN(category_id) FROM reader_categories))");
                        int category = Convert.ToInt32(cat);
                        long readerId = Db.Insert(conn, tx,
                            "INSERT INTO readers (last_name, first_name, middle_name, category_id, status_id, phone, email, registered_on, valid_until) " +
                            "VALUES (@ln, @fn, @mn, @cat, 1, @ph, @em, CURRENT_DATE, DATE_ADD(CURRENT_DATE, INTERVAL 1 YEAR))",
                            "@ln", last, "@fn", first, "@mn", middle.Length == 0 ? null : middle, "@cat", category,
                            "@ph", phone.Length == 0 ? null : phone, "@em", email.Length == 0 ? null : email);
                        Db.Exec(conn, tx,
                            "INSERT INTO users (login, password_hash, salt, role_id, reader_id) " +
                            "VALUES (@l, @h, @s, (SELECT role_id FROM roles WHERE role_name = @role), @r)",
                            "@l", login, "@h", hash, "@s", salt, "@role", Session.Reader, "@r", readerId);
                        return Convert.ToString(Db.Scalar(conn, tx, "SELECT ticket_number FROM readers WHERE reader_id = @id", "@id", readerId));
                    });
                    message = "Регистрация выполнена.\nРоль: Ученик.\nНомер вашего читательского билета: " + ticket + "\nТеперь войдите в систему.";
                }
                else
                {
                    string dbRole = role == RoleAdmin ? Session.Admin : Session.Librarian;
                    string post = role == RoleAdmin ? "Администратор системы" : "Библиотекарь";
                    Db.InTransaction<int>(delegate (MySql.Data.MySqlClient.MySqlConnection conn, MySql.Data.MySqlClient.MySqlTransaction tx)
                    {
                        long employeeId = Db.Insert(conn, tx,
                            "INSERT INTO employees (last_name, first_name, middle_name, post) VALUES (@ln, @fn, @mn, @post)",
                            "@ln", last, "@fn", first, "@mn", middle.Length == 0 ? null : middle, "@post", post);
                        Db.Exec(conn, tx,
                            "INSERT INTO users (login, password_hash, salt, role_id, employee_id) " +
                            "VALUES (@l, @h, @s, (SELECT role_id FROM roles WHERE role_name = @role), @e)",
                            "@l", login, "@h", hash, "@s", salt, "@role", dbRole, "@e", employeeId);
                        return 0;
                    });
                    message = "Регистрация выполнена.\nРоль: " + role + ".\nТеперь войдите в систему.";
                }
                CreatedLogin = login;
                Ui.Info(message);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Ui.Error(Db.Explain(ex));
            }
        }
    }
}