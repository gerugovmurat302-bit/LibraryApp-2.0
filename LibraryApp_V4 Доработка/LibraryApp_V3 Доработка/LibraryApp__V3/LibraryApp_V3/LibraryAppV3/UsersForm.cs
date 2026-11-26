using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp
{
    /// <summary>Управление учётными записями (только администратор).</summary>
    public class UsersForm : SecureForm
    {
        DataGridView grid;
        Label lblCount;

        public UsersForm() : base(Session.Admin)
        {
            Text = "Пользователи системы";
            ClientSize = new Size(1100, 680);
            FlowLayoutPanel top = Ui.NewToolbar();
            Button bNew = new PillButton(); bNew.Text = "Создать учётную запись сотрудника"; bNew.AutoSize = true;
            Button bToggle = new PillButton(); bToggle.Text = "Заблокировать / разблокировать"; bToggle.AutoSize = true;
            Button bPass = new PillButton(); bPass.Text = "Сбросить пароль"; bPass.AutoSize = true;
            top.Controls.Add(bNew); top.Controls.Add(bToggle); top.Controls.Add(bPass);
            grid = Ui.NewGrid();
            lblCount = new Label(); lblCount.Dock = DockStyle.Bottom;
            Controls.Add(grid); Controls.Add(lblCount); Controls.Add(top);
            bNew.Click += delegate { CreateUser(); };
            bToggle.Click += delegate { Toggle(); };
            bPass.Click += delegate { ResetPassword(); };
            Shown += delegate { LoadData(); };
        }

        void LoadData()
        {
            try
            {
                DataTable t = Db.Table(
                    "SELECT u.user_id, u.login, CASE r.role_name WHEN 'Читатель' THEN 'Ученик' WHEN 'Библиотекарь' THEN 'Сотрудник' ELSE r.role_name END AS role_name, " +
                    "COALESCE(CONCAT(rd.last_name, ' ', rd.first_name), CONCAT(e.last_name, ' ', e.first_name)) AS owner_name, " +
                    "IF(u.is_active = 1, 'Активна', 'Заблокирована') AS state, u.created_at " +
                    "FROM users u JOIN roles r ON r.role_id = u.role_id " +
                    "LEFT JOIN readers rd ON rd.reader_id = u.reader_id LEFT JOIN employees e ON e.employee_id = u.employee_id " +
                    "ORDER BY u.user_id");
                grid.DataSource = t;
                Dictionary<string, string> cap = new Dictionary<string, string>();
                cap["login"] = "Логин"; cap["role_name"] = "Роль"; cap["owner_name"] = "Владелец"; cap["state"] = "Состояние"; cap["created_at"] = "Создана";
                Ui.Format(grid, cap, "user_id");
                lblCount.Text = "Учётных записей: " + t.Rows.Count;
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        void CreateUser()
        {
            string login = Prompt.Show("Новая учётная запись", "Логин (латиница, цифры, _; 4–30 символов)", false);
            if (login == null) return;
            login = login.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(login, "^[A-Za-z0-9_]{4,30}$")) { Ui.Warn("Недопустимый логин."); return; }
            string pass = Prompt.Show("Новая учётная запись", "Пароль (от 8 символов, A-Z, a-z, 0-9)", true);
            if (pass == null) return;
            string err = Security.CheckPasswordPolicy(pass);
            if (err != null) { Ui.Warn(err); return; }
            using (PairDialog d = new PairDialog("Сотрудник и роль",
                "Сотрудник (без учётной записи)", "SELECT e.employee_id, CONCAT(e.last_name, ' ', e.first_name, ' — ', e.post) AS n FROM employees e WHERE e.employee_id NOT IN (SELECT employee_id FROM users WHERE employee_id IS NOT NULL) ORDER BY e.last_name",
                "Роль", "SELECT role_id, CASE role_name WHEN 'Библиотекарь' THEN 'Сотрудник' ELSE role_name END AS role_name FROM roles WHERE role_name <> 'Читатель' ORDER BY role_id"))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string salt = Security.NewSalt();
                    Db.Exec("INSERT INTO users (login, password_hash, salt, role_id, employee_id) VALUES (@l, @h, @s, @r, @e)",
                        "@l", login, "@h", Security.Hash(pass, salt), "@s", salt, "@r", d.Second, "@e", d.First);
                    LoadData();
                }
                catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
            }
        }

        void Toggle()
        {
            if (grid.CurrentRow == null) { Ui.Warn("Выберите пользователя."); return; }
            int id = Convert.ToInt32(grid.CurrentRow.Cells[0].Value);
            if (id == Session.UserId) { Ui.Warn("Нельзя заблокировать собственную учётную запись."); return; }
            try
            {
                Db.Exec("UPDATE users SET is_active = 1 - is_active WHERE user_id = @id", "@id", id);
                LoadData();
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        void ResetPassword()
        {
            if (grid.CurrentRow == null) { Ui.Warn("Выберите пользователя."); return; }
            string pass = Prompt.Show("Сброс пароля", "Новый пароль (от 8 символов, A-Z, a-z, 0-9)", true);
            if (pass == null) return;
            string err = Security.CheckPasswordPolicy(pass);
            if (err != null) { Ui.Warn(err); return; }
            try
            {
                string salt = Security.NewSalt();
                Db.Exec("UPDATE users SET password_hash = @h, salt = @s WHERE user_id = @id",
                    "@h", Security.Hash(pass, salt), "@s", salt, "@id", grid.CurrentRow.Cells[0].Value);
                Ui.Info("Пароль изменён.");
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }
    }
}
