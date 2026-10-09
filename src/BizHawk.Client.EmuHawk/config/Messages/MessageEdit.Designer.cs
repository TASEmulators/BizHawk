namespace BizHawk.Client.EmuHawk
{
	partial class MessageEdit
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MessageEdit));
            this.PositionGroupBox = new System.Windows.Forms.GroupBox();
            this.BR = new System.Windows.Forms.RadioButton();
            this.BL = new System.Windows.Forms.RadioButton();
            this.TR = new System.Windows.Forms.RadioButton();
            this.TL = new System.Windows.Forms.RadioButton();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.YNumeric = new System.Windows.Forms.NumericUpDown();
            this.XNumeric = new System.Windows.Forms.NumericUpDown();
            this.PositionPanel = new System.Windows.Forms.Panel();
            this.PositionGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // PositionGroupBox
            // 
            resources.ApplyResources(this.PositionGroupBox, "PositionGroupBox");
            this.PositionGroupBox.Controls.Add(this.BR);
            this.PositionGroupBox.Controls.Add(this.BL);
            this.PositionGroupBox.Controls.Add(this.TR);
            this.PositionGroupBox.Controls.Add(this.TL);
            this.PositionGroupBox.Controls.Add(this.label2);
            this.PositionGroupBox.Controls.Add(this.label1);
            this.PositionGroupBox.Controls.Add(this.YNumeric);
            this.PositionGroupBox.Controls.Add(this.XNumeric);
            this.PositionGroupBox.Controls.Add(this.PositionPanel);
            this.PositionGroupBox.Name = "PositionGroupBox";
            this.PositionGroupBox.TabStop = false;
            // 
            // BR
            // 
            resources.ApplyResources(this.BR, "BR");
            this.BR.Name = "BR";
            this.BR.TabStop = true;
            this.BR.UseVisualStyleBackColor = true;
            this.BR.CheckedChanged += new System.EventHandler(this.BR_CheckedChanged);
            // 
            // BL
            // 
            resources.ApplyResources(this.BL, "BL");
            this.BL.Name = "BL";
            this.BL.TabStop = true;
            this.BL.UseVisualStyleBackColor = true;
            this.BL.CheckedChanged += new System.EventHandler(this.BL_CheckedChanged);
            // 
            // TR
            // 
            resources.ApplyResources(this.TR, "TR");
            this.TR.Name = "TR";
            this.TR.TabStop = true;
            this.TR.UseVisualStyleBackColor = true;
            this.TR.CheckedChanged += new System.EventHandler(this.TR_CheckedChanged);
            // 
            // TL
            // 
            resources.ApplyResources(this.TL, "TL");
            this.TL.Name = "TL";
            this.TL.TabStop = true;
            this.TL.UseVisualStyleBackColor = true;
            this.TL.CheckedChanged += new System.EventHandler(this.TL_CheckedChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
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
            180,
            0,
            0,
            0});
            this.YNumeric.Name = "YNumeric";
            this.YNumeric.Value = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.YNumeric.ValueChanged += new System.EventHandler(this.YNumeric_ValueChanged);
            // 
            // XNumeric
            // 
            resources.ApplyResources(this.XNumeric, "XNumeric");
            this.XNumeric.Maximum = new decimal(new int[] {
            244,
            0,
            0,
            0});
            this.XNumeric.Name = "XNumeric";
            this.XNumeric.Value = new decimal(new int[] {
            244,
            0,
            0,
            0});
            this.XNumeric.ValueChanged += new System.EventHandler(this.XNumeric_ValueChanged);
            // 
            // PositionPanel
            // 
            resources.ApplyResources(this.PositionPanel, "PositionPanel");
            this.PositionPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PositionPanel.Name = "PositionPanel";
            this.PositionPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.PositionPanel_Paint);
            this.PositionPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PositionPanel_MouseDown);
            this.PositionPanel.MouseEnter += new System.EventHandler(this.PositionPanel_MouseEnter);
            this.PositionPanel.MouseLeave += new System.EventHandler(this.PositionPanel_MouseLeave);
            this.PositionPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PositionPanel_MouseMove);
            this.PositionPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PositionPanel_MouseUp);
            // 
            // MessageEdit
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PositionGroupBox);
            this.Name = "MessageEdit";
            this.PositionGroupBox.ResumeLayout(false);
            this.PositionGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox PositionGroupBox;
		private System.Windows.Forms.RadioButton BR;
		private System.Windows.Forms.RadioButton BL;
		private System.Windows.Forms.RadioButton TR;
		private System.Windows.Forms.RadioButton TL;
		private WinForms.Controls.LocLabelEx label2;
		private WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.NumericUpDown YNumeric;
		private System.Windows.Forms.NumericUpDown XNumeric;
		private System.Windows.Forms.Panel PositionPanel;
	}
}
