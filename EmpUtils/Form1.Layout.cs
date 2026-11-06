using System.Drawing;
using System.Windows.Forms;

namespace EmpUtils
{
    public partial class Form1
    {
        private void ConfigureHomeLayout()
        {
            SuspendLayout();
            // These tools open as dialogs; the unused MDI surface obscures the home layout.
            IsMdiContainer = false;
            Controls.Clear();
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(243, 246, 250);
            ForeColor = Color.FromArgb(32, 47, 67);
            MinimumSize = new Size(1020, 580);
            Text = "Emp Utilities";

            var root = new TableLayoutPanel
            {
                Name = "homeLayout", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                Padding = new Padding(14, 8, 14, 4), Margin = new Padding(0)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));

            var header = new TableLayoutPanel
            {
                Name = "homeHeader", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                Margin = new Padding(0), TabIndex = 1
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            label1.Text = "Emp Utilities";
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            label1.Margin = new Padding(0); label1.Anchor = AnchorStyles.Left;
            header.Controls.Add(label1, 0, 0);
            var navigation = new FlowLayoutPanel
            {
                Name = "homeNavigation", Dock = DockStyle.Fill, AutoSize = true,
                WrapContents = false, Margin = new Padding(0), Anchor = AnchorStyles.Right
            };
            StyleHomeButton(button4, "Call Analytics", 118, 0);
            StyleHomeButton(button2, "Resources", 93, 1);
            StyleHomeButton(button3, "Agent info", 92, 2);
            StyleHomeButton(button5, "Qadence", 88, 3);
            StyleHomeButton(button6, "Logging", 84, 4);
            navigation.Controls.AddRange(new Control[] { button4, button2, button3, button5, button6 });
            header.Controls.Add(navigation, 1, 0); root.Controls.Add(header, 0, 0);

            var filters = new TableLayoutPanel
            {
                Name = "employeeFilters", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                Padding = new Padding(12, 8, 12, 9), Margin = new Padding(0),
                BackColor = Color.White, TabIndex = 0
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var lookupTitle = new Label
            {
                Text = "Employee lookup", Font = new Font(Font, FontStyle.Bold),
                AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0)
            };
            filters.Controls.Add(lookupTitle, 0, 0);
            var inputs = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0)
            };
            inputs.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 171));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            inputs.Controls.Add(HomeField("Domain ID starts with", label2, txtDomainid, 0), 0, 0);
            inputs.Controls.Add(HomeField("Manager ID starts with", label3, txtDept, 1), 1, 0);
            chkPrgcode.Text = "Include program codes";
            chkPrgcode.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            chkPrgcode.Margin = new Padding(8, 0, 8, 8); chkPrgcode.TabIndex = 2;
            inputs.Controls.Add(chkPrgcode, 2, 0);
            StyleHomeButton(button1, "Refresh", 100, 3);
            button1.BackColor = Color.FromArgb(35, 91, 170); button1.ForeColor = Color.White;
            button1.FlatAppearance.BorderColor = button1.BackColor;
            button1.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            button1.Margin = new Padding(5, 0, 0, 5);
            inputs.Controls.Add(button1, 3, 0); filters.Controls.Add(inputs, 0, 1);
            root.Controls.Add(filters, 0, 1);

            var resultsHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0)
            };
            resultsHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            resultsHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var countLabel = new Label
            {
                Name = "employeeCount", Text = "Employees", AutoSize = true,
                Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0), Anchor = AnchorStyles.Left
            };
            var limitLabel = new Label
            {
                Text = "Up to 40 results", AutoSize = true, Margin = new Padding(0),
                ForeColor = Color.FromArgb(89, 107, 129), Anchor = AnchorStyles.Right
            };
            resultsHeader.Controls.Add(countLabel, 0, 0); resultsHeader.Controls.Add(limitLabel, 1, 0);
            root.Controls.Add(resultsHeader, 0, 2);

            var resultPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0),
                ColumnCount = 1, RowCount = 2
            };
            resultPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            resultPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            resultPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            dataGridView1.Dock = DockStyle.Fill; dataGridView1.AutoSize = false;
            dataGridView1.BackgroundColor = Color.White; dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.EnableHeadersVisualStyles = false; dataGridView1.ColumnHeadersHeight = 30;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(229, 236, 245);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(215, 231, 251);
            dataGridView1.DefaultCellStyle.SelectionForeColor = ForeColor;
            dataGridView1.GridColor = Color.FromArgb(231, 236, 243);
            dataGridView1.ReadOnly = true; dataGridView1.AllowUserToAddRows = false;
            dataGridView1.RowHeadersWidth = 28;
            var emptyState = new Label
            {
                Name = "employeeEmptyState", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true, Padding = new Padding(12, 9, 12, 9), Margin = new Padding(0),
                Text = "Search by Domain ID or manager to load employees.",
                ForeColor = Color.FromArgb(111, 126, 145), BackColor = Color.White
            };
            resultPanel.Controls.Add(emptyState, 0, 0); resultPanel.Controls.Add(dataGridView1, 0, 1);
            dataGridView1.DataBindingComplete += (sender, args) =>
            {
                int rows = dataGridView1.Rows.Count;
                countLabel.Text = rows == 1 ? "1 employee" : rows + " employees";
                emptyState.Text = "No employees match this lookup.";
                emptyState.Visible = rows == 0;
            };
            root.Controls.Add(resultPanel, 0, 3);
            lblStatus.Dock = DockStyle.Fill; lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.ForeColor = Color.FromArgb(89, 107, 129); lblStatus.Margin = new Padding(0);
            lblStatus.Text = "Ready. Type in either field to search; Refresh repeats the last lookup.";
            root.Controls.Add(lblStatus, 0, 4);

            var tips = new ToolTip(components);
            tips.SetToolTip(txtDomainid, "Starts-with lookup after typing pauses for 500 ms. Enter searches immediately.");
            tips.SetToolTip(txtDept, "Manager starts-with lookup after typing pauses for 500 ms. Enter searches immediately.");
            tips.SetToolTip(button1, "Search the last edited employee filter immediately, with the current program-code option.");
            tips.SetToolTip(chkPrgcode, "Include skill-group/program-code information on the next lookup or refresh.");
            Controls.Add(root); AcceptButton = button1;
            Shown += (sender, args) => txtDomainid.Focus();
            ResumeLayout(true);
        }

        private static Control HomeField(string caption, Label label, TextBox input, int tabIndex)
        {
            var field = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                Margin = new Padding(0, 0, 14, 0), TabIndex = tabIndex
            };
            field.RowStyles.Add(new RowStyle(SizeType.Absolute, 21));
            field.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            label.Text = caption; label.AutoSize = true; label.Margin = new Padding(0);
            label.Anchor = AnchorStyles.Left; label.BackColor = Color.White;
            label.ForeColor = Color.FromArgb(83, 101, 124); label.TabStop = false;
            input.Dock = DockStyle.Fill; input.Margin = new Padding(0, 0, 0, 2); input.TabIndex = 0;
            field.Controls.Add(label, 0, 0); field.Controls.Add(input, 0, 1); return field;
        }

        private void StyleHomeButton(Button button, string caption, int width, int tabIndex)
        {
            button.Text = caption; button.Size = new Size(width, 28);
            button.Anchor = AnchorStyles.None; button.Margin = new Padding(5, 7, 0, 0);
            button.TabIndex = tabIndex; button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false; button.BackColor = Color.White; button.ForeColor = ForeColor;
            button.FlatAppearance.BorderColor = Color.FromArgb(207, 217, 229);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(233, 240, 250);
        }
    }
}
