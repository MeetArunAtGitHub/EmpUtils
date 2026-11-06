using System;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmpUtils
{
    internal sealed class ActivityWindow
    {
        internal readonly DateTime FromUtc;
        internal readonly DateTime ThroughUtc;
        internal readonly DateTime FromEastern;
        internal readonly DateTime ThroughEastern;
        internal ActivityWindow(DateTime nowUtc)
        {
            if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Activity window must use UTC.");
            ThroughUtc = nowUtc; FromUtc = nowUtc.AddHours(-72);
            var eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            FromEastern = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(FromUtc, eastern), DateTimeKind.Unspecified);
            ThroughEastern = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(ThroughUtc, eastern), DateTimeKind.Unspecified);
        }
    }

    internal static class ActivityQueries
    {
        internal static QuerySpec Logins(string domain, ActivityWindow window)
        {
            var query = new QuerySpec("Qadence", "SELECT s.sentdatetimeutc AS LoginUTC, " +
                "s.lastheartbeatdatetimeutc AS LastHeartbeatUTC, s.hostname AS Hostname, s.hostnamevdi AS VDIHostname " +
                "FROM dbo.sessions s INNER JOIN dbo.employee e ON e.employeeid = s.empid " +
                "WHERE lower(e.domainid) = lower(@domain) AND s.sentdatetimeutc >= @from " +
                "AND s.sentdatetimeutc < @through ORDER BY s.sentdatetimeutc DESC, s.sessid DESC LIMIT 201");
            query.Add("domain", domain); query.AddTime("from", window.FromUtc); query.AddTime("through", window.ThroughUtc);
            return query;
        }
        internal static QuerySpec Calls(string domain, ActivityWindow window)
        {
            var query = new QuerySpec("Qatch", "SELECT cd.CallDate::date AS CallDateET, COUNT(*) AS CallCount, " +
                "COUNT(*) FILTER (WHERE cd.CallId = 99999) AS VideoOnlyCount FROM dbo.call_details cd " +
                "WHERE lower(cd.AgentName) = lower(@domain) AND cd.CallDate >= @from AND cd.CallDate < @through " +
                "GROUP BY cd.CallDate::date ORDER BY cd.CallDate::date DESC");
            query.Add("domain", domain); query.AddTime("from", window.FromEastern); query.AddTime("through", window.ThroughEastern);
            return query;
        }
        internal static DataTable FillCallDays(DataTable source, ActivityWindow window)
        {
            var result = new DataTable(); result.Columns.Add("Date (ET)", typeof(DateTime));
            result.Columns.Add("Calls (all)", typeof(long)); result.Columns.Add("Video only", typeof(long));
            for (var day = window.ThroughEastern.Date; day >= window.FromEastern.Date; day = day.AddDays(-1))
            {
                // Upper bound is exclusive; do not add an empty day when the window ends at midnight.
                if (day == window.ThroughEastern) continue;
                long calls = 0, video = 0;
                foreach (DataRow row in source.Rows)
                {
                    if (Convert.ToDateTime(row["CallDateET"]).Date != day) continue;
                    calls += Convert.ToInt64(row["CallCount"]); video += Convert.ToInt64(row["VideoOnlyCount"]);
                }
                result.Rows.Add(day, calls, video);
            }
            return result;
        }
    }

    public partial class Form1
    {
        private IQueryRunner activityRunner;
        private CancellationTokenSource activityRequest;
        private long activityVersion;
        private string activityDomain;
        private TableLayoutPanel activityPanel;
        private Label activityHeading, loginStatus, callStatus;
        private DataGridView loginGrid, callGrid;
        private Button loadActivity, cancelActivity;

        private void ConfigureEmployeeActivity()
        {
            var root = (TableLayoutPanel)Controls.Find("homeLayout", true)[0];
            root.SetRow(lblStatus, 5); root.RowCount = 6;
            root.RowStyles[4] = new RowStyle(SizeType.Absolute, 0);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            activityPanel = new TableLayoutPanel
            {
                Name = "employeeActivity", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
                Margin = new Padding(0, 7, 0, 0), Visible = false
            };
            activityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            activityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            activityPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            activityPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            activityHeading = new Label { Name = "activityEmployee", Dock = DockStyle.Fill, AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0) };
            loadActivity = new Button { Name = "loadEmployeeActivity", Text = "Load details", Dock = DockStyle.Fill,
                Margin = new Padding(4, 1, 0, 3), FlatStyle = FlatStyle.Flat, BackColor = Color.White };
            cancelActivity = new Button { Name = "cancelEmployeeActivity", Text = "Cancel", Dock = DockStyle.Fill,
                Margin = new Padding(4, 1, 0, 3), FlatStyle = FlatStyle.Flat, Enabled = false };
            loadActivity.Click += async (sender, args) => await LoadEmployeeActivityAsync();
            cancelActivity.Click += (sender, args) => activityRequest?.Cancel();
            heading.Controls.Add(activityHeading, 0, 0); heading.Controls.Add(loadActivity, 1, 0);
            heading.Controls.Add(cancelActivity, 2, 0); activityPanel.Controls.Add(heading, 0, 0); activityPanel.SetColumnSpan(heading, 2);
            loginGrid = ActivityGrid("employeeLogins"); callGrid = ActivityGrid("employeeCallCounts");
            loginStatus = ActivityLabel("loginActivityStatus"); callStatus = ActivityLabel("callActivityStatus");
            activityPanel.Controls.Add(ActivityCard("Qadence logins • UTC", loginGrid, loginStatus), 0, 1);
            activityPanel.Controls.Add(ActivityCard("Qatch daily counts • Eastern", callGrid, callStatus), 1, 1);
            root.Controls.Add(activityPanel, 0, 4);
            dataGridView1.SelectionChanged += (sender, args) => UpdateActivityEmployee();
            dataGridView1.CurrentCellChanged += (sender, args) => UpdateActivityEmployee();
            dataGridView1.DataBindingComplete += (sender, args) => UpdateActivityEmployee(true);
            FormClosing += (sender, args) => { activityVersion++; activityRequest?.Cancel(); };
        }
        private static Label ActivityLabel(string name) => new Label { Name = name, Dock = DockStyle.Fill,
            Text = "Click Load details to query the most recent 72 hours.", AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(89, 107, 129), Margin = new Padding(3, 0, 3, 0) };
        private DataGridView ActivityGrid(string name)
        {
            return new DataGridView { Name = name, Dock = DockStyle.Fill, ReadOnly = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(229, 236, 245), ForeColor = ForeColor },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, Margin = new Padding(0) };
        }
        private Control ActivityCard(string title, DataGridView grid, Label status)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Color.White, Margin = new Padding(0, 0, 7, 0) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 23)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold), Margin = new Padding(3, 0, 3, 0) }, 0, 0);
            panel.Controls.Add(grid, 0, 1); panel.Controls.Add(status, 0, 2); return panel;
        }
        private string SelectedActivityDomain()
        {
            if (dataGridView1.CurrentRow == null || !dataGridView1.Columns.Contains("DomainId")) return null;
            return Convert.ToString(dataGridView1.CurrentRow.Cells["DomainId"].Value)?.Trim();
        }
        private void UpdateActivityEmployee(bool force = false)
        {
            string selected = SelectedActivityDomain();
            if (!force && string.Equals(selected, activityDomain, StringComparison.OrdinalIgnoreCase)) return;
            activityVersion++; activityRequest?.Cancel(); activityRequest = null; activityDomain = selected;
            loginGrid.DataSource = null; callGrid.DataSource = null;
            activityPanel.Visible = !string.IsNullOrWhiteSpace(selected);
            var root = (TableLayoutPanel)Controls.Find("homeLayout", true)[0];
            root.RowStyles[4].Height = activityPanel.Visible ? 230 : 0;
            activityHeading.Text = "Last 72 hours • " + selected;
            loginStatus.Text = callStatus.Text = "Click Load details to query the most recent 72 hours.";
            loadActivity.Enabled = activityPanel.Visible; cancelActivity.Enabled = false;
        }
        internal async Task LoadEmployeeActivityAsync()
        {
            string domain = activityDomain;
            if (string.IsNullOrWhiteSpace(domain) || activityRequest != null) return;
            long version = activityVersion;
            using (var request = new CancellationTokenSource())
            {
                activityRequest = request; loadActivity.Enabled = false; cancelActivity.Enabled = true;
                var window = new ActivityWindow(DateTime.UtcNow);
                activityHeading.Text = "Last 72 hours • " + domain + " • to " + window.ThroughUtc.ToString("yyyy-MM-dd HH:mm") + " UTC";
                loginGrid.DataSource = null; callGrid.DataSource = null;
                loginStatus.Text = callStatus.Text = "Loading…";
                await Task.WhenAll(LoadActivityPart(true, domain, window, version, request.Token),
                    LoadActivityPart(false, domain, window, version, request.Token));
                if (!IsDisposed && version == activityVersion && activityRequest == request)
                { activityRequest = null; loadActivity.Enabled = true; cancelActivity.Enabled = false; }
            }
        }
        private async Task LoadActivityPart(bool logins, string domain, ActivityWindow window, long version, CancellationToken token)
        {
            var label = logins ? loginStatus : callStatus;
            try
            {
                var table = await activityRunner.ExecuteAsync(logins ? ActivityQueries.Logins(domain, window)
                    : ActivityQueries.Calls(domain, window), token);
                if (IsDisposed || version != activityVersion) return;
                token.ThrowIfCancellationRequested();
                if (logins)
                {
                    bool partial = table.Rows.Count > 200; if (partial) table.Rows.RemoveAt(200);
                    loginGrid.DataSource = table;
                    foreach (DataGridViewColumn column in loginGrid.Columns)
                        if (column.ValueType == typeof(DateTime)) column.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
                    loginStatus.Text = partial ? "Showing newest 200 sessions; additional sessions exist." : table.Rows.Count + " logins in the last 72 hours (UTC).";
                }
                else
                {
                    callGrid.DataSource = ActivityQueries.FillCallDays(table, window);
                    callGrid.Columns[0].DefaultCellStyle.Format = "yyyy-MM-dd";
                    callStatus.Text = "All calls include video-only (CallId 99999); boundary days are partial.";
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed && version == activityVersion)
                    label.Text = token.IsCancellationRequested ? "Canceled. Click Load details to retry."
                        : "Unavailable: " + ex.Message;
            }
        }
    }
}
