namespace BizHawk.Client.EmuHawk
{
	partial class BatchRun
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
			this.label1 = new System.Windows.Forms.Label();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonClear = new System.Windows.Forms.Button();
			this.buttonGo = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.numericUpDownFrames = new System.Windows.Forms.NumericUpDown();
			this.label4 = new System.Windows.Forms.Label();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.buttonDump = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownFrames)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(83, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Drag Files Here:";
			// 
			// listBox1
			// 
			this.listBox1.AllowDrop = true;
			this.listBox1.FormattingEnabled = true;
			this.listBox1.Location = new System.Drawing.Point(12, 25);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(268, 147);
			this.listBox1.TabIndex = 2;
			this.listBox1.DragDrop += new System.Windows.Forms.DragEventHandler(this.listBox1_DragDrop);
			this.listBox1.DragEnter += new System.Windows.Forms.DragEventHandler(this.listBox1_DragEnter);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 175);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(92, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Number of Files: 0";
			// 
			// buttonClear
			// 
			this.buttonClear.Location = new System.Drawing.Point(12, 191);
			this.buttonClear.Name = "buttonClear";
			this.buttonClear.Size = new System.Drawing.Size(75, 23);
			this.buttonClear.TabIndex = 4;
			this.buttonClear.Text = "Clear!";
			this.buttonClear.UseVisualStyleBackColor = true;
			this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
			// 
			// buttonGo
			// 
			this.buttonGo.Location = new System.Drawing.Point(12, 220);
			this.buttonGo.Name = "buttonGo";
			this.buttonGo.Size = new System.Drawing.Size(75, 23);
			this.buttonGo.TabIndex = 5;
			this.buttonGo.Text = "Go!";
			this.buttonGo.UseVisualStyleBackColor = true;
			this.buttonGo.Click += new System.EventHandler(this.buttonGo_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 246);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(40, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Status:";
			// 
			// numericUpDownFrames
			// 
			this.numericUpDownFrames.Location = new System.Drawing.Point(160, 194);
			this.numericUpDownFrames.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
			this.numericUpDownFrames.Name = "numericUpDownFrames";
			this.numericUpDownFrames.Size = new System.Drawing.Size(120, 20);
			this.numericUpDownFrames.TabIndex = 7;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(157, 175);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(74, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "Frames to run:";
			// 
			// progressBar1
			// 
			this.progressBar1.Location = new System.Drawing.Point(12, 262);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(268, 23);
			this.progressBar1.TabIndex = 9;
			// 
			// buttonDump
			// 
			this.buttonDump.Location = new System.Drawing.Point(93, 220);
			this.buttonDump.Name = "buttonDump";
			this.buttonDump.Size = new System.Drawing.Size(75, 23);
			this.buttonDump.TabIndex = 10;
			this.buttonDump.Text = "Dump...";
			this.buttonDump.UseVisualStyleBackColor = true;
			this.buttonDump.Click += new System.EventHandler(this.buttonDump_Click);
			// 
			// BatchRun
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(292, 327);
			this.Controls.Add(this.buttonDump);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.numericUpDownFrames);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.buttonGo);
			this.Controls.Add(this.buttonClear);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.listBox1);
			this.Controls.Add(this.label1);
			this.Name = "BatchRun";
			this.Text = "BatchRun";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BatchRun_FormClosing);
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownFrames)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonClear;
		private System.Windows.Forms.Button buttonGo;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown numericUpDownFrames;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.Button buttonDump;
	}
}