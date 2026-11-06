using System;
using System.Linq;
using System.Windows.Forms;

namespace EmpUtils
{
    public partial class Logging : Form
    {
        private readonly PageShell shell;
        public Logging() : this(new QueryRunner()) { }
        internal Logging(IQueryRunner runner)
        {
            InitializeComponent(); AppBranding.Apply(this); txtRows.MaxLength = 4;
            button1.Text = "Load logs"; button2.Text = "Error summary";
            shell = new PageShell(this, "Application logs", dataGridView1, lblStatus,
                new[] { new[] { new PageField("From", startDate), new PageField("Through", endDate),
                        new PageField("Domain ID", txtDomainid), new PageField("Hostname", txtHostname) },
                    new[] { new PageField("Message contains", txtMessage), new PageField("Application IDs (comma-separated)", txtApplicationId),
                        new PageField("Exclude subcomponent IDs", txtAppsubCompId), new PageField("Max rows (1–1000)", txtRows) },
                    new[] { new PageField("Additional SQL expression (optional)", txtAddlCondition) } },
                new[] { button1, button2 }, runner);
        }
        internal QuerySpec BuildQuery(bool errors)
        {
            var query = new QuerySpec("Logging", errors ? "SELECT TOP (@limit) tl.Message, COUNT(1) AS ErrorCount " :
                "SELECT TOP (@limit) tl.LogDateTime, tl.Message, tl.LogLevel, tl.Hostname, tl.DomainId, " +
                "tasc.AppSubComponentID, tl.Counter, tasc.ApplicationID ", true);
            query.Sql += "FROM tbllogging tl WITH (NOLOCK) INNER JOIN tblAppSubComponents tasc WITH (NOLOCK) " +
                "ON tl.AppSubComponentId = tasc.AppSubComponentID ";
            QueryFilters.DateRange(query, startDate.Value, endDate.Value, "tl.LogDateTime");
            QueryFilters.Exact(query, "tl.DomainId", "domain", txtDomainid.Text);
            QueryFilters.Exact(query, "tl.Hostname", "host", txtHostname.Text);
            if (!string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                string message = QueryFilters.Prefix(txtMessage.Text);
                message = message.Replace("[", "~[").Replace("]", "~]");
                query.Sql += " AND tl.Message LIKE " + query.Add("message", "%" + message) + " ESCAPE '~' ";
            }
            IdFilter(query, txtApplicationId.Text, "tasc.ApplicationID", "app", false);
            IdFilter(query, txtAppsubCompId.Text, "tasc.AppSubComponentID", "sub", true);
            if (errors) query.Sql += " AND tl.LogLevel = 3 ";
            if (!string.IsNullOrWhiteSpace(txtAddlCondition.Text)) query.Sql += " AND (" + txtAddlCondition.Text.Trim() + ") ";
            query.Sql += errors ? " GROUP BY tl.Message ORDER BY ErrorCount DESC" : " ORDER BY tl.LogDateTime DESC";
            query.Add("limit", QueryFilters.Rows(txtRows.Text)); return query;
        }
        private static void IdFilter(QuerySpec query, string input, string column, string name, bool exclude)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            var values = input.Split(',').Select(v => v.Trim()).ToArray();
            var parameters = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                int id;
                if (!int.TryParse(values[i], out id) || id < 0) throw new ArgumentException("Application/subcomponent IDs must be comma-separated non-negative integers.");
                parameters[i] = query.Add(name + i, id);
            }
            query.Sql += " AND " + column + (exclude ? " NOT IN (" : " IN (") + string.Join(",", parameters) + ") ";
        }
        private async void button1_Click(object sender, EventArgs e) => await shell.LoadAsync(() => BuildQuery(false));
        private async void button2_Click(object sender, EventArgs e) => await shell.LoadAsync(() => BuildQuery(true));
        private void txtDomainid_TextChanged(object sender, EventArgs e) { }
        private void txtDept_TextChanged(object sender, EventArgs e) { }
        private void Qatch_Load(object sender, EventArgs e)
        { startDate.Value = DateTime.Today; endDate.Value = DateTime.Now; }
        private void dataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        { if (dataGridView1.Columns.Contains("Message")) dataGridView1.Columns["Message"].Width = 500; }
    }
}
