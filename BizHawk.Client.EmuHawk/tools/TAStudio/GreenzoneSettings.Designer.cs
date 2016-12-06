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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StateHistorySettingsForm));
			this.CancelBtn = new System.Windows.Forms.Button();
			this.OkBtn = new System.Windows.Forms.Button();
			this.MemCapacityNumeric = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.SavestateSizeLabel = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.NumStatesLabel = new System.Windows.Forms.Label();
			this.DiskCapacityNumeric = new System.Windows.Forms.NumericUpDown();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.SaveCapacityNumeric = new System.Windows.Forms.NumericUpDown();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.NumSaveStatesLabel = new System.Windows.Forms.Label();
			this.BranchStatesInTasproj = new System.Windows.Forms.CheckBox();
			this.EraseBranchStatesFirst = new System.Windows.Forms.CheckBox();
			this.StateGap = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.NumFramesLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.MemCapacityNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.DiskCapacityNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.SaveCapacityNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.StateGap)).BeginInit();
			this.SuspendLayout();
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(217, 220);
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
			this.OkBtn.Location = new System.Drawing.Point(151, 220);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 1;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// MemCapacityNumeric
			// 
			this.MemCapacityNumeric.Location = new System.Drawing.Point(15, 49);
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
			this.label1.Location = new System.Drawing.Point(70, 52);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(21, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "mb";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(88, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Memory Capacity";
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
			this.label4.Location = new System.Drawing.Point(12, 136);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(84, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "Max num states:";
			// 
			// NumStatesLabel
			// 
			this.NumStatesLabel.AutoSize = true;
			this.NumStatesLabel.Location = new System.Drawing.Point(99, 136);
			this.NumStatesLabel.Name = "NumStatesLabel";
			this.NumStatesLabel.Size = new System.Drawing.Size(25, 13);
			this.NumStatesLabel.TabIndex = 9;
			this.NumStatesLabel.Text = "1kb";
			// 
			// DiskCapacityNumeric
			// 
			this.DiskCapacityNumeric.Location = new System.Drawing.Point(15, 113);
			this.DiskCapacityNumeric.Maximum = new decimal(new int[] {
            65536,
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
            512,
            0,
            0,
            0});
			this.DiskCapacityNumeric.ValueChanged += new System.EventHandler(this.CapacityNumeric_ValueChanged);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(70, 116);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(21, 13);
			this.label5.TabIndex = 4;
			this.label5.Text = "mb";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(12, 96);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(72, 13);
			this.label6.TabIndex = 5;
			this.label6.Text = "Disk Capacity";
			// 
			// SaveCapacityNumeric
			// 
			this.SaveCapacityNumeric.Location = new System.Drawing.Point(150, 49);
			this.SaveCapacityNumeric.Maximum = new decimal(new int[] {
            65536,
            0,
            0,
            0});
			this.SaveCapacityNumeric.Name = "SaveCapacityNumeric";
			this.SaveCapacityNumeric.Size = new System.Drawing.Size(55, 20);
			this.SaveCapacityNumeric.TabIndex = 3;
			this.SaveCapacityNumeric.Value = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.SaveCapacityNumeric.ValueChanged += new System.EventHandler(this.SaveCapacityNumeric_ValueChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(205, 52);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(21, 13);
			this.label7.TabIndex = 4;
			this.label7.Text = "mb";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(147, 32);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(112, 13);
			this.label8.TabIndex = 5;
			this.label8.Text = "Project Save Capacity";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(147, 72);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(84, 13);
			this.label9.TabIndex = 8;
			this.label9.Text = "Max num states:";
			// 
			// NumSaveStatesLabel
			// 
			this.NumSaveStatesLabel.AutoSize = true;
			this.NumSaveStatesLabel.Location = new System.Drawing.Point(234, 72);
			this.NumSaveStatesLabel.Name = "NumSaveStatesLabel";
			this.NumSaveStatesLabel.Size = new System.Drawing.Size(25, 13);
			this.NumSaveStatesLabel.TabIndex = 9;
			this.NumSaveStatesLabel.Text = "1kb";
			// 
			// BranchStatesInTasproj
			// 
			this.BranchStatesInTasproj.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.BranchStatesInTasproj.AutoSize = true;
			this.BranchStatesInTasproj.Location = new System.Drawing.Point(15, 165);
			this.BranchStatesInTasproj.Name = "BranchStatesInTasproj";
			this.BranchStatesInTasproj.Size = new System.Drawing.Size(158, 17);
			this.BranchStatesInTasproj.TabIndex = 10;
			this.BranchStatesInTasproj.Text = "Put branch states to .tasproj";
			this.BranchStatesInTasproj.UseVisualStyleBackColor = true;
			this.BranchStatesInTasproj.CheckedChanged += new System.EventHandler(this.BranchStatesInTasproj_CheckedChanged);
			// 
			// EraseBranchStatesFirst
			// 
			this.EraseBranchStatesFirst.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.EraseBranchStatesFirst.AutoSize = true;
			this.EraseBranchStatesFirst.Checked = true;
			this.EraseBranchStatesFirst.CheckState = System.Windows.Forms.CheckState.Checked;
			this.EraseBranchStatesFirst.Location = new System.Drawing.Point(15, 190);
			this.EraseBranchStatesFirst.Name = "EraseBranchStatesFirst";
			this.EraseBranchStatesFirst.Size = new System.Drawing.Size(139, 17);
			this.EraseBranchStatesFirst.TabIndex = 11;
			this.EraseBranchStatesFirst.Text = "Erase branch states first";
			this.EraseBranchStatesFirst.UseVisualStyleBackColor = true;
			this.EraseBranchStatesFirst.CheckedChanged += new System.EventHandler(this.EraseBranchStatesFIrst_CheckedChanged);
			// 
			// StateGap
			// 
			this.StateGap.Location = new System.Drawing.Point(151, 112);
			this.StateGap.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.StateGap.Name = "StateGap";
			this.StateGap.Size = new System.Drawing.Size(55, 20);
			this.StateGap.TabIndex = 12;
			this.StateGap.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.StateGap.ValueChanged += new System.EventHandler(this.StateGap_ValueChanged);
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(148, 96);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(91, 13);
			this.label10.TabIndex = 13;
			this.label10.Text = "Project State Gap";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(147, 135);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(61, 13);
			this.label11.TabIndex = 14;
			this.label11.Text = "State every";
			// 
			// NumFramesLabel
			// 
			this.NumFramesLabel.AutoSize = true;
			this.NumFramesLabel.Location = new System.Drawing.Point(204, 135);
			this.NumFramesLabel.Name = "NumFramesLabel";
			this.NumFramesLabel.Size = new System.Drawing.Size(47, 13);
			this.NumFramesLabel.TabIndex = 15;
			this.NumFramesLabel.Text = "0 frames";
			// 
			// StateHistorySettingsForm
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(289, 255);
			this.Controls.Add(this.NumFramesLabel);
			this.Controls.Add(this.label11);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.StateGap);
			this.Controls.Add(this.EraseBranchStatesFirst);
			this.Controls.Add(this.BranchStatesInTasproj);
			this.Controls.Add(this.NumSaveStatesLabel);
			this.Controls.Add(this.NumStatesLabel);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.SavestateSizeLabel);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.SaveCapacityNumeric);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.DiskCapacityNumeric);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.MemCapacityNumeric);
			this.Controls.Add(this.OkBtn);
			this.Controls.Add(this.CancelBtn);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(225, 165);
			this.Name = "StateHistorySettingsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Savestate History Settings";
			this.Load += new System.EventHandler(this.StateHistorySettings_Load);
			((System.ComponentModel.ISupportInitialize)(this.MemCapacityNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.DiskCapacityNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.SaveCapacityNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.StateGap)).EndInit();
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
		private NumericUpDown SaveCapacityNumeric;
		private Label label7;
		private Label label8;
		private Label label9;
		private Label NumSaveStatesLabel;
		private CheckBox BranchStatesInTasproj;
		private CheckBox EraseBranchStatesFirst;
		private NumericUpDown StateGap;
		private Label label10;
		private Label label11;
		private Label NumFramesLabel;
	}
}
