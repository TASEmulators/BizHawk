namespace BizHawk.Client.EmuHawk
{
	partial class VirtualPadTargetScreen
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
			this.TargetPanel = new System.Windows.Forms.Panel();
			this.XNumeric = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.YNumeric = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.XNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.YNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// TargetPanel
			// 
			this.TargetPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TargetPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.TargetPanel.Location = new System.Drawing.Point(0, 0);
			this.TargetPanel.Name = "TargetPanel";
			this.TargetPanel.Size = new System.Drawing.Size(256, 224);
			this.TargetPanel.TabIndex = 0;
			this.TargetPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.TargetPanel_Paint);
			this.TargetPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TargetPanel_MouseDown);
			this.TargetPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TargetPanel_MouseMove);
			this.TargetPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TargetPanel_MouseUp);
			// 
			// XNumeric
			// 
			this.XNumeric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.XNumeric.Location = new System.Drawing.Point(23, 229);
			this.XNumeric.Name = "XNumeric";
			this.XNumeric.Size = new System.Drawing.Size(50, 20);
			this.XNumeric.TabIndex = 1;
			this.XNumeric.ValueChanged += new System.EventHandler(this.XNumeric_ValueChanged);
			this.XNumeric.KeyUp += new System.Windows.Forms.KeyEventHandler(this.XNumeric_KeyUp);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 233);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(14, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "X";
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(91, 233);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(14, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Y";
			// 
			// YNumeric
			// 
			this.YNumeric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.YNumeric.Location = new System.Drawing.Point(111, 229);
			this.YNumeric.Name = "YNumeric";
			this.YNumeric.Size = new System.Drawing.Size(50, 20);
			this.YNumeric.TabIndex = 3;
			this.YNumeric.ValueChanged += new System.EventHandler(this.YNumeric_ValueChanged);
			this.YNumeric.KeyUp += new System.Windows.Forms.KeyEventHandler(this.YNumeric_KeyUp);
			// 
			// VirtualPadTargetScreen
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label2);
			this.Controls.Add(this.YNumeric);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.XNumeric);
			this.Controls.Add(this.TargetPanel);
			this.Name = "VirtualPadTargetScreen";
			this.Size = new System.Drawing.Size(256, 254);
			this.Load += new System.EventHandler(this.VirtualPadTargetScreen_Load);
			((System.ComponentModel.ISupportInitialize)(this.XNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.YNumeric)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel TargetPanel;
		private System.Windows.Forms.NumericUpDown XNumeric;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown YNumeric;
	}
}
