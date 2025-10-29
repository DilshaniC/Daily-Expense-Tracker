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
    public partial class frmManageCategories : Form
    {
        private DataTable _categories;
        public frmManageCategories()
        {
            InitializeComponent();
        }

        private void frmManageCategories_Load(object sender, EventArgs e)
        {
            LoadCategories();
        }
        private void LoadCategories()
        {
            lstCats.Items.Clear();
            _categories = new DataTable();

            using (var conn = Db.GetConn())
            using (var cmd = new SQLiteCommand(
                "SELECT id, name, user_id FROM categories WHERE user_id IS NULL OR user_id=@uid ORDER BY name", conn))
            {
                cmd.Parameters.AddWithValue("@uid", Session.CurrentUserId);
                using (var da = new SQLiteDataAdapter(cmd))
                {
                    da.Fill(_categories);
                }
            }

            foreach (DataRow row in _categories.Rows)
            {
                string name = row["name"].ToString();
                bool isUserCat = !Convert.IsDBNull(row["user_id"]);
                lstCats.Items.Add($"{name}" + (isUserCat ? "" : " (default)"));
            }

            lstCats.SelectedIndex = -1;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lstCats.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a category to delete.");
                return;
            }

            DataRow selectedRow = _categories.Rows[lstCats.SelectedIndex];
            bool isDefault = Convert.IsDBNull(selectedRow["user_id"]);

            if (isDefault)
            {
                MessageBox.Show("Default categories cannot be deleted.");
                return;
            }

            int catId = Convert.ToInt32(selectedRow["id"]);

            var confirm = MessageBox.Show("Are you sure you want to delete this category?",
                                          "Confirm Delete", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.No) return;

            using (var conn = Db.GetConn())
            using (var cmd = new SQLiteCommand(
                "DELETE FROM categories WHERE id=@id AND user_id=@uid", conn))
            {
                cmd.Parameters.AddWithValue("@id", catId);
                cmd.Parameters.AddWithValue("@uid", Session.CurrentUserId);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Category deleted successfully!");
            LoadCategories();

        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string name = txtNewCat.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a category name.", "Missing name");
                return;
            }

            using (var conn = Db.GetConn())
            using (var cmd = new SQLiteCommand(
                "INSERT OR IGNORE INTO categories (name, user_id) VALUES (@n, @uid)", conn))
            {
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@uid", Session.CurrentUserId);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Category added successfully!");
            txtNewCat.Clear();
            LoadCategories();

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
