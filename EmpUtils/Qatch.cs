using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmpUtils
{
    public partial class Qatch : Form
    {
        Stopwatch sw;
        public Qatch()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            string query = "Select cd.CallDetailId, CallDate, AgentName, crd.AudioFile, ProgramCode, CallDuration, Phone, CallDirection, Avayaucid, Department, cd.CreatedBy, cd.CreatedDt, CallId, WrapCode, StateCode, crd.AudioFileLocation, " +
                "crd.VideoFile, crd.VideoFileLocation, crd.AudioFileSize, agentidid from dbo.call_details cd join dbo.call_recording_details crd on cd.calldetailid = crd.calldetailid ";

            string strFilterCondition = $" Where cd.Calldate between '{startDate.Value.Date}' and '{endDate.Value}' ";
            if (string.IsNullOrWhiteSpace(txtCallDetailId.Text))
            {
                strFilterCondition += string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" and lower(cd.AgentName) = lower('{txtDomainid.Text}') ";
                strFilterCondition += (string.IsNullOrWhiteSpace(txtClient.Text) ? string.Empty : $" and exists ( select 1 from dbo.program p inner join dbo.client c on p.clientid = c.clientid where p.programcode = cd.programcode and c.clientcode = '{txtClient.Text}')");
                strFilterCondition += (string.IsNullOrWhiteSpace(txtCallDetailId.Text) ? string.Empty : $" and cd.CallDetailid = '{txtCallDetailId.Text}'");
                strFilterCondition += (string.IsNullOrWhiteSpace(txtAudioFile.Text) ? string.Empty : $" and crd.AudioFile = '{txtAudioFile.Text}'");
                strFilterCondition += (string.IsNullOrWhiteSpace(txtAddlCondition.Text) ? string.Empty : $" and {txtAddlCondition.Text}");
            }
            else
                strFilterCondition = (string.IsNullOrWhiteSpace(txtCallDetailId.Text) ? string.Empty : $" where cd.CallDetailid = '{txtCallDetailId.Text}'");

            int intRows = string.IsNullOrEmpty(txtRows.Text) ? 200 : int.Parse(txtRows.Text);
            bool noFilters = true;
            if (string.IsNullOrWhiteSpace(txtDomainid.Text) && string.IsNullOrWhiteSpace(txtCallDetailId.Text) && string.IsNullOrWhiteSpace(txtAudioFile.Text) && string.IsNullOrEmpty(txtClient.Text))
                noFilters = true;
            else noFilters = false;
            if (!noFilters) strFilterCondition += $" Order by 1 desc Limit {intRows}";
            else
                strFilterCondition += $" Limit {intRows}";
            LoadCalls(query, strFilterCondition);
            this.Cursor = Cursors.Default;
        }

        string LoadCalls(string query, string strFilterCondition = "")
        {
            sw = Stopwatch.StartNew();
            string connectionString = Utilities.Decode(ConfigurationManager.ConnectionStrings["Qatch"].ConnectionString);

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
            if (!string.IsNullOrWhiteSpace(txtCallDetailId.Text))
            {
                string strresourcefilter = string.IsNullOrWhiteSpace(txtCallDetailId.Text) ? string.Empty : $" cd.CallDetailid like '{txtCallDetailId.Text}' and ";
                filterCondition = $" Where cd.Calldate between '{startDate.Value}' and '{endDate.Value}')";
            }
            //LoadCalls(filterCondition);
        }

        private void Qatch_Load(object sender, EventArgs e)
        {
            startDate.Value = DateTime.Now.AddDays(-1);
            endDate.Value = DateTime.Now.AddDays(1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            int intRows = string.IsNullOrEmpty(txtRows.Text) ? 200 : int.Parse(txtRows.Text);
            string query = "Select cd.CallDate::Date calldat, Count(1) CallCount from dbo.call_details cd ";

            string strFilterCondition = $" Where cd.Calldate between '{startDate.Value.Date}' and '{endDate.Value}' ";
            strFilterCondition += string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" and lower(cd.AgentName) = lower('{txtDomainid.Text}') ";
            strFilterCondition += (string.IsNullOrWhiteSpace(txtAddlCondition.Text) ? string.Empty : $" and {txtAddlCondition.Text}");
            strFilterCondition += $" group by calldat Order by 1 desc limit {intRows}";
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
            strFilterCondition += (string.IsNullOrWhiteSpace(txtClient.Text) ? string.Empty : $" and exists ( select 1 from dbo.program p inner join dbo.client c on p.clientid = c.clientid where p.programcode = cd.programcode and c.clientcode = '{txtClient.Text}')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtAddlCondition.Text) ? string.Empty : $" and {txtAddlCondition.Text}");
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
            strFilterCondition += (string.IsNullOrWhiteSpace(txtClient.Text) ? string.Empty : $" and exists ( select 1 from dbo.program p inner join dbo.client c on p.clientid = c.clientid where p.programcode = cd.programcode and c.clientcode = '{txtClient.Text}')");
            strFilterCondition += (string.IsNullOrWhiteSpace(txtAddlCondition.Text) ? string.Empty : $" and {txtAddlCondition.Text}");
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
