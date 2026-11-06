using System;
using System.Windows.Forms;

namespace EmpUtils
{
    public partial class Qadence : Form
    {
        private readonly PageShell shell;
        public Qadence() : this(new QueryRunner()) { }
        internal Qadence(IQueryRunner runner)
        {
            InitializeComponent(); AppBranding.Apply(this);
            button1.Text = "Load sessions"; button2.Text = "Distinct employees"; txtRows.MaxLength = 4;
            shell = new PageShell(this, "Qadence sessions", dataGridView1, lblStatus,
                new[] { new[] { new PageField("From (UTC)", startDate), new PageField("Through (UTC)", endDate),
                        new PageField("Domain ID", txtDomainid) },
                    new[] { new PageField("Profit center starts with", txtProfitCenter),
                        new PageField("Hostname", txtHostname), new PageField("Max rows (1–1000)", txtRows) } },
                new[] { button1, button2 }, runner);
        }
        internal QuerySpec BuildQuery(bool distinct)
        {
            const string ip = "CAST(s.clip / 16777216 & 255 AS VARCHAR(3)) || '.' || CAST(s.clip / 65536 & 255 AS VARCHAR(3)) || '.' || CAST(s.clip / 256 & 255 AS VARCHAR(3)) || '.' || CAST(s.clip & 255 AS VARCHAR(3))";
            const string vdi = "CAST(s.vmip / 16777216 & 255 AS VARCHAR(3)) || '.' || CAST(s.vmip / 65536 & 255 AS VARCHAR(3)) || '.' || CAST(s.vmip / 256 & 255 AS VARCHAR(3)) || '.' || CAST(s.vmip & 255 AS VARCHAR(3))";
            string select = distinct ? "SELECT DISTINCT employee.domainid " :
                "SELECT employee.domainid, s.sessid AS sessionid, s.sentdatetimeutc AS sessionstart, " +
                "s.lastheartbeatdatetimeutc AS sessionend, s.hostname, s.hostnamevdi, " + ip + " AS ipaddress, " +
                vdi + " AS ipaddressvdi, location.locationname, Profitcentername, s.Domain, s.IsShutdown ";
            var query = new QuerySpec("Qadence", select + "FROM dbo.sessions s " +
                "INNER JOIN dbo.employee ON s.empid = employee.employeeid " +
                "INNER JOIN dbo.location ON s.locationid = location.locationid ");
            QueryFilters.DateRange(query, startDate.Value, endDate.Value, "s.sentdatetimeutc");
            QueryFilters.Exact(query, "employee.domainid", "domain", txtDomainid.Text, true);
            QueryFilters.Exact(query, "s.Hostname", "host", txtHostname.Text, true);
            if (!string.IsNullOrWhiteSpace(txtProfitCenter.Text))
                query.Sql += " AND lower(Profitcentername) LIKE lower(" + query.Add("profit", QueryFilters.Prefix(txtProfitCenter.Text)) + ") ESCAPE '~' ";
            query.Sql += distinct ? " ORDER BY employee.domainid LIMIT @limit" : " ORDER BY s.sentdatetimeutc DESC, s.sessid DESC LIMIT @limit";
            query.Add("limit", QueryFilters.Rows(txtRows.Text)); return query;
        }
        private async void button1_Click(object sender, EventArgs e) => await shell.LoadAsync(() => BuildQuery(false));
        private async void button2_Click(object sender, EventArgs e) => await shell.LoadAsync(() => BuildQuery(true));
        private void txtDomainid_TextChanged(object sender, EventArgs e) { }
        private void txtDept_TextChanged(object sender, EventArgs e) { }
        private void Qatch_Load(object sender, EventArgs e)
        { startDate.Value = DateTime.UtcNow.AddDays(-5); endDate.Value = DateTime.UtcNow; }
        private void dataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e) { }
    }
}
