using System;
using System.Windows.Forms;

namespace LibraryApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            while (true)
            {
                Session.Clear();
                using (LoginForm login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                }
                using (MainForm main = new MainForm())
                {
                    Application.Run(main);
                    if (!main.LogoutRequested) return;
                }
            }
        }
    }
}
