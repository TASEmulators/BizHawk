namespace BizHawk.Client.EmuHawk.tools.Debugger
{
	partial class BreakpointControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.AddBreakpointButton = new System.Windows.Forms.Button();
			this.BreakpointStatsLabel = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.ToggleButton = new System.Windows.Forms.Button();
			this.RemoveBreakpointButton = new System.Windows.Forms.Button();
			this.DuplicateBreakpointButton = new System.Windows.Forms.Button();
			this.EditBreakpointButton = new System.Windows.Forms.Button();
			this.BreakpointView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// AddBreakpointButton
			// 
			this.AddBreakpointButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddBreakpointButton.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.add;
			this.AddBreakpointButton.Location = new System.Drawing.Point(0, 387);
			this.AddBreakpointButton.Name = "AddBreakpointButton";
			this.AddBreakpointButton.Size = new System.Drawing.Size(23, 23);
			this.AddBreakpointButton.TabIndex = 6;
			this.toolTip1.SetToolTip(this.AddBreakpointButton, "Add a new breakpoint");
			this.AddBreakpointButton.UseVisualStyleBackColor = true;
			this.AddBreakpointButton.Click += new System.EventHandler(this.AddBreakpointButton_Click);
			// 
			// BreakpointStatsLabel
			// 
			this.BreakpointStatsLabel.AutoSize = true;
			this.BreakpointStatsLabel.Location = new System.Drawing.Point(3, 3);
			this.BreakpointStatsLabel.Name = "BreakpointStatsLabel";
			this.BreakpointStatsLabel.Size = new System.Drawing.Size(35, 13);
			this.BreakpointStatsLabel.TabIndex = 8;
			this.BreakpointStatsLabel.Text = "label1";
			// 
			// ToggleButton
			// 
			this.ToggleButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ToggleButton.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Refresh;
			this.ToggleButton.Location = new System.Drawing.Point(311, 387);
			this.ToggleButton.Name = "ToggleButton";
			this.ToggleButton.Size = new System.Drawing.Size(23, 23);
			this.ToggleButton.TabIndex = 9;
			this.toolTip1.SetToolTip(this.ToggleButton, "Toggle the selected breakpoints");
			this.ToggleButton.UseVisualStyleBackColor = true;
			this.ToggleButton.Click += new System.EventHandler(this.ToggleButton_Click);
			// 
			// RemoveBreakpointButton
			// 
			this.RemoveBreakpointButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RemoveBreakpointButton.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Delete;
			this.RemoveBreakpointButton.Location = new System.Drawing.Point(340, 387);
			this.RemoveBreakpointButton.Name = "RemoveBreakpointButton";
			this.RemoveBreakpointButton.Size = new System.Drawing.Size(23, 23);
			this.RemoveBreakpointButton.TabIndex = 7;
			this.toolTip1.SetToolTip(this.RemoveBreakpointButton, "Remove selected breakpoints");
			this.RemoveBreakpointButton.UseVisualStyleBackColor = true;
			this.RemoveBreakpointButton.Click += new System.EventHandler(this.RemoveBreakpointButton_Click);
			// 
			// DuplicateBreakpointButton
			// 
			this.DuplicateBreakpointButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.DuplicateBreakpointButton.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Duplicate;
			this.DuplicateBreakpointButton.Location = new System.Drawing.Point(282, 387);
			this.DuplicateBreakpointButton.Name = "DuplicateBreakpointButton";
			this.DuplicateBreakpointButton.Size = new System.Drawing.Size(23, 23);
			this.DuplicateBreakpointButton.TabIndex = 10;
			this.toolTip1.SetToolTip(this.DuplicateBreakpointButton, "Duplicate the selected breakpoint");
			this.DuplicateBreakpointButton.UseVisualStyleBackColor = true;
			this.DuplicateBreakpointButton.Click += new System.EventHandler(this.DuplicateBreakpointButton_Click);
			// 
			// EditBreakpointButton
			// 
			this.EditBreakpointButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.EditBreakpointButton.BackgroundImage = global::BizHawk.Client.EmuHawk.Properties.Resources.pencil;
			this.EditBreakpointButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.EditBreakpointButton.Location = new System.Drawing.Point(253, 387);
			this.EditBreakpointButton.Name = "EditBreakpointButton";
			this.EditBreakpointButton.Size = new System.Drawing.Size(23, 23);
			this.EditBreakpointButton.TabIndex = 11;
			this.toolTip1.SetToolTip(this.EditBreakpointButton, "Edit the selected breakpoint");
			this.EditBreakpointButton.UseVisualStyleBackColor = true;
			this.EditBreakpointButton.Click += new System.EventHandler(this.EditBreakpointButton_Click);
			// 
			// BreakpointView
			// 
			this.BreakpointView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.BreakpointView.BlazingFast = false;
			this.BreakpointView.CheckBoxes = true;
			this.BreakpointView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1,
			this.columnHeader4,
			this.columnHeader2,
			this.columnHeader3});
			this.BreakpointView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.BreakpointView.FullRowSelect = true;
			this.BreakpointView.GridLines = true;
			this.BreakpointView.HideSelection = false;
			this.BreakpointView.ItemCount = 0;
			this.BreakpointView.Location = new System.Drawing.Point(0, 19);
			this.BreakpointView.Name = "BreakpointView";
			this.BreakpointView.SelectAllInProgress = false;
			this.BreakpointView.selectedItem = -1;
			this.BreakpointView.Size = new System.Drawing.Size(366, 365);
			this.BreakpointView.TabIndex = 5;
			this.BreakpointView.TabStop = false;
			this.BreakpointView.UseCompatibleStateImageBehavior = false;
			this.BreakpointView.UseCustomBackground = true;
			this.BreakpointView.View = System.Windows.Forms.View.Details;
			this.BreakpointView.ItemActivate += new System.EventHandler(this.BreakpointView_ItemActivate);
			this.BreakpointView.SelectedIndexChanged += new System.EventHandler(this.BreakpointView_SelectedIndexChanged);
			this.BreakpointView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BreakpointView_KeyDown);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Address";
			this.columnHeader1.Width = 85;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Type";
			this.columnHeader2.Width = 103;
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "Name";
			this.columnHeader3.Width = 80;
			// 
			// columnHeader4
			// 
			this.columnHeader4.Text = "Mask";
			this.columnHeader4.Width = 91;
			// 
			// BreakpointControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.EditBreakpointButton);
			this.Controls.Add(this.DuplicateBreakpointButton);
			this.Controls.Add(this.ToggleButton);
			this.Controls.Add(this.BreakpointStatsLabel);
			this.Controls.Add(this.RemoveBreakpointButton);
			this.Controls.Add(this.AddBreakpointButton);
			this.Controls.Add(this.BreakpointView);
			this.Name = "BreakpointControl";
			this.Size = new System.Drawing.Size(366, 413);
			this.Load += new System.EventHandler(this.BreakpointControl_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private VirtualListView BreakpointView;
		public System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button AddBreakpointButton;
		private System.Windows.Forms.Button RemoveBreakpointButton;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.Label BreakpointStatsLabel;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button ToggleButton;
		private System.Windows.Forms.Button DuplicateBreakpointButton;
		private System.Windows.Forms.Button EditBreakpointButton;
		private System.Windows.Forms.ColumnHeader columnHeader4;
	}
}
