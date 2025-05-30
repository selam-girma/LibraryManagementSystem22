// DatabaseHelper.cs
using System;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms; // For MessageBox

namespace MyLibraryApp
{
    public static class DatabaseHelper
    {
        private static string dbFileName = "Library.db";
        private static string connectionString = $"Data Source={dbFileName};Version=3;";

        public static string ConnectionString
        {
            get { return connectionString; }
        }

        public static void InitializeDatabase()
        {
            if (!File.Exists(dbFileName))
            {
                SQLiteConnection.CreateFile(dbFileName);
                CreateTables();
                AddDefaultUser(); // Add a default user after tables are created
            }
            else
            {
                // Ensure tables exist even if the file was previously created but tables weren't for some reason
                // This call is idempotent, meaning it won't re-create tables if they already exist
                CreateTables();
            }
        }

        private static void CreateTables()
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string createBooksTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Books (
                        BookID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Author TEXT NOT NULL,
                        ISBN TEXT UNIQUE NOT NULL,
                        TotalCopies INTEGER NOT NULL,
                        AvailableCopies INTEGER NOT NULL
                    );";

                string createBorrowersTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Borrowers (
                        BorrowerID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        ContactInfo TEXT UNIQUE NOT NULL
                    );";

                string createBorrowingsTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Borrowings (
                        BorrowingID INTEGER PRIMARY KEY AUTOINCREMENT,
                        BookID INTEGER NOT NULL,
                        BorrowerID INTEGER NOT NULL,
                        BorrowDate TEXT NOT NULL,
                        ReturnDate TEXT,
                        FOREIGN KEY (BookID) REFERENCES Books(BookID),
                        FOREIGN KEY (BorrowerID) REFERENCES Borrowers(BorrowerID)
                    );";

                // New Users table for login
                string createUsersTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Password TEXT NOT NULL -- In a real application, hash and salt passwords!
                    );";

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = createBooksTableQuery;
                    command.ExecuteNonQuery();

                    command.CommandText = createBorrowersTableQuery;
                    command.ExecuteNonQuery();

                    command.CommandText = createBorrowingsTableQuery;
                    command.ExecuteNonQuery();

                    command.CommandText = createUsersTableQuery; // Execute command for Users table
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void AddDefaultUser()
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                // Check if any user exists to avoid adding duplicate default users on subsequent runs
                string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = 'admin';";
                using (SQLiteCommand checkCmd = new SQLiteCommand(checkUserQuery, connection))
                {
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                    {
                        string insertUserQuery = "INSERT INTO Users (Username, Password) VALUES (@username, @password);";
                        using (SQLiteCommand insertCmd = new SQLiteCommand(insertUserQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@username", "admin");
                            insertCmd.Parameters.AddWithValue("@password", "password"); // Simple password for demo. Hash this in production!
                            insertCmd.ExecuteNonQuery();
                            MessageBox.Show("Default user 'admin' with password 'password' added to the database.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }

        // Method for user authentication
        public static bool AuthenticateUser(string username, string password)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Users WHERE Username = @username AND Password = @password;";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", password); // Again, use hashed passwords in real apps
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error during authentication: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (AuthenticateUser): {ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred during authentication: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (AuthenticateUser): {ex.Message}");
                    return false;
                }
            }
        }
    }
}