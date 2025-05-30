// AddEditBookForm.cs
// Form for adding new books or editing existing book details.

using System;
using System.Windows.Forms;
using System.Data.SQLite;

namespace MyLibraryApp
{
    public partial class AddEditBookForm : Form
    {
        private int _bookId = 0; // 0 for Add mode, actual BookID for Edit mode

        // Constructor for Add mode
        public AddEditBookForm()
        {
            InitializeComponent();
            this.Text = "Add New Book";
            this.btnSave.Text = "Add Book";
            this.Load += AddEditBookForm_Load;
            this.btnSave.Click += btnSave_Click;
            this.btnCancel.Click += btnCancel_Click;
        }

        // Constructor for Edit mode
        public AddEditBookForm(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            this.Text = "Edit Book";
            this.btnSave.Text = "Update Book";
            this.Load += AddEditBookForm_Load;
            this.btnSave.Click += btnSave_Click;
            this.btnCancel.Click += btnCancel_Click;
        }

        private void AddEditBookForm_Load(object sender, EventArgs e)
        {
            if (_bookId != 0) // If in Edit mode, load book data
            {
                LoadBookData();
            }
        }

        /// <summary>
        /// Loads existing book data into the form controls for editing.
        /// </summary>
        private void LoadBookData()
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Title, Author, ISBN, TotalCopies, AvailableCopies FROM Books WHERE BookID = @bookId;";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@bookId", _bookId);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtTitle.Text = reader["Title"].ToString();
                                txtAuthor.Text = reader["Author"].ToString();
                                txtISBN.Text = reader["ISBN"].ToString();
                                numTotalCopies.Value = Convert.ToInt32(reader["TotalCopies"]);
                                numAvailableCopies.Value = Convert.ToInt32(reader["AvailableCopies"]);

                                // In edit mode, AvailableCopies should not be directly editable as it's derived from borrowings.
                                // Instead, we can ensure total copies is not set below available copies.
                                numAvailableCopies.Enabled = false; // Disable direct editing
                            }
                            else
                            {
                                MessageBox.Show("Book not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.DialogResult = DialogResult.Cancel; // Close form if book not found
                            }
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error loading book data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoadBookData): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading book data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoadBookData): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the Save/Update button click event.
        /// Performs validation and saves/updates book data to the database.
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs())
            {
                return; // Stop if validation fails
            }

            string title = txtTitle.Text.Trim();
            string author = txtAuthor.Text.Trim();
            string isbn = txtISBN.Text.Trim();
            int totalCopies = (int)numTotalCopies.Value;
            int availableCopies = (int)numAvailableCopies.Value; // This value is only directly set on ADD. On EDIT, it's loaded.

            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    if (_bookId == 0) // Add new book
                    {
                        // Check for duplicate ISBN
                        if (IsISBNTaken(isbn, 0))
                        {
                            MessageBox.Show("A book with this ISBN already exists.", "Duplicate ISBN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        string query = "INSERT INTO Books (Title, Author, ISBN, TotalCopies, AvailableCopies) VALUES (@title, @author, @isbn, @totalCopies, @availableCopies);";
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@title", title);
                            command.Parameters.AddWithValue("@author", author);
                            command.Parameters.AddWithValue("@isbn", isbn);
                            command.Parameters.AddWithValue("@totalCopies", totalCopies);
                            command.Parameters.AddWithValue("@availableCopies", totalCopies); // For new book, available = total
                            command.ExecuteNonQuery();
                            MessageBox.Show("Book added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else // Update existing book
                    {
                        // Check for duplicate ISBN, excluding the current book being edited
                        if (IsISBNTaken(isbn, _bookId))
                        {
                            MessageBox.Show("Another book with this ISBN already exists.", "Duplicate ISBN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Get current available copies to ensure totalCopies is not set below it
                        int currentAvailableCopies = GetAvailableCopies(_bookId);
                        if (totalCopies < currentAvailableCopies)
                        {
                            MessageBox.Show($"Total Copies cannot be less than current Available Copies ({currentAvailableCopies}). Please adjust borrowings first.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Calculate new available copies based on the change in total copies
                        int newAvailableCopies = currentAvailableCopies + (totalCopies - (int)numTotalCopies.Tag); // (int)numTotalCopies.Tag stores old TotalCopies value.
                                                                                                                   // This logic updates AvailableCopies proportionally to TotalCopies changes,
                                                                                                                   // maintaining the count of currently borrowed books.
                                                                                                                   // A more robust approach would be to calculate borrowed copies (Total - Available)
                                                                                                                   // and then set NewAvailable = NewTotal - Borrowed.
                        int borrowedCopies = (int)numTotalCopies.Tag - currentAvailableCopies;
                        newAvailableCopies = totalCopies - borrowedCopies;
                        if (newAvailableCopies < 0) newAvailableCopies = 0; // Should not happen if previous check is correct

                        string query = "UPDATE Books SET Title = @title, Author = @author, ISBN = @isbn, TotalCopies = @totalCopies, AvailableCopies = @availableCopies WHERE BookID = @bookId;";
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@title", title);
                            command.Parameters.AddWithValue("@author", author);
                            command.Parameters.AddWithValue("@isbn", isbn);
                            command.Parameters.AddWithValue("@totalCopies", totalCopies);
                            command.Parameters.AddWithValue("@availableCopies", newAvailableCopies); // Update available copies based on new total
                            command.Parameters.AddWithValue("@bookId", _bookId);
                            command.ExecuteNonQuery();
                            MessageBox.Show("Book updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    this.DialogResult = DialogResult.OK; // Indicate success and close form
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (SaveBook): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (SaveBook): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Retrieves the current number of available copies for a given book.
        /// </summary>
        private int GetAvailableCopies(int bookId)
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT AvailableCopies FROM Books WHERE BookID = @bookId;";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@bookId", bookId);
                    object result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }


        /// <summary>
        /// Validates user input before saving.
        /// </summary>
        /// <returns>True if inputs are valid, false otherwise.</returns>
        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Book Title cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtAuthor.Text))
            {
                MessageBox.Show("Author cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAuthor.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtISBN.Text))
            {
                MessageBox.Show("ISBN cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtISBN.Focus();
                return false;
            }
            // Basic ISBN format validation (e.g., check length, numeric characters)
            // For simplicity, we'll just check for non-empty. Real-world might need regex.
            if (txtISBN.Text.Length < 10) // Common ISBN lengths are 10 or 13
            {
                MessageBox.Show("ISBN seems too short. Please enter a valid ISBN.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtISBN.Focus();
                return false;
            }

            if (numTotalCopies.Value <= 0)
            {
                MessageBox.Show("Total Copies must be greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numTotalCopies.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if an ISBN is already taken by another book (excluding the current book in edit mode).
        /// </summary>
        /// <param name="isbn">The ISBN to check.</param>
        /// <param name="currentBookId">The ID of the book being edited (0 for add mode).</param>
        /// <returns>True if ISBN is taken, false otherwise.</returns>
        private bool IsISBNTaken(string isbn, int currentBookId)
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Books WHERE ISBN = @isbn AND BookID != @currentBookId COLLATE NOCASE;";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@isbn", isbn);
                    command.Parameters.AddWithValue("@currentBookId", currentBookId);
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

        private void btnSave_Click_1(object sender, EventArgs e)
        {

        }
    }
}