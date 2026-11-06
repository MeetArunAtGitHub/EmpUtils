using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmpUtils
{
    internal sealed class PageField
    {
        internal string Label; internal Control Input;
        internal PageField(string label, Control input) { Label = label; Input = input; }
    }

    internal sealed class PageShell
    {
        private readonly Form form;
        private readonly DataGridView grid;
        private readonly Label status;
        private readonly TableLayoutPanel filters;
        private readonly FlowLayoutPanel actions;
        private readonly Button cancel;
        private readonly Label count;
        private readonly IQueryRunner runner;
        private CancellationTokenSource current;
        private bool closing;

        internal PageShell(Form form, string title, DataGridView grid, Label status,
            PageField[][] rows, Button[] buttons, IQueryRunner runner)
        {
            this.form = form; this.grid = grid; this.status = status; this.runner = runner;
            form.SuspendLayout(); form.Controls.Clear(); form.Text = title;
            form.Font = new Font("Segoe UI", 9F); form.BackColor = Color.FromArgb(243, 246, 250);
            form.ForeColor = Color.FromArgb(32, 47, 67); form.MinimumSize = new Size(1000, 580);
            var root = new TableLayoutPanel
            {
                Name = "pageLayout", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                Padding = new Padding(12, 7, 12, 4)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, rows.Length * 54 + 12));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            root.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font(form.Font.FontFamily, 17F,
                FontStyle.Bold), Anchor = AnchorStyles.Left, Margin = new Padding(0) }, 0, 0);
            filters = new TableLayoutPanel
            {
                Name = "pageFilters", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = rows.Length,
                Padding = new Padding(10, 6, 10, 6), Margin = new Padding(0), BackColor = Color.White
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int r = 0; r < rows.Length; r++)
            {
                filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows.Length));
                var row = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1,
                    ColumnCount = rows[r].Length, Margin = new Padding(0), TabIndex = r };
                row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                for (int c = 0; c < rows[r].Length; c++)
                {
                    row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / rows[r].Length));
                    var field = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1,
                        RowCount = 2, Margin = new Padding(0, 0, 10, 4), TabIndex = c };
                    field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    field.RowStyles.Add(new RowStyle(SizeType.Absolute, 21));
                    field.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    field.Controls.Add(new Label { Text = rows[r][c].Label, AutoSize = true,
                        Anchor = AnchorStyles.Left, Margin = new Padding(0), ForeColor = Color.FromArgb(83, 101, 124) }, 0, 0);
                    var input = rows[r][c].Input; input.Dock = DockStyle.Fill; input.Margin = new Padding(0);
                    if (input is DateTimePicker picker) { picker.Format = DateTimePickerFormat.Custom;
                        picker.CustomFormat = "yyyy-MM-dd HH:mm"; }
                    field.Controls.Add(input, 0, 1); row.Controls.Add(field, c, 0);
                }
                filters.Controls.Add(row, 0, r);
            }
            root.Controls.Add(filters, 0, 1);
            var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2,
                RowCount = 1, Margin = new Padding(0) };
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            count = new Label { Text = "Ready • click Load to query", AutoSize = true,
                Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            toolbar.Controls.Add(count, 0, 0);
            actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false,
                Dock = DockStyle.Fill, Margin = new Padding(0) };
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i]; button.AutoSize = true; button.MinimumSize = new Size(100, 28);
                button.Size = new Size(110, 28); button.Padding = new Padding(8, 0, 8, 0);
                button.Margin = new Padding(5, 4, 0, 0); button.FlatStyle = FlatStyle.Flat;
                button.UseVisualStyleBackColor = false; button.BackColor = i == 0 ? Color.FromArgb(35, 91, 170) : Color.White;
                button.ForeColor = i == 0 ? Color.White : form.ForeColor;
                button.FlatAppearance.BorderColor = i == 0 ? button.BackColor : Color.FromArgb(207, 217, 229);
                actions.Controls.Add(button);
            }
            cancel = new Button { Name = "cancelQuery", Text = "Cancel", Enabled = false,
                Size = new Size(75, 28), Margin = new Padding(5, 4, 0, 0), FlatStyle = FlatStyle.Flat };
            cancel.Click += (sender, args) => current?.Cancel(); actions.Controls.Add(cancel);
            toolbar.Controls.Add(actions, 1, 0); root.Controls.Add(toolbar, 0, 2);
            grid.Dock = DockStyle.Fill; grid.AutoSize = false; grid.Margin = new Padding(0);
            grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.None;
            grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.RowHeadersWidth = 28;
            grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersHeight = 30;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(229, 236, 245);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = form.ForeColor;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(form.Font, FontStyle.Bold);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(215, 231, 251);
            grid.DefaultCellStyle.SelectionForeColor = form.ForeColor;
            grid.GridColor = Color.FromArgb(231, 236, 243);
            root.Controls.Add(grid, 0, 3);
            status.Dock = DockStyle.Fill; status.Margin = new Padding(0); status.TextAlign = ContentAlignment.MiddleLeft;
            status.ForeColor = Color.FromArgb(89, 107, 129); status.Text = "Ready. No query has been run.";
            root.Controls.Add(status, 0, 4); form.Controls.Add(root); form.AcceptButton = buttons[0];
            form.FormClosing += (sender, args) => { if (current != null) { args.Cancel = true; closing = true; current.Cancel(); } };
            form.ResumeLayout(true);
        }

        internal async Task LoadAsync(Func<QuerySpec> build)
        {
            if (current != null) return;
            using (var tokenSource = new CancellationTokenSource())
            {
                current = tokenSource; filters.Enabled = false;
                foreach (Control action in actions.Controls) action.Enabled = action == cancel;
                var watch = Stopwatch.StartNew(); status.Text = "Loading… the window remains responsive.";
                grid.DataSource = null; count.Text = "Loading…";
                try
                {
                    var table = await runner.ExecuteAsync(build(), tokenSource.Token);
                    tokenSource.Token.ThrowIfCancellationRequested();
                    grid.DataSource = table; grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    count.Text = table.Rows.Count + " rows";
                    status.Text = "Loaded " + table.Rows.Count + " rows in " + watch.Elapsed.TotalSeconds.ToString("0.00") + " s";
                }
                catch (Exception ex)
                {
                    count.Text = tokenSource.IsCancellationRequested ? "Canceled" : "Load failed";
                    status.Text = tokenSource.IsCancellationRequested ? "Canceled. Click Load to retry."
                        : "Unable to load: " + ex.Message;
                }
                finally
                {
                    current = null; filters.Enabled = true;
                    foreach (Control action in actions.Controls) action.Enabled = action != cancel;
                    if (closing) form.Close();
                }
            }
        }
    }
}
