namespace BizHawk.Client.EmuHawk
{
	partial class HistoryBox
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
			this.HistoryView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.UndoButton = new System.Windows.Forms.Button();
			this.ClearButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// HistoryView
			// 
			this.HistoryView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HistoryView.BlazingFast = false;
			this.HistoryView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.HistoryView.ItemCount = 0;
			this.HistoryView.Location = new System.Drawing.Point(0, 0);
			this.HistoryView.Name = "HistoryView";
			this.HistoryView.SelectAllInProgress = false;
			this.HistoryView.selectedItem = -1;
			this.HistoryView.Size = new System.Drawing.Size(204, 318);
			this.HistoryView.TabIndex = 0;
			this.HistoryView.UseCompatibleStateImageBehavior = false;
			this.HistoryView.UseCustomBackground = true;
			this.HistoryView.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "ID";
			this.columnHeader1.Width = 40;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Undo Step";
			this.columnHeader2.Width = 160;
			// 
			// UndoButton
			// 
			this.UndoButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.UndoButton.Location = new System.Drawing.Point(3, 324);
			this.UndoButton.Name = "UndoButton";
			this.UndoButton.Size = new System.Drawing.Size(55, 23);
			this.UndoButton.TabIndex = 1;
			this.UndoButton.Text = "Undo";
			this.UndoButton.UseVisualStyleBackColor = true;
			// 
			// ClearButton
			// 
			this.ClearButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ClearButton.Location = new System.Drawing.Point(146, 324);
			this.ClearButton.Name = "ClearButton";
			this.ClearButton.Size = new System.Drawing.Size(55, 23);
			this.ClearButton.TabIndex = 1;
			this.ClearButton.Text = "Clear";
			this.ClearButton.UseVisualStyleBackColor = true;
			// 
			// HistoryBox
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.ClearButton);
			this.Controls.Add(this.UndoButton);
			this.Controls.Add(this.HistoryView);
			this.Name = "HistoryBox";
			this.Size = new System.Drawing.Size(204, 350);
			this.ResumeLayout(false);

		}

		#endregion

		private VirtualListView HistoryView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button UndoButton;
		private System.Windows.Forms.Button ClearButton;


	}
}
