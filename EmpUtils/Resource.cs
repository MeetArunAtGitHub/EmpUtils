using System;
using System.Windows.Forms;

namespace EmpUtils
{
    public partial class Resource : Form
    {
        private readonly PageShell shell;
        private readonly ComboBox domainMatch = new ComboBox { Name = "domainMatch", DropDownStyle = ComboBoxStyle.DropDownList };
        public Resource() : this(new QueryRunner()) { }
        internal Resource(IQueryRunner runner)
        {
            InitializeComponent(); AppBranding.Apply(this);
            domainMatch.Items.AddRange(new object[] { "Exact", "Starts with" }); domainMatch.SelectedIndex = 0;
            button1.Text = "Load resources";
            shell = new PageShell(this, "Resources", dataGridView1, lblStatus,
                new[] { new[] { new PageField("Domain ID", txtDomainid), new PageField("Domain match", domainMatch),
                    new PageField("Security group starts with", txtResourceSecurityGroup) } }, new[] { button1 }, runner);
            lblStatus.Text = "Enter a Domain ID and click Load. Typing does not query the database. Limit: 200 rows.";
        }
        internal QuerySpec BuildQuery()
        {
            var query = new QuerySpec("Automation",
                "SELECT tar.DomainId, tr.ResourceSecurityGroup, tr.ResourceName, pc.profitcentercode, " +
                "pcc.clientname, tr.resourcepathdesktop, tr.resourceargumentsdesktop, tr.ResourcePath, tr.ResourceArguments " +
                "FROM apps.employeeresourcemapping tar " +
                "JOIN efo.Employee te ON tar.domainid = te.domainid " +
                "JOIN apps.resource tr ON tar.resourceid = tr.resourceid AND tr.activeind = true " +
                "JOIN efo.profitcenter pc ON pc.profitcentercode = te.profitcentercode " +
                "JOIN efo.profitcenterclient pcc ON pcc.clientcode = pc.clientcode WHERE tar.activeind = true ");
            if (!string.IsNullOrWhiteSpace(txtDomainid.Text))
            {
                // Filter employees first; the mapping join can then use its existing domainid index.
                if (domainMatch.SelectedIndex == 0)
                    QueryFilters.Exact(query, "te.DomainId", "domain", txtDomainid.Text, true);
                else query.Sql += " AND lower(te.DomainId) LIKE lower(" + query.Add("domain", QueryFilters.Prefix(txtDomainid.Text)) + ") ESCAPE '~' ";
            }
            if (!string.IsNullOrWhiteSpace(txtResourceSecurityGroup.Text))
                query.Sql += " AND lower(tr.ResourceSecurityGroup) LIKE lower(" + query.Add("group", QueryFilters.Prefix(txtResourceSecurityGroup.Text)) + ") ESCAPE '~' ";
            query.Sql += " ORDER BY tar.DomainId, tr.ResourceSecurityGroup LIMIT 200";
            return query;
        }
        private async void button1_Click(object sender, EventArgs e) => await shell.LoadAsync(BuildQuery);
        private void txtDomainid_TextChanged(object sender, EventArgs e) { }
        private void txtDept_TextChanged(object sender, EventArgs e) { }
    }
}
