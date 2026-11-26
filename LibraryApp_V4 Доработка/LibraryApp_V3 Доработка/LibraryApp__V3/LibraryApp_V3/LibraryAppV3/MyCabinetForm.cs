using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp
{
    /// <summary>Личный кабинет читателя: только его собственные выдачи, брони и штрафы.</summary>
    public class MyCabinetForm : SecureForm
    {
        DataGridView gLoans, gRes, gFines;

        public MyCabinetForm(int tab = 0) : base(Session.Reader)
        {
            Text = "Мой кабинет";
            ClientSize = new Size(1100, 680);
            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            gLoans = Ui.NewGrid(); gRes = Ui.NewGrid(); gFines = Ui.NewGrid();
            TabPage p1 = new TabPage("Мои выдачи"); p1.Controls.Add(gLoans);
            TabPage p2 = new TabPage("Мои бронирования"); p2.Controls.Add(gRes);
            Button bCancel = new PillButton(); bCancel.Text = "Отменить бронь"; bCancel.Dock = DockStyle.Bottom; bCancel.Height = 44;
            p2.Controls.Add(bCancel);
            TabPage p3 = new TabPage("Мои штрафы"); p3.Controls.Add(gFines);
            tabs.TabPages.Add(p1); tabs.TabPages.Add(p2); tabs.TabPages.Add(p3);
            if (tab >= 0 && tab < tabs.TabPages.Count) tabs.SelectedIndex = tab;
            Controls.Add(tabs);
            bCancel.Click += delegate { CancelReservation(); };
            Shown += delegate { LoadData(); };
        }

        void LoadData()
        {
            if (Session.ReaderId == null) { Ui.Warn("Учётная запись не связана с читателем."); return; }
            try
            {
                gLoans.DataSource = Db.Table(
                    "SELECT loan_id, loan_number, title, inventory_number, issue_date, due_date, return_date, loan_state " +
                    "FROM v_loans_full WHERE reader_id = @r ORDER BY loan_id DESC", "@r", Session.ReaderId);
                Dictionary<string, string> c1 = new Dictionary<string, string>();
                c1["loan_number"] = "№ выдачи"; c1["title"] = "Издание"; c1["inventory_number"] = "Инв. №";
                c1["issue_date"] = "Выдано"; c1["due_date"] = "Вернуть до"; c1["return_date"] = "Возвращено"; c1["loan_state"] = "Состояние";
                Ui.Format(gLoans, c1, "loan_id");

                gRes.DataSource = Db.Table(
                    "SELECT reservation_id, reservation_number, title, reserved_on, status_name " +
                    "FROM v_reservations_full WHERE reader_id = @r ORDER BY reservation_id DESC", "@r", Session.ReaderId);
                Dictionary<string, string> c2 = new Dictionary<string, string>();
                c2["reservation_number"] = "№ брони"; c2["title"] = "Издание"; c2["reserved_on"] = "Дата"; c2["status_name"] = "Статус";
                Ui.Format(gRes, c2, "reservation_id");

                gFines.DataSource = Db.Table(
                    "SELECT fine_id, loan_number, amount, accrued_on, paid_on, fine_state " +
                    "FROM v_fines_full WHERE reader_id = @r ORDER BY fine_id DESC", "@r", Session.ReaderId);
                Dictionary<string, string> c3 = new Dictionary<string, string>();
                c3["loan_number"] = "№ выдачи"; c3["amount"] = "Сумма, руб."; c3["accrued_on"] = "Начислен"; c3["paid_on"] = "Оплачен"; c3["fine_state"] = "Состояние";
                Ui.Format(gFines, c3, "fine_id");
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        void CancelReservation()
        {
            if (gRes.CurrentRow == null) { Ui.Warn("Выберите бронь."); return; }
            if (Convert.ToString(gRes.CurrentRow.Cells["status_name"].Value) != "Активна") { Ui.Warn("Можно отменить только активную бронь."); return; }
            if (!Ui.Ask("Отменить выбранную бронь?")) return;
            try
            {
                // условие reader_id гарантирует, что читатель отменяет только свою бронь
                Db.Exec("UPDATE reservations SET status_id = 3 WHERE reservation_id = @id AND reader_id = @r AND status_id = 1",
                    "@id", gRes.CurrentRow.Cells[0].Value, "@r", Session.ReaderId);
                LoadData();
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }
    }
}
