using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace LibraryApp
{
    /// <summary>Штрафы за просрочку (библиотекарь, администратор).</summary>
    public class FinesForm : SecureForm
    {
        DataGridView grid;
        CheckBox chkUnpaid;
        TextBox txtSearch;
        Label lblSum;

        public FinesForm() : base(Session.Admin, Session.Librarian)
        {
            Text = "Штрафы";
            ClientSize = new Size(1100, 680);
            FlowLayoutPanel top = Ui.NewToolbar();
            txtSearch = new TextBox(); txtSearch.Width = 240;
            Button bFind = new PillButton(); bFind.Text = "Найти"; bFind.AutoSize = true;
            chkUnpaid = new CheckBox(); chkUnpaid.Text = "Только неоплаченные"; chkUnpaid.Checked = true; chkUnpaid.AutoSize = true;
            Button bPay = new PillButton(); bPay.Text = "Принять оплату"; bPay.AutoSize = true;
            top.Controls.Add(Ui.NewLabel("Билет / ФИО / № выдачи:"));
            top.Controls.Add(txtSearch); top.Controls.Add(bFind); top.Controls.Add(chkUnpaid); top.Controls.Add(bPay);
            if (Session.InRole(Session.Admin))
            {
                PillButton bOff = new PillButton(); bOff.Text = "Списать штраф"; bOff.Kind = PillKind.Danger; bOff.AutoSize = true;
                bOff.Click += delegate { WriteOff(); };
                top.Controls.Add(bOff);
            }
            grid = Ui.NewGrid();
            lblSum = new Label(); lblSum.Dock = DockStyle.Bottom;
            Controls.Add(grid); Controls.Add(lblSum); Controls.Add(top);
            AcceptButton = bFind;
            bFind.Click += delegate { LoadData(); };
            chkUnpaid.CheckedChanged += delegate { LoadData(); };
            bPay.Click += delegate { Pay(); };
            Shown += delegate { LoadData(); };
        }

        void LoadData()
        {
            try
            {
                DataTable t = Db.Table(
                    "SELECT fine_id, loan_number, ticket_number, reader_name, amount, accrued_on, paid_on, fine_state " +
                    "FROM v_fines_full WHERE CONCAT_WS(' ', loan_number, ticket_number, reader_name) LIKE @q " +
                    "AND (@u = 0 OR paid_on IS NULL) ORDER BY fine_id DESC",
                    "@q", Ui.Like(txtSearch.Text), "@u", chkUnpaid.Checked ? 1 : 0);
                grid.DataSource = t;
                Dictionary<string, string> cap = new Dictionary<string, string>();
                cap["loan_number"] = "№ выдачи"; cap["ticket_number"] = "Билет"; cap["reader_name"] = "Читатель";
                cap["amount"] = "Сумма, руб."; cap["accrued_on"] = "Начислен"; cap["paid_on"] = "Оплачен"; cap["fine_state"] = "Состояние";
                Ui.Format(grid, cap, "fine_id");
                decimal sum = 0m;
                foreach (DataRow row in t.Rows) if (Db.IsNull(row["paid_on"])) sum += Convert.ToDecimal(row["amount"]);
                lblSum.Text = "Записей: " + t.Rows.Count + "   |   Не оплачено на сумму: " + sum.ToString("N2") + " руб.";
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        /// <summary>Списание (прощение) неоплаченного штрафа — право только администратора.</summary>
        void WriteOff()
        {
            if (!Session.InRole(Session.Admin)) { Ui.Warn("Списывать штрафы может только администратор."); return; }
            if (grid.CurrentRow == null) { Ui.Warn("Выберите штраф."); return; }
            if (!Db.IsNull(grid.CurrentRow.Cells["paid_on"].Value)) { Ui.Warn("Штраф уже оплачен, списывать его не нужно."); return; }
            if (!Ui.Ask("Списать выбранный штраф? Запись о штрафе будет удалена.")) return;
            int fineId = Convert.ToInt32(grid.CurrentRow.Cells[0].Value);
            try
            {
                Db.InTransaction<int>(delegate (MySqlConnection conn, MySqlTransaction tx)
                {
                    object readerId = Db.Scalar(conn, tx, "SELECT l.reader_id FROM fines f JOIN loans l ON l.loan_id = f.loan_id WHERE f.fine_id = @id", "@id", fineId);
                    Db.Exec(conn, tx, "DELETE FROM fines WHERE fine_id = @id AND paid_on IS NULL", "@id", fineId);
                    if (readerId != null && !Db.IsNull(readerId))
                        Db.Exec(conn, tx,
                            "UPDATE readers r SET r.status_id = IF(r.valid_until < CURRENT_DATE, 2, 1) " +
                            "WHERE r.reader_id = @r AND r.status_id = 3 " +
                            "AND NOT EXISTS (SELECT 1 FROM fines f2 JOIN loans l2 ON l2.loan_id = f2.loan_id WHERE l2.reader_id = r.reader_id AND f2.paid_on IS NULL)",
                            "@r", readerId);
                    return 0;
                });
                LoadData();
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        void Pay()
        {
            if (grid.CurrentRow == null) { Ui.Warn("Выберите штраф."); return; }
            if (!Db.IsNull(grid.CurrentRow.Cells["paid_on"].Value)) { Ui.Warn("Штраф уже оплачен."); return; }
            if (!Ui.Ask("Подтвердить получение оплаты штрафа?")) return;
            int fineId = Convert.ToInt32(grid.CurrentRow.Cells[0].Value);
            try
            {
                Db.InTransaction<int>(delegate (MySqlConnection conn, MySqlTransaction tx)
                {
                    Db.Exec(conn, tx, "UPDATE fines SET paid_on = CURRENT_DATE WHERE fine_id = @id AND paid_on IS NULL", "@id", fineId);
                    // разблокировать читателя, если неоплаченных штрафов больше нет
                    Db.Exec(conn, tx,
                        "UPDATE readers r SET r.status_id = IF(r.valid_until < CURRENT_DATE, 2, 1) " +
                        "WHERE r.status_id = 3 AND r.reader_id = (SELECT l.reader_id FROM fines f JOIN loans l ON l.loan_id = f.loan_id WHERE f.fine_id = @id) " +
                        "AND NOT EXISTS (SELECT 1 FROM fines f2 JOIN loans l2 ON l2.loan_id = f2.loan_id WHERE l2.reader_id = r.reader_id AND f2.paid_on IS NULL)",
                        "@id", fineId);
                    return 0;
                });
                LoadData();
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }
    }
}
