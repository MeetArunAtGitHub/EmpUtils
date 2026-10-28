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
using Npgsql;

namespace EmpUtils
{
    public partial class Qadence : Form
    {
        Stopwatch sw;
        public Qadence()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            string query = "SELECT employee.domainid, s.sessid AS sessionid, s.sentdatetimeutc sessionstart, s.lastheartbeatdatetimeutc AS sessionend, s.hostname, s.hostnamevdi, CAST (s.clip / 16777216 & 255 AS VARCHAR(3)) || '.' || CAST (s.clip / 65536 & 255 AS VARCHAR(3)) || '.' || CAST (s.clip / 256 & 255 AS VARCHAR(3)) || '.' || CAST (s.clip & 255 AS VARCHAR(3)) AS ipaddress, " +
                "CAST (s.vmip / 16777216 & 255 AS VARCHAR(3)) || '.' || CAST (s.vmip / 65536 & 255 AS VARCHAR(3)) || '.' || CAST (s.vmip / 256 & 255 AS VARCHAR(3)) || '.' || CAST (s.vmip & 255 AS VARCHAR(3)) AS ipaddressvdi, location.locationname, Profitcentername, s.Domain, s.IsShutdown " +
                "FROM dbo.sessions AS s -- select distinct domainid from dbo.sessions as s \r\n" +
                "INNER JOIN dbo.employee ON s.empid = employee.employeeid INNER JOIN dbo.location ON s.locationid = location.locationid ";
                //"INNER JOIN dbo.subunit su on su.subunitid = s.subunitid  ";

            string strFilterCondition = $" Where s.sentdatetimeutc between '{startDate.Value.Date}' and '{endDate.Value}' ";
            strFilterCondition += string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" and lower(employee.domainid) = lower('{txtDomainid.Text}') ";
            strFilterCondition += (string.IsNullOrWhiteSpace(txtProfitCenter.Text) ? string.Empty : $" and lower(Profitcentername) like lower('{txtProfitCenter.Text}%')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtHostname.Text) ? string.Empty : $" and lower(s.Hostname) = lower('{txtHostname.Text}')");

            int intRows = string.IsNullOrEmpty(txtRows.Text) ? 200 : int.Parse(txtRows.Text);
            bool noFilters = true;
            if (string.IsNullOrWhiteSpace(txtDomainid.Text) && string.IsNullOrWhiteSpace(txtProfitCenter.Text) && string.IsNullOrWhiteSpace(txtHostname.Text))
                noFilters = true;
            else noFilters = false;
            if (!noFilters) strFilterCondition += $" Order by employee.domainid,sessionstart desc Limit {intRows}";
            else
                strFilterCondition += $" Limit {intRows}";
            LoadCalls(query, strFilterCondition);
            this.Cursor = Cursors.Default;
        }

        string LoadCalls(string query, string strFilterCondition = "")
        {
            sw = Stopwatch.StartNew();
            string connectionString = Utilities.Decode(ConfigurationManager.ConnectionStrings["Qadence"].ConnectionString);

            query += $"{strFilterCondition}";

            string errMessage = string.Empty;
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    DataTable dtResult = new DataTable();
                    using (var command = new NpgsqlCommand())
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
            if (!string.IsNullOrWhiteSpace(txtProfitCenter.Text))
            {
                string strresourcefilter = string.IsNullOrWhiteSpace(txtProfitCenter.Text) ? string.Empty : $" cd.CallDetailid like '{txtProfitCenter.Text}' and ";
                filterCondition = $" Where cd.Calldate between '{startDate.Value}' and '{endDate.Value}')";
            }
            //LoadCalls(filterCondition);
        }

        private void Qatch_Load(object sender, EventArgs e)
        {
            startDate.Value = DateTime.Now.AddDays(-5);
            endDate.Value = DateTime.Now.AddDays(1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            int intRows = string.IsNullOrEmpty(txtRows.Text) ? 200 : int.Parse(txtRows.Text);
            string query = "SELECT distinct domainid from dbo.sessions as s INNER JOIN dbo.employee ON s.empid = employee.employeeid " +
                "INNER JOIN dbo.location ON s.locationid = location.locationid INNER JOIN dbo.subunit su on su.subunitid = s.subunitid ";

            string strFilterCondition = $" Where s.sentdatetimeutc between '{startDate.Value.Date}' and '{endDate.Value}' ";
            strFilterCondition += string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" and lower(employee.domainid) = lower('{txtDomainid.Text}') ";
            strFilterCondition += (string.IsNullOrWhiteSpace(txtProfitCenter.Text) ? string.Empty : $" and lower(su.subunitname) like lower('{txtProfitCenter.Text}%')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtHostname.Text) ? string.Empty : $" and lower(s.Hostname) = lower('{txtHostname.Text}')");
            strFilterCondition += $" group by domainid Order by 1 desc limit {intRows}";
            LoadCalls(query, strFilterCondition);
            this.Cursor = Cursors.Default;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            int intRows = string.IsNullOrEmpty(txtRows.Text) ? 200 : int.Parse(txtRows.Text);
            string query = "Select cd.ProgramCode ProgCode, Count(1) CallCount from dbo.call_details cd ";

            string strFilterCondition = $" Where cd.Calldate between '{startDate.Value.Date}' and '{endDate.Value}' ";
            strFilterCondition += string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" and lower(cd.AgentName) = lower('{txtDomainid.Text}') ";
            strFilterCondition += $" group by cd.ProgramCode Order by 2 desc limit {intRows}";
            LoadCalls(query, strFilterCondition);
            this.Cursor = Cursors.Default;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            int intRows = string.IsNullOrEmpty(txtRows.Text) ? 200 : int.Parse(txtRows.Text);
            string query = "Select CallDate::date CallDat, Left(cd.ProgramCode, 3) Client, Count(1) CallCount from dbo.call_details cd ";

            string strFilterCondition = $" Where cd.Calldate between '{startDate.Value.Date}' and '{endDate.Value}' ";
            //strFilterCondition += string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" and lower(cd.AgentName) = lower('{txtDomainid.Text}') ";
            strFilterCondition += $" group by CallDat, Client Order by 1 desc, 3 desc, 2 limit {intRows}";
            LoadCalls(query, strFilterCondition);
            this.Cursor = Cursors.Default;
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
    }
}
