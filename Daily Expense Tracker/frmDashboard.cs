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
    public partial class frmDashboard : Form
    {
        private SQLiteDataAdapter _expAdapter;
        private DataTable _expTable;

        private SQLiteDataAdapter _incAdapter;
        private DataTable _incTable;

        private DataTable _categories; // id, name
        public frmDashboard()
        {
            InitializeComponent();
            dtFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dtTo.Value = DateTime.Today;
            rbBoth.Checked = true;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void frmDashboard_Load(object sender, EventArgs e)
        {
            LoadCategories();
            SetupGridForExpenses(); // prepares columns including category Combo column
            LoadData();
            UpdateTotals();
        }
        

        private void LoadCategories()
        {
            _categories = new DataTable();
            using (var conn = Db.GetConn())
            using (var cmd = new SQLiteCommand(
                "SELECT id, name FROM categories WHERE user_id IS NULL OR user_id=@uid ORDER BY name", conn))
            {
                cmd.Parameters.AddWithValue("@uid", Session.CurrentUserId);
                using (var da = new SQLiteDataAdapter(cmd))
                {
                    da.Fill(_categories);
                }
            }

            // Fill category filter
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("All");
            foreach (DataRow r in _categories.Rows)
                cmbCategory.Items.Add(r["name"].ToString());
            cmbCategory.SelectedIndex = 0;
        }

        private string ExpenseFilterSql()
        {
            // Note: we filter by date and category (optional) and search text (optional)
            string sql = @"SELECT id, user_id, category_id, amount, note, spent_on
                       FROM expenses
                       WHERE user_id = @uid
                         AND spent_on >= @from AND spent_on <= @to";
            if (cmbCategory.SelectedIndex > 0) sql += " AND category_id = @catId";
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                sql += " AND (note LIKE @q)";
            sql += " ORDER BY spent_on DESC, id DESC";
            return sql;
        }

        private string IncomeFilterSql()
        {
            string sql = @"SELECT id, user_id, amount, note, received_on
                       FROM income
                       WHERE user_id = @uid
                         AND received_on >= @from AND received_on <= @to";
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                sql += " AND (note LIKE @q)";
            sql += " ORDER BY received_on DESC, id DESC";
            return sql;
        }

        private void LoadExpenses()
        {
            _expTable = new DataTable();
            var sql = ExpenseFilterSql();

            using (var conn = Db.GetConn())
            {
                _expAdapter = new SQLiteDataAdapter(sql, conn);
                _expAdapter.SelectCommand.Parameters.AddWithValue("@uid", Session.CurrentUserId);
                _expAdapter.SelectCommand.Parameters.AddWithValue("@from", dtFrom.Value.Date);
                _expAdapter.SelectCommand.Parameters.AddWithValue("@to", dtTo.Value.Date);
                if (cmbCategory.SelectedIndex > 0)
                {
                    int catId = FindCategoryIdByName(cmbCategory.SelectedItem.ToString());
                    _expAdapter.SelectCommand.Parameters.AddWithValue("@catId", catId);
                }
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                    _expAdapter.SelectCommand.Parameters.AddWithValue("@q", "%" + txtSearch.Text.Trim() + "%");

                var builder = new SQLiteCommandBuilder(_expAdapter); // auto CRUD
                _expAdapter.Fill(_expTable);
            }

            // Bind to grid (for Both/Expenses mode)
            if (rbBoth.Checked || rbExpenses.Checked)
            {
                gridRecords.DataSource = _expTable;
                SetupGridForExpenses(); // ensure columns (including category combo) are present
            }
        }

        private void LoadIncome()
        {
            _incTable = new DataTable();
            var sql = IncomeFilterSql();

            using (var conn = Db.GetConn())
            {
                _incAdapter = new SQLiteDataAdapter(sql, conn);
                _incAdapter.SelectCommand.Parameters.AddWithValue("@uid", Session.CurrentUserId);
                _incAdapter.SelectCommand.Parameters.AddWithValue("@from", dtFrom.Value.Date);
                _incAdapter.SelectCommand.Parameters.AddWithValue("@to", dtTo.Value.Date);
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                    _incAdapter.SelectCommand.Parameters.AddWithValue("@q", "%" + txtSearch.Text.Trim() + "%");

                var builder = new SQLiteCommandBuilder(_incAdapter);
                _incAdapter.Fill(_incTable);
            }

            if (rbIncome.Checked)
            {
                gridRecords.DataSource = _incTable;
                SetupGridForIncome();
            }
        }

        private void LoadData()
        {
            if (rbIncome.Checked)
            {
                LoadIncome();
            }
            else if (rbExpenses.Checked)
            {
                LoadExpenses();
            }
            else // Both
            {
                // Show expenses by default in grid; totals will include both.
                LoadExpenses();
                LoadIncome();
            }
        }

        private void UpdateTotals()
        {
            decimal totalExp = 0, totalInc = 0;

            using (var conn = Db.GetConn())
            {
                // Expenses total
                using (var cmd = new SQLiteCommand(
                    @"SELECT IFNULL(SUM(amount),0) FROM expenses
                  WHERE user_id=@uid AND spent_on>=@from AND spent_on<=@to", conn))
                {
                    cmd.Parameters.AddWithValue("@uid", Session.CurrentUserId);
                    cmd.Parameters.AddWithValue("@from", dtFrom.Value.Date);
                    cmd.Parameters.AddWithValue("@to", dtTo.Value.Date);
                    totalExp = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // Income total
                using (var cmd = new SQLiteCommand(
                    @"SELECT IFNULL(SUM(amount),0) FROM income
                  WHERE user_id=@uid AND received_on>=@from AND received_on<=@to", conn))
                {
                    cmd.Parameters.AddWithValue("@uid", Session.CurrentUserId);
                    cmd.Parameters.AddWithValue("@from", dtFrom.Value.Date);
                    cmd.Parameters.AddWithValue("@to", dtTo.Value.Date);
                    totalInc = Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }

            lblTotalExpenses.Text = $"Rs. {totalExp:0.00}";
            lblTotalIncome.Text = $"Rs. {totalInc:0.00}";
            lblBalance.Text = $"Rs. {(totalInc - totalExp):0.00}";
        }

        private int FindCategoryIdByName(string name)
        {
            foreach (DataRow r in _categories.Rows)
                if (string.Equals(r["name"].ToString(), name, StringComparison.OrdinalIgnoreCase))
                    return Convert.ToInt32(r["id"]);
            return -1;
        }

        // === UI wiring ===
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadData();
            UpdateTotals();
        }

        private void filterChanged(object sender, EventArgs e)
        {
            LoadData();
            UpdateTotals();
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            try
            {
                if (gridRecords.DataSource == _expTable)
                {
                    _expAdapter?.Update(_expTable);
                }
                else if (gridRecords.DataSource == _incTable)
                {
                    _incAdapter?.Update(_incTable);
                }
                MessageBox.Show("Changes saved.");
                LoadData();
                UpdateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }
        private void SetupGridForExpenses()
        {
            gridRecords.AutoGenerateColumns = false;
            gridRecords.Columns.Clear();

            // ID (read-only)
            gridRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "id",
                HeaderText = "ID",
                ReadOnly = true,
                Width = 60
            });

            // Category (Combo bound to categories)
            var catCol = new DataGridViewComboBoxColumn
            {
                DataPropertyName = "category_id", // value in the expenses table
                HeaderText = "Category",
                DataSource = _categories,
                DisplayMember = "name",
                ValueMember = "id",
                Width = 160
            };
            gridRecords.Columns.Add(catCol);

            // Amount
            gridRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "amount",
                HeaderText = "Amount",
                Width = 100
            });

            // Note
            gridRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "note",
                HeaderText = "Note",
                Width = 250
            });

            // Date
            gridRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "spent_on",
                HeaderText = "Date",
                Width = 110
            });

            // Hide user_id in the grid but keep it in the table (enforce it on new rows)
            var uidCol = new DataGridViewTextBoxColumn { DataPropertyName = "user_id", Visible = false };
            gridRecords.Columns.Add(uidCol);

            // Ensure new rows get current user
            gridRecords.DefaultValuesNeeded -= grid_DefaultValuesNeeded;
            gridRecords.DefaultValuesNeeded += grid_DefaultValuesNeeded;
        }

        private void grid_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            if (gridRecords.DataSource == _expTable)
            {
                e.Row.Cells["user_id"].Value = Session.CurrentUserId;
                e.Row.Cells["spent_on"].Value = DateTime.Today;
            }
            else if (gridRecords.DataSource == _incTable)
            {
                e.Row.Cells["user_id"].Value = Session.CurrentUserId;
                e.Row.Cells["received_on"].Value = DateTime.Today;
            }
        }

        private void SetupGridForIncome()
        {
            gridRecords.AutoGenerateColumns = false;
            gridRecords.Columns.Clear();

            gridRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "id",
                HeaderText = "ID",
                ReadOnly = true,
                Width = 60
            });
            gridRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "amount",
                HeaderText = "Amount",
                Width = 100
            });
            gridRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "note",
                HeaderText = "Note",
                Width = 250
            });
            gridRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "received_on",
                HeaderText = "Date",
                Width = 110
            });
            var uidCol = new DataGridViewTextBoxColumn { DataPropertyName = "user_id", Visible = false };
            gridRecords.Columns.Add(uidCol);
        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                LoadData();       // Re-run query with filters (category/date/radio/search text)
                UpdateTotals();   // Update total income/expense labels

                MessageBox.Show("Search completed!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while searching: " + ex.Message);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click_1(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                // Check which dataset is currently displayed in the grid
                if (gridRecords.DataSource == _expTable && _expAdapter != null)
                {
                    _expAdapter.Update(_expTable);   // Save expense edits
                }
                else if (gridRecords.DataSource == _incTable && _incAdapter != null)
                {
                    _incAdapter.Update(_incTable);   // Save income edits
                }

                MessageBox.Show("Changes saved successfully!");
                LoadData();       // Refresh the latest data
                UpdateTotals();   // Recalculate totals
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while saving changes: " + ex.Message);
            }
        }

        private void btnAddExpense_Click(object sender, EventArgs e)
        {
            using (var dlg = new frmAddExpense())
            if (dlg.ShowDialog() == DialogResult.OK) { LoadData(); UpdateTotals(); }
        }

        private void btnAddIncome_Click(object sender, EventArgs e)
        {
            using (var dlg = new frmAddIncome())
            if (dlg.ShowDialog() == DialogResult.OK) { LoadData(); UpdateTotals(); }
        }

        private void btnCategories_Click(object sender, EventArgs e)
        {
            using (var dlg = new frmManageCategories())
            if (dlg.ShowDialog() == DialogResult.OK) { LoadCategories(); LoadData(); }
        }

        private void btnRefresh_Click_1(object sender, EventArgs e)
        {
            try
            {
                dtFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                dtTo.Value = DateTime.Today;
                rbBoth.Checked = true;
                if (cmbCategory.Items.Count > 0)
                    cmbCategory.SelectedIndex = 0;
                txtSearch.Clear();
                LoadCategories();
                LoadData();
                UpdateTotals();
                MessageBox.Show("Filters have been reset to default view!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while resetting dashboard: " + ex.Message);
            }

        }
    }
}
