// LoginForm.cs
// This file contains the C# code for the Login Form.

using System;
using System.Windows.Forms;
using System.Data.SQLite; // Required for SQLite database interaction

namespace MyLibraryApp
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            // Hook up the login button click event
            this.btnLogin.Click += new EventHandler(btnLogin_Click);
        }

        /// <summary>
        /// Handles the Click event of the Login button.
        /// Authenticates the user against the database.
        /// </summary>
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text; // Password is not trimmed for potential leading/trailing spaces

            // Basic validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connectionString = DatabaseHelper.ConnectionString; // Get connection string from DatabaseHelper
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Users WHERE Username = @username AND Password = @password;";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", password); // IMPORTANT: In a real application, passwords should be securely hashed!

                        int userCount = Convert.ToInt32(command.ExecuteScalar());

                        if (userCount > 0)
                        {
                            MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK; // Indicate successful login to Program.cs
                            this.Close(); // Close the LoginForm
                        }
                        else
                        {
                            MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtPassword.Clear(); // Clear password field on failed login
                            txtUsername.Focus(); // Set focus back to username for re-entry
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoginForm): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoginForm): {ex.Message}");
                }
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click_1(object sender, EventArgs e)
        {

        }
    }
}