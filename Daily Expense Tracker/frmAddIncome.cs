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
    public partial class frmAddIncome : Form
    {
        public frmAddIncome()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {


        }

        private void txtNote_Load(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (var conn = Db.GetConn())
            using (var cmd = new SQLiteCommand(
                "INSERT INTO income (user_id, amount, note, received_on) VALUES (@u,@a,@n,@d)", conn))
            {
                cmd.Parameters.AddWithValue("@u", Session.CurrentUserId);
                cmd.Parameters.AddWithValue("@a", decimal.Parse(txtAmount.Text));
                cmd.Parameters.AddWithValue("@n", txtIncomeNote.Text.Trim());
                cmd.Parameters.AddWithValue("@d", dtReceivedOn.Value.Date);
                cmd.ExecuteNonQuery();
            }
            DialogResult = DialogResult.OK;
        }
    }
}
