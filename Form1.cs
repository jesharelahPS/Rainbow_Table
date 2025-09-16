using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Xml.Linq;

namespace INFASS_SETENTA
{
    public partial class Form1 : Form
    {
        string connectionString = @"Data Source=LAB3-PC14\LAB2PC45;Initial Catalog=test_hashing;Integrated Security=True;";

        public Form1()
        {
            InitializeComponent();

            lblSignup.Click += (s, e) =>
            {
                var regForm = new Register();
                regForm.Show();
                this.Hide();
            };

      
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter username and password.");
                txtUsername.Clear();
                txtPassword.Clear();
                txtUsername.Focus();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get stored hash and salt for the username
                    string query = "SELECT Password, Salt FROM [user] WHERE Username = @username";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHash = reader["Password"].ToString();
                                string storedSalt = reader["Salt"].ToString();
                           
                                string passwordWithSalt = password + storedSalt;
                                string hashedInput = HashHelper.ComputeSha256Hash(passwordWithSalt);

                                if (hashedInput.Equals(storedHash, StringComparison.OrdinalIgnoreCase))
                                {
                                    MessageBox.Show("Login successful!");
                                    MainForm mainForm = new MainForm();
                                    mainForm.Show();

                                    txtUsername.Clear();
                                    txtPassword.Clear();
                                    this.Hide();
                                }
                                else
                                {
                                    MessageBox.Show("Invalid username or password.");
                                }
                            }
                            else
                            {
                                MessageBox.Show("Invalid username or password.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

    }
}
