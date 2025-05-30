// BorrowBookForm.cs
// Form for recording a new book borrowing.

using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SQLite;

namespace MyLibraryApp
{
    public partial class BorrowBookForm : Form
    {
        public BorrowBookForm()
        {
            InitializeComponent();
            this.Load += BorrowBookForm_Load;
            this.btnBorrow.Click += btnBorrow_Click;
            this.btnCancel.Click += btnCancel_Click;

            // Set default Borrow Date to today
            dtpBorrowDate.Value = DateTime.Today;
        }

        private void BorrowBookForm_Load(object sender, EventArgs e)
        {
            LoadBooksIntoComboBox();
            LoadBorrowersIntoComboBox();
        }

        /// <summary>
        /// Populates the ComboBox with available books.
        /// </summary>
        private void LoadBooksIntoComboBox()
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    // Only show books with available copies
                    string query = "SELECT BookID, Title, AvailableCopies FROM Books WHERE AvailableCopies > 0 ORDER BY Title;";
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                    {
                        DataTable booksTable = new DataTable();
                        adapter.Fill(booksTable);

                        cmbBook.DataSource = booksTable;
                        cmbBook.DisplayMember = "Title"; // What the user sees
                        cmbBook.ValueMember = "BookID"; // The actual ID used in the database

                        if (cmbBook.Items.Count > 0)
                        {
                            cmbBook.SelectedIndex = 0; // Select the first item by default
                            UpdateAvailableCopiesLabel();
                        }
                        else
                        {
                            lblAvailableCopies.Text = "Available: 0";
                            btnBorrow.Enabled = false; // Disable borrow button if no books are available
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error loading books: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoadBooksIntoComboBox): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading books: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoadBooksIntoComboBox): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Updates the label showing available copies when a book is selected.
        /// </summary>
        private void cmbBook_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateAvailableCopiesLabel();
        }

        private void UpdateAvailableCopiesLabel()
        {
            if (cmbBook.SelectedItem is DataRowView selectedRow)
            {
                int availableCopies = Convert.ToInt32(selectedRow["AvailableCopies"]);
                lblAvailableCopies.Text = $"Available: {availableCopies}";
                btnBorrow.Enabled = (availableCopies > 0);
            }
            else
            {
                lblAvailableCopies.Text = "Available: 0";
                btnBorrow.Enabled = false;
            }
        }

        /// <summary>
        /// Populates the ComboBox with borrowers.
        /// </summary>
        private void LoadBorrowersIntoComboBox()
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT BorrowerID, Name FROM Borrowers ORDER BY Name;";
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                    {
                        DataTable borrowersTable = new DataTable();
                        adapter.Fill(borrowersTable);

                        cmbBorrower.DataSource = borrowersTable;
                        cmbBorrower.DisplayMember = "Name";
                        cmbBorrower.ValueMember = "BorrowerID";

                        if (cmbBorrower.Items.Count > 0)
                        {
                            cmbBorrower.SelectedIndex = 0; // Select the first item by default
                        }
                        else
                        {
                            btnBorrow.Enabled = false; // Disable borrow button if no borrowers exist
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error loading borrowers: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoadBorrowersIntoComboBox): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading borrowers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoadBorrowersIntoComboBox): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the Borrow button click event.
        /// Records a new borrowing in the database and updates book's available copies.
        /// </summary>
        private void btnBorrow_Click(object sender, EventArgs e)
        {
            if (cmbBook.SelectedValue == null)
            {
                MessageBox.Show("Please select a book.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbBorrower.SelectedValue == null)
            {
                MessageBox.Show("Please select a borrower.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int bookId = Convert.ToInt32(cmbBook.SelectedValue);
            int borrowerId = Convert.ToInt32(cmbBorrower.SelectedValue);
            string borrowDate = dtpBorrowDate.Value.ToString("yyyy-MM-dd");

            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                // Use a transaction to ensure both operations (insert and update) succeed or fail together
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Check if there are available copies before borrowing
                        string checkCopiesQuery = "SELECT AvailableCopies FROM Books WHERE BookID = @bookId;";
                        using (SQLiteCommand checkCmd = new SQLiteCommand(checkCopiesQuery, connection, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@bookId", bookId);
                            int availableCopies = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (availableCopies <= 0)
                            {
                                MessageBox.Show("No available copies of this book. Cannot borrow.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                transaction.Rollback(); // Rollback any potential changes (though none yet)
                                return;
                            }
                        }

                        // 2. Insert new borrowing record
                        string insertBorrowingQuery = "INSERT INTO Borrowings (BookID, BorrowerID, BorrowDate, ReturnDate) VALUES (@bookId, @borrowerId, @borrowDate, NULL);";
                        using (SQLiteCommand insertCmd = new SQLiteCommand(insertBorrowingQuery, connection, transaction))
                        {
                            insertCmd.Parameters.AddWithValue("@bookId", bookId);
                            insertCmd.Parameters.AddWithValue("@borrowerId", borrowerId);
                            insertCmd.Parameters.AddWithValue("@borrowDate", borrowDate);
                            insertCmd.ExecuteNonQuery();
                        }

                        // 3. Decrement AvailableCopies for the book
                        string updateBookCopiesQuery = "UPDATE Books SET AvailableCopies = AvailableCopies - 1 WHERE BookID = @bookId;";
                        using (SQLiteCommand updateCmd = new SQLiteCommand(updateBookCopiesQuery, connection, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@bookId", bookId);
                            updateCmd.ExecuteNonQuery();
                        }

                        transaction.Commit(); // Commit both operations
                        MessageBox.Show("Book borrowed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK; // Indicate success and close form
                    }
                    catch (SQLiteException ex)
                    {
                        transaction.Rollback(); // Rollback if any error occurs
                        MessageBox.Show($"Database error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Console.WriteLine($"SQLite Error (BorrowBook): {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback if any error occurs
                        MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Console.WriteLine($"General Error (BorrowBook): {ex.Message}");
                    }
                } // End using transaction
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