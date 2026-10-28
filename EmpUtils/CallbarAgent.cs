using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmpUtils
{
    public partial class CallbarAgent : Form
    {
        public CallbarAgent()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        string LoadCalls(string query, string strFilterCondition = "")
        {
            string connectionString = Utilities.Decode(ConfigurationManager.ConnectionStrings["Logging"].ConnectionString);

            query += $"{strFilterCondition}";

            string errMessage = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    DataTable dtResult = new DataTable();
                    using (var command = new SqlCommand())
                    {
                        command.Connection = conn;
                        command.CommandText = query;

                        using (var reader = command.ExecuteReader())
                        {
                            dtResult.Load(reader);
                        }
                        dataGridView1.DataSource = dtResult;
                        dataGridView1.AutoSize = true;
                        dataGridView1.AutoResizeColumns();
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                errMessage = ex.Message;
                MessageBox.Show($"Error Loading details, Error: + {ex.Message + ex.StackTrace}");
            }
            return errMessage;
        }

        private void CallbarAgent_Load(object sender, EventArgs e)
        {
            startDate.Value = DateTime.Now.AddDays(-1);
            endDate.Value = DateTime.Now;
        }

        private void dataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            // Get the current row
            var row = dataGridView1.Rows[e.RowIndex];

            if (!dataGridView1.Columns.Contains("CallDuration"))
                return;
            // Assuming the column you're checking is called "Status"
            string strCallDuration = row.Cells["CallDuration"].Value?.ToString();
            strCallDuration = string.IsNullOrWhiteSpace(strCallDuration) ? "0" : strCallDuration;
            int callDuration = int.Parse(strCallDuration);

            // Change color based on the value
            if (callDuration < 10)
            {
                row.DefaultCellStyle.BackColor = Color.LightGray;
            }
        }

        private void InvokeIfRequired(Action action)
        {
            if (InvokeRequired)
                Invoke(action);
            else
                action();
        }

        private void ShowLoader(bool show)
        {
            pictureBoxLoader.Visible = show;
            pictureBoxLoader.BringToFront();
        }

    }
}
