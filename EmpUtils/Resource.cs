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
    public partial class Resource : Form
    {
        public Resource()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string strFilterCondition = string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" Where tar.Domainid like '{txtDomainid.Text}%' ";
            strFilterCondition = strFilterCondition + (string.IsNullOrWhiteSpace(txtDept.Text) ? string.Empty : $" and tar.ResourceSecurityGroupName like '{txtDept.Text}%'");
            LoadResources(strFilterCondition);
        }
        string LoadResources(string strFilterCondition = "")
        {
            string connectionString = ConfigurationManager.ConnectionStrings["Automation"].ConnectionString;
            strFilterCondition = strFilterCondition + " Order by DomainId, ResourceSecurityGroupName";
            
            string query = "Select Top 200 tar.DomainId, ResourceSecurityGroupName, ResourceName, td.DepartmentCode, tr.ResourcePath, tr.ResourceArguments, tr.resourcepathdesktop, tr.resourceargumentsdesktop from tblagentresources tar join Workbar..tblEmployee te on tar.domainid = te.domainid and tar.inactiveind = 0 " +
                "join tblResource tr on tar.resourcesecurityGroupName = tr.ResourceSecurityGroup and tr.inactiveind = 0 " +
                "join tblDepartmentCode td on td.departmentcodeid = te.departmentcode " +
                $"{strFilterCondition}";
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
            return errMessage;
        }
  
        private void txtDomainid_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            if (!string.IsNullOrWhiteSpace(txtDomainid.Text))
            {
                string strdomainidfilter = string.IsNullOrWhiteSpace(txtDomainid.Text) ? string.Empty : $" tar.Domainid like '{txtDomainid.Text}%' and ";
                filterCondition = $" Where {strdomainidfilter} tar.ResourceSecurityGroupName like '{txtDept.Text}%' ";
            }
            LoadResources(filterCondition);
        }

        private void txtDept_TextChanged(object sender, EventArgs e)
        {
            string filterCondition = string.Empty;
            if (!string.IsNullOrWhiteSpace(txtDept.Text))
            {
                string strresourcefilter = string.IsNullOrWhiteSpace(txtDept.Text) ? string.Empty : $" tar.ResourceSecurityGroupName like '{txtDept.Text}%' and ";
                filterCondition = $" Where {strresourcefilter} tar.DomainId like '{txtDomainid.Text}%' ";
            }
            LoadResources(filterCondition);
        }
    }
}
