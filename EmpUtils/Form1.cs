using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmpUtils
{
    public partial class Form1 : Form
    {
        Resource frmResource;
        AgentMachineInfo frmAgentMachineInfo;
        Qatch frmQatch;
        string prevFilterCondition = string.Empty;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoadEmployees(prevFilterCondition);
        }
       
        string LoadEmployees(string strFilterCondition="")
        {
            this.Cursor = Cursors.WaitCursor;
            bool isProgramCodeRequired = chkPrgcode.Checked;
            string connectionString = ConfigurationManager.ConnectionStrings["Workbar"].ConnectionString;
            string query = "SELECT TOP 40 emp.EmployeeId, emp.DomainId, emp.FirstName, emp.LastName, td.DepartmentCode, case when IsNull(emp.OwnerInd, 0) = 0 Then 'Agent' Else 'AVP' End IsOwner, TitleDescription, mgr.domainid Manager, " +
                "Case when emp.EmployeeStatusId = 802 Then 'Resigned' Else 'Active' End Status, " + (isProgramCodeRequired ? "tp.ProgramCode, " : string.Empty) + "tl.LocationName, IsNull(emp.ProgramCode, '') ProgramCode, " +
                "IsNull(emp.HireDt, '') HireDt, IsNull(emp.TerminationDt, '') TerminationDt, IsNull(emp.UpdatedBy, '') UpdatedBy, IsNull(emp.UpdatedDt, '') UpdatedDt FROM tblEmployee emp join tblDepartmentCode td on emp.departmentcode = td.DepartmentCodeId ";
            query += isProgramCodeRequired ? "join tblProgram tp ON tp.ProgramID IN (SELECT * FROM dbo.fnGetTableFromString(emp.ProgramCode, ';')) " : string.Empty;
            query += $"join tblEmployee mgr on emp.managerid = mgr.employeeid join tblTitle tt on emp.titleid = tt.titleid join tblLocation tl on emp.locationid = tl.locationid {strFilterCondition} Order by Status, emp.DomainId";

            //Add the below join to get programcode,
            //join tblProgram tp ON tp.ProgramID IN (SELECT * FROM dbo.fnGetTableFromString(emp.ProgramCode, ';')) 
            string errMessage = string.Empty;
            try
            {
                //MessageBox.Show(query);
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
                    #region load db data to List and then load datagridview
                    //SqlDataReader reader = command.ExecuteReader();
                    //List<clsEmployee> employees = new List<clsEmployee>();
                    //while (reader.Read())
                    //{
                    //    Employee emp = new Employee
                    //    {
                    //        EmployeeId = (long) reader.GetValue(0),
                    //        DomainId = reader.GetString(1),
                    //        FirstName = reader.GetString(2),
                    //        LastName = reader.GetString(3),
                    //        DepartmentCode = reader.GetString(4),
                    //        IsOwner = reader.GetString(5),
                    //        TitleDescription = reader.GetString(6),
                    //        Status = reader.GetString(7),
                    //        ProgramCode = reader.GetString(8),
                    //        HireDt = reader.GetDateTime(9),
                    //        TerminationDt = reader.GetDateTime(10),
                    //        UpdatedBy = reader.GetString(11),
                    //        UpdatedDt = reader.GetDateTime(12)
                    //    };
                    //    employees.Add(emp);
                    //}
                    //reader.Close();
                    //connection.Dispose();
                    //dataGridView1 = new DataGridView();
                    //dataGridView1.AllowUserToAddRows = false;
                    //dataGridView1.AllowUserToDeleteRows = false;
                    //dataGridView1.DataSource = employees;
                    #endregion 
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
            return errMessage;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            filterCondition = $" Where emp.DomainId like '{txtDomainid.Text}%' ";
            LoadEmployees(filterCondition);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            filterCondition = $" Where td.departmentcode like '{txtDept.Text}%' ";
            LoadEmployees(filterCondition);
        }

        private void DataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            // Get the current row
            var row = dataGridView1.Rows[e.RowIndex];

            // Assuming the column you're checking is called "Status"
            string statusValue = row.Cells["Status"].Value?.ToString();

            // Change color based on the value
            if (statusValue == "Resigned")
            {
                row.DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ContextMenuStrip = contextMenuStrip1;
        }

        public class clsEmployee
        {
            public long EmployeeId { get; set; }
            public string DomainId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string DepartmentCode { get; set; }
            public string IsOwner { get; set; }
            public string TitleDescription { get; set; }
            public DateTime HireDt { get; set; }
            public string Status { get; set; }
            public string ProgramCode { get; set; }
            public DateTime TerminationDt { get; set; }
            public string UpdatedBy { get; set; }
            public DateTime UpdatedDt { get; set; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (frmResource is null)
                frmResource = new Resource();
            frmAgentMachineInfo?.Hide();
            frmResource.ShowDialog();
            frmQatch?.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (frmAgentMachineInfo is null)
                frmAgentMachineInfo = new AgentMachineInfo();
            frmResource?.Hide();
            frmAgentMachineInfo.ShowDialog();
            frmQatch?.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (frmQatch is null)
                frmQatch = new Qatch();
            frmResource?.Hide();
            frmAgentMachineInfo?.Hide();
            frmQatch.ShowDialog();
        }
    }
}
