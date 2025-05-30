// MainWindow.cs
// This file contains the main logic for the MyLibrary application's main window.

using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SQLite; // Make sure this namespace is included

namespace MyLibraryApp
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            // Hook up tab control selection changed event
            this.tabControlMain.SelectedIndexChanged += new EventHandler(tabControlMain_SelectedIndexChanged);

            // Hook up Books Management button click events and DataGridView selection changed event
            this.btnAddBook.Click += new EventHandler(btnAddBook_Click);
            this.btnEditBook.Click += new EventHandler(btnEditBook_Click);
            this.btnDeleteBook.Click += new EventHandler(btnDeleteBook_Click);
            this.dgvBooks.SelectionChanged += new EventHandler(dgvBooks_SelectionChanged);
            this.dgvBooks.CellDoubleClick += new DataGridViewCellEventHandler(dgvBooks_CellDoubleClick);

            // Hook up Book Search functionality
            this.btnSearchBook.Click += new EventHandler(btnSearchBook_Click);
            this.btnClearBookSearch.Click += new EventHandler(btnClearBookSearch_Click);
            this.txtSearchBook.KeyDown += new KeyEventHandler(txtSearchBook_KeyDown); // Allows searching with Enter key

            // Hook up Borrowers Management button click events and DataGridView selection changed event
            this.btnAddBorrower.Click += new EventHandler(btnAddBorrower_Click);
            this.btnEditBorrower.Click += new EventHandler(btnEditBorrower_Click);
            this.btnDeleteBorrower.Click += new EventHandler(btnDeleteBorrower_Click);
            this.dgvBorrowers.SelectionChanged += new EventHandler(dgvBorrowers_SelectionChanged);
            this.dgvBorrowers.CellDoubleClick += new DataGridViewCellEventHandler(dgvBorrowers_CellDoubleClick);

            // Hook up Borrower Search functionality
            this.btnSearchBorrower.Click += new EventHandler(btnSearchBorrower_Click);
            this.btnClearBorrowerSearch.Click += new EventHandler(btnClearBorrowerSearch_Click);
            this.txtSearchBorrower.KeyDown += new KeyEventHandler(txtSearchBorrower_KeyDown); // Allows searching with Enter key

            // Hook up Borrowings Management button click events and DataGridView selection changed event
            this.btnBorrowBook.Click += new EventHandler(btnBorrowBook_Click);
            this.btnReturnBook.Click += new EventHandler(btnReturnBook_Click);
            this.dgvBorrowings.SelectionChanged += new EventHandler(dgvBorrowings_SelectionChanged);

            // Hook up Borrowings Search functionality (NEW)
            this.btnSearchBorrowing.Click += new EventHandler(btnSearchBorrowing_Click);
            this.btnClearBorrowingSearch.Click += new EventHandler(btnClearBorrowingSearch_Click);
            this.txtSearchBorrowing.KeyDown += new KeyEventHandler(txtSearchBorrowing_KeyDown); // Allows searching with Enter key

            // Initial load for the default selected tab (Books)
            this.Load += new EventHandler(MainWindow_Load);
        }

        /// <summary>
        /// Handles the main window load event.
        /// </summary>
        private void MainWindow_Load(object sender, EventArgs e)
        {
            // Ensure the database is initialized and tables exist
            DatabaseHelper.InitializeDatabase();
            // Load data for the currently selected tab (Books tab is usually default first)
            LoadBooks();
            UpdateBookButtonsState(); // Set initial button states
            UpdateBorrowerButtonsState(); // Set initial button states
            UpdateBorrowingButtonsState(); // Set initial button states
        }

        /// <summary>
        /// Handles tab change in the main TabControl.
        /// </summary>
        private void tabControlMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Load data specific to a tab when it's selected.
            if (tabControlMain.SelectedTab == tabPageBooks)
            {
                LoadBooks(); // Ensure books are refreshed if tab is switched back
                UpdateBookButtonsState();
            }
            else if (tabControlMain.SelectedTab == tabPageBorrowers)
            {
                LoadBorrowers(); // Load borrowers when this tab is selected
                UpdateBorrowerButtonsState(); // Ensure button states are correct on tab switch
            }
            else if (tabControlMain.SelectedTab == tabPageBorrowings)
            {
                LoadBorrowings(); // Load borrowings when this tab is selected
                UpdateBorrowingButtonsState(); // Ensure button states are correct on tab switch
            }
        }

        #region Books Management Methods

        /// <summary>
        /// Loads book data from the database into the DataGridView, with optional search filtering.
        /// </summary>
        /// <param name="searchTerm">Optional search term to filter books by title, author, or ISBN.</param>
        public void LoadBooks(string searchTerm = null)
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT BookID, Title, Author, ISBN, TotalCopies, AvailableCopies FROM Books";
                    string whereClause = "";
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        // Use COLLATE NOCASE for case-insensitive search
                        whereClause = " WHERE Title LIKE @searchTerm OR Author LIKE @searchTerm OR ISBN LIKE @searchTerm COLLATE NOCASE";
                    }
                    query += whereClause + " ORDER BY Title;";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%"); // Add wildcards for LIKE search
                        }

                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            dgvBooks.DataSource = dataTable;

                            // Optional: Adjust column headers for better readability
                            dgvBooks.Columns["BookID"].HeaderText = "ID";
                            dgvBooks.Columns["Title"].HeaderText = "Title";
                            dgvBooks.Columns["Author"].HeaderText = "Author";
                            dgvBooks.Columns["ISBN"].HeaderText = "ISBN";
                            dgvBooks.Columns["TotalCopies"].HeaderText = "Total Copies";
                            dgvBooks.Columns["AvailableCopies"].HeaderText = "Available Copies";

                            // Hide BookID column
                            dgvBooks.Columns["BookID"].Visible = false;
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error loading books: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoadBooks): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading books: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoadBooks): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Add Book button.
        /// Opens the AddEditBookForm in add mode.
        /// </summary>
        private void btnAddBook_Click(object sender, EventArgs e)
        {
            AddEditBookForm addBookForm = new AddEditBookForm();
            if (addBookForm.ShowDialog() == DialogResult.OK)
            {
                LoadBooks(); // Refresh the list after adding
            }
        }

        /// <summary>
        /// Handles the Click event of the Edit Book button.
        /// Opens the AddEditBookForm in edit mode with selected book data.
        /// </summary>
        private void btnEditBook_Click(object sender, EventArgs e)
        {
            EditSelectedBook();
        }

        /// <summary>
        /// Handles the Double Click event of the Books DataGridView cell.
        /// Opens the AddEditBookForm in edit mode.
        /// </summary>
        private void dgvBooks_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Ensure a valid row was double-clicked
            {
                EditSelectedBook();
            }
        }

        /// <summary>
        /// Helper method to encapsulate editing selected book logic.
        /// </summary>
        private void EditSelectedBook()
        {
            if (dgvBooks.SelectedRows.Count > 0)
            {
                // Get the BookID of the selected row
                int bookId = Convert.ToInt32(dgvBooks.SelectedRows[0].Cells["BookID"].Value);

                AddEditBookForm editBookForm = new AddEditBookForm(bookId); // Pass the BookID to the constructor
                if (editBookForm.ShowDialog() == DialogResult.OK)
                {
                    LoadBooks(); // Refresh the list after editing
                }
            }
            else
            {
                MessageBox.Show("Please select a book to edit.", "No Book Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the Click event of the Delete Book button.
        /// Deletes the selected book from the database.
        /// </summary>
        private void btnDeleteBook_Click(object sender, EventArgs e)
        {
            if (dgvBooks.SelectedRows.Count > 0)
            {
                int bookId = Convert.ToInt32(dgvBooks.SelectedRows[0].Cells["BookID"].Value);
                string bookTitle = dgvBooks.SelectedRows[0].Cells["Title"].Value.ToString();

                DialogResult confirmResult = MessageBox.Show($"Are you sure you want to delete the book '{bookTitle}'?",
                                                             "Confirm Delete",
                                                             MessageBoxButtons.YesNo,
                                                             MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    string connectionString = DatabaseHelper.ConnectionString;
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                            // First, check if there are any active borrowings for this book
                            string checkBorrowingsQuery = "SELECT COUNT(*) FROM Borrowings WHERE BookID = @bookId AND ReturnDate IS NULL;";
                            using (SQLiteCommand checkCmd = new SQLiteCommand(checkBorrowingsQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@bookId", bookId);
                                int activeBorrowings = Convert.ToInt32(checkCmd.ExecuteScalar());

                                if (activeBorrowings > 0)
                                {
                                    MessageBox.Show("Cannot delete book: There are active borrowings for this book. Please ensure all copies are returned first.", "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }

                            // If no active borrowings, proceed with deletion
                            string deleteQuery = "DELETE FROM Books WHERE BookID = @bookId;";
                            using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                            {
                                command.Parameters.AddWithValue("@bookId", bookId);
                                int rowsAffected = command.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Book deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    LoadBooks(); // Refresh the list after deleting
                                }
                                else
                                {
                                    MessageBox.Show("Failed to delete book. It might not exist.", "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show($"Database error deleting book: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Console.WriteLine($"SQLite Error (DeleteBook): {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An unexpected error occurred while deleting book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Console.WriteLine($"General Error (DeleteBook): {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a book to delete.", "No Book Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the Books DataGridView.
        /// Enables/disables Edit and Delete buttons based on row selection.
        /// </summary>
        private void dgvBooks_SelectionChanged(object sender, EventArgs e)
        {
            UpdateBookButtonsState();
        }

        /// <summary>
        /// Updates the enabled state of the Edit and Delete buttons for Books.
        /// </summary>
        private void UpdateBookButtonsState()
        {
            bool hasSelection = dgvBooks.SelectedRows.Count > 0;
            btnEditBook.Enabled = hasSelection;
            btnDeleteBook.Enabled = hasSelection;
        }

        /// <summary>
        /// Handles the Click event of the Search Book button.
        /// Filters the book list based on the search term.
        /// </summary>
        private void btnSearchBook_Click(object sender, EventArgs e)
        {
            string searchTerm = txtSearchBook.Text.Trim();
            LoadBooks(searchTerm);
        }

        /// <summary>
        /// Handles the Click event of the Clear Book Search button.
        /// Clears the search box and reloads all books.
        /// </summary>
        private void btnClearBookSearch_Click(object sender, EventArgs e)
        {
            txtSearchBook.Clear();
            LoadBooks(); // Load all books
        }

        /// <summary>
        /// Allows searching by pressing Enter in the search textbox.
        /// </summary>
        private void txtSearchBook_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearchBook.PerformClick(); // Simulate button click
                e.Handled = true; // Prevent the 'ding' sound
                e.SuppressKeyPress = true; // Suppress further key processing
            }
        }

        #endregion

        #region Borrowers Management Methods

        /// <summary>
        /// Loads borrower data from the database into the DataGridView, with optional search filtering.
        /// </summary>
        /// <param name="searchTerm">Optional search term to filter borrowers by name or contact info.</param>
        public void LoadBorrowers(string searchTerm = null)
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT BorrowerID, Name, ContactInfo FROM Borrowers";
                    string whereClause = "";
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        // Use COLLATE NOCASE for case-insensitive search
                        whereClause = " WHERE Name LIKE @searchTerm OR ContactInfo LIKE @searchTerm COLLATE NOCASE";
                    }
                    query += whereClause + " ORDER BY Name;";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%"); // Add wildcards for LIKE search
                        }

                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            dgvBorrowers.DataSource = dataTable;

                            // Optional: Adjust column headers for better readability
                            dgvBorrowers.Columns["BorrowerID"].HeaderText = "ID";
                            dgvBorrowers.Columns["Name"].HeaderText = "Name";
                            dgvBorrowers.Columns["ContactInfo"].HeaderText = "Contact Info";

                            // Hide BorrowerID column
                            dgvBorrowers.Columns["BorrowerID"].Visible = false;
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error loading borrowers: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoadBorrowers): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading borrowers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoadBorrowers): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Add Borrower button.
        /// Opens the AddEditBorrowerForm in add mode.
        /// </summary>
        private void btnAddBorrower_Click(object sender, EventArgs e)
        {
            AddEditBorrowerForm addBorrowerForm = new AddEditBorrowerForm();
            if (addBorrowerForm.ShowDialog() == DialogResult.OK)
            {
                LoadBorrowers(); // Refresh the list after adding
            }
        }

        /// <summary>
        /// Handles the Click event of the Edit Borrower button.
        /// Opens the AddEditBorrowerForm in edit mode with selected borrower data.
        /// </summary>
        private void btnEditBorrower_Click(object sender, EventArgs e)
        {
            EditSelectedBorrower();
        }

        /// <summary>
        /// Handles the Double Click event of the Borrowers DataGridView cell.
        /// Opens the AddEditBorrowerForm in edit mode.
        /// </summary>
        private void dgvBorrowers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Ensure a valid row was double-clicked
            {
                EditSelectedBorrower();
            }
        }

        /// <summary>
        /// Helper method to encapsulate editing selected borrower logic.
        /// </summary>
        private void EditSelectedBorrower()
        {
            if (dgvBorrowers.SelectedRows.Count > 0)
            {
                // Get the BorrowerID of the selected row
                int borrowerId = Convert.ToInt32(dgvBorrowers.SelectedRows[0].Cells["BorrowerID"].Value);

                AddEditBorrowerForm editBorrowerForm = new AddEditBorrowerForm(borrowerId); // Pass the BorrowerID
                if (editBorrowerForm.ShowDialog() == DialogResult.OK)
                {
                    LoadBorrowers(); // Refresh the list after editing
                }
            }
            else
            {
                MessageBox.Show("Please select a borrower to edit.", "No Borrower Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the Click event of the Delete Borrower button.
        /// Deletes the selected borrower from the database.
        /// </summary>
        private void btnDeleteBorrower_Click(object sender, EventArgs e)
        {
            if (dgvBorrowers.SelectedRows.Count > 0)
            {
                int borrowerId = Convert.ToInt32(dgvBorrowers.SelectedRows[0].Cells["BorrowerID"].Value);
                string borrowerName = dgvBorrowers.SelectedRows[0].Cells["Name"].Value.ToString();

                DialogResult confirmResult = MessageBox.Show($"Are you sure you want to delete the borrower '{borrowerName}'?",
                                                             "Confirm Delete",
                                                             MessageBoxButtons.YesNo,
                                                             MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    string connectionString = DatabaseHelper.ConnectionString;
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                            // First, check if there are any active borrowings by this borrower
                            string checkBorrowingsQuery = "SELECT COUNT(*) FROM Borrowings WHERE BorrowerID = @borrowerId AND ReturnDate IS NULL;";
                            using (SQLiteCommand checkCmd = new SQLiteCommand(checkBorrowingsQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@borrowerId", borrowerId);
                                int activeBorrowings = Convert.ToInt32(checkCmd.ExecuteScalar());

                                if (activeBorrowings > 0)
                                {
                                    MessageBox.Show("Cannot delete borrower: There are active borrowings associated with this borrower. Please ensure all books are returned first.", "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }

                            // If no active borrowings, proceed with deletion
                            string deleteQuery = "DELETE FROM Borrowers WHERE BorrowerID = @borrowerId;";
                            using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                            {
                                command.Parameters.AddWithValue("@borrowerId", borrowerId);
                                int rowsAffected = command.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Borrower deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    LoadBorrowers(); // Refresh the list after deleting
                                }
                                else
                                {
                                    MessageBox.Show("Failed to delete borrower. It might not exist.", "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show($"Database error deleting borrower: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Console.WriteLine($"SQLite Error (DeleteBorrower): {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An unexpected error occurred while deleting borrower: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Console.WriteLine($"General Error (DeleteBorrower): {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a borrower to delete.", "No Borrower Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the Borrowers DataGridView.
        /// Enables/disables Edit and Delete buttons based on row selection.
        /// </summary>
        private void dgvBorrowers_SelectionChanged(object sender, EventArgs e)
        {
            UpdateBorrowerButtonsState();
        }

        /// <summary>
        /// Updates the enabled state of the Edit and Delete buttons for Borrowers.
        /// </summary>
        private void UpdateBorrowerButtonsState()
        {
            bool hasSelection = dgvBorrowers.SelectedRows.Count > 0;
            btnEditBorrower.Enabled = hasSelection;
            btnDeleteBorrower.Enabled = hasSelection;
        }

        /// <summary>
        /// Handles the Click event of the Search Borrower button.
        /// Filters the borrower list based on the search term.
        /// </summary>
        private void btnSearchBorrower_Click(object sender, EventArgs e)
        {
            string searchTerm = txtSearchBorrower.Text.Trim();
            LoadBorrowers(searchTerm);
        }

        /// <summary>
        /// Handles the Click event of the Clear Borrower Search button.
        /// Clears the search box and reloads all borrowers.
        /// </summary>
        private void btnClearBorrowerSearch_Click(object sender, EventArgs e)
        {
            txtSearchBorrower.Clear();
            LoadBorrowers(); // Load all borrowers
        }

        /// <summary>
        /// Allows searching by pressing Enter in the search textbox.
        /// </summary>
        private void txtSearchBorrower_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearchBorrower.PerformClick(); // Simulate button click
                e.Handled = true; // Prevent the 'ding' sound
                e.SuppressKeyPress = true; // Suppress further key processing
            }
        }

        #endregion

        #region Borrowings Management Methods

        /// <summary>
        /// Loads borrowing data from the database into the DataGridView, with optional search filtering.
        /// </summary>
        /// <param name="searchTerm">Optional search term to filter borrowings by book title or borrower name.</param>
        public void LoadBorrowings(string searchTerm = null)
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    // Join Books and Borrowers tables to show meaningful information
                    string query = @"
                    SELECT
                        b.BorrowingID,
                        bk.Title AS BookTitle,
                        br.Name AS BorrowerName,
                        b.BorrowDate,
                        b.ReturnDate
                    FROM Borrowings b
                    JOIN Books bk ON b.BookID = bk.BookID
                    JOIN Borrowers br ON b.BorrowerID = br.BorrowerID";

                    string whereClause = "";
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        // Search by Book Title or Borrower Name
                        whereClause = " WHERE bk.Title LIKE @searchTerm OR br.Name LIKE @searchTerm COLLATE NOCASE";
                    }
                    query += whereClause + " ORDER BY b.BorrowDate DESC;"; // Order by most recent borrowings

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%"); // Add wildcards for LIKE search
                        }

                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            dgvBorrowings.DataSource = dataTable;

                            // Optional: Adjust column headers for better readability
                            dgvBorrowings.Columns["BorrowingID"].HeaderText = "ID";
                            dgvBorrowings.Columns["BookTitle"].HeaderText = "Book Title";
                            dgvBorrowings.Columns["BorrowerName"].HeaderText = "Borrower";
                            dgvBorrowings.Columns["BorrowDate"].HeaderText = "Borrow Date";
                            dgvBorrowings.Columns["ReturnDate"].HeaderText = "Return Date";

                            // Hide BorrowingID column
                            dgvBorrowings.Columns["BorrowingID"].Visible = false;
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error loading borrowings: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoadBorrowings): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading borrowings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoadBorrowings): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Borrow Book button.
        /// Opens a form to record a new borrowing.
        /// </summary>
        private void btnBorrowBook_Click(object sender, EventArgs e)
        {
            BorrowBookForm borrowForm = new BorrowBookForm();
            if (borrowForm.ShowDialog() == DialogResult.OK)
            {
                LoadBorrowings(); // Refresh the borrowings list
                LoadBooks(); // Refresh the books list as AvailableCopies might have changed
            }
        }

        /// <summary>
        /// Handles the Click event of the Return Book button.
        /// Marks the selected borrowing as returned.
        /// </summary>
        private void btnReturnBook_Click(object sender, EventArgs e)
        {
            if (dgvBorrowings.SelectedRows.Count > 0)
            {
                // Accessing cells by their column name for robustness
                int borrowingId = Convert.ToInt32(dgvBorrowings.SelectedRows[0].Cells["BorrowingID"].Value);
                string bookTitle = dgvBorrowings.SelectedRows[0].Cells["BookTitle"].Value.ToString();
                string borrowerName = dgvBorrowings.SelectedRows[0].Cells["BorrowerName"].Value.ToString();
                // Check if ReturnDate is DBNull or an empty string, indicating it's not yet returned
                object returnDateObj = dgvBorrowings.SelectedRows[0].Cells["ReturnDate"].Value;
                bool isReturned = (returnDateObj != DBNull.Value && !string.IsNullOrEmpty(returnDateObj.ToString()));

                if (isReturned)
                {
                    MessageBox.Show("This book has already been returned.", "Already Returned", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DialogResult confirmResult = MessageBox.Show($"Are you sure you want to mark '{bookTitle}' borrowed by '{borrowerName}' as returned?",
                                                             "Confirm Return",
                                                             MessageBoxButtons.YesNo,
                                                             MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    string connectionString = DatabaseHelper.ConnectionString;
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                            using (SQLiteTransaction transaction = connection.BeginTransaction()) // Use transaction
                            {
                                // Get BookID from Borrowing to increment AvailableCopies
                                string getBookIdQuery = "SELECT BookID FROM Borrowings WHERE BorrowingID = @borrowingId;";

                                int bookId = 0;
                                using (SQLiteCommand cmd = new SQLiteCommand(getBookIdQuery, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@borrowingId", borrowingId);
                                    object result = cmd.ExecuteScalar();
                                    if (result != null)
                                    {
                                        bookId = Convert.ToInt32(result);
                                    }
                                }

                                // Update Borrowing record
                                string updateBorrowingQuery = "UPDATE Borrowings SET ReturnDate = @returnDate WHERE BorrowingID = @borrowingId;";
                                using (SQLiteCommand command = new SQLiteCommand(updateBorrowingQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@returnDate", DateTime.Now.ToString("yyyy-MM-dd")); // Current date
                                    command.Parameters.AddWithValue("@borrowingId", borrowingId);
                                    command.ExecuteNonQuery();
                                }

                                // Increment AvailableCopies for the book
                                if (bookId > 0)
                                {
                                    string updateBookCopiesQuery = "UPDATE Books SET AvailableCopies = AvailableCopies + 1 WHERE BookID = @bookId;";
                                    using (SQLiteCommand command = new SQLiteCommand(updateBookCopiesQuery, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@bookId", bookId);
                                        command.ExecuteNonQuery();
                                    }
                                }
                                transaction.Commit(); // Commit changes

                                MessageBox.Show("Book marked as returned successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadBorrowings(); // Refresh borrowings list
                                LoadBooks(); // Refresh books list as AvailableCopies changed
                            } // End using transaction
                        }
                        catch (SQLiteException ex)
                        {
                            // Rollback transaction in case of error
                            // transaction.Rollback(); // This implicitly happens when using (SQLiteTransaction) if commit is not reached
                            MessageBox.Show($"Database error returning book: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Console.WriteLine($"SQLite Error (ReturnBook): {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An unexpected error occurred while returning book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Console.WriteLine($"General Error (ReturnBook): {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a borrowing record to mark as returned.", "No Borrowing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the Borrowings DataGridView.
        /// Enables/disables Return button based on row selection and return status.
        /// </summary>
        private void dgvBorrowings_SelectionChanged(object sender, EventArgs e)
        {
            UpdateBorrowingButtonsState();
        }

        /// <summary>
        /// Updates the enabled state of the Return button for Borrowings.
        /// </summary>
        private void UpdateBorrowingButtonsState()
        {
            bool hasSelection = dgvBorrowings.SelectedRows.Count > 0;
            btnReturnBook.Enabled = hasSelection; // Initially enable if row is selected

            if (hasSelection)
            {
                // Check if the selected borrowing record already has a return date
                object returnDateObj = dgvBorrowings.SelectedRows[0].Cells["ReturnDate"].Value;
                if (returnDateObj != DBNull.Value && !string.IsNullOrEmpty(returnDateObj.ToString()))
                {
                    btnReturnBook.Enabled = false; // Disable if already returned
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Search Borrowing button.
        /// Filters the borrowing list based on the search term.
        /// </summary>
        private void btnSearchBorrowing_Click(object sender, EventArgs e)
        {
            string searchTerm = txtSearchBorrowing.Text.Trim();
            LoadBorrowings(searchTerm);
        }

        /// <summary>
        /// Handles the Click event of the Clear Borrowing Search button.
        /// Clears the search box and reloads all borrowings.
        /// </summary>
        private void btnClearBorrowingSearch_Click(object sender, EventArgs e)
        {
            txtSearchBorrowing.Clear();
            LoadBorrowings(); // Load all borrowings
        }

        /// <summary>
        /// Allows searching by pressing Enter in the search textbox.
        /// </summary>
        private void txtSearchBorrowing_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearchBorrowing.PerformClick(); // Simulate button click
                e.Handled = true; // Prevent the 'ding' sound
                e.SuppressKeyPress = true; // Suppress further key processing
            }
        }

        #endregion

        private void btnAddBook_Click_1(object sender, EventArgs e)
        {

        }
    }
}