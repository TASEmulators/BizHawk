namespace BizHawk.MultiClient
{
	partial class GifAnimator
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
			this.Exit = new System.Windows.Forms.Button();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.TB_Frame_Skip = new System.Windows.Forms.TextBox();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.TB_Num_Frames = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// Exit
			// 
			this.Exit.Location = new System.Drawing.Point(12, 108);
			this.Exit.Name = "Exit";
			this.Exit.Size = new System.Drawing.Size(75, 23);
			this.Exit.TabIndex = 5;
			this.Exit.Text = "&Save";
			this.Exit.UseVisualStyleBackColor = true;
			this.Exit.Click += new System.EventHandler(this.Exit_Click);
			// 
			// checkBox1
			// 
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(73, 85);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(93, 17);
			this.checkBox1.TabIndex = 4;
			this.checkBox1.Text = "Reverse Loop";
			this.checkBox1.UseVisualStyleBackColor = true;
			// 
			// TB_Frame_Skip
			// 
			this.TB_Frame_Skip.Location = new System.Drawing.Point(108, 32);
			this.TB_Frame_Skip.Name = "TB_Frame_Skip";
			this.TB_Frame_Skip.Size = new System.Drawing.Size(58, 20);
			this.TB_Frame_Skip.TabIndex = 2;
			// 
			// comboBox1
			// 
			this.comboBox1.FormattingEnabled = true;
			this.comboBox1.Location = new System.Drawing.Point(108, 58);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(58, 21);
			this.comboBox1.TabIndex = 3;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 35);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(80, 13);
			this.label1.TabIndex = 8;
			this.label1.Text = "Frames to Skip:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 61);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(90, 13);
			this.label2.TabIndex = 9;
			this.label2.Text = "Animation Speed:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 9);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(75, 13);
			this.label3.TabIndex = 7;
			this.label3.Text = "Length of GIF:";
			// 
			// TB_Num_Frames
			// 
			this.TB_Num_Frames.Location = new System.Drawing.Point(108, 6);
			this.TB_Num_Frames.Name = "TB_Num_Frames";
			this.TB_Num_Frames.Size = new System.Drawing.Size(58, 20);
			this.TB_Num_Frames.TabIndex = 1;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(93, 108);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 6;
			this.button1.Text = "&Cancel";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// GifAnimator
			// 
			this.AcceptButton = this.Exit;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(184, 140);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.TB_Num_Frames);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBox1);
			this.Controls.Add(this.TB_Frame_Skip);
			this.Controls.Add(this.checkBox1);
			this.Controls.Add(this.Exit);
			this.Name = "GifAnimator";
			this.Text = "GifAnimator";
			this.Load += new System.EventHandler(this.GifAnimator_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button Exit;
		private System.Windows.Forms.CheckBox checkBox1;
		private System.Windows.Forms.TextBox TB_Frame_Skip;
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox TB_Num_Frames;
		private System.Windows.Forms.Button button1;
	}
}