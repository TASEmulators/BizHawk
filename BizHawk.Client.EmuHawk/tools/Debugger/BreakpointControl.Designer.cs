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
			this.AddBreakpointButton = new System.Windows.Forms.Button();
			this.RemoveBreakpointButton = new System.Windows.Forms.Button();
			this.BreakpointView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// AddBreakpointButton
			// 
			this.AddBreakpointButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddBreakpointButton.Location = new System.Drawing.Point(0, 387);
			this.AddBreakpointButton.Name = "AddBreakpointButton";
			this.AddBreakpointButton.Size = new System.Drawing.Size(60, 23);
			this.AddBreakpointButton.TabIndex = 6;
			this.AddBreakpointButton.Text = "&Add";
			this.AddBreakpointButton.UseVisualStyleBackColor = true;
			this.AddBreakpointButton.Click += new System.EventHandler(this.AddBreakpointButton_Click);
			// 
			// RemoveBreakpointButton
			// 
			this.RemoveBreakpointButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RemoveBreakpointButton.Location = new System.Drawing.Point(130, 387);
			this.RemoveBreakpointButton.Name = "RemoveBreakpointButton";
			this.RemoveBreakpointButton.Size = new System.Drawing.Size(60, 23);
			this.RemoveBreakpointButton.TabIndex = 7;
			this.RemoveBreakpointButton.Text = "&Remove";
			this.RemoveBreakpointButton.UseVisualStyleBackColor = true;
			this.RemoveBreakpointButton.Click += new System.EventHandler(this.RemoveBreakpointButton_Click);
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
            this.columnHeader2,
            this.columnHeader3});
			this.BreakpointView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.BreakpointView.FullRowSelect = true;
			this.BreakpointView.GridLines = true;
			this.BreakpointView.HideSelection = false;
			this.BreakpointView.ItemCount = 0;
			this.BreakpointView.Location = new System.Drawing.Point(0, 0);
			this.BreakpointView.Name = "BreakpointView";
			this.BreakpointView.SelectAllInProgress = false;
			this.BreakpointView.selectedItem = -1;
			this.BreakpointView.Size = new System.Drawing.Size(193, 384);
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
			// BreakpointControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.RemoveBreakpointButton);
			this.Controls.Add(this.AddBreakpointButton);
			this.Controls.Add(this.BreakpointView);
			this.Name = "BreakpointControl";
			this.Size = new System.Drawing.Size(193, 413);
			this.Load += new System.EventHandler(this.BreakpointControl_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private VirtualListView BreakpointView;
		public System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button AddBreakpointButton;
		private System.Windows.Forms.Button RemoveBreakpointButton;
		private System.Windows.Forms.ColumnHeader columnHeader3;
	}
}
