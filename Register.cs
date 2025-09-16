using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace INFASS_SETENTA
{
    public partial class Register : Form
    {
        string connectionString = @"Data Source=LAB3-PC14\LAB2PC45;Initial Catalog=test_hashing;Integrated Security=True;";
        Random rnd = new Random();

        public Register()
        {
            InitializeComponent();

            lblLogin.Click += (s, e) =>
            {
                var loginForm = new Form1();
                loginForm.Show();
                this.Hide();
            };
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please fill in all fields.");
                txtName.Clear();
                txtUsername.Clear();
                txtPassword.Clear();
                txtUsername.Focus();
                return;
            }


            string salt = GenerateSalt(10);
            string passwordWithSalt = password + salt;
            string hashedPassword = ComputeSha256Hash(passwordWithSalt);

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                 
                    string checkQuery = "SELECT COUNT(*) FROM [user] WHERE Username = @username";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@username", username);
                        int existing = (int)checkCmd.ExecuteScalar();
                        if (existing > 0)
                        {
                            MessageBox.Show("Username already exists. Choose another.");
                            txtName.Clear();
                            txtUsername.Clear();
                            txtPassword.Clear();
                            txtUsername.Focus();
                            return;
                        }
                    }

            
                    string insertQuery = "INSERT INTO [user] (Name, Username, Password, Salt) VALUES (@name, @username, @password, @salt)";
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@name", name);
                        insertCmd.Parameters.AddWithValue("@username", username);
                        insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                        insertCmd.Parameters.AddWithValue("@salt", salt);

                        int rows = insertCmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            MessageBox.Show("Registration successful! You can now log in.");
                            txtName.Clear();
                            txtUsername.Clear();
                            txtPassword.Clear();
                            txtUsername.Focus();

                            var loginForm = new Form1();
                            loginForm.Show();
                            this.Hide();
                        }
                        else
                        {
                            MessageBox.Show("Registration failed. Try again.");
                            txtName.Clear();
                            txtUsername.Clear();
                            txtPassword.Clear();
                            txtUsername.Focus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }


        private string GenerateSalt(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] saltChars = new char[length];
            for (int i = 0; i < length; i++)
            {
                saltChars[i] = chars[rnd.Next(chars.Length)];
            }
            return new string(saltChars);
        }


        private string ComputeSha256Hash(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }
}
