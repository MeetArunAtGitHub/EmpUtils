using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EmpUtils;

internal static class Program
{
    private static int checks;
    internal static void Assert(bool condition, string description)
    { if (!condition) throw new Exception(description); checks++; }
    internal static T Find<T>(Form form, string name) where T : Control
    { var found = form.Controls.Find(name, true); Assert(found.Length == 1, "Missing control: " + name); return (T)found[0]; }
    internal static void PumpUntil(Func<bool> completed)
    {
        var end = DateTime.UtcNow.AddSeconds(8);
        while (!completed() && DateTime.UtcNow < end) { Application.DoEvents(); Thread.Sleep(5); }
        Assert(completed(), "Async operation timed out.");
    }
    private sealed class FakeRunner : IQueryRunner
    {
        internal int Calls;
        internal QuerySpec Last;
        internal bool Slow;
        public Task<DataTable> ExecuteAsync(QuerySpec query, CancellationToken token)
        {
            Calls++; Last = query;
            if (Slow)
            {
                var pending = new TaskCompletionSource<DataTable>();
                token.Register(() => pending.TrySetCanceled()); return pending.Task;
            }
            var table = new DataTable(); table.Columns.Add("DomainId"); table.Columns.Add("Message");
            table.Rows.Add("sample.agent", "Synthetic validation row"); return Task.FromResult(table);
        }
    }
    private static void Show(Form form)
    {
        form.WindowState = FormWindowState.Normal;
        form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-30000, -30000); form.Show(); Application.DoEvents();
    }
    private static void Layout(Form form, string file)
    {
        foreach (int width in new[] { 1020, 1500 })
        {
            form.ClientSize = new Size(width, 760); form.PerformLayout(); Application.DoEvents();
            CheckBounds(form, form);
        }
        string folder = Environment.GetEnvironmentVariable("EMPUTILS_SCREENSHOTS");
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder);
        using (var image = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
            image.Save(Path.Combine(folder, file + ".png"), System.Drawing.Imaging.ImageFormat.Png);
        }
    }
    private static void CheckBounds(Form form, Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (!control.Visible) continue;
            if (control is TextBox || control is DateTimePicker || control is ComboBox || control is Button || control is CheckBox)
            {
                var bounds = control.RectangleToScreen(control.ClientRectangle);
                for (Control ancestor = control.Parent; ancestor != null && ancestor != form; ancestor = ancestor.Parent)
                    Assert(ancestor.RectangleToScreen(ancestor.ClientRectangle).Contains(bounds), "Clipped: " + form.Text + "/" + control.Name);
            }
            if (!(control is DataGridView)) CheckBounds(form, control);
        }
    }
    private static void Pages()
    {
        var fake = new FakeRunner();
        using (var form = new Resource(fake))
        {
            Show(form);
            Find<TextBox>(form, "txtDomainid").Text = "agent' OR 1=1 --";
            Find<TextBox>(form, "txtResourceSecurityGroup").Text = "group_%";
            Application.DoEvents(); Assert(fake.Calls == 0, "Typing must not query Resources.");
            var query = form.BuildQuery();
            Assert(!query.Sql.Contains("agent'"), "Resource values must be parameterized.");
            Assert(query.Sql.Contains("lower(te.DomainId) = lower(@domain)"), "Filter the employee side first so mapping DomainId can use its index.");
            Assert(query.Sql.Contains("tar.domainid = te.domainid"), "Employee-side filtering requires the equality join.");
            Assert(query.Parameters["group"].Equals("group~_~%%"), "Prefix metacharacters must be literal.");
            Find<Button>(form, "button1").PerformClick();
            PumpUntil(() => fake.Calls == 1 && Find<DataGridView>(form, "dataGridView1").Rows.Count == 1);
            Layout(form, "resources");
            fake.Slow = true; Find<Button>(form, "button1").PerformClick();
            Assert(Find<Button>(form, "cancelQuery").Enabled, "Cancel must stay usable.");
            Find<Button>(form, "cancelQuery").PerformClick();
            PumpUntil(() => Find<Button>(form, "button1").Enabled);
            Assert(Find<Label>(form, "lblStatus").Text.Contains("Canceled"), "Cancellation state missing.");
        }
        using (var form = new Qadence(new FakeRunner()))
        {
            Show(form); Find<TextBox>(form, "txtDomainid").Text = "O'Neil";
            var detail = form.BuildQuery(false); var distinct = form.BuildQuery(true);
            Assert(detail.ConnectionName == "Qadence" && !detail.Sql.Contains("O'Neil"), "Qadence must use parameters.");
            Assert(distinct.Sql.Contains("DISTINCT employee.domainid") && distinct.Parameters.ContainsKey("domain"), "Distinct query missing filters.");
            Find<TextBox>(form, "txtRows").Text = "1001";
            bool rejected = false; try { form.BuildQuery(false); } catch (ArgumentException) { rejected = true; }
            Assert(rejected, "Invalid row count must be rejected."); Find<TextBox>(form, "txtRows").Text = "200";
            Layout(form, "qadence");
        }
        using (var form = new Logging(new FakeRunner()))
        {
            Show(form); Find<TextBox>(form, "txtApplicationId").Text = "1, 2";
            Find<TextBox>(form, "txtAppsubCompId").Text = "3,4";
            Find<TextBox>(form, "txtMessage").Text = "50% [test]_";
            var query = form.BuildQuery(true);
            Assert(query.SqlServer && query.Parameters["app1"].Equals(2), "Logging IDs must be separate typed parameters.");
            Assert(query.Sql.Contains("tl.LogLevel = 3") && query.Sql.Contains("GROUP BY"), "Error summary changed.");
            Assert(!query.Sql.Contains("50%"), "Message should not be interpolated.");
            Layout(form, "logging");
        }
    }
    private sealed class ActivityRunner : IQueryRunner
    {
        internal readonly List<QuerySpec> Queries = new List<QuerySpec>();
        internal readonly List<TaskCompletionSource<DataTable>> Pending = new List<TaskCompletionSource<DataTable>>();
        internal bool Delay;
        internal bool PartialLogins;
        internal string FailureConnection;
        public Task<DataTable> ExecuteAsync(QuerySpec query, CancellationToken token)
        {
            Queries.Add(query);
            if (Delay)
            {
                // Intentionally ignore cancellation to verify stale-result protection.
                var pending = new TaskCompletionSource<DataTable>(); Pending.Add(pending); return pending.Task;
            }
            if (query.ConnectionName == FailureConnection) return Task.FromException<DataTable>(new Exception("Synthetic unavailable source"));
            var result = Result(query);
            if (PartialLogins && query.ConnectionName == "Qadence")
                for (int i = 0; i < 200; i++) result.Rows.Add(DateTime.UtcNow.AddHours(-1), "sample-host");
            return Task.FromResult(result);
        }
        internal static DataTable Result(QuerySpec query)
        {
            var table = new DataTable();
            if (query.ConnectionName == "Qadence")
            {
                table.Columns.Add("LoginUTC", typeof(DateTime)); table.Columns.Add("Hostname");
                table.Columns.Add("LastHeartbeatUTC", typeof(DateTime)); table.Columns.Add("IPAddress");
                table.Columns.Add("VDIIPAddress"); table.Columns.Add("VDIHostname");
                table.Rows.Add(DateTime.UtcNow.AddHours(-1), "sample-host", DateTime.UtcNow,
                    "10.1.2.3", "10.9.8.7", "sample-vdi");
            }
            else
            {
                table.Columns.Add("CallDateET", typeof(DateTime)); table.Columns.Add("CallCount", typeof(long));
                table.Columns.Add("VideoOnlyCount", typeof(long));
                table.Columns.Add("NonVideoCallCount", typeof(long));
                table.Rows.Add(new ActivityWindow(DateTime.UtcNow).ThroughEastern.Date, 7L, 2L, 5L);
            }
            return table;
        }
        internal void Complete(int index) { Pending[index].SetResult(Result(Queries[index])); }
    }
    private static DataGridView Employees(Form1 form)
    {
        var table = new DataTable(); table.Columns.Add("DomainId"); table.Columns.Add("Status");
        table.Rows.Add("sample.agent", "Active"); table.Rows.Add("sample.other", "Active");
        var grid = Find<DataGridView>(form, "dataGridView1"); grid.DataSource = table;
        grid.CurrentCell = grid.Rows[0].Cells[0]; Application.DoEvents(); return grid;
    }
    private static void Activity()
    {
        var window = new ActivityWindow(new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc));
        Assert(window.FromEastern == new DateTime(2026, 10, 30), "Window must begin at midnight three Eastern dates ago.");
        Assert(window.FromUtc.Hour == 4 && window.ThroughEastern.Hour == 7, "Eastern boundaries must honor DST.");
        var example = new ActivityWindow(new DateTime(2026, 11, 6, 19, 0, 0, DateTimeKind.Utc));
        Assert(example.FromEastern == new DateTime(2026, 11, 3) && example.ThroughEastern == new DateTime(2026, 11, 6, 14, 0, 0), "User's Nov 6 2 PM EST example must match exactly.");
        Assert(example.FromUtc == new DateTime(2026, 11, 3, 5, 0, 0, DateTimeKind.Utc), "Midnight EST must map to 05:00 UTC.");
        foreach (int count in new[] { 1, 90 })
        {
            var custom = new ActivityWindow(example.ThroughUtc, count);
            Assert(custom.FromEastern == example.ThroughEastern.Date.AddDays(-count), "Custom day boundary incorrect.");
            Assert(ActivityQueries.FillCallDays(new DataTable(), custom).Rows.Count == count + 1, "Custom window must include today.");
        }
        foreach (int count in new[] { 0, 91 })
        {
            bool rejected = false;
            try { new ActivityWindow(example.ThroughUtc, count); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Assert(rejected, "Out-of-range activity window must be rejected.");
        }
        var calls = ActivityQueries.Calls("O'Neil", window); var logins = ActivityQueries.Logins("O'Neil", window);
        Assert(!calls.Sql.Contains("O'Neil") && calls.Parameters["domain"].Equals("O'Neil"), "Activity domain must be a parameter.");
        Assert(calls.Sql.Contains("cd.CallDate >= @from") && !calls.Sql.Contains("WHERE cd.CallDate::date"), "Date predicates must remain on the original column.");
        Assert(calls.Parameters["from"].ToString().StartsWith("2026-10-30 00:00"), "Qatch must use Eastern time.");
        Assert(logins.Parameters["from"].ToString().StartsWith("2026-10-30 04:00") && logins.Parameters["from"].ToString().EndsWith("Z"), "Qadence query bounds must use UTC.");
        Assert(logins.Sql.Contains("s.clip") && logins.Sql.Contains("AS IPAddress") && logins.Sql.Contains("AS VDIIPAddress"), "Logins must query separate IP columns.");
        Assert(calls.Sql.Contains("WHERE cd.CallId <> 99999"), "Non-video count must use the requested SQL condition.");
        var rawLogins = ActivityRunner.Result(logins);
        rawLogins.Rows[0]["LoginUTC"] = new DateTime(2026, 11, 6, 14, 0, 0, DateTimeKind.Utc);
        rawLogins.Rows[0]["LastHeartbeatUTC"] = DBNull.Value;
        var easternLogins = ActivityQueries.EasternLogins(rawLogins);
        Assert((DateTime)easternLogins.Rows[0]["Login (ET)"] == new DateTime(2026, 11, 6, 9, 0, 0), "Winter login display must use EST.");
        Assert(easternLogins.Rows[0]["IP address"].Equals("10.1.2.3") && easternLogins.Rows[0]["VDI IP address"].Equals("10.9.8.7"), "IP addresses must remain separate from hostname.");
        Assert(easternLogins.Rows[0].IsNull("Last heartbeat (ET)"), "Missing heartbeat must remain blank.");
        rawLogins.Rows[0]["LoginUTC"] = new DateTime(2026, 7, 6, 14, 0, 0, DateTimeKind.Utc);
        Assert((DateTime)ActivityQueries.EasternLogins(rawLogins).Rows[0]["Login (ET)"] == new DateTime(2026, 7, 6, 10, 0, 0), "Summer login display must honor EDT.");
        using (var command = calls.CreateCommand(null))
            Assert(command.Parameters.Count == 3 && command.Parameters["domain"].Value.Equals("O'Neil"), "PostgreSQL parameter binding failed.");
        var noCalls = new DataTable(); var days = ActivityQueries.FillCallDays(noCalls, window);
        Assert(days.Rows.Count == 4 && days.Rows.Cast<DataRow>().All(r => Convert.ToInt64(r[1]) == 0), "Previous three days plus today should include zero-count dates.");
        var midnight = new ActivityWindow(new DateTime(2026, 11, 6, 5, 0, 0, DateTimeKind.Utc));
        Assert(ActivityQueries.FillCallDays(noCalls, midnight).Rows.Count == 3, "Exclusive midnight end must not add a fourth empty date.");
        var runner = new ActivityRunner();
        using (var form = new Form1(runner))
        {
            Show(form); Assert(!Find<Control>(form, "employeeActivity").Visible, "Activity panel should wait for an employee.");
            var grid = Employees(form);
            Assert(runner.Queries.Count == 0, "Selecting an employee must not start database calls.");
            var daysInput = Find<TextBox>(form, "employeeActivityDays");
            Assert(daysInput.Text == "3", "Days must default to three.");
            foreach (string invalid in new[] { "", "abc", "0", "91", "1.5", "999999999999" })
            {
                daysInput.Text = invalid;
                var invalidLoad = form.LoadEmployeeActivityAsync(); PumpUntil(() => invalidLoad.IsCompleted);
                Assert(runner.Queries.Count == 0, "Invalid days must not query databases.");
                Assert(Find<Label>(form, "loginActivityStatus").Text.Contains("1 to 90"), "Invalid days feedback missing.");
            }
            daysInput.Text = "3";
            var load = form.LoadEmployeeActivityAsync(); PumpUntil(() => load.IsCompleted); load.GetAwaiter().GetResult();
            Assert(runner.Queries.Count == 2, "Load details should query exactly two sources.");
            Assert(runner.Queries.All(q => q.Parameters["domain"].Equals("sample.agent")), "Wrong employee domain.");
            Assert(Find<DataGridView>(form, "employeeLogins").Rows.Count == 1, "Login results missing.");
            var callGrid = Find<DataGridView>(form, "employeeCallCounts");
            Assert(callGrid.Rows.Cast<DataGridViewRow>().Sum(r => Convert.ToInt64(r.Cells[1].Value)) == 7, "Call counts lost.");
            Assert(callGrid.Rows.Cast<DataGridViewRow>().Sum(r => Convert.ToInt64(r.Cells["Calls excl. video"].Value)) == 5, "Non-video counts must have a separate column.");
            Layout(form, "employee-activity");
            daysInput.Text = "90";
            Assert(callGrid.DataSource == null, "Changing days must clear old results.");
            var customLoad = form.LoadEmployeeActivityAsync(); PumpUntil(() => customLoad.IsCompleted);
            var callQuery = runner.Queries.Last(q => q.ConnectionName == "Qatch");
            var loginQuery = runner.Queries.Last(q => q.ConnectionName == "Qadence");
            var throughUtc = DateTime.Parse(loginQuery.Parameters["through"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
            var expectedWindow = new ActivityWindow(throughUtc, 90);
            Assert(callQuery.Parameters["from"].Equals(ActivityQueries.Calls("sample.agent", expectedWindow).Parameters["from"]), "Days must control Qatch bounds.");
            Assert(loginQuery.Parameters["from"].Equals(ActivityQueries.Logins("sample.agent", expectedWindow).Parameters["from"]), "Days must control Qadence bounds.");
            Assert(callGrid.Rows.Count == 91 && daysInput.Enabled, "90-day results or restored input missing.");
            grid.CurrentCell = grid.Rows[1].Cells[0]; Application.DoEvents();
            Assert(daysInput.Text == "90", "Employee switch should retain days.");
            Assert(Find<DataGridView>(form, "employeeLogins").DataSource == null && callGrid.DataSource == null, "Employee switch must clear prior results.");
        }
        runner = new ActivityRunner { FailureConnection = "Qadence" };
        using (var form = new Form1(runner))
        {
            Show(form); Employees(form); var load = form.LoadEmployeeActivityAsync(); PumpUntil(() => load.IsCompleted);
            Assert(Find<Label>(form, "loginActivityStatus").Text.StartsWith("Unavailable"), "Login failure must be explicit.");
            Assert(Find<DataGridView>(form, "employeeCallCounts").Rows.Count > 0, "One failure must not hide the other dataset.");
        }
        runner = new ActivityRunner { Delay = true };
        using (var form = new Form1(runner))
        {
            Show(form); var grid = Employees(form);
            var oldLoad = form.LoadEmployeeActivityAsync();
            Assert(!Find<TextBox>(form, "employeeActivityDays").Enabled, "Days must be disabled while loading.");
            grid.CurrentCell = grid.Rows[1].Cells[0]; Application.DoEvents();
            Find<TextBox>(form, "employeeActivityDays").Text = "1";
            var newLoad = form.LoadEmployeeActivityAsync();
            Assert(runner.Pending.Count == 4, "Switching employees must permit a fresh load.");
            runner.Complete(0); runner.Complete(1); PumpUntil(() => oldLoad.IsCompleted);
            Assert(Find<DataGridView>(form, "employeeLogins").DataSource == null, "Stale results were displayed.");
            runner.Complete(2); runner.Complete(3); PumpUntil(() => newLoad.IsCompleted);
            Assert(Find<DataGridView>(form, "employeeLogins").Rows.Count == 1, "New employee results missing.");
            Assert(Find<Label>(form, "activityEmployee").Text.Contains("sample.other"), "Activity heading is stale.");
            Assert(Find<DataGridView>(form, "employeeCallCounts").Rows.Count == 2, "One-day query should include yesterday and today.");
        }
        runner = new ActivityRunner { Delay = true };
        using (var form = new Form1(runner))
        {
            Show(form); Employees(form); var load = form.LoadEmployeeActivityAsync();
            Find<Button>(form, "cancelEmployeeActivity").PerformClick();
            runner.Complete(0); runner.Complete(1); PumpUntil(() => load.IsCompleted);
            Assert(Find<Label>(form, "loginActivityStatus").Text.StartsWith("Canceled"), "Canceled results must not remain Loading.");
            Assert(Find<DataGridView>(form, "employeeLogins").DataSource == null, "Canceled results were applied.");
            Assert(Find<TextBox>(form, "employeeActivityDays").Enabled, "Days must be restored after cancellation.");
        }
        runner = new ActivityRunner { PartialLogins = true };
        using (var form = new Form1(runner))
        {
            Show(form); Employees(form); var load = form.LoadEmployeeActivityAsync(); PumpUntil(() => load.IsCompleted);
            Assert(Find<DataGridView>(form, "employeeLogins").Rows.Count == 200, "Login preview must be bounded.");
            Assert(Find<Label>(form, "loginActivityStatus").Text.Contains("additional sessions"), "Partial sessions must be labeled.");
        }
        runner = new ActivityRunner { Delay = true };
        using (var form = new Form1(runner))
        {
            Show(form); Employees(form); var load = form.LoadEmployeeActivityAsync(); form.Close();
            runner.Complete(0); runner.Complete(1); PumpUntil(() => load.IsCompleted);
            Assert(form.IsDisposed && !load.IsFaulted, "Closing during activity load must safely discard callbacks.");
        }
    }
    private static void AnalyticsTime()
    {
        using (var form = new Qatch())
        {
            Show(form);
            var from = Find<DateTimePicker>(form, "startDate");
            from.Value = new DateTime(2026, 11, 3, 11, 47, 23);
            Find<DateTimePicker>(form, "endDate").Value = new DateTime(2026, 11, 6, 14, 5, 0);
            Assert(from.CustomFormat.Contains("HH:mm"), "Call Analytics must expose start time.");
            Assert(form.CallDateFilter.Contains("2026-11-03 11:47:23") && form.CallDateFilter.Contains("2026-11-06 14:05:00"), "Analytics filters must preserve the selected times.");
            var getter = typeof(Qatch).GetProperty("CallDateFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetGetMethod(true);
            var expected = BitConverter.GetBytes(getter.MetadataToken);
            foreach (var name in new[] { "button1_Click", "button2_Click", "button3_Click", "button4_Click" })
            {
                var code = typeof(Qatch).GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetMethodBody().GetILAsByteArray();
                bool found = false;
                for (int i = 0; i < code.Length - 4; i++)
                    if (code[i] == 0x28 && code.Skip(i + 1).Take(4).SequenceEqual(expected)) { found = true; break; }
                Assert(found, "Every Analytics view must use the date/time filter: " + name);
            }
            Layout(form, "analytics-time");
        }
    }
    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Pages();
            Activity();
            AnalyticsTime();
            Console.WriteLine("PASS: " + checks + " assertions. Synthetic data only; no database connections."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
