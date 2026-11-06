using System;
using System.Data;
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
    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Pages();
            Console.WriteLine("PASS: " + checks + " assertions. Synthetic data only; no database connections."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
