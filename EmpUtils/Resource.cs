using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using Npgsql;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace EmpUtils
{
    public partial class Resource : Form
    {
        Stopwatch sw;
        public Resource()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string strFilterCondition = string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" Where lower(tar.Domainid) like 'lower({txtDomainid.Text})%' ";
            strFilterCondition = strFilterCondition + (string.IsNullOrWhiteSpace(txtResourceSecurityGroup.Text) ? string.Empty : $" and lower(tr.ResourceSecurityGroup) like lower('{txtResourceSecurityGroup.Text}%')");
            LoadResources(strFilterCondition);
        }
        string LoadResources(string strFilterCondition = "")
        {
            sw = Stopwatch.StartNew();
            string connectionString = ConfigurationManager.ConnectionStrings["Automation"].ConnectionString;
            strFilterCondition = strFilterCondition + " Order by DomainId, ResourceSecurityGroup LIMIT 200";

            //string query = "Select tar.DomainId, ResourceSecurityGroupName, ResourceName, td.DepartmentCode, tr.ResourcePath, tr.ResourceArguments, tr.resourcepathdesktop, tr.resourceargumentsdesktop from apps.employeeresourcemapping tar " +
            //    "join efo.Employee te on tar.domainid = te.domainid and tar.activeind = 0 " +
            //    "join apps.resource tr on tar.resourcesecurityGroupName = tr.ResourceSecurityGroup and tr.activeind = 0 " +
            //    "join tblDepartmentCode td on td.departmentcodeid = te.departmentcode " +
            //    $"{strFilterCondition}";
            string query = "SELECT  tar.DomainId, tr.ResourceSecurityGroup, tr.ResourceName, pc.profitcentercode, pcc.clientname, tr.resourcepathdesktop, tr.resourceargumentsdesktop, tr.ResourcePath, tr.ResourceArguments From " +
                "apps.employeeresourcemapping tar JOIN efo.Employee te ON tar.domainid = te.domainid AND tar.activeind = true " +
                "JOIN apps.resource tr ON tar.resourceid = tr.resourceid AND tr.activeind = true " +
                "JOIN efo.profitcenter pc ON pc.profitcentercode = te.profitcentercode " +
                "JOIN efo.profitcenterclient pcc ON pcc.clientcode = pc.clientcode " +
                 $"{strFilterCondition}";
            string errMessage = string.Empty;
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    NpgsqlCommand command = new NpgsqlCommand(query, connection);
                    connection.Open();
                    NpgsqlDataAdapter da = new NpgsqlDataAdapter(command);
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
            sw.Stop();
            lblStatus.Text = $"Loaded in {sw.Elapsed} ms";
            return errMessage;
        }
  
        private void txtDomainid_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            if (!string.IsNullOrWhiteSpace(txtDomainid.Text))
            {
                string strdomainidfilter = string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" lower(tar.Domainid) like lower('{txtDomainid.Text}%') and ";
                filterCondition = $" Where {strdomainidfilter} lower(tr.ResourceSecurityGroup) like lower('{txtResourceSecurityGroup.Text}%') ";
            }
            LoadResources(filterCondition);
        }

        private void txtDept_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            if (!string.IsNullOrWhiteSpace(txtResourceSecurityGroup.Text))
            {
                string strresourcefilter = string.IsNullOrWhiteSpace(txtResourceSecurityGroup.Text) ? string.Empty : $" lower(tr.ResourceSecurityGroup) like lower('{txtResourceSecurityGroup.Text}%') and ";
                filterCondition = $" Where {strresourcefilter} lower(tar.DomainId) like lower('{txtDomainid.Text}%') ";
            }
            LoadResources(filterCondition);
        }
    }
}
