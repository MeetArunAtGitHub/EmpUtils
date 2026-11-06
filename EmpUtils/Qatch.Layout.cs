using System;
using System.Drawing;
using System.Windows.Forms;

namespace EmpUtils
{
    public partial class Qatch
    {
        private TextBox txtAni;
        private ComboBox cmbAniMatch;

        private void ConfigureAnalyticsLayout()
        {
            SuspendLayout();
            Controls.Clear();
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(243, 246, 250);
            ForeColor = Color.FromArgb(32, 47, 67);
            MinimumSize = new Size(1020, 600);
            Text = "Call Analytics";

            var root = new TableLayoutPanel
            {
                Name = "analyticsLayout", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                Padding = new Padding(12, 7, 12, 4), BackColor = BackColor
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));

            label1.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            label1.Text = "Call Analytics";
            label1.AutoSize = true;
            label1.Margin = new Padding(0);
            label1.Anchor = AnchorStyles.Left;
            root.Controls.Add(label1, 0, 0);

            var filters = new TableLayoutPanel
            {
                Name = "filtersPanel", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                Padding = new Padding(10, 6, 10, 5), BackColor = Color.White,
                Margin = new Padding(0), CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filters.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            filters.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            var primary = FilterRow(20, 20, 20, 10, 30);
            startDate.Format = DateTimePickerFormat.Custom; startDate.CustomFormat = "yyyy-MM-dd";
            endDate.Format = DateTimePickerFormat.Custom; endDate.CustomFormat = "yyyy-MM-dd HH:mm";
            startDate.TabIndex = 0; endDate.TabIndex = 0; txtDomainid.TabIndex = 0; txtClient.TabIndex = 0;
            txtDomainid.MaxLength = 64;
            txtAni = new TextBox { Name = "txtAni", MaxLength = 80, TabIndex = 0 };
            cmbAniMatch = new ComboBox { Name = "cmbAniMatch", DropDownStyle = ComboBoxStyle.DropDownList, TabIndex = 1 };
            cmbAniMatch.Items.AddRange(new object[] { "Contains", "Exact" }); cmbAniMatch.SelectedIndex = 0;
            var ani = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            ani.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); ani.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            txtAni.Dock = DockStyle.Fill; txtAni.Margin = new Padding(0, 0, 5, 0);
            cmbAniMatch.Dock = DockStyle.Fill; cmbAniMatch.Margin = new Padding(0);
            ani.Controls.Add(txtAni, 0, 0); ani.Controls.Add(cmbAniMatch, 1, 0);
            primary.Controls.Add(FilterField("From date", startDate, 0), 0, 0);
            primary.Controls.Add(FilterField("Through date / time", endDate, 1), 1, 0);
            primary.Controls.Add(FilterField("Domain ID", txtDomainid, 2, label2), 2, 0);
            primary.Controls.Add(FilterField("Client", txtClient, 3, label5), 3, 0);
            primary.Controls.Add(FilterField("ANI / Phone", ani, 4), 4, 0);

            var secondary = FilterRow(20, 35, 45);
            secondary.TabIndex = 1;
            secondary.Controls.Add(FilterField("CallDetail ID (details only)", txtCallDetailId, 0, label3), 0, 0);
            secondary.Controls.Add(FilterField("Audio filename (details only)", txtAudioFile, 1, label4), 1, 0);
            secondary.Controls.Add(FilterField("Additional SQL condition", txtAddlCondition, 2, label6), 2, 0);
            filters.Controls.Add(primary, 0, 0); filters.Controls.Add(secondary, 0, 1);
            root.Controls.Add(filters, 0, 1);

            var toolbar = new TableLayoutPanel { Name = "queryToolbar", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            chkExcludeVideoOnly.Anchor = AnchorStyles.Left;
            chkExcludeVideoOnly.Margin = new Padding(0, 0, 10, 0);
            chkExcludeVideoOnly.TabIndex = 0;
            toolbar.Controls.Add(chkExcludeVideoOnly, 0, 0);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = new Padding(0), Anchor = AnchorStyles.Right, TabIndex = 1 };
            var rowLabel = new Label { Text = "Max rows", AutoSize = true, Margin = new Padding(0, 8, 5, 0) };
            txtRows.Anchor = AnchorStyles.None; txtRows.Width = 48; txtRows.Margin = new Padding(0, 5, 9, 0); txtRows.TabIndex = 0;
            actions.Controls.Add(rowLabel); actions.Controls.Add(txtRows);
            StyleQueryButton(button1, "Call details", true, 1);
            StyleQueryButton(button2, "By date", false, 2);
            StyleQueryButton(button3, "By program", false, 3);
            StyleQueryButton(button4, "By client", false, 4);
            actions.Controls.AddRange(new Control[] { button1, button2, button3, button4 });
            var clear = new Button { Name = "clearFiltersButton", Text = "Reset", Size = new Size(65, 28), FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 3, 0, 0), TabIndex = 5, BackColor = Color.White };
            clear.FlatAppearance.BorderColor = Color.FromArgb(207, 217, 229);
            clear.Click += (sender, args) =>
            {
                txtDomainid.Clear(); txtClient.Clear(); txtAni.Clear(); txtCallDetailId.Clear(); txtAudioFile.Clear(); txtAddlCondition.Clear();
                cmbAniMatch.SelectedIndex = 0; chkExcludeVideoOnly.Checked = false; txtRows.Text = "200";
                startDate.Value = DateTime.Now.AddDays(-1); endDate.Value = DateTime.Now.AddDays(1);
                lblStatus.Text = "Filters reset. Select a view to load results."; txtDomainid.Focus();
            };
            actions.Controls.Add(clear); toolbar.Controls.Add(actions, 1, 0); root.Controls.Add(toolbar, 0, 2);

            dataGridView1.Dock = DockStyle.Fill; dataGridView1.AutoSize = false; dataGridView1.Margin = new Padding(0);
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
            root.Controls.Add(dataGridView1, 0, 3);
            lblStatus.Dock = DockStyle.Fill; lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Margin = new Padding(0); lblStatus.ForeColor = Color.FromArgb(89, 107, 129);
            root.Controls.Add(lblStatus, 0, 4);

            var tips = new ToolTip(components);
            tips.SetToolTip(txtAni, "Searches cd.Phone in every view. Contains matches literal digits/text, including within a +country-code number; no % needed.");
            tips.SetToolTip(cmbAniMatch, "Contains: literal substring. Exact: the complete stored Phone value, including any +country code.");
            tips.SetToolTip(txtCallDetailId, "Call details only. A CallDetail ID overrides date, Domain ID, Client, audio filename, and SQL condition. ANI and video exclusion still apply.");
            tips.SetToolTip(txtAudioFile, "Exact audio filename; applied to Call details unless a CallDetail ID is supplied.");
            tips.SetToolTip(txtAddlCondition, "Optional SQL expression, without WHERE or AND. Call details uses cd/crd aliases; summaries use cd only.");
            tips.SetToolTip(chkExcludeVideoOnly, "Applies cd.CallId <> 99999 to all four views.");
            Controls.Add(root); AcceptButton = button1;
            ResumeLayout(true);
        }

        private static TableLayoutPanel FilterRow(params float[] percentages)
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = percentages.Length, RowCount = 1, Margin = new Padding(0) };
            foreach (var percent in percentages) row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percent));
            return row;
        }

        private static Control FilterField(string caption, Control input, int tabIndex, Label label = null)
        {
            var field = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 1, 10, 3), TabIndex = tabIndex };
            field.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); field.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            label = label ?? new Label(); label.Text = caption; label.AutoSize = true; label.Margin = new Padding(0); label.Anchor = AnchorStyles.Left;
            label.ForeColor = Color.FromArgb(83, 101, 124); label.TabStop = false;
            input.Dock = DockStyle.Fill; input.Margin = new Padding(0);
            field.Controls.Add(label, 0, 0); field.Controls.Add(input, 0, 1); return field;
        }

        private void StyleQueryButton(Button button, string caption, bool primary, int tabIndex)
        {
            button.Text = caption; button.Size = new Size(primary ? 110 : 94, 28); button.Margin = new Padding(0, 3, 5, 0);
            button.Anchor = AnchorStyles.None; button.TabIndex = tabIndex; button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.BackColor = primary ? Color.FromArgb(35, 91, 170) : Color.White;
            button.ForeColor = primary ? Color.White : ForeColor;
            button.FlatAppearance.BorderColor = primary ? button.BackColor : Color.FromArgb(207, 217, 229);
        }
    }
}
