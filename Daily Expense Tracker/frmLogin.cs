using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;

namespace Daily_Expense_Tracker
{
    public partial class frmLogin : Form
    {
        public frmLogin()
        {
            InitializeComponent();
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text; // later: hash & compare

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter username and password.");
                return;
            }

            using (var conn = Db.GetConn())
            using (var cmd = new SQLiteCommand("SELECT id, username FROM users WHERE username=@u AND password_hash=@p", conn))
            {
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", password);

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        int uid = r.GetInt32(0);
                        string uname = r.GetString(1);
                        Session.SignIn(uid, uname);

                        // Go to dashboard
                        var dash = new frmDashboard();
                        dash.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Invalid credentials.");
                    }
                }
            }
        }
    }
}
