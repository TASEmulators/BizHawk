namespace BizHawk.Client.EmuHawk
{
	partial class SameBoyColorChooserForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.OK = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.buttonInterpolateBG = new System.Windows.Forms.Button();
            this.buttonLoad = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Location = new System.Drawing.Point(56, 18);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(40, 32);
            this.panel1.TabIndex = 0;
            this.panel1.DoubleClick += new System.EventHandler(this.Panel12_DoubleClick);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Location = new System.Drawing.Point(103, 18);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(40, 32);
            this.panel2.TabIndex = 1;
            this.panel2.DoubleClick += new System.EventHandler(this.Panel12_DoubleClick);
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel3.Location = new System.Drawing.Point(149, 18);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(40, 32);
            this.panel3.TabIndex = 2;
            this.panel3.DoubleClick += new System.EventHandler(this.Panel12_DoubleClick);
            // 
            // panel4
            // 
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel4.Location = new System.Drawing.Point(195, 18);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(40, 32);
            this.panel4.TabIndex = 3;
            this.panel4.DoubleClick += new System.EventHandler(this.Panel12_DoubleClick);
            // 
            // panel5
            // 
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel5.Location = new System.Drawing.Point(241, 18);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(40, 32);
            this.panel5.TabIndex = 4;
            this.panel5.DoubleClick += new System.EventHandler(this.Panel12_DoubleClick);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(14, 27);
            this.label1.Name = "label1";
            this.label1.Text = "Colors";
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OK.Location = new System.Drawing.Point(206, 84);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 22;
            this.OK.Text = "&OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(287, 84);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 23;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            // 
            // buttonInterpolateBG
            // 
            this.buttonInterpolateBG.Location = new System.Drawing.Point(287, 22);
            this.buttonInterpolateBG.Name = "buttonInterpolateBG";
            this.buttonInterpolateBG.Size = new System.Drawing.Size(75, 23);
            this.buttonInterpolateBG.TabIndex = 25;
            this.buttonInterpolateBG.Text = "Interpolate";
            this.buttonInterpolateBG.UseVisualStyleBackColor = true;
            this.buttonInterpolateBG.Click += new System.EventHandler(this.Button3_Click);
            // 
            // buttonLoad
            // 
            this.buttonLoad.Location = new System.Drawing.Point(17, 84);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(60, 23);
            this.buttonLoad.TabIndex = 28;
            this.buttonLoad.Text = "&Load...";
            this.buttonLoad.UseVisualStyleBackColor = true;
            this.buttonLoad.Click += new System.EventHandler(this.Button6_Click);
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new System.Drawing.Point(83, 84);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(60, 23);
            this.buttonSave.TabIndex = 29;
            this.buttonSave.Text = "&Save...";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.Button7_Click);
            // 
            // SameBoyColorChooserForm
            // 
            this.AcceptButton = this.OK;
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(373, 119);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.buttonLoad);
            this.Controls.Add(this.buttonInterpolateBG);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.MinimumSize = new System.Drawing.Size(310, 140);
            this.Name = "SameBoyColorChooserForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SameBoy Palette Config";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.ColorChooserForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.ColorChooserForm_DragEnter);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Panel panel5;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Button buttonInterpolateBG;
		private System.Windows.Forms.Button buttonLoad;
		private System.Windows.Forms.Button buttonSave;
	}
}