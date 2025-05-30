// AddEditBorrowerForm.cs
// Form for adding new borrowers or editing existing borrower details.

using System;
using System.Data.SQLite;
using System.Windows.Forms;
using System.Xml.Linq;

namespace MyLibraryApp
{
    public partial class AddEditBorrowerForm : Form
    {
        private int _borrowerId = 0; // 0 for Add mode, actual BorrowerID for Edit mode

        // Constructor for Add mode
        public AddEditBorrowerForm()
        {
            InitializeComponent();
            this.Text = "Add New Borrower";
            this.btnSave.Text = "Add Borrower";
            this.Load += AddEditBorrowerForm_Load;
            this.btnSave.Click += btnSave_Click;
            this.btnCancel.Click += btnCancel_Click;
        }

        // Constructor for Edit mode
        public AddEditBorrowerForm(int borrowerId)
        {
            InitializeComponent();
            _borrowerId = borrowerId;
            this.Text = "Edit Borrower";
            this.btnSave.Text = "Update Borrower";
            this.Load += AddEditBorrowerForm_Load;
            this.btnSave.Click += btnSave_Click;
            this.btnCancel.Click += btnCancel_Click;
        }

        private void AddEditBorrowerForm_Load(object sender, EventArgs e)
        {
            if (_borrowerId != 0) // If in Edit mode, load borrower data
            {
                LoadBorrowerData();
            }
        }

        /// <summary>
        /// Loads existing borrower data into the form controls for editing.
        /// </summary>
        private void LoadBorrowerData()
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Name, ContactInfo FROM Borrowers WHERE BorrowerID = @borrowerId;";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@borrowerId", _borrowerId);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtName.Text = reader["Name"].ToString();
                                txtContactInfo.Text = reader["ContactInfo"].ToString();
                            }
                            else
                            {
                                MessageBox.Show("Borrower not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.DialogResult = DialogResult.Cancel; // Close form if borrower not found
                            }
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error loading borrower data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoadBorrowerData): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading borrower data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoadBorrowerData): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the Save/Update button click event.
        /// Performs validation and saves/updates borrower data to the database.
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs())
            {
                return; // Stop if validation fails
            }

            string name = txtName.Text.Trim();
            string contactInfo = txtContactInfo.Text.Trim();

            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    if (_borrowerId == 0) // Add new borrower
                    {
                        // Check for duplicate borrower name (case-insensitive)
                        if (IsBorrowerNameTaken(name, 0))
                        {
                            MessageBox.Show("A borrower with this name already exists.", "Duplicate Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        string query = "INSERT INTO Borrowers (Name, ContactInfo) VALUES (@name, @contactInfo);";
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@name", name);
                            command.Parameters.AddWithValue("@contactInfo", contactInfo);
                            command.ExecuteNonQuery();
                            MessageBox.Show("Borrower added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else // Update existing borrower
                    {
                        // Check for duplicate borrower name, excluding the current borrower being edited
                        if (IsBorrowerNameTaken(name, _borrowerId))
                        {
                            MessageBox.Show("Another borrower with this name already exists.", "Duplicate Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        string query = "UPDATE Borrowers SET Name = @name, ContactInfo = @contactInfo WHERE BorrowerID = @borrowerId;";
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@name", name);
                            command.Parameters.AddWithValue("@contactInfo", contactInfo);
                            command.Parameters.AddWithValue("@borrowerId", _borrowerId);
                            command.ExecuteNonQuery();
                            MessageBox.Show("Borrower updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    this.DialogResult = DialogResult.OK; // Indicate success and close form
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (SaveBorrower): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (SaveBorrower): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Validates user input before saving.
        /// </summary>
        /// <returns>True if inputs are valid, false otherwise.</returns>
        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Borrower Name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtContactInfo.Text))
            {
                MessageBox.Show("Contact Info cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtContactInfo.Focus();
                return false;
            }
            // Basic contact info validation (e.g., email or phone format, but not strictly enforced here)
            // For simplicity, we'll just check for non-empty.
            return true;
        }

        /// <summary>
        /// Checks if a borrower name is already taken by another borrower (excluding the current borrower in edit mode).
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <param name="currentBorrowerId">The ID of the borrower being edited (0 for add mode).</param>
        /// <returns>True if name is taken, false otherwise.</returns>
        private bool IsBorrowerNameTaken(string name, int currentBorrowerId)
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                // Use COLLATE NOCASE for case-insensitive comparison
                string query = "SELECT COUNT(*) FROM Borrowers WHERE Name = @name AND BorrowerID != @currentBorrowerId COLLATE NOCASE;";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@currentBorrowerId", currentBorrowerId);
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        /// <summary>
        /// Handles the Cancel button click event.
        /// Closes the form without saving.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}