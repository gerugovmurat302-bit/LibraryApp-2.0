using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp
{
    /// <summary>Просмотр содержимого таблиц и представлений БД в режиме только чтения.</summary>
    public class DbTablesForm : SecureForm
    {
        private readonly ComboBox tables;
        private readonly DataGridView grid;
        private readonly Label info;

        // Имена объектов заданы в коде, пользователь не может подставить произвольный SQL.
        private readonly Dictionary<string, string> queries = new Dictionary<string, string>
        {
            { "Роли (roles)", "SELECT * FROM roles ORDER BY role_id" },
            { "Категории читателей (reader_categories)", "SELECT * FROM reader_categories ORDER BY category_id" },
            { "Статусы читателей (reader_statuses)", "SELECT * FROM reader_statuses ORDER BY status_id" },
            { "Статусы экземпляров (copy_statuses)", "SELECT * FROM copy_statuses ORDER BY status_id" },
            { "Статусы бронирования (reservation_statuses)", "SELECT * FROM reservation_statuses ORDER BY status_id" },
            { "Читатели (readers)", "SELECT * FROM readers ORDER BY reader_id" },
            { "Сотрудники (employees)", "SELECT * FROM employees ORDER BY employee_id" },
            { "Пользователи (users)", "SELECT user_id, login, role_id, reader_id, employee_id, is_active, created_at FROM users ORDER BY user_id" },
            { "Издания (editions)", "SELECT * FROM editions ORDER BY edition_id" },
            { "Экземпляры (copies)", "SELECT * FROM copies ORDER BY copy_id" },
            { "Выдачи (loans)", "SELECT * FROM loans ORDER BY loan_id" },
            { "Штрафы (fines)", "SELECT * FROM fines ORDER BY fine_id" },
            { "Бронирования (reservations)", "SELECT * FROM reservations ORDER BY reservation_id" },
            { "Счётчики документов (doc_counters)", "SELECT * FROM doc_counters ORDER BY doc_type, doc_year" },
            { "Каталог и наличие (v_editions_availability)", "SELECT * FROM v_editions_availability ORDER BY title" },
            { "Полные сведения о выдачах (v_loans_full)", "SELECT * FROM v_loans_full ORDER BY loan_id" },
            { "Полные сведения о бронированиях (v_reservations_full)", "SELECT * FROM v_reservations_full ORDER BY reservation_id" },
            { "Полные сведения о штрафах (v_fines_full)", "SELECT * FROM v_fines_full ORDER BY fine_id" }
        };

        public DbTablesForm() : base(Session.Admin)
        {
            Text = "Просмотр таблиц базы данных";
            ClientSize = new Size(1100, 680);
            MinimumSize = new Size(850, 500);

            Panel top = Ui.NewToolbar();
            top.Dock = DockStyle.Top;
            top.Height = 58;
            Label caption = Ui.NewLabel("Таблица базы данных:");
            tables = new ComboBox();
            tables.DropDownStyle = ComboBoxStyle.DropDownList;
            tables.Width = 390;
            foreach (string name in queries.Keys) tables.Items.Add(name);
            PillButton load = new PillButton();
            load.Text = "Показать";
            load.Kind = PillKind.Primary;
            load.AutoSize = true;
            top.Controls.Add(caption);
            top.Controls.Add(tables);
            top.Controls.Add(load);

            grid = Ui.NewGrid();
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.MultiSelect = false;
            grid.Dock = DockStyle.Fill;

            info = new Label();
            info.Dock = DockStyle.Bottom;
            info.Height = 32;
            info.Padding = new Padding(12, 7, 0, 0);
            info.Text = "Выберите таблицу и нажмите «Показать». Данные доступны только для просмотра.";

            Controls.Add(grid);
            Controls.Add(info);
            Controls.Add(top);

            load.Click += delegate { LoadSelected(); };
            tables.SelectedIndexChanged += delegate { LoadSelected(); };
            if (tables.Items.Count > 0) tables.SelectedIndex = 0;
        }

        private void LoadSelected()
        {
            if (tables.SelectedItem == null) return;
            string name = Convert.ToString(tables.SelectedItem);
            string sql;
            if (!queries.TryGetValue(name, out sql)) return;
            try
            {
                DataTable data = Db.Table(sql);
                grid.DataSource = data;
                info.Text = "Объект: " + name + "    |    Количество записей: " + data.Rows.Count + "    |    Режим: только чтение";
            }
            catch (Exception ex)
            {
                grid.DataSource = null;
                info.Text = "Не удалось загрузить данные.";
                Ui.Error(Db.Explain(ex));
            }
        }
    }
}
