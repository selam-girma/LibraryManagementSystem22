// BookDetailsForm.cs
// This file contains the C# code for the Book Details Form (Add/Edit).

using System;
using System.Windows.Forms;
using System.Data.SQLite; // Required for SQLite database interaction
using System.Data;       // Required for DataTable, DataRow

namespace MyLibraryApp
{
    public partial class BookDetailsForm : Form
    {
        private int _bookId = 0; // 0 for Add mode, actual BookID for Edit mode

        /// <summary>
        /// Constructor for adding a new book.
        /// </summary>
        public BookDetailsForm()
        {
            InitializeComponent();
            this.Text = "Add New Book";
            // Hook up event handlers defined in Designer.cs
            this.btnSave.Click += new EventHandler(btnSave_Click);
            this.btnCancel.Click += new EventHandler(btnCancel_Click);
        }

        /// <summary>
        /// Constructor for editing an existing book.
        /// </summary>
        /// <param name="bookId">The ID of the book to edit.</param>
        public BookDetailsForm(int bookId) : this() // Call the default constructor first
        {
            _bookId = bookId;
            this.Text = "Edit Book";
            LoadBookDetails(); // Load existing book details if in edit mode
        }

        /// <summary>
        /// Loads existing book details into the form fields when in edit mode.
        /// </summary>
        private void LoadBookDetails()
        {
            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Title, Author, Year, AvailableCopies FROM Books WHERE BookID = @bookId;";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@bookId", _bookId);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtTitle.Text = reader["Title"].ToString();
                                txtAuthor.Text = reader["Author"].ToString();
                                txtYear.Text = reader["Year"].ToString();
                                txtAvailableCopies.Text = reader["AvailableCopies"].ToString();
                            }
                            else
                            {
                                MessageBox.Show("Book not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.Close(); // Close the form if book not found
                            }
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error loading book details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (LoadBookDetails): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading book details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (LoadBookDetails): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Save button.
        /// Validates input and saves/updates the book in the database.
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            // 1. Input Validation
            if (!ValidateInput())
            {
                return; // Stop if validation fails
            }

            string title = txtTitle.Text.Trim();
            string author = txtAuthor.Text.Trim();
            int year = int.Parse(txtYear.Text); // Already validated to be an int
            int availableCopies = int.Parse(txtAvailableCopies.Text); // Already validated

            string connectionString = DatabaseHelper.ConnectionString;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SQLiteCommand command = connection.CreateCommand();

                    if (_bookId == 0) // Add New Book
                    {
                        command.CommandText = "INSERT INTO Books (Title, Author, Year, AvailableCopies) VALUES (@title, @author, @year, @availableCopies);";
                        command.Parameters.AddWithValue("@title", title);
                        command.Parameters.AddWithValue("@author", author);
                        command.Parameters.AddWithValue("@year", year);
                        command.Parameters.AddWithValue("@availableCopies", availableCopies);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Book added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else // Edit Existing Book
                    {
                        command.CommandText = "UPDATE Books SET Title = @title, Author = @author, Year = @year, AvailableCopies = @availableCopies WHERE BookID = @bookId;";
                        command.Parameters.AddWithValue("@title", title);
                        command.Parameters.AddWithValue("@author", author);
                        command.Parameters.AddWithValue("@year", year);
                        command.Parameters.AddWithValue("@availableCopies", availableCopies);
                        command.Parameters.AddWithValue("@bookId", _bookId);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Book updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    this.DialogResult = DialogResult.OK; // Indicate success to the calling form
                    this.Close(); // Close the form
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"Database error saving book: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"SQLite Error (SaveBook): {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while saving book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"General Error (SaveBook): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Validates the user input in the form fields.
        /// </summary>
        /// <returns>True if all input is valid, false otherwise.</returns>
        private bool ValidateInput()
        {
            // Validate Title
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Book Title cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return false;
            }

            // Validate Author
            if (string.IsNullOrWhiteSpace(txtAuthor.Text))
            {
                MessageBox.Show("Author cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAuthor.Focus();
                return false;
            }

            // Validate Year
            int year;
            if (!int.TryParse(txtYear.Text, out year) || year < 1000 || year > DateTime.Now.Year + 1) // Year must be a number and reasonable range
            {
                MessageBox.Show($"Please enter a valid year (e.g., between 1000 and {DateTime.Now.Year + 1}).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtYear.Focus();
                return false;
            }

            // Validate Available Copies
            int availableCopies;
            if (!int.TryParse(txtAvailableCopies.Text, out availableCopies) || availableCopies < 0) // Copies must be a non-negative number
            {
                MessageBox.Show("Please enter a valid number for available copies (non-negative).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAvailableCopies.Focus();
                return false;
            }

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