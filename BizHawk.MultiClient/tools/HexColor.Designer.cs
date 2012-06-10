namespace BizHawk.MultiClient
{
    partial class HexColors_Form
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.HexMenubar = new System.Windows.Forms.Panel();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.HexForegrnd = new System.Windows.Forms.Panel();
			this.HexBackgrnd = new System.Windows.Forms.Panel();
			this.colorDialog1 = new System.Windows.Forms.ColorDialog();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.HexMenubar);
			this.groupBox1.Location = new System.Drawing.Point(3, 2);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(154, 173);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			// 
			// HexMenubar
			// 
			this.HexMenubar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.HexMenubar.Location = new System.Drawing.Point(4, 122);
			this.HexMenubar.Name = "HexMenubar";
			this.HexMenubar.Size = new System.Drawing.Size(46, 42);
			this.HexMenubar.TabIndex = 8;
			this.HexMenubar.MouseClick += new System.Windows.Forms.MouseEventHandler(this.HexMenubar_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(59, 143);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(76, 13);
			this.label3.TabIndex = 11;
			this.label3.Text = "Menubar Color";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(59, 86);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(55, 13);
			this.label2.TabIndex = 10;
			this.label2.Text = "Font Color";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(59, 30);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(92, 13);
			this.label1.TabIndex = 9;
			this.label1.Text = "Background Color";
			// 
			// HexForegrnd
			// 
			this.HexForegrnd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.HexForegrnd.Location = new System.Drawing.Point(7, 71);
			this.HexForegrnd.Name = "HexForegrnd";
			this.HexForegrnd.Size = new System.Drawing.Size(46, 42);
			this.HexForegrnd.TabIndex = 7;
			this.HexForegrnd.MouseClick += new System.Windows.Forms.MouseEventHandler(this.HexForegrnd_Click);
			// 
			// HexBackgrnd
			// 
			this.HexBackgrnd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.HexBackgrnd.Location = new System.Drawing.Point(7, 15);
			this.HexBackgrnd.Name = "HexBackgrnd";
			this.HexBackgrnd.Size = new System.Drawing.Size(46, 42);
			this.HexBackgrnd.TabIndex = 6;
			this.HexBackgrnd.MouseClick += new System.Windows.Forms.MouseEventHandler(this.HexBackgrnd_Click);
			// 
			// HexColors_Form
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(159, 178);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.HexForegrnd);
			this.Controls.Add(this.HexBackgrnd);
			this.Controls.Add(this.groupBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "HexColors_Form";
			this.Text = "Colors";
			this.Load += new System.EventHandler(this.HexColors_Form_Load);
			this.groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel HexForegrnd;
        private System.Windows.Forms.Panel HexBackgrnd;
        private System.Windows.Forms.ColorDialog colorDialog1;
		private System.Windows.Forms.Panel HexMenubar;

    }
}