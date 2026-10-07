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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditSubtitlesForm));
            this.Cancel = new System.Windows.Forms.Button();
            this.OK = new System.Windows.Forms.Button();
            this.SubGrid = new System.Windows.Forms.DataGridView();
            this.Export = new System.Windows.Forms.Button();
            this.ConcatMultilines = new System.Windows.Forms.CheckBox();
            this.AddColorTag = new System.Windows.Forms.CheckBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.Frame = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.X = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Y = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Length = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DispColor = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Message = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.SubGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // Cancel
            // 
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // SubGrid
            // 
            resources.ApplyResources(this.SubGrid, "SubGrid");
            this.SubGrid.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.SubGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.SubGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Frame,
            this.X,
            this.Y,
            this.Length,
            this.DispColor,
            this.Message});
            this.SubGrid.Name = "SubGrid";
            this.SubGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.SubGrid_CellContentClick);
            this.SubGrid.DefaultValuesNeeded += new System.Windows.Forms.DataGridViewRowEventHandler(this.SubGrid_DefaultValuesNeeded);
            this.SubGrid.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.SubGrid_MouseDoubleClick);
            // 
            // Export
            // 
            resources.ApplyResources(this.Export, "Export");
            this.Export.Name = "Export";
            this.Export.UseVisualStyleBackColor = true;
            this.Export.Click += new System.EventHandler(this.Export_Click);
            // 
            // ConcatMultilines
            // 
            resources.ApplyResources(this.ConcatMultilines, "ConcatMultilines");
            this.ConcatMultilines.Name = "ConcatMultilines";
            this.ConcatMultilines.UseVisualStyleBackColor = true;
            this.ConcatMultilines.CheckedChanged += new System.EventHandler(this.ConcatMultilines_CheckedChanged);
            // 
            // AddColorTag
            // 
            resources.ApplyResources(this.AddColorTag, "AddColorTag");
            this.AddColorTag.Name = "AddColorTag";
            this.AddColorTag.UseVisualStyleBackColor = true;
            this.AddColorTag.CheckedChanged += new System.EventHandler(this.AddColorTag_CheckedChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // Frame
            // 
            resources.ApplyResources(this.Frame, "Frame");
            this.Frame.MaxInputLength = 7;
            this.Frame.Name = "Frame";
            // 
            // X
            // 
            resources.ApplyResources(this.X, "X");
            this.X.MaxInputLength = 3;
            this.X.Name = "X";
            // 
            // Y
            // 
            resources.ApplyResources(this.Y, "Y");
            this.Y.MaxInputLength = 3;
            this.Y.Name = "Y";
            // 
            // Length
            // 
            resources.ApplyResources(this.Length, "Length");
            this.Length.MaxInputLength = 5;
            this.Length.Name = "Length";
            // 
            // DispColor
            // 
            resources.ApplyResources(this.DispColor, "DispColor");
            this.DispColor.MaxInputLength = 8;
            this.DispColor.Name = "DispColor";
            // 
            // Message
            // 
            this.Message.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            resources.ApplyResources(this.Message, "Message");
            this.Message.MaxInputLength = 255;
            this.Message.Name = "Message";
            // 
            // EditSubtitlesForm
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.AddColorTag);
            this.Controls.Add(this.ConcatMultilines);
            this.Controls.Add(this.Export);
            this.Controls.Add(this.SubGrid);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.Cancel);
            this.Name = "EditSubtitlesForm";
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
		private System.Windows.Forms.CheckBox ConcatMultilines;
		private System.Windows.Forms.CheckBox AddColorTag;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.DataGridViewTextBoxColumn Frame;
		private System.Windows.Forms.DataGridViewTextBoxColumn X;
		private System.Windows.Forms.DataGridViewTextBoxColumn Y;
		private System.Windows.Forms.DataGridViewTextBoxColumn Length;
		private System.Windows.Forms.DataGridViewTextBoxColumn DispColor;
		private System.Windows.Forms.DataGridViewTextBoxColumn Message;
	}
}