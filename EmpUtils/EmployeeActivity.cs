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
        internal static readonly TimeZoneInfo EasternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        internal readonly DateTime FromUtc;
        internal readonly DateTime ThroughUtc;
        internal readonly DateTime FromEastern;
        internal readonly DateTime ThroughEastern;
        internal ActivityWindow(DateTime nowUtc, int days = 3)
        {
            if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Activity window must use UTC.");
            if (days < 1 || days > 90) throw new ArgumentOutOfRangeException(nameof(days), "Days must be between 1 and 90.");
            ThroughUtc = nowUtc;
            ThroughEastern = ToEastern(nowUtc);
            FromEastern = ThroughEastern.Date.AddDays(-days);
            FromUtc = TimeZoneInfo.ConvertTimeToUtc(FromEastern, EasternZone);
        }
        internal static DateTime ToEastern(DateTime utc) => DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), EasternZone), DateTimeKind.Unspecified);
    }

    internal static class ActivityQueries
    {
        internal static QuerySpec Logins(string domain, ActivityWindow window)
        {
            var query = new QuerySpec("Qadence", "SELECT s.sentdatetimeutc AS LoginUTC, " +
                "s.lastheartbeatdatetimeutc AS LastHeartbeatUTC, s.hostname AS Hostname, s.hostnamevdi AS VDIHostname, " +
                IpAddress("s.clip") + " AS IPAddress, " + IpAddress("s.vmip") + " AS VDIIPAddress " +
                "FROM dbo.sessions s INNER JOIN dbo.employee e ON e.employeeid = s.empid " +
                "WHERE lower(e.domainid) = lower(@domain) AND s.sentdatetimeutc >= @from " +
                "AND s.sentdatetimeutc < @through ORDER BY s.sentdatetimeutc DESC, s.sessid DESC LIMIT 201");
            query.Add("domain", domain); query.AddTime("from", window.FromUtc); query.AddTime("through", window.ThroughUtc);
            return query;
        }
        private static string IpAddress(string column) =>
            "CAST(" + column + " / 16777216 & 255 AS VARCHAR(3)) || '.' || CAST(" + column +
            " / 65536 & 255 AS VARCHAR(3)) || '.' || CAST(" + column + " / 256 & 255 AS VARCHAR(3)) || '.' || CAST(" + column + " & 255 AS VARCHAR(3))";

        internal static DataTable EasternLogins(DataTable source)
        {
            var result = new DataTable(); result.Columns.Add("Login (ET)", typeof(DateTime));
            result.Columns.Add("IP address"); result.Columns.Add("Hostname");
            result.Columns.Add("Last heartbeat (ET)", typeof(DateTime));
            result.Columns.Add("VDI IP address"); result.Columns.Add("VDI hostname");
            foreach (DataRow row in source.Rows)
            {
                object login = row.IsNull("LoginUTC") ? DBNull.Value : (object)ActivityWindow.ToEastern(Convert.ToDateTime(row["LoginUTC"]));
                object heartbeat = row.IsNull("LastHeartbeatUTC") ? DBNull.Value : (object)ActivityWindow.ToEastern(Convert.ToDateTime(row["LastHeartbeatUTC"]));
                result.Rows.Add(login, row["IPAddress"], row["Hostname"], heartbeat, row["VDIIPAddress"], row["VDIHostname"]);
            }
            return result;
        }
        internal static QuerySpec Calls(string domain, ActivityWindow window)
        {
            var query = new QuerySpec("Qatch", "SELECT cd.CallDate::date AS CallDateET, COUNT(*) AS CallCount, " +
                "COUNT(*) FILTER (WHERE cd.CallId = 99999) AS VideoOnlyCount, " +
                "COUNT(*) FILTER (WHERE cd.CallId <> 99999) AS NonVideoCallCount FROM dbo.call_details cd " +
                "WHERE lower(cd.AgentName) = lower(@domain) AND cd.CallDate >= @from AND cd.CallDate < @through " +
                "GROUP BY cd.CallDate::date ORDER BY cd.CallDate::date DESC");
            query.Add("domain", domain); query.AddTime("from", window.FromEastern); query.AddTime("through", window.ThroughEastern);
            return query;
        }
        internal static DataTable FillCallDays(DataTable source, ActivityWindow window)
        {
            var result = new DataTable(); result.Columns.Add("Date (ET)", typeof(DateTime));
            result.Columns.Add("Calls (all)", typeof(long)); result.Columns.Add("Calls excl. video", typeof(long));
            result.Columns.Add("Video only", typeof(long));
            for (var day = window.ThroughEastern.Date; day >= window.FromEastern.Date; day = day.AddDays(-1))
            {
                // Upper bound is exclusive; do not add an empty day when the window ends at midnight.
                if (day == window.ThroughEastern) continue;
                long calls = 0, video = 0, nonVideo = 0;
                foreach (DataRow row in source.Rows)
                {
                    if (Convert.ToDateTime(row["CallDateET"]).Date != day) continue;
                    calls += Convert.ToInt64(row["CallCount"]); video += Convert.ToInt64(row["VideoOnlyCount"]);
                    nonVideo += Convert.ToInt64(row["NonVideoCallCount"]);
                }
                result.Rows.Add(day, calls, nonVideo, video);
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
        private TextBox activityDays;
        private SplitContainer employeeSplit;
        private bool activitySplitInitialized;

        private void ConfigureEmployeeActivity()
        {
            var root = (TableLayoutPanel)Controls.Find("homeLayout", true)[0];
            var results = root.GetControlFromPosition(0, 3);
            employeeSplit = new SplitContainer
            {
                Name = "employeeActivitySplit", Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
                Size = new Size(800, 450), SplitterWidth = 7, SplitterDistance = 220,
                Panel1MinSize = 100, Panel2MinSize = 150, Margin = new Padding(0), Panel2Collapsed = true,
                AccessibleName = "Resize employee results and activity panels"
            };
            employeeSplit.Panel1.Controls.Add(results);
            root.Controls.Add(employeeSplit, 0, 3);
            employeeSplit.Paint += (sender, args) =>
            {
                if (employeeSplit.Panel2Collapsed) return;
                var divider = employeeSplit.SplitterRectangle;
                using (var pen = new Pen(Color.FromArgb(145, 163, 184), 2))
                    args.Graphics.DrawLine(pen, divider.Left + divider.Width / 2 - 18, divider.Top + divider.Height / 2,
                        divider.Left + divider.Width / 2 + 18, divider.Top + divider.Height / 2);
            };
            new ToolTip(components).SetToolTip(employeeSplit, "Drag the divider to resize employee results and activity.");
            activityPanel = new TableLayoutPanel
            {
                Name = "employeeActivity", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
                Margin = new Padding(0), Visible = false
            };
            activityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            activityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            activityPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            activityPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Margin = new Padding(0) };
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            activityHeading = new Label { Name = "activityEmployee", Dock = DockStyle.Fill, AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0) };
            loadActivity = new Button { Name = "loadEmployeeActivity", Text = "Load details", Dock = DockStyle.Fill,
                Margin = new Padding(4, 1, 0, 3), FlatStyle = FlatStyle.Flat, BackColor = Color.White };
            cancelActivity = new Button { Name = "cancelEmployeeActivity", Text = "Cancel", Dock = DockStyle.Fill,
                Margin = new Padding(4, 1, 0, 3), FlatStyle = FlatStyle.Flat, Enabled = false };
            activityDays = new TextBox { Name = "employeeActivityDays", Text = "3", Width = 40,
                Anchor = AnchorStyles.Left, TextAlign = HorizontalAlignment.Center, Margin = new Padding(2, 1, 4, 3),
                AccessibleName = "Number of days (1 to 90)" };
            activityDays.TextChanged += (sender, args) => UpdateActivityEmployee(true);
            loadActivity.Click += async (sender, args) => await LoadEmployeeActivityAsync();
            cancelActivity.Click += (sender, args) => activityRequest?.Cancel();
            heading.Controls.Add(activityHeading, 0, 0);
            heading.Controls.Add(new Label { Text = "Days", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 1, 0);
            heading.Controls.Add(activityDays, 2, 0); heading.Controls.Add(loadActivity, 3, 0);
            heading.Controls.Add(cancelActivity, 4, 0); activityPanel.Controls.Add(heading, 0, 0); activityPanel.SetColumnSpan(heading, 2);
            loginGrid = ActivityGrid("employeeLogins"); callGrid = ActivityGrid("employeeCallCounts");
            loginStatus = ActivityLabel("loginActivityStatus"); callStatus = ActivityLabel("callActivityStatus");
            activityPanel.Controls.Add(ActivityCard("Qadence logins • Eastern", loginGrid, loginStatus), 0, 1);
            activityPanel.Controls.Add(ActivityCard("Qatch daily counts • Eastern", callGrid, callStatus), 1, 1);
            employeeSplit.Panel2.Controls.Add(activityPanel);
            dataGridView1.SelectionChanged += (sender, args) => UpdateActivityEmployee();
            dataGridView1.CurrentCellChanged += (sender, args) => UpdateActivityEmployee();
            dataGridView1.DataBindingComplete += (sender, args) => UpdateActivityEmployee(true);
            FormClosing += (sender, args) => { activityVersion++; activityRequest?.Cancel(); };
        }
        private static Label ActivityLabel(string name) => new Label { Name = name, Dock = DockStyle.Fill,
            Text = "Choose 1–90 days, then Load details.", AutoEllipsis = true,
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
            bool hasSelection = !string.IsNullOrWhiteSpace(selected);
            employeeSplit.Panel2Collapsed = !hasSelection;
            activityPanel.Visible = hasSelection;
            if (hasSelection && !activitySplitInitialized)
            {
                employeeSplit.SplitterDistance = Math.Max(employeeSplit.Panel1MinSize,
                    employeeSplit.Height - employeeSplit.SplitterWidth - Math.Max(230, employeeSplit.Panel2MinSize));
                activitySplitInitialized = true;
            }
            int days;
            bool valid = TryActivityDays(out days);
            activityHeading.Text = valid ? "Previous " + days + " days + today • " + selected : "Activity • " + selected;
            loginStatus.Text = callStatus.Text = valid
                ? "Load details: midnight Eastern " + days + " days ago through now."
                : "Enter a whole number of days from 1 to 90.";
            activityDays.Enabled = true;
            loadActivity.Enabled = hasSelection; cancelActivity.Enabled = false;
        }
        private bool TryActivityDays(out int days)
        {
            bool valid = int.TryParse(activityDays.Text, out days) && days >= 1 && days <= 90;
            activityDays.BackColor = valid ? Color.White : Color.MistyRose;
            return valid;
        }
        internal async Task LoadEmployeeActivityAsync()
        {
            string domain = activityDomain;
            if (string.IsNullOrWhiteSpace(domain) || activityRequest != null) return;
            int days;
            if (!TryActivityDays(out days))
            {
                loginStatus.Text = callStatus.Text = "Enter a whole number of days from 1 to 90.";
                activityDays.Focus(); return;
            }
            long version = activityVersion;
            using (var request = new CancellationTokenSource())
            {
                activityRequest = request; loadActivity.Enabled = false; cancelActivity.Enabled = true; activityDays.Enabled = false;
                var window = new ActivityWindow(DateTime.UtcNow, days);
                activityHeading.Text = domain + " • " + window.FromEastern.ToString("yyyy-MM-dd HH:mm") +
                    " → " + window.ThroughEastern.ToString("yyyy-MM-dd HH:mm") + " ET";
                loginGrid.DataSource = null; callGrid.DataSource = null;
                loginStatus.Text = callStatus.Text = "Loading…";
                await Task.WhenAll(LoadActivityPart(true, domain, window, version, request.Token),
                    LoadActivityPart(false, domain, window, version, request.Token));
                if (!IsDisposed && version == activityVersion && activityRequest == request)
                { activityRequest = null; loadActivity.Enabled = true; cancelActivity.Enabled = false; activityDays.Enabled = true; }
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
                    loginGrid.DataSource = ActivityQueries.EasternLogins(table);
                    foreach (DataGridViewColumn column in loginGrid.Columns)
                        if (column.ValueType == typeof(DateTime)) column.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
                    loginStatus.Text = partial ? "Showing newest 200 sessions; additional sessions exist." : table.Rows.Count + " logins in this window (Eastern).";
                }
                else
                {
                    callGrid.DataSource = ActivityQueries.FillCallDays(table, window);
                    callGrid.Columns[0].DefaultCellStyle.Format = "yyyy-MM-dd";
                    callStatus.Text = "Excl. video uses CallId <> 99999. Today is partial.";
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
