using System;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace LibraryApp
{
    /// <summary>Доступ к MySQL. Все запросы выполняются только с параметрами (защита от SQL-инъекций).
    /// Параметры передаются парами: "@имя", значение.</summary>
    public static class Db
    {
        public const decimal FineRatePerDay = 5m;   // штраф за один день просрочки, руб.
        public const int MaxOpenLoans = 5;          // максимум изданий на руках у одного читателя

        public static string ConnectionString
        {
            get
            {
                ConnectionStringSettings s = ConfigurationManager.ConnectionStrings["LibraryDb"];
                if (s == null) throw new InvalidOperationException("В App.config не найдена строка подключения LibraryDb.");
                return s.ConnectionString;
            }
        }

        public static MySqlConnection Open()
        {
            MySqlConnection c = new MySqlConnection(ConnectionString);
            c.Open();
            return c;
        }

        static MySqlCommand Build(string sql, MySqlConnection conn, MySqlTransaction tx, object[] args)
        {
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            if (tx != null) cmd.Transaction = tx;
            if (args != null)
            {
                if (args.Length % 2 != 0) throw new ArgumentException("Параметры задаются парами: имя, значение.");
                for (int i = 0; i < args.Length; i += 2)
                    cmd.Parameters.AddWithValue((string)args[i], args[i + 1] ?? DBNull.Value);
            }
            return cmd;
        }

        // ---------- без транзакции ----------
        public static DataTable Table(string sql, params object[] args)
        {
            using (MySqlConnection conn = Open())
            {
                return Table(conn, null, sql, args);
            }
        }

        public static object Scalar(string sql, params object[] args)
        {
            using (MySqlConnection conn = Open())
            {
                return Scalar(conn, null, sql, args);
            }
        }

        public static int Exec(string sql, params object[] args)
        {
            using (MySqlConnection conn = Open())
            {
                return Exec(conn, null, sql, args);
            }
        }

        // ---------- в составе транзакции ----------
        public static DataTable Table(MySqlConnection conn, MySqlTransaction tx, string sql, params object[] args)
        {
            using (MySqlCommand cmd = Build(sql, conn, tx, args))
            {
                DataTable t = new DataTable();
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(t);
                return t;
            }
        }

        public static object Scalar(MySqlConnection conn, MySqlTransaction tx, string sql, params object[] args)
        {
            using (MySqlCommand cmd = Build(sql, conn, tx, args))
            {
                object v = cmd.ExecuteScalar();
                return v == DBNull.Value ? null : v;
            }
        }

        public static int Exec(MySqlConnection conn, MySqlTransaction tx, string sql, params object[] args)
        {
            using (MySqlCommand cmd = Build(sql, conn, tx, args))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>INSERT с возвратом значения AUTO_INCREMENT.</summary>
        public static long Insert(MySqlConnection conn, MySqlTransaction tx, string sql, params object[] args)
        {
            using (MySqlCommand cmd = Build(sql, conn, tx, args))
            {
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }

        /// <summary>Выполняет работу в транзакции: при исключении откат, иначе фиксация.</summary>
        public static T InTransaction<T>(Func<MySqlConnection, MySqlTransaction, T> work)
        {
            using (MySqlConnection conn = Open())
            {
                MySqlTransaction tx = conn.BeginTransaction();
                try
                {
                    T result = work(conn, tx);
                    tx.Commit();
                    return result;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public static bool IsNull(object v)
        {
            return v == null || v == DBNull.Value;
        }

        /// <summary>Понятное пользователю сообщение об ошибке.</summary>
        public static string Explain(Exception ex)
        {
            MySqlException my = ex as MySqlException;
            if (my != null)
            {
                switch (my.Number)
                {
                    case 1062: return "Запись с такими данными уже существует (нарушена уникальность).";
                    case 1451: return "Запись нельзя удалить: на неё ссылаются другие записи.";
                    case 1452: return "Указана ссылка на несуществующую запись.";
                    case 3819:
                    case 4025: return "Значение не удовлетворяет ограничению целостности.";
                    case 1045: return "Неверное имя пользователя или пароль MySQL (проверьте App.config).";
                    case 1049: return "База данных library_db не найдена. Выполните скрипт library_db.sql.";
                    case 1042:
                    case 0: return "Не удалось подключиться к серверу MySQL. Проверьте, что служба запущена.";
                }
                return "Ошибка базы данных (" + my.Number + "): " + my.Message;
            }
            return ex.Message;
        }
    }
}
