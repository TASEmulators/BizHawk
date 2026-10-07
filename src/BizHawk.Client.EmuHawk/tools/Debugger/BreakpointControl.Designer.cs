namespace BizHawk.Client.EmuHawk
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BreakpointControl));
            this.AddBreakpointButton = new System.Windows.Forms.Button();
            this.BreakpointStatsLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.ToggleButton = new System.Windows.Forms.Button();
            this.RemoveBreakpointButton = new System.Windows.Forms.Button();
            this.DuplicateBreakpointButton = new System.Windows.Forms.Button();
            this.EditBreakpointButton = new System.Windows.Forms.Button();
            this.BreakpointView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // AddBreakpointButton
            // 
            resources.ApplyResources(this.AddBreakpointButton, "AddBreakpointButton");
            this.AddBreakpointButton.Name = "AddBreakpointButton";
            this.toolTip1.SetToolTip(this.AddBreakpointButton, resources.GetString("AddBreakpointButton.ToolTip"));
            this.AddBreakpointButton.UseVisualStyleBackColor = true;
            this.AddBreakpointButton.Click += new System.EventHandler(this.AddBreakpointButton_Click);
            // 
            // BreakpointStatsLabel
            // 
            resources.ApplyResources(this.BreakpointStatsLabel, "BreakpointStatsLabel");
            this.BreakpointStatsLabel.Name = "BreakpointStatsLabel";
            this.toolTip1.SetToolTip(this.BreakpointStatsLabel, resources.GetString("BreakpointStatsLabel.ToolTip"));
            // 
            // ToggleButton
            // 
            resources.ApplyResources(this.ToggleButton, "ToggleButton");
            this.ToggleButton.Name = "ToggleButton";
            this.toolTip1.SetToolTip(this.ToggleButton, resources.GetString("ToggleButton.ToolTip"));
            this.ToggleButton.UseVisualStyleBackColor = true;
            this.ToggleButton.Click += new System.EventHandler(this.ToggleButton_Click);
            // 
            // RemoveBreakpointButton
            // 
            resources.ApplyResources(this.RemoveBreakpointButton, "RemoveBreakpointButton");
            this.RemoveBreakpointButton.Name = "RemoveBreakpointButton";
            this.toolTip1.SetToolTip(this.RemoveBreakpointButton, resources.GetString("RemoveBreakpointButton.ToolTip"));
            this.RemoveBreakpointButton.UseVisualStyleBackColor = true;
            this.RemoveBreakpointButton.Click += new System.EventHandler(this.RemoveBreakpointButton_Click);
            // 
            // DuplicateBreakpointButton
            // 
            resources.ApplyResources(this.DuplicateBreakpointButton, "DuplicateBreakpointButton");
            this.DuplicateBreakpointButton.Name = "DuplicateBreakpointButton";
            this.toolTip1.SetToolTip(this.DuplicateBreakpointButton, resources.GetString("DuplicateBreakpointButton.ToolTip"));
            this.DuplicateBreakpointButton.UseVisualStyleBackColor = true;
            this.DuplicateBreakpointButton.Click += new System.EventHandler(this.DuplicateBreakpointButton_Click);
            // 
            // EditBreakpointButton
            // 
            resources.ApplyResources(this.EditBreakpointButton, "EditBreakpointButton");
            this.EditBreakpointButton.Name = "EditBreakpointButton";
            this.toolTip1.SetToolTip(this.EditBreakpointButton, resources.GetString("EditBreakpointButton.ToolTip"));
            this.EditBreakpointButton.UseVisualStyleBackColor = true;
            this.EditBreakpointButton.Click += new System.EventHandler(this.EditBreakpointButton_Click);
            // 
            // BreakpointView
            // 
            resources.ApplyResources(this.BreakpointView, "BreakpointView");
            this.BreakpointView.CheckBoxes = true;
            this.BreakpointView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader4,
            this.columnHeader2,
            this.columnHeader3});
            this.BreakpointView.FullRowSelect = true;
            this.BreakpointView.GridLines = true;
            this.BreakpointView.HideSelection = false;
            this.BreakpointView.Name = "BreakpointView";
            this.BreakpointView.TabStop = false;
            this.toolTip1.SetToolTip(this.BreakpointView, resources.GetString("BreakpointView.ToolTip"));
            this.BreakpointView.UseCompatibleStateImageBehavior = false;
            this.BreakpointView.View = System.Windows.Forms.View.Details;
            this.BreakpointView.ItemActivate += new System.EventHandler(this.BreakpointView_ItemActivate);
            this.BreakpointView.SelectedIndexChanged += new System.EventHandler(this.BreakpointView_SelectedIndexChanged);
            this.BreakpointView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BreakpointView_KeyDown);
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // columnHeader4
            // 
            resources.ApplyResources(this.columnHeader4, "columnHeader4");
            // 
            // columnHeader2
            // 
            resources.ApplyResources(this.columnHeader2, "columnHeader2");
            // 
            // columnHeader3
            // 
            resources.ApplyResources(this.columnHeader3, "columnHeader3");
            // 
            // BreakpointControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.EditBreakpointButton);
            this.Controls.Add(this.DuplicateBreakpointButton);
            this.Controls.Add(this.ToggleButton);
            this.Controls.Add(this.BreakpointStatsLabel);
            this.Controls.Add(this.RemoveBreakpointButton);
            this.Controls.Add(this.AddBreakpointButton);
            this.Controls.Add(this.BreakpointView);
            this.Name = "BreakpointControl";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.Load += new System.EventHandler(this.BreakpointControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView BreakpointView;
		public System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button AddBreakpointButton;
		private System.Windows.Forms.Button RemoveBreakpointButton;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private BizHawk.WinForms.Controls.LocLabelEx BreakpointStatsLabel;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button ToggleButton;
		private System.Windows.Forms.Button DuplicateBreakpointButton;
		private System.Windows.Forms.Button EditBreakpointButton;
		private System.Windows.Forms.ColumnHeader columnHeader4;
	}
}
