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
        using (var image = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
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
            Assert(query.Sql.Contains("lower(tar.DomainId) = lower(@domain)"), "Exact match must be default.");
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
                table.Rows.Add(DateTime.UtcNow.AddHours(-1), "sample-host");
            }
            else
            {
                table.Columns.Add("CallDateET", typeof(DateTime)); table.Columns.Add("CallCount", typeof(long));
                table.Columns.Add("VideoOnlyCount", typeof(long));
                table.Rows.Add(new ActivityWindow(DateTime.UtcNow).ThroughEastern.Date, 7L, 2L);
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
        Assert((window.ThroughUtc - window.FromUtc).TotalHours == 72, "Window must be exactly 72 UTC hours.");
        Assert(window.FromEastern.Hour == 8 && window.ThroughEastern.Hour == 7, "Eastern bounds must honor DST.");
        var calls = ActivityQueries.Calls("O'Neil", window); var logins = ActivityQueries.Logins("O'Neil", window);
        Assert(!calls.Sql.Contains("O'Neil") && calls.Parameters["domain"].Equals("O'Neil"), "Activity domain must be a parameter.");
        Assert(calls.Sql.Contains("cd.CallDate >= @from") && !calls.Sql.Contains("WHERE cd.CallDate::date"), "Date predicates must remain on the original column.");
        Assert(calls.Parameters["from"].ToString().StartsWith("2026-10-30 08:00"), "Qatch must use Eastern time.");
        Assert(logins.Parameters["from"].ToString().StartsWith("2026-10-30 12:00") && logins.Parameters["from"].ToString().EndsWith("Z"), "Qadence must use UTC.");
        using (var command = calls.CreateCommand(null))
            Assert(command.Parameters.Count == 3 && command.Parameters["domain"].Value.Equals("O'Neil"), "PostgreSQL parameter binding failed.");
        var noCalls = new DataTable(); var days = ActivityQueries.FillCallDays(noCalls, window);
        Assert(days.Rows.Count == 4 && days.Rows.Cast<DataRow>().All(r => Convert.ToInt64(r[1]) == 0), "Rolling 72h should include zero-count boundary dates.");
        var midnight = new ActivityWindow(new DateTime(2026, 11, 6, 5, 0, 0, DateTimeKind.Utc));
        Assert(ActivityQueries.FillCallDays(noCalls, midnight).Rows.Count == 3, "Exclusive midnight end must not add a fourth empty date.");
        var runner = new ActivityRunner();
        using (var form = new Form1(runner))
        {
            Show(form); Assert(!Find<Control>(form, "employeeActivity").Visible, "Activity panel should wait for an employee.");
            var grid = Employees(form);
            Assert(runner.Queries.Count == 0, "Selecting an employee must not start database calls.");
            var load = form.LoadEmployeeActivityAsync(); PumpUntil(() => load.IsCompleted); load.GetAwaiter().GetResult();
            Assert(runner.Queries.Count == 2, "Load details should query exactly two sources.");
            Assert(runner.Queries.All(q => q.Parameters["domain"].Equals("sample.agent")), "Wrong employee domain.");
            Assert(Find<DataGridView>(form, "employeeLogins").Rows.Count == 1, "Login results missing.");
            var callGrid = Find<DataGridView>(form, "employeeCallCounts");
            Assert(callGrid.Rows.Cast<DataGridViewRow>().Sum(r => Convert.ToInt64(r.Cells[1].Value)) == 7, "Call counts lost.");
            Layout(form, "employee-activity");
            grid.CurrentCell = grid.Rows[1].Cells[0]; Application.DoEvents();
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
            grid.CurrentCell = grid.Rows[1].Cells[0]; Application.DoEvents();
            var newLoad = form.LoadEmployeeActivityAsync();
            Assert(runner.Pending.Count == 4, "Switching employees must permit a fresh load.");
            runner.Complete(0); runner.Complete(1); PumpUntil(() => oldLoad.IsCompleted);
            Assert(Find<DataGridView>(form, "employeeLogins").DataSource == null, "Stale results were displayed.");
            runner.Complete(2); runner.Complete(3); PumpUntil(() => newLoad.IsCompleted);
            Assert(Find<DataGridView>(form, "employeeLogins").Rows.Count == 1, "New employee results missing.");
            Assert(Find<Label>(form, "activityEmployee").Text.Contains("sample.other"), "Activity heading is stale.");
        }
        runner = new ActivityRunner { Delay = true };
        using (var form = new Form1(runner))
        {
            Show(form); Employees(form); var load = form.LoadEmployeeActivityAsync();
            Find<Button>(form, "cancelEmployeeActivity").PerformClick();
            runner.Complete(0); runner.Complete(1); PumpUntil(() => load.IsCompleted);
            Assert(Find<Label>(form, "loginActivityStatus").Text.StartsWith("Canceled"), "Canceled results must not remain Loading.");
            Assert(Find<DataGridView>(form, "employeeLogins").DataSource == null, "Canceled results were applied.");
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
    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Pages();
            Activity();
            Console.WriteLine("PASS: " + checks + " assertions. Synthetic data only; no database connections."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
