using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp
{
    public enum FieldKind { Text, Int, Decimal, Date, Lookup }

    /// <summary>Описание поля таблицы для универсального редактора.</summary>
    public class FieldDef
    {
        public string Column;
        public string Caption;
        public FieldKind Kind;
        public bool Required;
        public int MaxLen;
        public string LookupSql;   // SELECT id, name FROM ...

        public FieldDef(string column, string caption, FieldKind kind, bool required, int maxLen, string lookupSql)
        {
            Column = column; Caption = caption; Kind = kind; Required = required; MaxLen = maxLen; LookupSql = lookupSql;
        }
    }

    /// <summary>Универсальная форма: таблица DataGridView + поиск + добавление / изменение / удаление (CRUD).</summary>
    public class TableEditorForm : SecureForm
    {
        readonly string selectSql;      // SELECT ... FROM ... (без WHERE и ORDER BY), первый столбец — первичный ключ
        readonly string searchExpr;     // выражение для поиска, например CONCAT_WS(' ', a, b)
        readonly string orderBy;
        readonly string table;
        readonly string pk;
        readonly FieldDef[] fields;
        readonly Dictionary<string, string> captions;
        DataGridView grid;
        TextBox txtSearch;
        Label lblCount;
        PillButton btnDelete;

        /// <summary>Роли, которым разрешено удалять записи (null — всем, кто может открыть форму).</summary>
        public string[] DeleteRoles;

        public TableEditorForm(string title, string table, string pk, string selectSql, string searchExpr, string orderBy,
            FieldDef[] fields, Dictionary<string, string> captions, params string[] roles)
            : base(roles)
        {
            this.table = table; this.pk = pk; this.selectSql = selectSql; this.searchExpr = searchExpr;
            this.orderBy = orderBy; this.fields = fields; this.captions = captions;
            Text = title;
            ClientSize = new Size(1100, 680);

            FlowLayoutPanel top = Ui.NewToolbar();
            txtSearch = new TextBox(); txtSearch.Width = 320;
            PillButton bFind = new PillButton(); bFind.Text = "Найти"; bFind.AutoSize = true;
            PillButton bReset = new PillButton(); bReset.Text = "Сбросить"; bReset.AutoSize = true;
            PillButton bAdd = new PillButton(); bAdd.Text = "Добавить"; bAdd.AutoSize = true;
            PillButton bEdit = new PillButton(); bEdit.Text = "Изменить"; bEdit.AutoSize = true;
            PillButton bDel = new PillButton(); bDel.Text = "Удалить"; bDel.AutoSize = true; btnDelete = bDel;
            top.Controls.Add(Ui.NewLabel("Поиск:"));
            top.Controls.Add(txtSearch);
            top.Controls.Add(bFind);
            top.Controls.Add(bReset);
            top.Controls.Add(bAdd);
            top.Controls.Add(bEdit);
            top.Controls.Add(bDel);

            grid = Ui.NewGrid();
            lblCount = new Label(); lblCount.Dock = DockStyle.Bottom;

            Controls.Add(grid);
            Controls.Add(lblCount);
            Controls.Add(top);
            AcceptButton = bFind;

            bFind.Click += delegate { LoadData(); };
            bReset.Click += delegate { txtSearch.Clear(); LoadData(); };
            bAdd.Click += delegate { Edit(null); };
            bEdit.Click += delegate { Edit(CurrentId()); };
            bDel.Click += delegate { Delete(); };
            grid.CellDoubleClick += delegate (object s, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) Edit(CurrentId()); };
            Shown += delegate { btnDelete.Visible = DeleteRoles == null || Session.InRole(DeleteRoles); LoadData(); };
        }

        object CurrentId()
        {
            if (grid.CurrentRow == null) return null;
            return grid.CurrentRow.Cells[0].Value;
        }

        void LoadData()
        {
            try
            {
                string sql = selectSql;
                DataTable t;
                string q = txtSearch.Text.Trim();
                if (q.Length > 0 && searchExpr != null)
                    t = Db.Table(sql + " WHERE " + searchExpr + " LIKE @q ESCAPE '\\\\' " + orderBy, "@q", Ui.Like(q));
                else
                    t = Db.Table(sql + " " + orderBy);
                grid.DataSource = t;
                Ui.Format(grid, captions, t.Columns[0].ColumnName);
                lblCount.Text = "Записей: " + t.Rows.Count;
            }
            catch (Exception ex)
            {
                Ui.Error(Db.Explain(ex));
            }
        }

        void Delete()
        {
            if (DeleteRoles != null && !Session.InRole(DeleteRoles)) { Ui.Warn("Удаление записей доступно только администратору."); return; }
            object id = CurrentId();
            if (id == null) { Ui.Warn("Выберите запись."); return; }
            // Модальное диалоговое окно: пользователь подтверждает действие.
            DialogResult answer = MessageBox.Show(
                "Вы действительно хотите удалить выбранную запись?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            // Анализируем результат MessageBox.Show, как показано в задании.
            switch (answer)
            {
                case DialogResult.Yes:
                    break; // продолжаем удаление
                case DialogResult.No:
                case DialogResult.Cancel:
                    return; // отменяем операцию и оставляем запись без изменений
                default:
                    return;
            }
            try
            {
                Db.Exec("DELETE FROM " + table + " WHERE " + pk + " = @id", "@id", id);
                LoadData();
            }
            catch (Exception ex)
            {
                Ui.Error(Db.Explain(ex));
            }
        }

        void Edit(object id)
        {
            DataRow current = null;
            try
            {
                if (id != null)
                {
                    DataTable t = Db.Table("SELECT * FROM " + table + " WHERE " + pk + " = @id", "@id", id);
                    if (t.Rows.Count == 0) { Ui.Warn("Запись не найдена."); return; }
                    current = t.Rows[0];
                }
                using (RecordDialog d = new RecordDialog(Text, fields, current))
                {
                    if (d.ShowDialog(this) != DialogResult.OK) return;
                    Save(id, d.Values);
                }
                LoadData();
            }
            catch (Exception ex)
            {
                Ui.Error(Db.Explain(ex));
            }
        }

        void Save(object id, object[] values)
        {
            List<object> args = new List<object>();
            if (id == null)
            {
                string cols = "", pars = "";
                for (int i = 0; i < fields.Length; i++)
                {
                    cols += (i > 0 ? ", " : "") + fields[i].Column;
                    pars += (i > 0 ? ", " : "") + "@p" + i;
                    args.Add("@p" + i); args.Add(values[i]);
                }
                Db.Exec("INSERT INTO " + table + " (" + cols + ") VALUES (" + pars + ")", args.ToArray());
            }
            else
            {
                string set = "";
                for (int i = 0; i < fields.Length; i++)
                {
                    set += (i > 0 ? ", " : "") + fields[i].Column + " = @p" + i;
                    args.Add("@p" + i); args.Add(values[i]);
                }
                args.Add("@id"); args.Add(id);
                Db.Exec("UPDATE " + table + " SET " + set + " WHERE " + pk + " = @id", args.ToArray());
            }
        }
    }

    /// <summary>Диалог ввода/редактирования записи; элементы создаются по описанию полей, значения проверяются.</summary>
    public class RecordDialog : Form
    {
        readonly FieldDef[] fields;
        readonly Control[] inputs;
        public object[] Values;

        public RecordDialog(string title, FieldDef[] fields, DataRow row)
        {
            this.fields = fields;
            inputs = new Control[fields.Length];
            Text = (row == null ? "Добавление: " : "Изменение: ") + title;
            Shown += delegate { Ui.ApplyTheme(this, false); };
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            int y = 12;
            for (int i = 0; i < fields.Length; i++)
            {
                FieldDef f = fields[i];
                Label l = new Label();
                l.Text = f.Caption + (f.Required ? " *" : "");
                l.SetBounds(16, y, 380, 20);
                Controls.Add(l);
                Control c;
                object val = row == null ? null : row[f.Column];
                if (val == DBNull.Value) val = null;
                if (f.Kind == FieldKind.Date)
                {
                    DateTimePicker dp = new DateTimePicker();
                    dp.Format = DateTimePickerFormat.Short;
                    dp.ShowCheckBox = !f.Required;
                    if (val != null) { dp.Value = Convert.ToDateTime(val); if (!f.Required) dp.Checked = true; }
                    else if (!f.Required) dp.Checked = false;
                    c = dp;
                }
                else if (f.Kind == FieldKind.Lookup)
                {
                    ComboBox cb = new ComboBox();
                    Ui.Lookup(cb, f.LookupSql);
                    if (val != null) cb.SelectedValue = Convert.ToInt32(val);
                    c = cb;
                }
                else
                {
                    TextBox tb = new TextBox();
                    if (f.MaxLen > 0) tb.MaxLength = f.MaxLen;
                    if (val != null) tb.Text = Convert.ToString(val);
                    c = tb;
                }
                c.SetBounds(16, y + 22, 380, 26);
                Controls.Add(c);
                inputs[i] = c;
                y += 54;
            }
            Button ok = new PillButton(); ok.Text = "Сохранить"; ok.SetBounds(206, y + 4, 100, 32);
            Button cancel = new PillButton(); cancel.Text = "Отмена"; cancel.SetBounds(314, y + 4, 82, 32);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(ok); Controls.Add(cancel);
            AcceptButton = ok; CancelButton = cancel;
            ClientSize = new Size(412, y + 48);
            ok.Click += delegate { if (Collect()) { DialogResult = DialogResult.OK; Close(); } };
        }

        bool Collect()
        {
            object[] v = new object[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                FieldDef f = fields[i];
                Control c = inputs[i];
                if (f.Kind == FieldKind.Date)
                {
                    DateTimePicker dp = (DateTimePicker)c;
                    v[i] = (dp.ShowCheckBox && !dp.Checked) ? null : (object)dp.Value.Date;
                }
                else if (f.Kind == FieldKind.Lookup)
                {
                    ComboBox cb = (ComboBox)c;
                    if (cb.SelectedValue == null) { return Bad(f, c, "выберите значение"); }
                    v[i] = cb.SelectedValue;
                }
                else
                {
                    string s = c.Text.Trim();
                    if (s.Length == 0)
                    {
                        if (f.Required) return Bad(f, c, "обязательное поле");
                        v[i] = null;
                        continue;
                    }
                    if (f.Kind == FieldKind.Int)
                    {
                        int n;
                        if (!int.TryParse(s, out n)) return Bad(f, c, "нужно целое число");
                        v[i] = n;
                    }
                    else if (f.Kind == FieldKind.Decimal)
                    {
                        decimal d;
                        if (!decimal.TryParse(s.Replace('.', ','), out d) && !decimal.TryParse(s, out d)) return Bad(f, c, "нужно число");
                        v[i] = d;
                    }
                    else v[i] = s;
                }
            }
            Values = v;
            return true;
        }

        bool Bad(FieldDef f, Control c, string why)
        {
            Ui.Warn("Поле «" + f.Caption + "»: " + why + ".");
            c.Focus();
            return false;
        }
    }
}
