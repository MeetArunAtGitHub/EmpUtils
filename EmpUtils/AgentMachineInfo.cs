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
    public partial class AgentMachineInfo : Form
    {
        Stopwatch sw;
        public AgentMachineInfo()
        {
            InitializeComponent();
        }

        string prevFilterCondition = string.Empty;
        private void button2_Click(object sender, EventArgs e)
        {
            LoadMachineInfo(prevFilterCondition);
        }
        string LoadMachineInfo(string strFilterCondition = "")
        {
            sw = Stopwatch.StartNew();
            this.Cursor = Cursors.WaitCursor;
            string connectionString = Utilities.Decode(ConfigurationManager.ConnectionStrings["Telephony"].ConnectionString);
            string query = "Select Top 200 DomainId, MachineName, MachineIP, CreatedDt, ProgramCode, Extension, Status, DepartmentCode, CreatedBy from agentmachineinfo " +
                $" Where createddt > Getdate()-7 {strFilterCondition} " +
                " Order by CreatedDt Desc, DomainId";
            string errMessage = string.Empty;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataAdapter da = new SqlDataAdapter(command);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    dataGridView1.DataSource = ds.Tables[0];
                    dataGridView1.AutoSize = true;
                    dataGridView1.AutoResizeColumns();
                }
            }
            catch (Exception ex)
            {
                errMessage = ex.Message;
                MessageBox.Show($"Error Loading details, Error: + {ex.Message + ex.StackTrace}");
            }
            finally
            {
                prevFilterCondition = strFilterCondition;
                this.Cursor = Cursors.Default;
            }
            sw.Stop();
            lblStatus.Text = $"Loaded in {sw.Elapsed} ms";
            return errMessage;
        }

        private void txtDomain_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            filterCondition = $" and DomainId like '{txtDomain.Text}%' ";
            LoadMachineInfo(filterCondition);
        }

        private void txtResource_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            filterCondition = $" and DepartmentCode like '{txtResource.Text}%' ";
            LoadMachineInfo(filterCondition);
        }

       
    }
}
