using System;
using System.Linq;

namespace LibraryApp
{
    /// <summary>Данные текущего пользователя (после успешного входа).</summary>
    public static class Session
    {
        public const string Admin = "Администратор";
        public const string Librarian = "Библиотекарь";
        public const string Reader = "Читатель";

        public static int UserId;
        public static string Login;
        public static string RoleName;
        public static int? ReaderId;
        public static int? EmployeeId;

        public static bool IsLoggedIn { get { return UserId > 0; } }

        /// <summary>Название роли для экрана: в базе «Читатель» и «Библиотекарь», пользователю показываем «Ученик» и «Сотрудник».</summary>
        public static string Title(string role)
        {
            if (role == Reader) return "Ученик";
            if (role == Librarian) return "Сотрудник";
            return role;
        }

        public static string RoleTitle { get { return Title(RoleName); } }

        /// <summary>Краткое описание прав текущей роли.</summary>
        public static string Rights
        {
            get
            {
                if (RoleName == Reader) return "каталог, бронирование, личный кабинет";
                if (RoleName == Librarian) return "обслуживание читателей: выдача и возврат, бронирования, штрафы";
                if (RoleName == Admin) return "полный доступ: фонд, сотрудники, пользователи, удаление и списание штрафов";
                return "";
            }
        }

        public static void Set(int userId, string login, string roleName, int? readerId, int? employeeId)
        {
            UserId = userId;
            Login = login;
            RoleName = roleName;
            ReaderId = readerId;
            EmployeeId = employeeId;
        }

        public static void Clear()
        {
            UserId = 0;
            Login = null;
            RoleName = null;
            ReaderId = null;
            EmployeeId = null;
        }

        public static bool InRole(params string[] roles)
        {
            return roles != null && roles.Contains(RoleName);
        }
    }
}
