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

namespace Daily_Expense_Tracker
{
    public partial class frmAddExpense : Form
    {
        private DataTable _categories; // id, name
        public frmAddExpense()
        {
            InitializeComponent();
        }

        private void frmAddExpense_Load(object sender, EventArgs e)
        {
            LoadCategories();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (var conn = Db.GetConn())
            using (var cmd = new SQLiteCommand(
                "INSERT INTO expenses (user_id, category_id, amount, note, spent_on) VALUES (@u,@c,@a,@n,@d)", conn))
            {
                cmd.Parameters.AddWithValue("@u", Session.CurrentUserId);
                cmd.Parameters.AddWithValue("@c", ((ComboItem)cmbCategory.SelectedItem).Id);
                cmd.Parameters.AddWithValue("@a", decimal.Parse(txtAmount.Text));
                cmd.Parameters.AddWithValue("@n", txtNote.Text.Trim());
                cmd.Parameters.AddWithValue("@d", dtSpentOn.Value.Date);
                cmd.ExecuteNonQuery();
            }
            DialogResult = DialogResult.OK;
        }
        private void LoadCategories()
        {
            cmbCategory.Items.Clear();

            using (var conn = Db.GetConn())
            using (var cmd = new SQLiteCommand(
                "SELECT id, name FROM categories WHERE user_id IS NULL OR user_id=@uid ORDER BY name", conn))
            {
                cmd.Parameters.AddWithValue("@uid", Session.CurrentUserId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        cmbCategory.Items.Add(new ComboItem(r.GetInt32(0), r.GetString(1)));
                    }
                }
            }

            if (cmbCategory.Items.Count > 0)
                cmbCategory.SelectedIndex = 0;
        }
        
    }
}
