using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace INFASS_SETENTA
{
    public partial class Rainbow : Form
    {
        string connectionString = @"Data Source=LAB3-PC14\LAB2PC45;Initial Catalog=test_hashing;Integrated Security=True;";
        string storedHash;
        string username;

        char[] charset;
        int maxLength = 3;
        int maxAttempts = 500;
        int attemptCounter = 0;
        Random rnd = new Random();

        private CancellationTokenSource cts;  

        public Rainbow()
        {
            InitializeComponent();
        }

        private void FetchFirstUser()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT TOP 1 Username, Password FROM [user] ORDER BY Id DESC";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        username = reader["Username"].ToString();
                        storedHash = reader["Password"].ToString();
                        AppendText($"Target user: {username}, hash: {storedHash}");
                    }
                    else
                    {
                        AppendText("No users found in database!");
                        username = null;
                        storedHash = null;
                    }
                }
            }
        }

        private char[] GenerateRandomCharset(int length)
        {
            string possible = "0123456789";
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = possible[rnd.Next(possible.Length)];
            }
            return result;
        }

        private async Task<bool> TryPasswordAsync(string prefix, int depth, CancellationToken token)
        {
            if (depth > maxLength || attemptCounter >= maxAttempts || string.IsNullOrEmpty(storedHash))
                return false;

            if (token.IsCancellationRequested)
            {
                AppendText("Attack stopped by user.");
                return false;
            }

            if (prefix.Length > 0)
            {
                attemptCounter++;
                string hash = ComputeSha256(prefix);
                AppendText($"Trying ({attemptCounter}): {prefix} -> {hash}");
                await Task.Delay(20);

                if (hash.Equals(storedHash, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"\nMATCH FOUND!\nUsername: {username}\nPassword: {prefix}");
                    return true;
                }

                if (attemptCounter >= maxAttempts)
                {
                    MessageBox.Show($"\nMaximum attempts reached ({maxAttempts})");
                    return false;
                }
            }

            foreach (char c in charset)
            {
                bool found = await TryPasswordAsync(prefix + c, depth + 1, token);
                if (found) return true;
                if (token.IsCancellationRequested) return false;
            }

            return false;
        }

        private void AppendText(string text)
        {
            if (textBox1.InvokeRequired)
            {
                textBox1.Invoke(new Action(() =>
                {
                    textBox1.AppendText(text + Environment.NewLine);
                    textBox1.SelectionStart = textBox1.Text.Length;
                    textBox1.ScrollToCaret();
                }));
            }
            else
            {
                textBox1.AppendText(text + Environment.NewLine);
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
            }
        }

        private string ComputeSha256(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            FetchFirstUser();

            if (string.IsNullOrEmpty(storedHash))
                return;

            charset = GenerateRandomCharset(36);
            AppendText($"Random charset: {new string(charset)}");

            AppendText("Starting dynamic rainbow table attack...\n");
            attemptCounter = 0;

            cts = new CancellationTokenSource();
            bool found = await TryPasswordAsync("", 0, cts.Token);

            if (!found && !cts.Token.IsCancellationRequested)
                MessageBox.Show($"\nNo match found within {maxAttempts} attempts.");
        }



        private void btnProceedToLogin_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 loginForm = new Form1();
            loginForm.Show();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                AppendText("Stop requested. Please wait for current task to finish...");
            }
        }
    }
}
