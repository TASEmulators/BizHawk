using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class GreenzoneSettingsForm
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

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GreenzoneSettingsForm));
			this.CancelBtn = new System.Windows.Forms.Button();
			this.OkBtn = new System.Windows.Forms.Button();
			this.SaveGreenzoneCheckbox = new System.Windows.Forms.CheckBox();
			this.CapacityNumeric = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.SavestateSizeLabel = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.NumStatesLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.CapacityNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(236, 96);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 0;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(170, 96);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 1;
			this.OkBtn.Text = "&Ok";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// SaveGreenzoneCheckbox
			// 
			this.SaveGreenzoneCheckbox.AutoSize = true;
			this.SaveGreenzoneCheckbox.Location = new System.Drawing.Point(13, 20);
			this.SaveGreenzoneCheckbox.Name = "SaveGreenzoneCheckbox";
			this.SaveGreenzoneCheckbox.Size = new System.Drawing.Size(234, 17);
			this.SaveGreenzoneCheckbox.TabIndex = 2;
			this.SaveGreenzoneCheckbox.Text = "Save savestate history information in proj file";
			this.SaveGreenzoneCheckbox.UseVisualStyleBackColor = true;
			// 
			// CapacityNumeric
			// 
			this.CapacityNumeric.Location = new System.Drawing.Point(13, 67);
			this.CapacityNumeric.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.CapacityNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.CapacityNumeric.Name = "CapacityNumeric";
			this.CapacityNumeric.Size = new System.Drawing.Size(48, 20);
			this.CapacityNumeric.TabIndex = 3;
			this.CapacityNumeric.Value = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.CapacityNumeric.ValueChanged += new System.EventHandler(this.CapacityNumeric_ValueChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(61, 70);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(21, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "mb";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(13, 50);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(99, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Savestate Capacity";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(127, 50);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(79, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Savestate size:";
			// 
			// SavestateSizeLabel
			// 
			this.SavestateSizeLabel.AutoSize = true;
			this.SavestateSizeLabel.Location = new System.Drawing.Point(209, 50);
			this.SavestateSizeLabel.Name = "SavestateSizeLabel";
			this.SavestateSizeLabel.Size = new System.Drawing.Size(25, 13);
			this.SavestateSizeLabel.TabIndex = 7;
			this.SavestateSizeLabel.Text = "1kb";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(122, 70);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(84, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "Max num states:";
			// 
			// NumStatesLabel
			// 
			this.NumStatesLabel.AutoSize = true;
			this.NumStatesLabel.Location = new System.Drawing.Point(209, 70);
			this.NumStatesLabel.Name = "NumStatesLabel";
			this.NumStatesLabel.Size = new System.Drawing.Size(25, 13);
			this.NumStatesLabel.TabIndex = 9;
			this.NumStatesLabel.Text = "1kb";
			// 
			// GreenzoneSettingsForm
			// 
			this.AcceptButton = this.OkBtn;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(308, 131);
			this.Controls.Add(this.NumStatesLabel);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.SavestateSizeLabel);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.CapacityNumeric);
			this.Controls.Add(this.SaveGreenzoneCheckbox);
			this.Controls.Add(this.OkBtn);
			this.Controls.Add(this.CancelBtn);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(225, 165);
			this.Name = "GreenzoneSettingsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Savestate History Settings";
			this.Load += new System.EventHandler(this.GreenzoneSettings_Load);
			((System.ComponentModel.ISupportInitialize)(this.CapacityNumeric)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		private Button CancelBtn;
		private Button OkBtn;
		private CheckBox SaveGreenzoneCheckbox;
		private NumericUpDown CapacityNumeric;
		private Label label1;
		private Label label2;
		private Label label3;
		private Label SavestateSizeLabel;
		private Label label4;
		private Label NumStatesLabel;
	}
}
