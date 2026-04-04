namespace EmpUtils
{
    partial class Logging
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.txtDomainid = new System.Windows.Forms.TextBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Loggin = new System.Windows.Forms.Label();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.employeeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startDate = new System.Windows.Forms.DateTimePicker();
            this.endDate = new System.Windows.Forms.DateTimePicker();
            this.label4 = new System.Windows.Forms.Label();
            this.txtHostname = new System.Windows.Forms.TextBox();
            this.txtRows = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtApplicationId = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtAppsubCompId = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtAddlCondition = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Location = new System.Drawing.Point(134, 86);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(52, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "DomainId";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1574, 81);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 23);
            this.button1.TabIndex = 12;
            this.button1.Text = "Click to Load";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtMessage
            // 
            this.txtMessage.Location = new System.Drawing.Point(767, 82);
            this.txtMessage.MaxLength = 50;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(350, 20);
            this.txtMessage.TabIndex = 11;
            this.txtMessage.TextChanged += new System.EventHandler(this.txtDept_TextChanged);
            // 
            // txtDomainid
            // 
            this.txtDomainid.Location = new System.Drawing.Point(26, 83);
            this.txtDomainid.MaxLength = 24;
            this.txtDomainid.Name = "txtDomainid";
            this.txtDomainid.Size = new System.Drawing.Size(165, 20);
            this.txtDomainid.TabIndex = 10;
            this.txtDomainid.Text = "arun.selvan";
            this.txtDomainid.TextChanged += new System.EventHandler(this.txtDomainid_TextChanged);
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(17, 111);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(1760, 565);
            this.dataGridView1.TabIndex = 9;
            this.dataGridView1.RowPrePaint += new System.Windows.Forms.DataGridViewRowPrePaintEventHandler(this.dataGridView1_RowPrePaint);
            // 
            // Loggin
            // 
            this.Loggin.AutoSize = true;
            this.Loggin.Font = new System.Drawing.Font("Microsoft Sans Serif", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Loggin.Location = new System.Drawing.Point(775, 24);
            this.Loggin.Name = "Loggin";
            this.Loggin.Size = new System.Drawing.Size(159, 42);
            this.Loggin.TabIndex = 8;
            this.Loggin.Text = "Logging";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.employeeToolStripMenuItem,
            this.resourceToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(127, 48);
            // 
            // employeeToolStripMenuItem
            // 
            this.employeeToolStripMenuItem.Name = "employeeToolStripMenuItem";
            this.employeeToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.employeeToolStripMenuItem.Text = "Employee";
            // 
            // resourceToolStripMenuItem
            // 
            this.resourceToolStripMenuItem.Name = "resourceToolStripMenuItem";
            this.resourceToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.resourceToolStripMenuItem.Text = "Resource";
            // 
            // startDate
            // 
            this.startDate.Location = new System.Drawing.Point(193, 83);
            this.startDate.Name = "startDate";
            this.startDate.Size = new System.Drawing.Size(200, 20);
            this.startDate.TabIndex = 15;
            // 
            // endDate
            // 
            this.endDate.Location = new System.Drawing.Point(399, 83);
            this.endDate.Name = "endDate";
            this.endDate.Size = new System.Drawing.Size(200, 20);
            this.endDate.TabIndex = 16;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(706, 86);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(55, 13);
            this.label4.TabIndex = 18;
            this.label4.Text = "Hostname";
            // 
            // txtHostname
            // 
            this.txtHostname.Location = new System.Drawing.Point(606, 82);
            this.txtHostname.MaxLength = 50;
            this.txtHostname.Name = "txtHostname";
            this.txtHostname.Size = new System.Drawing.Size(158, 20);
            this.txtHostname.TabIndex = 17;
            // 
            // txtRows
            // 
            this.txtRows.Location = new System.Drawing.Point(1543, 83);
            this.txtRows.MaxLength = 3;
            this.txtRows.Name = "txtRows";
            this.txtRows.Size = new System.Drawing.Size(26, 20);
            this.txtRows.TabIndex = 19;
            this.txtRows.Text = "200";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(1677, 81);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 23);
            this.button2.TabIndex = 20;
            this.button2.Text = "Show Error Logs";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(1063, 86);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(50, 13);
            this.label5.TabIndex = 22;
            this.label5.Text = "Message";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1165, 87);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "AppId";
            // 
            // txtApplicationId
            // 
            this.txtApplicationId.Location = new System.Drawing.Point(1123, 83);
            this.txtApplicationId.MaxLength = 4;
            this.txtApplicationId.Name = "txtApplicationId";
            this.txtApplicationId.Size = new System.Drawing.Size(80, 20);
            this.txtApplicationId.TabIndex = 23;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(1255, 87);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 13);
            this.label3.TabIndex = 26;
            this.label3.Text = "AppSubComId";
            // 
            // txtAppsubCompId
            // 
            this.txtAppsubCompId.Location = new System.Drawing.Point(1209, 83);
            this.txtAppsubCompId.MaxLength = 4;
            this.txtAppsubCompId.Name = "txtAppsubCompId";
            this.txtAppsubCompId.Size = new System.Drawing.Size(126, 20);
            this.txtAppsubCompId.TabIndex = 25;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(1483, 88);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(51, 13);
            this.label6.TabIndex = 28;
            this.label6.Text = "Condition";
            // 
            // txtAddlCondition
            // 
            this.txtAddlCondition.Location = new System.Drawing.Point(1336, 84);
            this.txtAddlCondition.MaxLength = 4;
            this.txtAddlCondition.Name = "txtAddlCondition";
            this.txtAddlCondition.Size = new System.Drawing.Size(201, 20);
            this.txtAddlCondition.TabIndex = 27;
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(14, 677);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(47, 13);
            this.lblStatus.TabIndex = 29;
            this.lblStatus.Text = "Ready...";
            // 
            // Logging
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1794, 691);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtAddlCondition);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtAppsubCompId);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtApplicationId);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.txtRows);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtHostname);
            this.Controls.Add(this.endDate);
            this.Controls.Add(this.startDate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.txtDomainid);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.Loggin);
            this.Name = "Logging";
            this.Text = "Qadence";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Qatch_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.TextBox txtDomainid;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label Loggin;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem employeeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resourceToolStripMenuItem;
        private System.Windows.Forms.DateTimePicker startDate;
        private System.Windows.Forms.DateTimePicker endDate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtHostname;
        private System.Windows.Forms.TextBox txtRows;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtApplicationId;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtAppsubCompId;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtAddlCondition;
        private System.Windows.Forms.Label lblStatus;
    }
}
