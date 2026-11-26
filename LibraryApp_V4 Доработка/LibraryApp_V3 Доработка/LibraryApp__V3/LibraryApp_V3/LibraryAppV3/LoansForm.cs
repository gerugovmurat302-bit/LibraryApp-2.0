using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace LibraryApp
{
    /// <summary>Выдача и возврат изданий (библиотекарь, администратор).</summary>
    public class LoansForm : SecureForm
    {
        DataGridView grid;
        CheckBox chkOpen;
        TextBox txtSearch;
        CheckBox chkOverdue;
        Label lblCount;

        public LoansForm(bool overdueOnly = false) : base(Session.Admin, Session.Librarian)
        {
            Text = "Выдача и возврат";
            ClientSize = new Size(1100, 680);
            FlowLayoutPanel top = Ui.NewToolbar();
            txtSearch = new TextBox(); txtSearch.Width = 240;
            Button bFind = new PillButton(); bFind.Text = "Найти"; bFind.AutoSize = true;
            chkOpen = new CheckBox(); chkOpen.Text = "Только на руках"; chkOpen.Checked = true; chkOpen.AutoSize = true;
            chkOverdue = new CheckBox(); chkOverdue.Text = "Только просроченные"; chkOverdue.Checked = overdueOnly; chkOverdue.AutoSize = true;
            Button bIssue = new PillButton(); bIssue.Text = "Выдать"; bIssue.AutoSize = true;
            Button bReturn = new PillButton(); bReturn.Text = "Принять возврат"; bReturn.AutoSize = true;
            top.Controls.Add(Ui.NewLabel("№ / билет / ФИО / издание:"));
            top.Controls.Add(txtSearch); top.Controls.Add(bFind); top.Controls.Add(chkOpen); top.Controls.Add(chkOverdue);
            top.Controls.Add(bIssue); top.Controls.Add(bReturn);
            grid = Ui.NewGrid();
            lblCount = new Label(); lblCount.Dock = DockStyle.Bottom;
            Controls.Add(grid); Controls.Add(lblCount); Controls.Add(top);
            AcceptButton = bFind;
            bFind.Click += delegate { LoadData(); };
            chkOpen.CheckedChanged += delegate { LoadData(); };
            chkOverdue.CheckedChanged += delegate { LoadData(); };
            bIssue.Click += delegate { Issue(); };
            bReturn.Click += delegate { Return(); };
            grid.CellFormatting += delegate (object s, DataGridViewCellFormattingEventArgs e)
            {
                if (e.RowIndex < 0) return;
                if (Convert.ToString(grid.Rows[e.RowIndex].Cells["loan_state"].Value) == "Просрочена")
                    grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.Firebrick;
            };
            Shown += delegate { LoadData(); };
        }

        void LoadData()
        {
            try
            {
                DataTable t = Db.Table(
                    "SELECT loan_id, loan_number, ticket_number, reader_name, inventory_number, title, issue_date, due_date, return_date, loan_state " +
                    "FROM v_loans_full WHERE CONCAT_WS(' ', loan_number, ticket_number, reader_name, inventory_number, title) LIKE @q " +
                    "AND (@o = 0 OR return_date IS NULL) AND (@od = 0 OR loan_state = 'Просрочена') ORDER BY loan_id DESC",
                    "@q", Ui.Like(txtSearch.Text), "@o", chkOpen.Checked ? 1 : 0, "@od", chkOverdue.Checked ? 1 : 0);
                grid.DataSource = t;
                Dictionary<string, string> cap = new Dictionary<string, string>();
                cap["loan_number"] = "№ выдачи"; cap["ticket_number"] = "Билет"; cap["reader_name"] = "Читатель";
                cap["inventory_number"] = "Инв. №"; cap["title"] = "Издание"; cap["issue_date"] = "Выдано";
                cap["due_date"] = "Вернуть до"; cap["return_date"] = "Возвращено"; cap["loan_state"] = "Состояние";
                Ui.Format(grid, cap, "loan_id");
                int late = 0;
                foreach (DataRow row in t.Rows) if (Convert.ToString(row["loan_state"]) == "Просрочена") late++;
                lblCount.Text = "Записей: " + t.Rows.Count + "   |   Просрочено: " + late;
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        void Issue()
        {
            if (Session.EmployeeId == null) { Ui.Warn("Учётная запись не связана с сотрудником библиотеки."); return; }
            using (PairDialog d = new PairDialog("Выдача издания",
                "Читатель", "SELECT reader_id, CONCAT(ticket_number, ' — ', last_name, ' ', first_name) AS n FROM readers ORDER BY last_name, first_name",
                "Свободный экземпляр", "SELECT c.copy_id, CONCAT(c.inventory_number, ' — ', e.title) AS n FROM copies c JOIN editions e ON e.edition_id = c.edition_id WHERE c.status_id = 1 ORDER BY e.title, c.inventory_number"))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string result = IssueCopy(d.First, d.Second, Session.EmployeeId.Value);
                    if (result.StartsWith("!")) { Ui.Warn(result.Substring(1)); return; }
                    Ui.Info("Выдача оформлена. Номер: " + result);
                    LoadData();
                }
                catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
            }
        }

        /// <summary>Выдача экземпляра в транзакции. Возвращает номер выдачи, либо текст ошибки с префиксом "!".</summary>
        public static string IssueCopy(int readerId, int copyId, int employeeId)
        {
            return Db.InTransaction<string>(delegate (MySqlConnection conn, MySqlTransaction tx)
            {
                DataTable r = Db.Table(conn, tx,
                    "SELECT r.status_id, r.valid_until, c.loan_days FROM readers r JOIN reader_categories c ON c.category_id = r.category_id " +
                    "WHERE r.reader_id = @id FOR UPDATE", "@id", readerId);
                if (r.Rows.Count == 0) return "!Читатель не найден.";
                if (Convert.ToInt32(r.Rows[0]["status_id"]) == 3) return "!Читатель заблокирован (есть неоплаченные штрафы или решение библиотекаря).";
                if (Convert.ToDateTime(r.Rows[0]["valid_until"]).Date < DateTime.Today) return "!Срок действия читательского билета истёк.";
                int unpaid = Convert.ToInt32(Db.Scalar(conn, tx,
                    "SELECT COUNT(*) FROM fines f JOIN loans l ON l.loan_id = f.loan_id WHERE l.reader_id = @id AND f.paid_on IS NULL", "@id", readerId));
                if (unpaid > 0) return "!У читателя есть неоплаченные штрафы.";
                int open = Convert.ToInt32(Db.Scalar(conn, tx,
                    "SELECT COUNT(*) FROM loans WHERE reader_id = @id AND return_date IS NULL", "@id", readerId));
                if (open >= Db.MaxOpenLoans) return "!Превышен лимит: не более " + Db.MaxOpenLoans + " изданий на руках.";
                DataTable c = Db.Table(conn, tx, "SELECT status_id, edition_id FROM copies WHERE copy_id = @c FOR UPDATE", "@c", copyId);
                if (c.Rows.Count == 0) return "!Экземпляр не найден.";
                if (Convert.ToInt32(c.Rows[0]["status_id"]) != 1) return "!Экземпляр уже выдан или недоступен.";
                int days = Convert.ToInt32(r.Rows[0]["loan_days"]);
                long id = Db.Insert(conn, tx,
                    "INSERT INTO loans (reader_id, copy_id, employee_id, issue_date, due_date) " +
                    "VALUES (@r, @c, @e, CURRENT_DATE, DATE_ADD(CURRENT_DATE, INTERVAL @d DAY))",
                    "@r", readerId, "@c", copyId, "@e", employeeId, "@d", days);
                Db.Exec(conn, tx, "UPDATE copies SET status_id = 2 WHERE copy_id = @c", "@c", copyId);
                // если у читателя была активная бронь на это издание — она выполнена
                Db.Exec(conn, tx, "UPDATE reservations SET status_id = 2 WHERE reader_id = @r AND edition_id = @e AND status_id = 1",
                    "@r", readerId, "@e", c.Rows[0]["edition_id"]);
                return Convert.ToString(Db.Scalar(conn, tx, "SELECT loan_number FROM loans WHERE loan_id = @id", "@id", id));
            });
        }

        void Return()
        {
            if (grid.CurrentRow == null) { Ui.Warn("Выберите выдачу."); return; }
            object loanId = grid.CurrentRow.Cells[0].Value;
            if (!Db.IsNull(grid.CurrentRow.Cells["return_date"].Value)) { Ui.Warn("Издание уже возвращено."); return; }
            if (!Ui.Ask("Принять возврат выбранного издания?")) return;
            try
            {
                decimal fine = ReturnLoan(Convert.ToInt32(loanId));
                if (fine > 0) Ui.Warn("Издание возвращено с просрочкой. Начислен штраф: " + fine.ToString("N2") + " руб. Читатель заблокирован до оплаты.");
                else Ui.Info("Возврат оформлен.");
                LoadData();
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        /// <summary>Возврат в транзакции. Возвращает сумму начисленного штрафа (0, если просрочки нет).</summary>
        public static decimal ReturnLoan(int loanId)
        {
            return Db.InTransaction<decimal>(delegate (MySqlConnection conn, MySqlTransaction tx)
            {
                DataTable l = Db.Table(conn, tx,
                    "SELECT reader_id, copy_id, due_date, return_date, DATEDIFF(CURRENT_DATE, due_date) AS late FROM loans WHERE loan_id = @id FOR UPDATE",
                    "@id", loanId);
                if (l.Rows.Count == 0) throw new InvalidOperationException("Выдача не найдена.");
                if (!Db.IsNull(l.Rows[0]["return_date"])) throw new InvalidOperationException("Издание уже возвращено.");
                Db.Exec(conn, tx, "UPDATE loans SET return_date = CURRENT_DATE WHERE loan_id = @id", "@id", loanId);
                Db.Exec(conn, tx, "UPDATE copies SET status_id = 1 WHERE copy_id = @c", "@c", l.Rows[0]["copy_id"]);
                int late = Convert.ToInt32(l.Rows[0]["late"]);
                if (late <= 0) return 0m;
                decimal amount = late * Db.FineRatePerDay;
                Db.Exec(conn, tx, "INSERT INTO fines (loan_id, amount, accrued_on) VALUES (@l, @a, CURRENT_DATE)", "@l", loanId, "@a", amount);
                Db.Exec(conn, tx, "UPDATE readers SET status_id = 3 WHERE reader_id = @r", "@r", l.Rows[0]["reader_id"]);
                return amount;
            });
        }
    }
}
