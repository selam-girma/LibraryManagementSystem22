// BorrowerDetailsForm.cs
// This file contains the C# code for the Borrower Details Form (Add/Edit).

using System;
using System.Windows.Forms;
using System.Data.SQLite; // Required for SQLite database interaction
using System.Text.RegularExpressions; // Required for email validation

namespace MyLibraryApp
{
    public partial class BorrowerDetailsForm : Form
    {
        private int _borrowerId = 0; // 0 for Add mode, actual BorrowerID for Edit mode

        /// <summary>
        /// Constructor for adding a new borrower.
        /// </summary>
        public BorrowerDetailsForm()
        {
            InitializeComponent();
            this.Text = "Add New Borrower";
            // Hook up event handlers defined in Designer.cs
            this.btnSave.Click += new EventHandler(btnSave_Click);
            this.btnCancel.Click += new EventHandler(btnCancel_Click);
        }

        /// <summary>
        /// Constructor for editing an existing borrower.
        /// </summary>
        /// <param name="borrowerId">The ID of the borrower to edit.</param>
        public BorrowerDetailsForm(int borrowerId) : this() // Call the default constructor first
        {
            _borrowerId = borrowerId;
            this.Text = "Edit Borrower";
            LoadBorrowerDetails(); // Load existing borrower details if in edit mode
        }

        /// <summary>
        /// Loads existing borrower details into the form fields when in edit mode.
        /// </summary>
        private void LoadBorrowerDetails()
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Name, Email, Phone FROM Borrowers WHERE BorrowerID = @borrowerId;";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@borrowerId", _borrowerId);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtName.Text = reader["Name"].ToString();
                                txtEmail.Text = reader["Email"].ToString();
                                txtPhone.Text = reader["Phone"].ToString();
                            }
                            else
                            {
                                MessageBox.Show("Borrower not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.Close(); // Close the form if borrower not found
                            }
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error loading borrower details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoadBorrowerDetails): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading borrower details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoadBorrowerDetails): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Save button.
        /// Validates input and saves/updates the borrower in the database.
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            // 1. Input Validation
            if (!ValidateInput())
            {
                return; // Stop if validation fails
            }

            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();

            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SQLiteCommand command = connection.CreateCommand();

                    if (_borrowerId == 0) // Add New Borrower
                    {
                        command.CommandText = "INSERT INTO Borrowers (Name, Email, Phone) VALUES (@name, @email, @phone);";
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@phone", phone);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Borrower added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else // Edit Existing Borrower
                    {
                        command.CommandText = "UPDATE Borrowers SET Name = @name, Email = @email, Phone = @phone WHERE BorrowerID = @borrowerId;";
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@phone", phone);
                        command.Parameters.AddWithValue("@borrowerId", _borrowerId);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Borrower updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    this.DialogResult = DialogResult.OK; // Indicate success to the calling form
                    this.Close(); // Close the form
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error saving borrower: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (SaveBorrower): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while saving borrower: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (SaveBorrower): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Validates the user input in the form fields.
        /// </summary>
        /// <returns>True if all input is valid, false otherwise.</returns>
        private bool ValidateInput()
        {
            // Validate Name
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Borrower Name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            // Validate Email (optional but good practice)
            if (!string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                string email = txtEmail.Text.Trim();
                // Simple regex for email validation (can be more robust)
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return false;
                }
            }

            // Validate Phone (optional, simple check for non-empty if provided)
            // You could add regex for specific phone number formats if needed
            // if (string.IsNullOrWhiteSpace(txtPhone.Text))
            // {
            //     MessageBox.Show("Phone number cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //     txtPhone.Focus();
            //     return false;
            // }

            return true; // All validations passed
        }

        /// <summary>
        /// Handles the Click event of the Cancel button.
        /// Closes the form without saving changes.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; // Indicate cancellation to the calling form
            this.Close(); // Close the form
        }
    }
}