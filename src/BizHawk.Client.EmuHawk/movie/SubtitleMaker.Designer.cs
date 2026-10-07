namespace BizHawk.Client.EmuHawk
{
	partial class SubtitleMaker
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubtitleMaker));
            this.OK = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.Message = new System.Windows.Forms.TextBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.YNumeric = new System.Windows.Forms.NumericUpDown();
            this.XNumeric = new System.Windows.Forms.NumericUpDown();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.DurationNumeric = new System.Windows.Forms.NumericUpDown();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ColorPanel = new System.Windows.Forms.Panel();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.FrameNumeric = new System.Windows.Forms.NumericUpDown();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DurationNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FrameNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // Cancel
            // 
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // Message
            // 
            resources.ApplyResources(this.Message, "Message");
            this.Message.Name = "Message";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // YNumeric
            // 
            resources.ApplyResources(this.YNumeric, "YNumeric");
            this.YNumeric.Maximum = new decimal(new int[] {
            240,
            0,
            0,
            0});
            this.YNumeric.Name = "YNumeric";
            // 
            // XNumeric
            // 
            resources.ApplyResources(this.XNumeric, "XNumeric");
            this.XNumeric.Maximum = new decimal(new int[] {
            320,
            0,
            0,
            0});
            this.XNumeric.Name = "XNumeric";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // DurationNumeric
            // 
            resources.ApplyResources(this.DurationNumeric, "DurationNumeric");
            this.DurationNumeric.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.DurationNumeric.Name = "DurationNumeric";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // ColorPanel
            // 
            resources.ApplyResources(this.ColorPanel, "ColorPanel");
            this.ColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ColorPanel.Name = "ColorPanel";
            this.ColorPanel.TabStop = true;
            this.ColorPanel.Click += new System.EventHandler(this.ColorPanel_DoubleClick);
            this.ColorPanel.DoubleClick += new System.EventHandler(this.ColorPanel_DoubleClick);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // FrameNumeric
            // 
            resources.ApplyResources(this.FrameNumeric, "FrameNumeric");
            this.FrameNumeric.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.FrameNumeric.Name = "FrameNumeric";
            this.FrameNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // SubtitleMaker
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.Controls.Add(this.label6);
            this.Controls.Add(this.FrameNumeric);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.ColorPanel);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.DurationNumeric);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.XNumeric);
            this.Controls.Add(this.YNumeric);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Message);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.Name = "SubtitleMaker";
            this.Load += new System.EventHandler(this.SubtitleMaker_Load);
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DurationNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FrameNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.TextBox Message;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.NumericUpDown YNumeric;
		private System.Windows.Forms.NumericUpDown XNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.NumericUpDown DurationNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private System.Windows.Forms.Panel ColorPanel;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private System.Windows.Forms.ColorDialog colorDialog1;
		private System.Windows.Forms.NumericUpDown FrameNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
	}
}