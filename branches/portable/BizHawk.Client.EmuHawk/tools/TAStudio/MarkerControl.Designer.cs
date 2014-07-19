namespace BizHawk.Client.EmuHawk
{
	partial class MarkerControl
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
			this.AddBtn = new System.Windows.Forms.Button();
			this.RemoveBtn = new System.Windows.Forms.Button();
			this.MarkerView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// AddBtn
			// 
			this.AddBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.AddBtn.Location = new System.Drawing.Point(157, 214);
			this.AddBtn.Name = "AddBtn";
			this.AddBtn.Size = new System.Drawing.Size(44, 23);
			this.AddBtn.TabIndex = 6;
			this.AddBtn.Text = "Add";
			this.AddBtn.UseVisualStyleBackColor = true;
			this.AddBtn.Click += new System.EventHandler(this.AddBtn_Click);
			// 
			// RemoveBtn
			// 
			this.RemoveBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.RemoveBtn.Enabled = false;
			this.RemoveBtn.Location = new System.Drawing.Point(3, 214);
			this.RemoveBtn.Name = "RemoveBtn";
			this.RemoveBtn.Size = new System.Drawing.Size(58, 23);
			this.RemoveBtn.TabIndex = 7;
			this.RemoveBtn.Text = "Remove";
			this.RemoveBtn.UseVisualStyleBackColor = true;
			this.RemoveBtn.Click += new System.EventHandler(this.RemoveBtn_Click);
			// 
			// MarkerView
			// 
			this.MarkerView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MarkerView.BlazingFast = false;
			this.MarkerView.CheckBoxes = true;
			this.MarkerView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.MarkerView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MarkerView.FullRowSelect = true;
			this.MarkerView.GridLines = true;
			this.MarkerView.HideSelection = false;
			this.MarkerView.ItemCount = 0;
			this.MarkerView.Location = new System.Drawing.Point(3, 0);
			this.MarkerView.Name = "MarkerView";
			this.MarkerView.SelectAllInProgress = false;
			this.MarkerView.selectedItem = -1;
			this.MarkerView.Size = new System.Drawing.Size(198, 209);
			this.MarkerView.TabIndex = 5;
			this.MarkerView.TabStop = false;
			this.MarkerView.UseCompatibleStateImageBehavior = false;
			this.MarkerView.View = System.Windows.Forms.View.Details;
			this.MarkerView.SelectedIndexChanged += new System.EventHandler(this.MarkerView_SelectedIndexChanged);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Frame";
			this.columnHeader1.Width = 64;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Label";
			this.columnHeader2.Width = 113;
			// 
			// MarkerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.RemoveBtn);
			this.Controls.Add(this.AddBtn);
			this.Controls.Add(this.MarkerView);
			this.Name = "MarkerControl";
			this.Size = new System.Drawing.Size(204, 241);
			this.Load += new System.EventHandler(this.MarkerControl_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private VirtualListView MarkerView;
		public System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button AddBtn;
		private System.Windows.Forms.Button RemoveBtn;

	}
}
