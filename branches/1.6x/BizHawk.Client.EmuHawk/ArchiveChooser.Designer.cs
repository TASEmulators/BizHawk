namespace BizHawk.Client.EmuHawk
{
	partial class ArchiveChooser
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
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.lvMembers = new System.Windows.Forms.ListView();
			this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.panel1 = new System.Windows.Forms.Panel();
			this.cbInstantFilter = new System.Windows.Forms.CheckBox();
			this.radRegEx = new System.Windows.Forms.RadioButton();
			this.radSimple = new System.Windows.Forms.RadioButton();
			this.btnFilter = new System.Windows.Forms.Button();
			this.btnSearch = new System.Windows.Forms.Button();
			this.tbFilter = new System.Windows.Forms.TextBox();
			this.tbSearch = new System.Windows.Forms.TextBox();
			this.flowLayoutPanel1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.Controls.Add(this.btnCancel);
			this.flowLayoutPanel1.Controls.Add(this.btnOK);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 317);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(472, 29);
			this.flowLayoutPanel1.TabIndex = 1;
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(394, 3);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 8;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnOK
			// 
			this.btnOK.Location = new System.Drawing.Point(313, 3);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(75, 23);
			this.btnOK.TabIndex = 7;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.lvMembers, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(472, 317);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// lvMembers
			// 
			this.lvMembers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colSize});
			this.lvMembers.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvMembers.FullRowSelect = true;
			this.lvMembers.GridLines = true;
			this.lvMembers.Location = new System.Drawing.Point(3, 3);
			this.lvMembers.MultiSelect = false;
			this.lvMembers.Name = "lvMembers";
			this.lvMembers.Size = new System.Drawing.Size(466, 229);
			this.lvMembers.TabIndex = 0;
			this.lvMembers.UseCompatibleStateImageBehavior = false;
			this.lvMembers.View = System.Windows.Forms.View.Details;
			this.lvMembers.DoubleClick += new System.EventHandler(this.lvMembers_ItemActivate);
			// 
			// colName
			// 
			this.colName.DisplayIndex = 1;
			this.colName.Text = "Name";
			this.colName.Width = 409;
			// 
			// colSize
			// 
			this.colSize.DisplayIndex = 0;
			this.colSize.Text = "Size";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.cbInstantFilter);
			this.panel1.Controls.Add(this.radRegEx);
			this.panel1.Controls.Add(this.radSimple);
			this.panel1.Controls.Add(this.btnFilter);
			this.panel1.Controls.Add(this.btnSearch);
			this.panel1.Controls.Add(this.tbFilter);
			this.panel1.Controls.Add(this.tbSearch);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(3, 238);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(466, 76);
			this.panel1.TabIndex = 4;
			// 
			// cbInstantFilter
			// 
			this.cbInstantFilter.AutoSize = true;
			this.cbInstantFilter.Checked = true;
			this.cbInstantFilter.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbInstantFilter.Location = new System.Drawing.Point(263, 5);
			this.cbInstantFilter.Name = "cbInstantFilter";
			this.cbInstantFilter.Size = new System.Drawing.Size(106, 17);
			this.cbInstantFilter.TabIndex = 3;
			this.cbInstantFilter.Text = "Filter while typing";
			this.cbInstantFilter.UseVisualStyleBackColor = true;
			this.cbInstantFilter.CheckedChanged += new System.EventHandler(this.cbInstantFilter_CheckedChanged);
			// 
			// radRegEx
			// 
			this.radRegEx.AutoSize = true;
			this.radRegEx.Location = new System.Drawing.Point(71, 58);
			this.radRegEx.Name = "radRegEx";
			this.radRegEx.Size = new System.Drawing.Size(116, 17);
			this.radRegEx.TabIndex = 6;
			this.radRegEx.Text = "Regular Expression";
			this.radRegEx.UseVisualStyleBackColor = true;
			this.radRegEx.CheckedChanged += new System.EventHandler(this.radRegEx_CheckedChanged);
			// 
			// radSimple
			// 
			this.radSimple.AutoSize = true;
			this.radSimple.Checked = true;
			this.radSimple.Location = new System.Drawing.Point(9, 57);
			this.radSimple.Name = "radSimple";
			this.radSimple.Size = new System.Drawing.Size(56, 17);
			this.radSimple.TabIndex = 6;
			this.radSimple.TabStop = true;
			this.radSimple.Text = "Simple";
			this.radSimple.UseVisualStyleBackColor = true;
			// 
			// btnFilter
			// 
			this.btnFilter.Location = new System.Drawing.Point(182, 1);
			this.btnFilter.Name = "btnFilter";
			this.btnFilter.Size = new System.Drawing.Size(75, 23);
			this.btnFilter.TabIndex = 2;
			this.btnFilter.Text = "Filter";
			this.btnFilter.UseVisualStyleBackColor = true;
			this.btnFilter.Click += new System.EventHandler(this.btnFilter_Click);
			// 
			// btnSearch
			// 
			this.btnSearch.Location = new System.Drawing.Point(182, 27);
			this.btnSearch.Name = "btnSearch";
			this.btnSearch.Size = new System.Drawing.Size(75, 23);
			this.btnSearch.TabIndex = 5;
			this.btnSearch.Text = "Find";
			this.btnSearch.UseVisualStyleBackColor = true;
			this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
			// 
			// tbFilter
			// 
			this.tbFilter.Location = new System.Drawing.Point(9, 3);
			this.tbFilter.Name = "tbFilter";
			this.tbFilter.Size = new System.Drawing.Size(167, 20);
			this.tbFilter.TabIndex = 1;
			this.tbFilter.TextChanged += new System.EventHandler(this.tbFilter_TextChanged);
			// 
			// tbSearch
			// 
			this.tbSearch.Location = new System.Drawing.Point(9, 29);
			this.tbSearch.Name = "tbSearch";
			this.tbSearch.Size = new System.Drawing.Size(167, 20);
			this.tbSearch.TabIndex = 4;
			// 
			// ArchiveChooser
			// 
			this.AcceptButton = this.btnOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(472, 346);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.flowLayoutPanel1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(112, 138);
			this.Name = "ArchiveChooser";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Choose File From Archive";
			this.Load += new System.EventHandler(this.ArchiveChooser_Load);
			this.flowLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ListView lvMembers;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colSize;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton radRegEx;
        private System.Windows.Forms.RadioButton radSimple;
        private System.Windows.Forms.Button btnFilter;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.TextBox tbFilter;
        private System.Windows.Forms.TextBox tbSearch;
        private System.Windows.Forms.CheckBox cbInstantFilter;
	}
}