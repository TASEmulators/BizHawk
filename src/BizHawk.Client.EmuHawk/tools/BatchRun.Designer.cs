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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BatchRun));
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.buttonClear = new System.Windows.Forms.Button();
            this.buttonGo = new System.Windows.Forms.Button();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.numericUpDownFrames = new System.Windows.Forms.NumericUpDown();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.buttonDump = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFrames)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // listBox1
            // 
            resources.ApplyResources(this.listBox1, "listBox1");
            this.listBox1.AllowDrop = true;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Name = "listBox1";
            this.listBox1.DragDrop += new System.Windows.Forms.DragEventHandler(this.ListBox1_DragDrop);
            this.listBox1.DragEnter += new System.Windows.Forms.DragEventHandler(this.ListBox1_DragEnter);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // buttonClear
            // 
            resources.ApplyResources(this.buttonClear, "buttonClear");
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.ButtonClear_Click);
            // 
            // buttonGo
            // 
            resources.ApplyResources(this.buttonGo, "buttonGo");
            this.buttonGo.Name = "buttonGo";
            this.buttonGo.UseVisualStyleBackColor = true;
            this.buttonGo.Click += new System.EventHandler(this.ButtonGo_Click);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // numericUpDownFrames
            // 
            resources.ApplyResources(this.numericUpDownFrames, "numericUpDownFrames");
            this.numericUpDownFrames.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownFrames.Name = "numericUpDownFrames";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // progressBar1
            // 
            resources.ApplyResources(this.progressBar1, "progressBar1");
            this.progressBar1.Name = "progressBar1";
            // 
            // buttonDump
            // 
            resources.ApplyResources(this.buttonDump, "buttonDump");
            this.buttonDump.Name = "buttonDump";
            this.buttonDump.UseVisualStyleBackColor = true;
            this.buttonDump.Click += new System.EventHandler(this.ButtonDump_Click);
            // 
            // BatchRun
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
            this.ShowIcon = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BatchRun_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFrames)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.ListBox listBox1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.Button buttonClear;
		private System.Windows.Forms.Button buttonGo;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.NumericUpDown numericUpDownFrames;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.Button buttonDump;
	}
}