using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp
{
    /// <summary>Диалог выбора двух значений из выпадающих списков (читатель + экземпляр, читатель + издание).</summary>
    public class PairDialog : Form
    {
        ComboBox cb1, cb2;
        public int First;
        public int Second;

        public PairDialog(string title, string label1, string sql1, string label2, string sql2)
        {
            Text = title;
            Shown += delegate { Ui.ApplyTheme(this, false); };
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 190);
            Label l1 = new Label(); l1.Text = label1; l1.SetBounds(16, 12, 428, 20);
            cb1 = new ComboBox(); cb1.SetBounds(16, 34, 428, 26);
            Label l2 = new Label(); l2.Text = label2; l2.SetBounds(16, 70, 428, 20);
            cb2 = new ComboBox(); cb2.SetBounds(16, 92, 428, 26);
            Button ok = new PillButton(); ok.Text = "Выполнить"; ok.SetBounds(240, 138, 100, 32);
            Button cancel = new PillButton(); cancel.Text = "Отмена"; cancel.SetBounds(348, 138, 96, 32);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.AddRange(new Control[] { l1, cb1, l2, cb2, ok, cancel });
            AcceptButton = ok; CancelButton = cancel;
            try
            {
                Ui.Lookup(cb1, sql1);
                Ui.Lookup(cb2, sql2);
            }
            catch (Exception ex) { Ui.Error(Db.Explain(ex)); }
            ok.Click += delegate
            {
                if (cb1.SelectedValue == null || cb2.SelectedValue == null) { Ui.Warn("Выберите значения в обоих списках."); return; }
                First = Convert.ToInt32(cb1.SelectedValue);
                Second = Convert.ToInt32(cb2.SelectedValue);
                DialogResult = DialogResult.OK;
                Close();
            };
        }
    }
}
