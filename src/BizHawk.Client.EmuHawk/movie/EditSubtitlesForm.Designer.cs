namespace BizHawk.Client.EmuHawk
{
	partial class EditSubtitlesForm
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
			this.Cancel = new System.Windows.Forms.Button();
			this.OK = new System.Windows.Forms.Button();
			this.SubGrid = new System.Windows.Forms.DataGridView();
			this.Frame = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.X = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Y = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Length = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DispColor = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Message = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Export = new System.Windows.Forms.Button();
			this.ConcatMultilines = new System.Windows.Forms.CheckBox();
			this.AddColorTag = new System.Windows.Forms.CheckBox();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			((System.ComponentModel.ISupportInitialize)(this.SubGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(485, 216);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 0;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(404, 216);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 1;
			this.OK.Text = "&OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.Ok_Click);
			// 
			// SubGrid
			// 
			this.SubGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SubGrid.BackgroundColor = System.Drawing.SystemColors.ControlLight;
			this.SubGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.SubGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Frame,
            this.X,
            this.Y,
            this.Length,
            this.DispColor,
            this.Message});
			this.SubGrid.Location = new System.Drawing.Point(12, 12);
			this.SubGrid.Name = "SubGrid";
			this.SubGrid.Size = new System.Drawing.Size(548, 198);
			this.SubGrid.TabIndex = 2;
			this.SubGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.SubGrid_CellContentClick);
			this.SubGrid.DefaultValuesNeeded += new System.Windows.Forms.DataGridViewRowEventHandler(this.SubGrid_DefaultValuesNeeded);
			this.SubGrid.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.SubGrid_MouseDoubleClick);
			// 
			// Frame
			// 
			this.Frame.HeaderText = "Frame";
			this.Frame.MaxInputLength = 7;
			this.Frame.Name = "Frame";
			this.Frame.ToolTipText = "The first frame the subtitle will be displayed (integer)";
			this.Frame.Width = 75;
			// 
			// X
			// 
			this.X.HeaderText = "X";
			this.X.MaxInputLength = 3;
			this.X.Name = "X";
			this.X.ToolTipText = "Screen coordinate (absolute)";
			this.X.Width = 30;
			// 
			// Y
			// 
			this.Y.HeaderText = "Y";
			this.Y.MaxInputLength = 3;
			this.Y.Name = "Y";
			this.Y.ToolTipText = "Screen coordinate (absolute)";
			this.Y.Width = 30;
			// 
			// Length
			// 
			this.Length.HeaderText = "Length";
			this.Length.MaxInputLength = 5;
			this.Length.Name = "Length";
			this.Length.ToolTipText = "How long subtitle will be displayed";
			this.Length.Width = 50;
			// 
			// DispColor
			// 
			this.DispColor.HeaderText = "Color";
			this.DispColor.MaxInputLength = 8;
			this.DispColor.Name = "DispColor";
			this.DispColor.ToolTipText = "Color of subtitle text";
			this.DispColor.Width = 60;
			// 
			// Message
			// 
			this.Message.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.Message.HeaderText = "Message";
			this.Message.MaxInputLength = 255;
			this.Message.MinimumWidth = 25;
			this.Message.Name = "Message";
			this.Message.ToolTipText = "What will be displayed";
			// 
			// Export
			// 
			this.Export.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.Export.Location = new System.Drawing.Point(12, 216);
			this.Export.Margin = new System.Windows.Forms.Padding(2);
			this.Export.Name = "Export";
			this.Export.Size = new System.Drawing.Size(95, 23);
			this.Export.TabIndex = 3;
			this.Export.Text = "&Export to SubRip";
			this.Export.UseVisualStyleBackColor = true;
			this.Export.Click += new System.EventHandler(this.Export_Click);
			// 
			// ConcatMultilines
			// 
			this.ConcatMultilines.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ConcatMultilines.AutoSize = true;
			this.ConcatMultilines.Location = new System.Drawing.Point(182, 220);
			this.ConcatMultilines.Name = "ConcatMultilines";
			this.ConcatMultilines.Size = new System.Drawing.Size(105, 17);
			this.ConcatMultilines.TabIndex = 4;
			this.ConcatMultilines.Text = "Concat multilines";
			this.ConcatMultilines.UseVisualStyleBackColor = true;
			this.ConcatMultilines.CheckedChanged += new System.EventHandler(this.ConcatMultilines_CheckedChanged);
			// 
			// AddColorTag
			// 
			this.AddColorTag.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddColorTag.AutoSize = true;
			this.AddColorTag.Location = new System.Drawing.Point(293, 220);
			this.AddColorTag.Name = "AddColorTag";
			this.AddColorTag.Size = new System.Drawing.Size(89, 17);
			this.AddColorTag.TabIndex = 5;
			this.AddColorTag.Text = "Add color tag";
			this.AddColorTag.UseVisualStyleBackColor = true;
			this.AddColorTag.CheckedChanged += new System.EventHandler(this.AddColorTag_CheckedChanged);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.Location = new System.Drawing.Point(120, 221);
			this.label1.Name = "label1";
			this.label1.Text = "On export:";
			// 
			// EditSubtitlesForm
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(572, 251);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.AddColorTag);
			this.Controls.Add(this.ConcatMultilines);
			this.Controls.Add(this.Export);
			this.Controls.Add(this.SubGrid);
			this.Controls.Add(this.OK);
			this.Controls.Add(this.Cancel);
			this.MinimumSize = new System.Drawing.Size(188, 121);
			this.Name = "EditSubtitlesForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Edit Subtitles";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnClosed);
			this.Load += new System.EventHandler(this.EditSubtitlesForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.SubGrid)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.DataGridView SubGrid;
		private System.Windows.Forms.Button Export;
		private System.Windows.Forms.DataGridViewTextBoxColumn Frame;
		private System.Windows.Forms.DataGridViewTextBoxColumn X;
		private System.Windows.Forms.DataGridViewTextBoxColumn Y;
		private System.Windows.Forms.DataGridViewTextBoxColumn Length;
		private System.Windows.Forms.DataGridViewTextBoxColumn DispColor;
		private System.Windows.Forms.DataGridViewTextBoxColumn Message;
		private System.Windows.Forms.CheckBox ConcatMultilines;
		private System.Windows.Forms.CheckBox AddColorTag;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
	}
}