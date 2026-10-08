namespace BizHawk.Client.EmuHawk
{
    partial class HexColorsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HexColorsForm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.HexFreezeHL = new System.Windows.Forms.Panel();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.HexFreeze = new System.Windows.Forms.Panel();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.HexHighlight = new System.Windows.Forms.Panel();
            this.HexForegrnd = new System.Windows.Forms.Panel();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.HexMenubar = new System.Windows.Forms.Panel();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.HexBackgrnd = new System.Windows.Forms.Panel();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.HexFreezeHL);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.HexFreeze);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.HexHighlight);
            this.groupBox1.Controls.Add(this.HexForegrnd);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.HexMenubar);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.HexBackgrnd);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // HexFreezeHL
            // 
            resources.ApplyResources(this.HexFreezeHL, "HexFreezeHL");
            this.HexFreezeHL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.HexFreezeHL.Name = "HexFreezeHL";
            this.HexFreezeHL.MouseClick += new System.Windows.Forms.MouseEventHandler(this.HexFreezeHL_Click);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // HexFreeze
            // 
            resources.ApplyResources(this.HexFreeze, "HexFreeze");
            this.HexFreeze.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.HexFreeze.Name = "HexFreeze";
            this.HexFreeze.MouseClick += new System.Windows.Forms.MouseEventHandler(this.HexFreeze_Click);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // HexHighlight
            // 
            resources.ApplyResources(this.HexHighlight, "HexHighlight");
            this.HexHighlight.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.HexHighlight.Name = "HexHighlight";
            this.HexHighlight.MouseClick += new System.Windows.Forms.MouseEventHandler(this.HexHighlight_Click);
            // 
            // HexForegrnd
            // 
            resources.ApplyResources(this.HexForegrnd, "HexForegrnd");
            this.HexForegrnd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.HexForegrnd.Name = "HexForegrnd";
            this.HexForegrnd.MouseClick += new System.Windows.Forms.MouseEventHandler(this.HexForeground_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // HexMenubar
            // 
            resources.ApplyResources(this.HexMenubar, "HexMenubar");
            this.HexMenubar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.HexMenubar.Name = "HexMenubar";
            this.HexMenubar.MouseClick += new System.Windows.Forms.MouseEventHandler(this.HexMenuBar_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // HexBackgrnd
            // 
            resources.ApplyResources(this.HexBackgrnd, "HexBackgrnd");
            this.HexBackgrnd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.HexBackgrnd.Name = "HexBackgrnd";
            this.HexBackgrnd.MouseClick += new System.Windows.Forms.MouseEventHandler(this.HexBackground_Click);
            // 
            // HexColorsForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HexColorsForm";
            this.Load += new System.EventHandler(this.HexColors_Form_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private BizHawk.WinForms.Controls.LocLabelEx label3;
        private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
        private System.Windows.Forms.Panel HexForegrnd;
        private System.Windows.Forms.Panel HexBackgrnd;
        private System.Windows.Forms.ColorDialog colorDialog1;
		private System.Windows.Forms.Panel HexMenubar;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private System.Windows.Forms.Panel HexFreezeHL;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private System.Windows.Forms.Panel HexFreeze;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private System.Windows.Forms.Panel HexHighlight;

    }
}