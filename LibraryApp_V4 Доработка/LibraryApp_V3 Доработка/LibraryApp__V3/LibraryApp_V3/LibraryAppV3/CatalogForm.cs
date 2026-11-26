using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp
{
    public class CatalogForm : SecureForm
    {
        DataGridView grid;
        TextBox txtSearch;
        CheckBox chkFree;
        Label lblCount;

        public CatalogForm(bool onlyFree = false) : base(Session.Admin, Session.Librarian, Session.Reader)
        {
            Text = "Каталог изданий";
            ClientSize = new Size(1100, 680);

            FlowLayoutPanel top = Ui.NewToolbar();
            txtSearch = new TextBox(); txtSearch.Width = 300;
            PillButton bFind = new PillButton(); bFind.Text = "Найти"; bFind.AutoSize = true;
            PillButton bReset = new PillButton(); bReset.Text = "Сбросить"; bReset.AutoSize = true;
            chkFree = new CheckBox(); chkFree.Text = "Только со свободными экземплярами"; chkFree.AutoSize = true;
            chkFree.Checked = onlyFree;
            PillButton bCopies = new PillButton(); bCopies.Text = "Экземпляры издания"; bCopies.AutoSize = true;
            top.Controls.Add(Ui.NewLabel("Название, автор или ISBN:"));
            top.Controls.Add(txtSearch);
            top.Controls.Add(bFind);
            top.Controls.Add(bReset);
            top.Controls.Add(chkFree);
            top.Controls.Add(bCopies);
            if (Session.InRole(Session.Reader))
            {
                PillButton bRes = new PillButton(); bRes.Text = "Забронировать"; bRes.AutoSize = true;
                bRes.Click += delegate { Reserve(); };
                top.Controls.Add(bRes);
            }
            grid = Ui.NewGrid();
            lblCount = new Label(); lblCount.Dock = DockStyle.Bottom;
            Controls.Add(grid); Controls.Add(lblCount); Controls.Add(top);
            AcceptButton = bFind;
            bFind.Click += delegate { LoadData(); };
            bReset.Click += delegate { txtSearch.Clear(); chkFree.Checked = false; LoadData(); };
            bCopies.Click += delegate { ShowCopies(); };
            chkFree.CheckedChanged += delegate { LoadData(); };
            grid.CellDoubleClick += delegate (object s, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) ShowCopies(); };
            Shown += delegate { LoadData(); };
        }

        void LoadData()
        {
            try
            {
                string sql = "SELECT edition_id, title, author, pub_year, isbn, total_copies, free_copies FROM v_editions_availability " +
                             "WHERE (CONCAT_WS(' ', title, author, isbn) LIKE @q) AND (@onlyFree = 0 OR free_copies > 0) ORDER BY title";
                DataTable t = Db.Table(sql, "@q", Ui.Like(txtSearch.Text), "@onlyFree", chkFree.Checked ? 1 : 0);
                grid.DataSource = t;
                Dictionary<string, string> cap = new Dictionary<string, string>();
                cap["title"] = "Название"; cap["author"] = "Автор"; cap["pub_year"] = "Год"; cap["isbn"] = "ISBN";
                cap["total_copies"] = "Всего экз."; cap["free_copies"] = "Свободно";
                Ui.Format(grid, cap, "edition_id");
                lblCount.Text = "Найдено изданий: " + t.Rows.Count;
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        void ShowCopies()
        {
            if (grid.CurrentRow == null) { Ui.Warn("Выберите издание."); return; }
            int editionId = Convert.ToInt32(grid.CurrentRow.Cells[0].Value);
            string title = Convert.ToString(grid.CurrentRow.Cells["title"].Value);
            try
            {
                DataTable t = Db.Table(
                    "SELECT c.inventory_number, s.status_name FROM copies c JOIN copy_statuses s ON s.status_id = c.status_id " +
                    "WHERE c.edition_id = @id ORDER BY c.inventory_number", "@id", editionId);
                using (CopiesDialog d = new CopiesDialog(title, t)) d.ShowDialog(this);
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }

        void Reserve()
        {
            if (grid.CurrentRow == null) { Ui.Warn("Выберите издание."); return; }
            if (Session.ReaderId == null) { Ui.Warn("Учётная запись не связана с читателем."); return; }
            object editionId = grid.CurrentRow.Cells[0].Value;
            try
            {
                string err = Reservations.Create(Session.ReaderId.Value, Convert.ToInt32(editionId));
                if (err != null) { Ui.Warn(err); return; }
                Ui.Info("Издание забронировано. Номер брони смотрите в разделе «Мой кабинет».");
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
        }
    }

    public class CopiesDialog : Form
    {
        public CopiesDialog(string title, DataTable copies)
        {
            Text = "Экземпляры: " + title;
            ClientSize = new Size(560, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            DataGridView g = Ui.NewGrid();
            g.DataSource = copies;
            Dictionary<string, string> cap = new Dictionary<string, string>();
            cap["inventory_number"] = "Инвентарный номер"; cap["status_name"] = "Статус";
            Ui.Format(g, cap);

            Panel bottom = new Panel(); bottom.Dock = DockStyle.Bottom; bottom.Height = 56; bottom.BackColor = Color.White;
            PillButton close = new PillButton(); close.Text = "Закрыть"; close.Kind = PillKind.Ghost;
            close.Location = new Point(ClientSize.Width - close.Width - 16, 10);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            close.DialogResult = DialogResult.Cancel;
            bottom.Controls.Add(close);

            Controls.Add(g); Controls.Add(bottom);
            CancelButton = close;
            Shown += delegate { Ui.ApplyTheme(this, false); };
        }
    }
}
