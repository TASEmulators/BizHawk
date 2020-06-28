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
	public partial class StateHistorySettingsForm
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
			this.CancelBtn = new System.Windows.Forms.Button();
			this.OkBtn = new System.Windows.Forms.Button();
			this.MemCapacityNumeric = new System.Windows.Forms.NumericUpDown();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.SavestateSizeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.NumStatesLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.DiskCapacityNumeric = new System.Windows.Forms.NumericUpDown();
			this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.FileCapacityNumeric = new System.Windows.Forms.NumericUpDown();
			this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label8 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label9 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.NumSaveStatesLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.FileStateGapNumeric = new System.Windows.Forms.NumericUpDown();
			this.label10 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label11 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.FileNumFramesLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label14 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.MemFramesLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label13 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.MemStateGapDividerNumeric = new System.Windows.Forms.NumericUpDown();
			this.label12 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			((System.ComponentModel.ISupportInitialize)(this.MemCapacityNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.DiskCapacityNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.FileCapacityNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.FileStateGapNumeric)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MemStateGapDividerNumeric)).BeginInit();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(242, 225);
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
			this.OkBtn.Location = new System.Drawing.Point(176, 225);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 1;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// MemCapacityNumeric
			// 
			this.MemCapacityNumeric.Location = new System.Drawing.Point(12, 36);
			this.MemCapacityNumeric.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.MemCapacityNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.MemCapacityNumeric.Name = "MemCapacityNumeric";
			this.MemCapacityNumeric.Size = new System.Drawing.Size(55, 20);
			this.MemCapacityNumeric.TabIndex = 3;
			this.MemCapacityNumeric.Value = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.MemCapacityNumeric.ValueChanged += new System.EventHandler(this.CapacityNumeric_ValueChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(73, 39);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(23, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "MB";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 19);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(48, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Capacity";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 9);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(79, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Savestate size:";
			// 
			// SavestateSizeLabel
			// 
			this.SavestateSizeLabel.AutoSize = true;
			this.SavestateSizeLabel.Location = new System.Drawing.Point(94, 9);
			this.SavestateSizeLabel.Name = "SavestateSizeLabel";
			this.SavestateSizeLabel.Size = new System.Drawing.Size(25, 13);
			this.SavestateSizeLabel.TabIndex = 7;
			this.SavestateSizeLabel.Text = "1kb";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(160, 9);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(84, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "Max num states:";
			// 
			// NumStatesLabel
			// 
			this.NumStatesLabel.AutoSize = true;
			this.NumStatesLabel.Location = new System.Drawing.Point(250, 9);
			this.NumStatesLabel.Name = "NumStatesLabel";
			this.NumStatesLabel.Size = new System.Drawing.Size(13, 13);
			this.NumStatesLabel.TabIndex = 9;
			this.NumStatesLabel.Text = "1";
			// 
			// DiskCapacityNumeric
			// 
			this.DiskCapacityNumeric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DiskCapacityNumeric.Enabled = false;
			this.DiskCapacityNumeric.Location = new System.Drawing.Point(24, 215);
			this.DiskCapacityNumeric.Maximum = new decimal(new int[] {
            16384,
            0,
            0,
            0});
			this.DiskCapacityNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.DiskCapacityNumeric.Name = "DiskCapacityNumeric";
			this.DiskCapacityNumeric.Size = new System.Drawing.Size(55, 20);
			this.DiskCapacityNumeric.TabIndex = 3;
			this.DiskCapacityNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.DiskCapacityNumeric.ValueChanged += new System.EventHandler(this.CapacityNumeric_ValueChanged);
			// 
			// label5
			// 
			this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label5.AutoSize = true;
			this.label5.Enabled = false;
			this.label5.Location = new System.Drawing.Point(79, 218);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(23, 13);
			this.label5.TabIndex = 4;
			this.label5.Text = "MB";
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label6.AutoSize = true;
			this.label6.Enabled = false;
			this.label6.Location = new System.Drawing.Point(21, 198);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(72, 13);
			this.label6.TabIndex = 5;
			this.label6.Text = "Disk Capacity";
			// 
			// FileCapacityNumeric
			// 
			this.FileCapacityNumeric.Location = new System.Drawing.Point(12, 36);
			this.FileCapacityNumeric.Maximum = new decimal(new int[] {
            65536,
            0,
            0,
            0});
			this.FileCapacityNumeric.Name = "FileCapacityNumeric";
			this.FileCapacityNumeric.Size = new System.Drawing.Size(55, 20);
			this.FileCapacityNumeric.TabIndex = 3;
			this.FileCapacityNumeric.Value = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.FileCapacityNumeric.ValueChanged += new System.EventHandler(this.SaveCapacityNumeric_ValueChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(73, 39);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(23, 13);
			this.label7.TabIndex = 4;
			this.label7.Text = "MB";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(9, 19);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(48, 13);
			this.label8.TabIndex = 5;
			this.label8.Text = "Capacity";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(9, 59);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(84, 13);
			this.label9.TabIndex = 8;
			this.label9.Text = "Max num states:";
			// 
			// NumSaveStatesLabel
			// 
			this.NumSaveStatesLabel.AutoSize = true;
			this.NumSaveStatesLabel.Location = new System.Drawing.Point(96, 59);
			this.NumSaveStatesLabel.Name = "NumSaveStatesLabel";
			this.NumSaveStatesLabel.Size = new System.Drawing.Size(13, 13);
			this.NumSaveStatesLabel.TabIndex = 9;
			this.NumSaveStatesLabel.Text = "1";
			// 
			// FileStateGapNumeric
			// 
			this.FileStateGapNumeric.Location = new System.Drawing.Point(12, 100);
			this.FileStateGapNumeric.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.FileStateGapNumeric.Name = "FileStateGapNumeric";
			this.FileStateGapNumeric.Size = new System.Drawing.Size(55, 20);
			this.FileStateGapNumeric.TabIndex = 12;
			this.FileStateGapNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.FileStateGapNumeric.ValueChanged += new System.EventHandler(this.FileStateGap_ValueChanged);
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(9, 84);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(55, 13);
			this.label10.TabIndex = 13;
			this.label10.Text = "State Gap";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(8, 123);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(61, 13);
			this.label11.TabIndex = 14;
			this.label11.Text = "State every";
			// 
			// FileNumFramesLabel
			// 
			this.FileNumFramesLabel.AutoSize = true;
			this.FileNumFramesLabel.Location = new System.Drawing.Point(65, 123);
			this.FileNumFramesLabel.Name = "FileNumFramesLabel";
			this.FileNumFramesLabel.Size = new System.Drawing.Size(47, 13);
			this.FileNumFramesLabel.TabIndex = 15;
			this.FileNumFramesLabel.Text = "0 frames";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label14);
			this.groupBox1.Controls.Add(this.MemFramesLabel);
			this.groupBox1.Controls.Add(this.label13);
			this.groupBox1.Controls.Add(this.MemStateGapDividerNumeric);
			this.groupBox1.Controls.Add(this.label12);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.MemCapacityNumeric);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Location = new System.Drawing.Point(12, 34);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(143, 147);
			this.groupBox1.TabIndex = 16;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Memory Usage";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(73, 102);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(21, 13);
			this.label14.TabIndex = 17;
			this.label14.Text = "KB";
			// 
			// MemFramesLabel
			// 
			this.MemFramesLabel.AutoSize = true;
			this.MemFramesLabel.Location = new System.Drawing.Point(66, 123);
			this.MemFramesLabel.Name = "MemFramesLabel";
			this.MemFramesLabel.Size = new System.Drawing.Size(47, 13);
			this.MemFramesLabel.TabIndex = 16;
			this.MemFramesLabel.Text = "0 frames";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(9, 123);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(61, 13);
			this.label13.TabIndex = 15;
			this.label13.Text = "State every";
			// 
			// MemStateGapDividerNumeric
			// 
			this.MemStateGapDividerNumeric.Location = new System.Drawing.Point(12, 100);
			this.MemStateGapDividerNumeric.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.MemStateGapDividerNumeric.Minimum = new decimal(new int[] {
            64,
            0,
            0,
            0});
			this.MemStateGapDividerNumeric.Name = "MemStateGapDividerNumeric";
			this.MemStateGapDividerNumeric.Size = new System.Drawing.Size(55, 20);
			this.MemStateGapDividerNumeric.TabIndex = 7;
			this.MemStateGapDividerNumeric.Value = new decimal(new int[] {
            65,
            0,
            0,
            0});
			this.MemStateGapDividerNumeric.ValueChanged += new System.EventHandler(this.MemStateGapDivider_ValueChanged);
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(9, 84);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(91, 13);
			this.label12.TabIndex = 6;
			this.label12.Text = "State Gap Divider";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label8);
			this.groupBox2.Controls.Add(this.FileCapacityNumeric);
			this.groupBox2.Controls.Add(this.FileNumFramesLabel);
			this.groupBox2.Controls.Add(this.label7);
			this.groupBox2.Controls.Add(this.label11);
			this.groupBox2.Controls.Add(this.label9);
			this.groupBox2.Controls.Add(this.label10);
			this.groupBox2.Controls.Add(this.NumSaveStatesLabel);
			this.groupBox2.Controls.Add(this.FileStateGapNumeric);
			this.groupBox2.Location = new System.Drawing.Point(163, 34);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(138, 147);
			this.groupBox2.TabIndex = 17;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Project File";
			// 
			// StateHistorySettingsForm
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(315, 260);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.DiskCapacityNumeric);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.SavestateSizeLabel);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.NumStatesLabel);
			this.Controls.Add(this.OkBtn);
			this.Controls.Add(this.CancelBtn);
			this.MinimumSize = new System.Drawing.Size(225, 165);
			this.Name = "StateHistorySettingsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Savestate History Settings";
			this.Load += new System.EventHandler(this.StateHistorySettings_Load);
			((System.ComponentModel.ISupportInitialize)(this.MemCapacityNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.DiskCapacityNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.FileCapacityNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.FileStateGapNumeric)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MemStateGapDividerNumeric)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		private Button CancelBtn;
		private Button OkBtn;
		private NumericUpDown MemCapacityNumeric;
		private Label label1;
		private Label label2;
		private Label label3;
		private Label SavestateSizeLabel;
		private Label label4;
		private Label NumStatesLabel;
		private NumericUpDown DiskCapacityNumeric;
		private Label label5;
		private Label label6;
		private NumericUpDown FileCapacityNumeric;
		private Label label7;
		private Label label8;
		private Label label9;
		private Label NumSaveStatesLabel;
		private NumericUpDown FileStateGapNumeric;
		private Label label10;
		private Label label11;
		private Label FileNumFramesLabel;
		private GroupBox groupBox1;
		private NumericUpDown MemStateGapDividerNumeric;
		private Label label12;
		private GroupBox groupBox2;
		private Label MemFramesLabel;
		private Label label13;
		private Label label14;
	}
}
