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
            this.PositionGroupBox.Controls.Add(this.BR);
            this.PositionGroupBox.Controls.Add(this.BL);
            this.PositionGroupBox.Controls.Add(this.TR);
            this.PositionGroupBox.Controls.Add(this.TL);
            this.PositionGroupBox.Controls.Add(this.label2);
            this.PositionGroupBox.Controls.Add(this.label1);
            this.PositionGroupBox.Controls.Add(this.YNumeric);
            this.PositionGroupBox.Controls.Add(this.XNumeric);
            this.PositionGroupBox.Controls.Add(this.PositionPanel);
            this.PositionGroupBox.Location = new System.Drawing.Point(3, 3);
            this.PositionGroupBox.Name = "PositionGroupBox";
            this.PositionGroupBox.Size = new System.Drawing.Size(307, 299);
            this.PositionGroupBox.TabIndex = 4;
            this.PositionGroupBox.TabStop = false;
            this.PositionGroupBox.Text = "Position";
            // 
            // BR
            // 
            this.BR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BR.AutoSize = true;
            this.BR.Location = new System.Drawing.Point(289, 253);
            this.BR.Name = "BR";
            this.BR.Size = new System.Drawing.Size(14, 13);
            this.BR.TabIndex = 8;
            this.BR.TabStop = true;
            this.BR.UseVisualStyleBackColor = true;
            this.BR.CheckedChanged += new System.EventHandler(this.BR_CheckedChanged);
            // 
            // BL
            // 
            this.BL.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BL.AutoSize = true;
            this.BL.Location = new System.Drawing.Point(6, 253);
            this.BL.Name = "BL";
            this.BL.Size = new System.Drawing.Size(14, 13);
            this.BL.TabIndex = 7;
            this.BL.TabStop = true;
            this.BL.UseVisualStyleBackColor = true;
            this.BL.CheckedChanged += new System.EventHandler(this.BL_CheckedChanged);
            // 
            // TR
            // 
            this.TR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.TR.AutoSize = true;
            this.TR.Location = new System.Drawing.Point(288, 18);
            this.TR.Name = "TR";
            this.TR.Size = new System.Drawing.Size(14, 13);
            this.TR.TabIndex = 6;
            this.TR.TabStop = true;
            this.TR.UseVisualStyleBackColor = true;
            this.TR.CheckedChanged += new System.EventHandler(this.TR_CheckedChanged);
            // 
            // TL
            // 
            this.TL.AutoSize = true;
            this.TL.Location = new System.Drawing.Point(6, 18);
            this.TL.Name = "TL";
            this.TL.Size = new System.Drawing.Size(14, 13);
            this.TL.TabIndex = 5;
            this.TL.TabStop = true;
            this.TL.UseVisualStyleBackColor = true;
            this.TL.CheckedChanged += new System.EventHandler(this.TL_CheckedChanged);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.Location = new System.Drawing.Point(92, 273);
            this.label2.Name = "label2";
            this.label2.Text = "y";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.Location = new System.Drawing.Point(27, 274);
            this.label1.Name = "label1";
            this.label1.Text = "x";
            // 
            // YNumeric
            // 
            this.YNumeric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.YNumeric.Location = new System.Drawing.Point(106, 271);
            this.YNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.YNumeric.Name = "YNumeric";
            this.YNumeric.Size = new System.Drawing.Size(44, 20);
            this.YNumeric.TabIndex = 2;
            this.YNumeric.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.YNumeric.ValueChanged += new System.EventHandler(this.YNumeric_ValueChanged);
            // 
            // XNumeric
            // 
            this.XNumeric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.XNumeric.Location = new System.Drawing.Point(43, 271);
            this.XNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.XNumeric.Name = "XNumeric";
            this.XNumeric.Size = new System.Drawing.Size(44, 20);
            this.XNumeric.TabIndex = 1;
            this.XNumeric.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.XNumeric.ValueChanged += new System.EventHandler(this.XNumeric_ValueChanged);
            // 
            // PositionPanel
            // 
            this.PositionPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PositionPanel.Location = new System.Drawing.Point(22, 18);
            this.PositionPanel.Name = "PositionPanel";
            this.PositionPanel.Size = new System.Drawing.Size(264, 248);
            this.PositionPanel.TabIndex = 0;
            this.PositionPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.PositionPanel_Paint);
            this.PositionPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PositionPanel_MouseDown);
            this.PositionPanel.MouseEnter += new System.EventHandler(this.PositionPanel_MouseEnter);
            this.PositionPanel.MouseLeave += new System.EventHandler(this.PositionPanel_MouseLeave);
            this.PositionPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PositionPanel_MouseMove);
            this.PositionPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PositionPanel_MouseUp);
            // 
            // MessageEdit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PositionGroupBox);
            this.Name = "MessageEdit";
            this.Size = new System.Drawing.Size(318, 310);
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
