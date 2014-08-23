namespace BizHawk.Client.EmuHawk
{
	partial class TasStudioExperiment
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
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.label1 = new System.Windows.Forms.Label();
			this.CurrentCellLabel = new System.Windows.Forms.Label();
			this.InputView = new BizHawk.Client.EmuHawk.InputRoll();
			this.OutputLabel = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.ScrollBarValueTable = new System.Windows.Forms.Label();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(404, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// settingsToolStripMenuItem
			// 
			this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoloadToolStripMenuItem});
			this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.settingsToolStripMenuItem.Text = "&Settings";
			this.settingsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.settingsToolStripMenuItem_DropDownOpened);
			// 
			// autoloadToolStripMenuItem
			// 
			this.autoloadToolStripMenuItem.Name = "autoloadToolStripMenuItem";
			this.autoloadToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.autoloadToolStripMenuItem.Text = "Autoload";
			this.autoloadToolStripMenuItem.Click += new System.EventHandler(this.autoloadToolStripMenuItem_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 87);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(67, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Current Cell: ";
			// 
			// CurrentCellLabel
			// 
			this.CurrentCellLabel.AutoSize = true;
			this.CurrentCellLabel.Location = new System.Drawing.Point(85, 87);
			this.CurrentCellLabel.Name = "CurrentCellLabel";
			this.CurrentCellLabel.Size = new System.Drawing.Size(35, 13);
			this.CurrentCellLabel.TabIndex = 3;
			this.CurrentCellLabel.Text = "label2";
			// 
			// InputView
			// 
			this.InputView.AllowColumnReorder = false;
			this.InputView.AllowColumnResize = false;
			this.InputView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.InputView.Font = new System.Drawing.Font("Courier New", 8F);
			this.InputView.HorizontalOrientation = false;
			this.InputView.ItemCount = 0;
			this.InputView.Location = new System.Drawing.Point(12, 103);
			this.InputView.MultiSelect = false;
			this.InputView.Name = "InputView";
			this.InputView.Size = new System.Drawing.Size(380, 303);
			this.InputView.TabIndex = 1;
			this.InputView.Text = "inputRoll1";
			this.InputView.VirtualMode = false;
			this.InputView.PointedCellChanged += new BizHawk.Client.EmuHawk.InputRoll.CellChangeEventHandler(this.InputView_PointedCellChanged);
			this.InputView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.InputView_ColumnClick);
			this.InputView.SelectedIndexChanged += new System.EventHandler(this.InputView_SelectedIndexChanged);
			// 
			// OutputLabel
			// 
			this.OutputLabel.AutoSize = true;
			this.OutputLabel.Location = new System.Drawing.Point(12, 51);
			this.OutputLabel.Name = "OutputLabel";
			this.OutputLabel.Size = new System.Drawing.Size(35, 13);
			this.OutputLabel.TabIndex = 4;
			this.OutputLabel.Text = "label2";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 28);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(76, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Scroll Position:";
			// 
			// ScrollBarValueTable
			// 
			this.ScrollBarValueTable.AutoSize = true;
			this.ScrollBarValueTable.Location = new System.Drawing.Point(94, 28);
			this.ScrollBarValueTable.Name = "ScrollBarValueTable";
			this.ScrollBarValueTable.Size = new System.Drawing.Size(76, 13);
			this.ScrollBarValueTable.TabIndex = 6;
			this.ScrollBarValueTable.Text = "Scroll Position:";
			// 
			// TasStudioExperiment
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(404, 418);
			this.Controls.Add(this.ScrollBarValueTable);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.OutputLabel);
			this.Controls.Add(this.CurrentCellLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.InputView);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "TasStudioExperiment";
			this.Text = "TasStudioExperiment";
			this.Load += new System.EventHandler(this.TasStudioExperiment_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
		private InputRoll InputView;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label CurrentCellLabel;
		private System.Windows.Forms.Label OutputLabel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label ScrollBarValueTable;
	}
}