namespace BizHawk.Client.EmuHawk
{
	partial class UndoHistoryForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UndoHistoryForm));
            this.ClearButton = new System.Windows.Forms.Button();
            this.UndoButton = new System.Windows.Forms.Button();
            this.RedoButton = new System.Windows.Forms.Button();
            this.RightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.undoHereToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.redoHereToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.sepToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.clearHistoryToHereToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.AutoScrollCheck = new System.Windows.Forms.CheckBox();
            this.MaxStepsNum = new System.Windows.Forms.NumericUpDown();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.HistoryView = new BizHawk.Client.EmuHawk.InputRoll();
            this.RightClickMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MaxStepsNum)).BeginInit();
            this.SuspendLayout();
            // 
            // ClearButton
            // 
            resources.ApplyResources(this.ClearButton, "ClearButton");
            this.ClearButton.Name = "ClearButton";
            this.ClearButton.UseVisualStyleBackColor = true;
            this.ClearButton.Click += new System.EventHandler(this.ClearButton_Click);
            // 
            // UndoButton
            // 
            resources.ApplyResources(this.UndoButton, "UndoButton");
            this.UndoButton.Name = "UndoButton";
            this.UndoButton.UseVisualStyleBackColor = true;
            this.UndoButton.Click += new System.EventHandler(this.UndoButton_Click);
            // 
            // RedoButton
            // 
            resources.ApplyResources(this.RedoButton, "RedoButton");
            this.RedoButton.Name = "RedoButton";
            this.RedoButton.UseVisualStyleBackColor = true;
            this.RedoButton.Click += new System.EventHandler(this.RedoButton_Click);
            // 
            // RightClickMenu
            // 
            resources.ApplyResources(this.RightClickMenu, "RightClickMenu");
            this.RightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoHereToolStripMenuItem,
            this.redoHereToolStripMenuItem,
            this.sepToolStripMenuItem,
            this.clearHistoryToHereToolStripMenuItem});
            this.RightClickMenu.Name = "RightClickMenu";
            // 
            // undoHereToolStripMenuItem
            // 
            resources.ApplyResources(this.undoHereToolStripMenuItem, "undoHereToolStripMenuItem");
            this.undoHereToolStripMenuItem.Click += new System.EventHandler(this.UndoHereMenuItem_Click);
            // 
            // redoHereToolStripMenuItem
            // 
            resources.ApplyResources(this.redoHereToolStripMenuItem, "redoHereToolStripMenuItem");
            this.redoHereToolStripMenuItem.Click += new System.EventHandler(this.RedoHereMenuItem_Click);
            // 
            // sepToolStripMenuItem
            // 
            resources.ApplyResources(this.sepToolStripMenuItem, "sepToolStripMenuItem");
            // 
            // clearHistoryToHereToolStripMenuItem
            // 
            resources.ApplyResources(this.clearHistoryToHereToolStripMenuItem, "clearHistoryToHereToolStripMenuItem");
            this.clearHistoryToHereToolStripMenuItem.Click += new System.EventHandler(this.ClearHistoryToHereMenuItem_Click);
            // 
            // AutoScrollCheck
            // 
            resources.ApplyResources(this.AutoScrollCheck, "AutoScrollCheck");
            this.AutoScrollCheck.Checked = true;
            this.AutoScrollCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AutoScrollCheck.Name = "AutoScrollCheck";
            this.AutoScrollCheck.UseVisualStyleBackColor = true;
            // 
            // MaxStepsNum
            // 
            resources.ApplyResources(this.MaxStepsNum, "MaxStepsNum");
            this.MaxStepsNum.Maximum = new decimal(new int[] {
            -1486618625,
            232830643,
            0,
            0});
            this.MaxStepsNum.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.MaxStepsNum.Name = "MaxStepsNum";
            this.MaxStepsNum.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.MaxStepsNum.ValueChanged += new System.EventHandler(this.MaxStepsNum_ValueChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // HistoryView
            // 
            resources.ApplyResources(this.HistoryView, "HistoryView");
            this.HistoryView.AllowColumnReorder = false;
            this.HistoryView.AllowColumnResize = false;
            this.HistoryView.AlwaysScroll = false;
            this.HistoryView.CellHeightPadding = 0;
            this.HistoryView.CellWidthPadding = 0;
            this.HistoryView.FullRowSelect = true;
            this.HistoryView.HorizontalOrientation = false;
            this.HistoryView.LetKeysModifySelection = false;
            this.HistoryView.MultiSelect = false;
            this.HistoryView.Name = "HistoryView";
            this.HistoryView.RowCount = 0;
            this.HistoryView.ScrollSpeed = 3;
            this.HistoryView.DoubleClick += new System.EventHandler(this.HistoryView_DoubleClick);
            this.HistoryView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HistoryView_MouseDown);
            this.HistoryView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.HistoryView_MouseUp);
            // 
            // UndoHistoryForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.MaxStepsNum);
            this.Controls.Add(this.AutoScrollCheck);
            this.Controls.Add(this.ClearButton);
            this.Controls.Add(this.RedoButton);
            this.Controls.Add(this.UndoButton);
            this.Controls.Add(this.HistoryView);
            this.Name = "UndoHistoryForm";
            this.ShowIcon = false;
            this.RightClickMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MaxStepsNum)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button ClearButton;
		private System.Windows.Forms.Button UndoButton;
		private InputRoll HistoryView;
		private System.Windows.Forms.Button RedoButton;
		private System.Windows.Forms.ContextMenuStrip RightClickMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx undoHereToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx redoHereToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx sepToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx clearHistoryToHereToolStripMenuItem;
		private System.Windows.Forms.CheckBox AutoScrollCheck;
		private System.Windows.Forms.NumericUpDown MaxStepsNum;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
	}
}