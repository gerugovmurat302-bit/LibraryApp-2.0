using System;
using MySql.Data.MySqlClient;

namespace LibraryApp
{
    /// <summary>Бизнес-правила бронирования.</summary>
    public static class Reservations
    {
        /// <summary>Создаёт бронь. Возвращает текст ошибки бизнес-правила или null, если бронь создана.</summary>
        public static string Create(int readerId, int editionId)
        {
            return Db.InTransaction<string>(delegate (MySqlConnection conn, MySqlTransaction tx)
            {
                System.Data.DataTable r = Db.Table(conn, tx,
                    "SELECT status_id, valid_until FROM readers WHERE reader_id = @id FOR UPDATE", "@id", readerId);
                if (r.Rows.Count == 0) return "Читатель не найден.";
                if (Convert.ToInt32(r.Rows[0]["status_id"]) != 1) return "Бронирование недоступно: читательский билет не действителен или заблокирован.";
                if (Convert.ToDateTime(r.Rows[0]["valid_until"]).Date < DateTime.Today) return "Срок действия читательского билета истёк.";
                object dup = Db.Scalar(conn, tx,
                    "SELECT COUNT(*) FROM reservations WHERE reader_id = @r AND edition_id = @e AND status_id = 1", "@r", readerId, "@e", editionId);
                if (Convert.ToInt32(dup) > 0) return "Это издание уже забронировано вами.";
                Db.Insert(conn, tx,
                    "INSERT INTO reservations (reader_id, edition_id, reserved_on, status_id) VALUES (@r, @e, CURRENT_DATE, 1)",
                    "@r", readerId, "@e", editionId);
                return null;
            });
        }
    }
}
