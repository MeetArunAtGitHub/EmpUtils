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
using Npgsql;

namespace EmpUtils
{
    public partial class Form1 : Form
    {
        Resource frmResource;
        AgentMachineInfo frmAgentMachineInfo;
        Qatch frmQatch;
        Qadence frmQadence;
        Logging frmLogging;

        Stopwatch sw;
        string prevFilterCondition = string.Empty;
        public Form1() : this(new QueryRunner()) { }

        internal Form1(IQueryRunner runner)
        {
            activityRunner = runner;
            InitializeComponent();
            AppBranding.Apply(this);
            ConfigureHomeLayout();
            ConfigureEmployeeActivity();
            ConfigureEmployeeLookup();
        }

        private readonly System.Windows.Forms.Timer employeeDebounce = new System.Windows.Forms.Timer { Interval = 500 };
        private System.Threading.CancellationTokenSource employeeRequest;
        private long employeeVersion;
        private bool managerLookup;

        private void ConfigureEmployeeLookup()
        {
            components.Add(employeeDebounce);
            employeeDebounce.Tick += async (sender, args) =>
            {
                employeeDebounce.Stop();
                await LoadEmployeesAsync();
            };
            FormClosing += (sender, args) =>
            {
                employeeDebounce.Stop(); employeeVersion++; employeeRequest?.Cancel();
            };
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            employeeDebounce.Stop();
            await LoadEmployeesAsync();
        }

        private void ScheduleEmployeeLookup(bool byManager)
        {
            managerLookup = byManager;
            employeeDebounce.Stop(); employeeVersion++; employeeRequest?.Cancel();
            // Clear the selected employee immediately so its activity cannot be mistaken for the new search.
            dataGridView1.DataSource = null;
            UpdateActivityEmployee(true);
            string prefix = byManager ? txtDept.Text : txtDomainid.Text;
            if (string.IsNullOrWhiteSpace(prefix))
            {
                lblStatus.Text = "Enter a Domain ID or manager to search.";
                return;
            }
            lblStatus.Text = "Waiting for typing to pause…";
            employeeDebounce.Start();
        }

        internal async Task LoadEmployeesAsync()
        {
            employeeDebounce.Stop(); employeeRequest?.Cancel();
            long version = ++employeeVersion;
            string prefix = managerLookup ? txtDept.Text : txtDomainid.Text;
            if (string.IsNullOrWhiteSpace(prefix))
            {
                dataGridView1.DataSource = null; UpdateActivityEmployee(true);
                lblStatus.Text = "Enter a Domain ID or manager to search."; return;
            }
            var query = new QuerySpec("Qatch",
                "SELECT emp.EmployeeId, emp.DomainId, emp.FirstName, emp.LastName, " +
                "case when coalesce(emp.OwnerInd, false) = false Then 'Agent' Else 'AVP' End IsOwner, " +
                "jobtitle TitleDescription, emp.managerlogonuserid Manager, EmploymentStatus Status, " +
                (chkPrgcode.Checked ? "tp.skill_group_code, " : string.Empty) +
                "workLocationName LocationName, CountryCode, WorkfromHome, coalesce(emp.HireDate, '1/1/2000') HireDt, " +
                "coalesce(emp.TerminationDate, '1/1/2000') TerminationDt, ProfitCenterName, ClientName, CostCenterName, " +
                "division, Functionalareaname, FunctionalSubareaname, VerticalName, coalesce(emp.UpdatedBy, '') UpdatedBy, " +
                "coalesce(emp.UpdatedDt, '1/1/2000') UpdatedDt FROM engagehr.employee emp " +
                (chkPrgcode.Checked ? "left join dbo.employee_skill_group_mapping tp on emp.domainid = tp.employee_domain_id " : string.Empty) +
                "WHERE lower(" + (managerLookup ? "emp.managerlogonuserid" : "emp.DomainId") +
                ") LIKE lower(@prefix) ESCAPE '~' ORDER BY Status, emp.DomainId LIMIT 40");
            query.Add("prefix", QueryFilters.Prefix(prefix));
            using (var request = new System.Threading.CancellationTokenSource())
            {
                employeeRequest = request;
                var elapsed = Stopwatch.StartNew(); lblStatus.Text = "Loading employees…";
                try
                {
                    var table = await activityRunner.ExecuteAsync(query, request.Token);
                    if (IsDisposed || version != employeeVersion || request.IsCancellationRequested) return;
                    dataGridView1.DataSource = table; dataGridView1.AutoResizeColumns();
                    lblStatus.Text = "Loaded " + table.Rows.Count + " employees in " + elapsed.Elapsed.TotalSeconds.ToString("0.00") + " s.";
                }
                catch (Exception ex)
                {
                    if (!IsDisposed && version == employeeVersion)
                        lblStatus.Text = request.IsCancellationRequested ? "Employee lookup canceled." : "Employee lookup unavailable: " + ex.Message;
                }
                finally { if (employeeRequest == request) employeeRequest = null; }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e) => ScheduleEmployeeLookup(false);
        private void textBox2_TextChanged(object sender, EventArgs e) => ScheduleEmployeeLookup(true);
        private void DataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            // Get the current row
            var row = dataGridView1.Rows[e.RowIndex];

            // Assuming the column you're checking is called "Status"
            string statusValue = row.Cells["Status"].Value?.ToString();

            // Change color based on the value
            if (statusValue == "Resigned" || statusValue == "Terminated")
            {
                row.DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ContextMenuStrip = contextMenuStrip1;
        }

        string LoadEmployees_workbardb(string strFilterCondition = "")
        {
            sw = Stopwatch.StartNew();
            this.Cursor = Cursors.WaitCursor;
            bool isProgramCodeRequired = chkPrgcode.Checked;
            string connectionString = Utilities.Decode(ConfigurationManager.ConnectionStrings["Telephony"].ConnectionString);
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
                    dataGridView1.AutoSize = false;
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
            sw.Stop();
            lblStatus.Text = $"Loaded in {sw.Elapsed} ms";
            return errMessage;
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
            frmQadence?.Hide();
            frmLogging?.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (frmAgentMachineInfo is null)
                frmAgentMachineInfo = new AgentMachineInfo();
            frmResource?.Hide();
            frmAgentMachineInfo.ShowDialog();
            frmQatch?.Hide();
            frmQadence?.Hide();
            frmLogging?.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (frmQatch is null)
                frmQatch = new Qatch();
            frmResource?.Hide();
            frmAgentMachineInfo?.Hide();
            frmQatch.ShowDialog();
            frmQadence?.Hide();
            frmLogging?.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (frmQadence is null)
                frmQadence = new Qadence();
            frmResource?.Hide();
            frmAgentMachineInfo?.Hide();
            frmQatch?.Hide();
            frmQadence.ShowDialog();
            frmLogging?.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (frmLogging is null)
                frmLogging = new Logging();
            frmResource?.Hide();
            frmAgentMachineInfo?.Hide();
            frmQatch?.Hide();
            frmQadence?.Hide();
            frmLogging.ShowDialog();
        }
    }
}
