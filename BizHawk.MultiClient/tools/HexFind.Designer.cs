namespace BizHawk.MultiClient
{
	partial class HexFind
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
			this.FindBox = new System.Windows.Forms.TextBox();
			this.Find_Prev = new System.Windows.Forms.Button();
			this.Find_Next = new System.Windows.Forms.Button();
			this.HexRadio = new System.Windows.Forms.RadioButton();
			this.TextRadio = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// FindBox
			// 
			this.FindBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.FindBox.Location = new System.Drawing.Point(13, 12);
			this.FindBox.Name = "FindBox";
			this.FindBox.Size = new System.Drawing.Size(156, 20);
			this.FindBox.TabIndex = 0;
			this.FindBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FindBox_KeyPress);
			// 
			// Find_Prev
			// 
			this.Find_Prev.Location = new System.Drawing.Point(13, 39);
			this.Find_Prev.Name = "Find_Prev";
			this.Find_Prev.Size = new System.Drawing.Size(75, 23);
			this.Find_Prev.TabIndex = 1;
			this.Find_Prev.Text = "Find Prev";
			this.Find_Prev.UseVisualStyleBackColor = true;
			this.Find_Prev.Click += new System.EventHandler(this.Find_Prev_Click);
			// 
			// Find_Next
			// 
			this.Find_Next.Location = new System.Drawing.Point(94, 39);
			this.Find_Next.Name = "Find_Next";
			this.Find_Next.Size = new System.Drawing.Size(75, 23);
			this.Find_Next.TabIndex = 2;
			this.Find_Next.Text = "Find Next";
			this.Find_Next.UseVisualStyleBackColor = true;
			this.Find_Next.Click += new System.EventHandler(this.Find_Next_Click);
			// 
			// HexRadio
			// 
			this.HexRadio.AutoSize = true;
			this.HexRadio.Checked = true;
			this.HexRadio.Location = new System.Drawing.Point(13, 69);
			this.HexRadio.Name = "HexRadio";
			this.HexRadio.Size = new System.Drawing.Size(44, 17);
			this.HexRadio.TabIndex = 3;
			this.HexRadio.TabStop = true;
			this.HexRadio.Text = "Hex";
			this.HexRadio.UseVisualStyleBackColor = true;
			this.HexRadio.CheckedChanged += new System.EventHandler(this.HexRadio_CheckedChanged);
			// 
			// TextRadio
			// 
			this.TextRadio.AutoSize = true;
			this.TextRadio.Location = new System.Drawing.Point(63, 69);
			this.TextRadio.Name = "TextRadio";
			this.TextRadio.Size = new System.Drawing.Size(46, 17);
			this.TextRadio.TabIndex = 4;
			this.TextRadio.Text = "Text";
			this.TextRadio.UseVisualStyleBackColor = true;
			this.TextRadio.CheckedChanged += new System.EventHandler(this.TextRadio_CheckedChanged);
			// 
			// HexFind
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(178, 93);
			this.Controls.Add(this.TextRadio);
			this.Controls.Add(this.HexRadio);
			this.Controls.Add(this.Find_Next);
			this.Controls.Add(this.Find_Prev);
			this.Controls.Add(this.FindBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "HexFind";
			this.ShowIcon = false;
			this.Text = "Find";
			this.Load += new System.EventHandler(this.HexFind_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox FindBox;
		private System.Windows.Forms.Button Find_Prev;
		private System.Windows.Forms.Button Find_Next;
		private System.Windows.Forms.RadioButton HexRadio;
		private System.Windows.Forms.RadioButton TextRadio;
	}
}