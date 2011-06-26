namespace BizHawk.MultiClient
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
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.Frame = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.X = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Y = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Length = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Color = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Message = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
			this.SuspendLayout();
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(439, 216);
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
			this.OK.Location = new System.Drawing.Point(358, 216);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 1;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// dataGridView1
			// 
			this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ControlLight;
			this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Frame,
            this.X,
            this.Y,
            this.Length,
            this.Color,
            this.Message});
			this.dataGridView1.Location = new System.Drawing.Point(12, 12);
			this.dataGridView1.Name = "dataGridView1";
			this.dataGridView1.Size = new System.Drawing.Size(502, 198);
			this.dataGridView1.TabIndex = 2;
			// 
			// Frame
			// 
			this.Frame.HeaderText = "Frame";
			this.Frame.MaxInputLength = 7;
			this.Frame.Name = "Frame";
			this.Frame.Width = 75;
			// 
			// X
			// 
			this.X.HeaderText = "X";
			this.X.MaxInputLength = 3;
			this.X.Name = "X";
			this.X.Width = 30;
			// 
			// Y
			// 
			this.Y.HeaderText = "Y";
			this.Y.MaxInputLength = 3;
			this.Y.Name = "Y";
			this.Y.Width = 30;
			// 
			// Length
			// 
			this.Length.HeaderText = "Length";
			this.Length.MaxInputLength = 5;
			this.Length.Name = "Length";
			this.Length.Width = 33;
			// 
			// Color
			// 
			this.Color.HeaderText = "Color";
			this.Color.MaxInputLength = 8;
			this.Color.Name = "Color";
			this.Color.Width = 40;
			// 
			// Message
			// 
			this.Message.HeaderText = "Message";
			this.Message.MaxInputLength = 255;
			this.Message.Name = "Message";
			this.Message.Width = 250;
			// 
			// EditSubtitlesForm
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(526, 251);
			this.Controls.Add(this.dataGridView1);
			this.Controls.Add(this.OK);
			this.Controls.Add(this.Cancel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "EditSubtitlesForm";
			this.Text = "Edit Subtitles";
			this.Load += new System.EventHandler(this.EditSubtitlesForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.DataGridView dataGridView1;
		private System.Windows.Forms.DataGridViewTextBoxColumn Frame;
		private System.Windows.Forms.DataGridViewTextBoxColumn X;
		private System.Windows.Forms.DataGridViewTextBoxColumn Y;
		private System.Windows.Forms.DataGridViewTextBoxColumn Length;
		private System.Windows.Forms.DataGridViewTextBoxColumn Color;
		private System.Windows.Forms.DataGridViewTextBoxColumn Message;
	}
}