using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmpUtils
{
    public partial class Logging : Form
    {
        Stopwatch sw;
        public Logging()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            this.Cursor = Cursors.WaitCursor;
            int intRows = string.IsNullOrEmpty(txtRows.Text) ? 200 : int.Parse(txtRows.Text);

            string query = $"Select top {intRows} LogDateTime, [Message], loglevel, Hostname, tl.domainid,tasc.AppSubComponentID, [Counter], tasc.applicationid " +
                "From tbllogging tl(nolock) inner join Logging..tblAppSubComponents tasc(nolock) on tl.AppSubComponentId = tasc.AppSubComponentID  ";

            string strFilterCondition = $" Where LogDateTime between '{startDate.Value.Date}' and '{endDate.Value}' ";
            strFilterCondition += string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" and tl.domainid = '{txtDomainid.Text}' ";
            strFilterCondition += (string.IsNullOrWhiteSpace(txtHostname.Text) ? string.Empty : $" and tl.Hostname = '{txtHostname.Text}'");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtMessage.Text) ? string.Empty : $" and tl.Message like ('%{txtMessage.Text}%')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtApplicationId.Text) ? string.Empty : $" and tasc.ApplicationID in ('{txtApplicationId.Text}')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtAppsubCompId.Text) ? string.Empty : $" and tasc.AppSubcomponentId not in ('{txtAppsubCompId.Text}')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtAddlCondition.Text) ? string.Empty : $" and {txtAddlCondition.Text}");
            strFilterCondition += $" Order by tl.logdatetime desc ";

            //bool noFilters = true;
            //if (string.IsNullOrWhiteSpace(txtDomainid.Text) && string.IsNullOrWhiteSpace(txtMessage.Text) && string.IsNullOrWhiteSpace(txtHostname.Text) && string.IsNullOrWhiteSpace(txtApplicationId.Text) && string.IsNullOrWhiteSpace(txtAppsubCompId.Text))
            //    noFilters = true;
            //else noFilters = false;
            //if (!noFilters) strFilterCondition += $" Order by tl.logdatetime desc ";
            //else
            //    strFilterCondition += $" Limit {intRows}";
            LoadCalls(query, strFilterCondition);
            this.Cursor = Cursors.Default;
        }

        string LoadCalls(string query, string strFilterCondition = "")
        {
            sw = Stopwatch.StartNew();
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
                        command.CommandTimeout = 60;
                        command.CommandText = query;

                        using (var reader = command.ExecuteReader())
                        {
                            dtResult.Load(reader);
                        }
                        dataGridView1.DataSource = dtResult;
                        dataGridView1.AutoSize = false;
                        //dataGridView1.AutoResizeColumns();
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                errMessage = ex.Message;
                MessageBox.Show($"Error Loading details, Error: + {ex.Message + ex.StackTrace}");
            }
            sw.Stop();
            lblStatus.Text = $"Loaded in {sw.Elapsed} ms";
            return errMessage;
        }

        private void txtDomainid_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            if (!string.IsNullOrWhiteSpace(txtDomainid.Text))
            {
                string strdomainidfilter = string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" and lower(cd.AgentName) = lower('{txtDomainid.Text}') and ";
                filterCondition = $" Where cd.Calldate between '{startDate.Value}' and '{endDate.Value}') ";
            }
            //LoadCalls(filterCondition);
        }

        private void txtDept_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            if (!string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                string strresourcefilter = string.IsNullOrWhiteSpace(txtMessage.Text) ? string.Empty : $" cd.CallDetailid like '{txtMessage.Text}' and ";
                filterCondition = $" Where cd.Calldate between '{startDate.Value}' and '{endDate.Value}')";
            }
            //LoadCalls(filterCondition);
        }

        private void Qatch_Load(object sender, EventArgs e)
        {
            startDate.Value = DateTime.Now;
            endDate.Value = DateTime.Now.AddDays(1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            int intRows = string.IsNullOrEmpty(txtRows.Text) ? 200 : int.Parse(txtRows.Text);
            string query = $"Select top {intRows} Tl.Message, Count(1) missngcnt " +
                "From tbllogging tl(nolock) inner join tblAppSubComponents tasc(nolock) on tl.AppSubComponentId = tasc.AppSubComponentID and tl.LogLevel = 3 ";

            string strFilterCondition = $" Where LogDateTime between '{startDate.Value.Date}' and '{endDate.Value}' ";
            strFilterCondition += string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" and tl.domainid = '{txtDomainid.Text}' ";
            strFilterCondition += (string.IsNullOrWhiteSpace(txtHostname.Text) ? string.Empty : $" and tl.Hostname = '{txtHostname.Text}'");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtMessage.Text) ? string.Empty : $" and tl.Message like ('%{txtMessage.Text}%')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtApplicationId.Text) ? string.Empty : $" and tasc.ApplicationID in ('{txtApplicationId.Text}')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtAppsubCompId.Text) ? string.Empty : $" and tasc.AppSubcomponentId not in ('{txtAppsubCompId.Text}')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtAddlCondition.Text) ? string.Empty : $" and {txtAddlCondition.Text}");
            strFilterCondition += $" Group by Tl.Message Order by 2 desc ";
            LoadCalls(query, strFilterCondition);
            this.Cursor = Cursors.Default;
        }

        private void dataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            // Get the current row
            var row = dataGridView1.Rows[e.RowIndex];

            if (!dataGridView1.Columns.Contains("Message"))
                dataGridView1.Columns["Message"].Width = 500;

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
    }
}
