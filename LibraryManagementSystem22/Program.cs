// Program.cs
// This is the entry point of the application.

using System;
using System.Windows.Forms;
using System.Data.SQLite; // Needed for SQLite operations

namespace MyLibraryApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow()); // This should open your main form
        }
    }
}
    