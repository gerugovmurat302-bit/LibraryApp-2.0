using System.Collections.Generic;

namespace LibraryApp
{
    /// <summary>Фабрика справочных форм (описание столбцов и полей ввода).</summary>
    public static class Forms
    {
        static Dictionary<string, string> Cap(params string[] pairs)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            for (int i = 0; i < pairs.Length; i += 2) d[pairs[i]] = pairs[i + 1];
            return d;
        }

        public static TableEditorForm Readers()
        {
            TableEditorForm f = new TableEditorForm("Читатели", "readers", "reader_id",
                "SELECT r.reader_id, r.ticket_number, CONCAT(r.last_name, ' ', r.first_name, ' ', COALESCE(r.middle_name, '')) AS fio, " +
                "c.category_name, s.status_name, r.phone, r.email, r.valid_until " +
                "FROM readers r JOIN reader_categories c ON c.category_id = r.category_id " +
                "JOIN reader_statuses s ON s.status_id = r.status_id",
                "CONCAT_WS(' ', r.ticket_number, r.last_name, r.first_name, r.phone, r.email)",
                "ORDER BY r.last_name, r.first_name",
                new FieldDef[] {
                    new FieldDef("last_name", "Фамилия", FieldKind.Text, true, 50, null),
                    new FieldDef("first_name", "Имя", FieldKind.Text, true, 50, null),
                    new FieldDef("middle_name", "Отчество", FieldKind.Text, false, 50, null),
                    new FieldDef("category_id", "Категория", FieldKind.Lookup, true, 0, "SELECT category_id, category_name FROM reader_categories ORDER BY category_id"),
                    new FieldDef("status_id", "Статус", FieldKind.Lookup, true, 0, "SELECT status_id, status_name FROM reader_statuses ORDER BY status_id"),
                    new FieldDef("phone", "Телефон", FieldKind.Text, false, 20, null),
                    new FieldDef("email", "E-mail", FieldKind.Text, false, 100, null),
                    new FieldDef("registered_on", "Дата регистрации", FieldKind.Date, true, 0, null),
                    new FieldDef("valid_until", "Билет действует до", FieldKind.Date, true, 0, null)
                },
                Cap("ticket_number", "Номер билета", "fio", "ФИО", "category_name", "Категория", "status_name", "Статус",
                    "phone", "Телефон", "email", "E-mail", "valid_until", "Действует до"),
                Session.Librarian, Session.Admin);
            f.DeleteRoles = new string[] { Session.Admin };   // сотрудник добавляет и изменяет, удалять может только администратор
            return f;
        }

        public static TableEditorForm Editions()
        {
            return new TableEditorForm("Издания", "editions", "edition_id",
                "SELECT edition_id, title, author, pub_year, isbn FROM editions",
                "CONCAT_WS(' ', title, author, isbn)",
                "ORDER BY title",
                new FieldDef[] {
                    new FieldDef("title", "Название", FieldKind.Text, true, 200, null),
                    new FieldDef("author", "Автор", FieldKind.Text, true, 150, null),
                    new FieldDef("pub_year", "Год издания", FieldKind.Int, true, 4, null),
                    new FieldDef("isbn", "ISBN", FieldKind.Text, true, 20, null)
                },
                Cap("title", "Название", "author", "Автор", "pub_year", "Год", "isbn", "ISBN"),
                Session.Admin);
        }

        public static TableEditorForm Copies()
        {
            return new TableEditorForm("Экземпляры", "copies", "copy_id",
                "SELECT c.copy_id, c.inventory_number, e.title, s.status_name " +
                "FROM copies c JOIN editions e ON e.edition_id = c.edition_id JOIN copy_statuses s ON s.status_id = c.status_id",
                "CONCAT_WS(' ', c.inventory_number, e.title, s.status_name)",
                "ORDER BY c.inventory_number",
                new FieldDef[] {
                    new FieldDef("inventory_number", "Инвентарный номер", FieldKind.Text, true, 20, null),
                    new FieldDef("edition_id", "Издание", FieldKind.Lookup, true, 0, "SELECT edition_id, title FROM editions ORDER BY title"),
                    new FieldDef("status_id", "Статус", FieldKind.Lookup, true, 0, "SELECT status_id, status_name FROM copy_statuses ORDER BY status_id")
                },
                Cap("inventory_number", "Инв. номер", "title", "Издание", "status_name", "Статус"),
                Session.Admin);
        }

        public static TableEditorForm Employees()
        {
            return new TableEditorForm("Сотрудники", "employees", "employee_id",
                "SELECT employee_id, last_name, first_name, middle_name, post FROM employees",
                "CONCAT_WS(' ', last_name, first_name, middle_name, post)",
                "ORDER BY last_name",
                new FieldDef[] {
                    new FieldDef("last_name", "Фамилия", FieldKind.Text, true, 50, null),
                    new FieldDef("first_name", "Имя", FieldKind.Text, true, 50, null),
                    new FieldDef("middle_name", "Отчество", FieldKind.Text, false, 50, null),
                    new FieldDef("post", "Должность", FieldKind.Text, true, 60, null)
                },
                Cap("last_name", "Фамилия", "first_name", "Имя", "middle_name", "Отчество", "post", "Должность"),
                Session.Admin);
        }
    }
}
