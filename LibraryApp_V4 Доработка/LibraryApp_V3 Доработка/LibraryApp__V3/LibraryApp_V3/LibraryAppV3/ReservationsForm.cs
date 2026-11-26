using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp
{
    /// <summary>Бронирования (библиотекарь, администратор).</summary>
    public class ReservationsForm : SecureForm
    {
        DataGridView grid;
        CheckBox chkActive;
        TextBox txtSearch;
        Label lblCount;

        public ReservationsForm() : base(Session.Admin, Session.Librarian)
        {
            Text = "Бронирования";
            ClientSize = new Size(1100, 680);
            FlowLayoutPanel top = Ui.NewToolbar();
            txtSearch = new TextBox(); txtSearch.Width = 240;
            Button bFind = new PillButton(); bFind.Text = "Найти"; bFind.AutoSize = true;
            chkActive = new CheckBox(); chkActive.Text = "Только активные"; chkActive.Checked = true; chkActive.AutoSize = true;
            Button bNew = new PillButton(); bNew.Text = "Создать бронь"; bNew.AutoSize = true;
            Button bCancel = new PillButton(); bCancel.Text = "Отменить бронь"; bCancel.AutoSize = true;
            top.Controls.Add(Ui.NewLabel("Билет / ФИО / издание:"));
            top.Controls.Add(txtSearch); top.Controls.Add(bFind); top.Controls.Add(chkActive);
            top.Controls.Add(bNew); top.Controls.Add(bCancel);
            grid = Ui.NewGrid();
            lblCount = new Label(); lblCount.Dock = DockStyle.Bottom;
            Controls.Add(grid); Controls.Add(lblCount); Controls.Add(top);
            AcceptButton = bFind;
            bFind.Click += delegate { LoadData(); };
            chkActive.CheckedChanged += delegate { LoadData(); };
            bNew.Click += delegate { Create(); };
            bCancel.Click += delegate { Cancel(); };
            Shown += delegate { LoadData(); };
        }

        void LoadData()
        {
            try
            {
                DataTable t = Db.Table(
                    "SELECT reservation_id, reservation_number, ticket_number, reader_name, title, reserved_on, status_name " +
                    "FROM v_reservations_full WHERE CONCAT_WS(' ', reservation_number, ticket_number, reader_name, title) LIKE @q " +
                    "AND (@a = 0 OR status_name = 'Активна') ORDER BY reservation_id DESC",
                    "@q", Ui.Like(txtSearch.Text), "@a", chkActive.Checked ? 1 : 0);
                grid.DataSource = t;
                Dictionary<string, string> cap = new Dictionary<string, string>();
                cap["reservation_number"] = "№ брони"; cap["ticket_number"] = "Билет"; cap["reader_name"] = "Читатель";
                cap["title"] = "Издание"; cap["reserved_on"] = "Дата"; cap["status_name"] = "Статус";
                Ui.Format(grid, cap, "reservation_id");
                lblCount.Text = "Записей: " + t.Rows.Count;
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        void Create()
        {
            using (PairDialog d = new PairDialog("Новая бронь",
                "Читатель", "SELECT reader_id, CONCAT(ticket_number, ' — ', last_name, ' ', first_name) AS n FROM readers ORDER BY last_name, first_name",
                "Издание", "SELECT edition_id, title FROM editions ORDER BY title"))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string err = Reservations.Create(d.First, d.Second);
                    if (err != null) { Ui.Warn(err); return; }
                    LoadData();
                }
                catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
            }
        }

        void Cancel()
        {
            if (grid.CurrentRow == null) { Ui.Warn("Выберите бронь."); return; }
            if (Convert.ToString(grid.CurrentRow.Cells["status_name"].Value) != "Активна") { Ui.Warn("Можно отменить только активную бронь."); return; }
            if (!Ui.Ask("Отменить выбранную бронь?")) return;
            try
            {
                Db.Exec("UPDATE reservations SET status_id = 3 WHERE reservation_id = @id AND status_id = 1", "@id", grid.CurrentRow.Cells[0].Value);
                LoadData();
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }
    }
}
