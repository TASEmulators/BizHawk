namespace BizHawk.Client.EmuHawk
{
	partial class VideoWriterChooserForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VideoWriterChooserForm));
			this.checkBoxResize = new System.Windows.Forms.CheckBox();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
			this.labelDescription = new BizHawk.WinForms.Controls.LocLabelEx();
			this.labelDescriptionBody = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.buttonAuto = new System.Windows.Forms.Button();
			this.panelSizeSelect = new System.Windows.Forms.Panel();
			this.lblSize = new BizHawk.WinForms.Controls.LocLabelEx();
			this.numericTextBoxW = new BizHawk.Client.EmuHawk.NumericTextBox();
			this.numericTextBoxH = new BizHawk.Client.EmuHawk.NumericTextBox();
			this.checkBoxPad = new System.Windows.Forms.CheckBox();
			this.checkBoxASync = new System.Windows.Forms.CheckBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.lblResolutionWarning = new BizHawk.WinForms.Controls.LocLabelEx();
			this.toolTip1 = new System.Windows.Forms.ToolTip();
			this.tableLayoutPanel4.SuspendLayout();
			this.panelSizeSelect.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// checkBoxResize
			// 
			this.checkBoxResize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxResize.AutoSize = true;
			this.checkBoxResize.Location = new System.Drawing.Point(347, 35);
			this.checkBoxResize.Name = "checkBoxResize";
			this.checkBoxResize.Size = new System.Drawing.Size(88, 17);
			this.checkBoxResize.TabIndex = 9;
			this.checkBoxResize.Text = "Resize Video";
			this.checkBoxResize.UseVisualStyleBackColor = true;
			this.checkBoxResize.CheckedChanged += new System.EventHandler(this.CheckBoxResize_CheckedChanged);
			// 
			// listBox1
			// 
			this.listBox1.FormattingEnabled = true;
			this.listBox1.IntegralHeight = false;
			this.listBox1.Location = new System.Drawing.Point(12, 12);
			this.listBox1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(329, 202);
			this.listBox1.TabIndex = 0;
			this.listBox1.SelectedIndexChanged += new System.EventHandler(this.ListBox1_SelectedIndexChanged);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(373, 405);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(65, 23);
			this.buttonOK.TabIndex = 1;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.ButtonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(444, 405);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(65, 23);
			this.buttonCancel.TabIndex = 2;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// tableLayoutPanel4
			// 
			this.tableLayoutPanel4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.tableLayoutPanel4.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Inset;
			this.tableLayoutPanel4.ColumnCount = 1;
			this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel4.Controls.Add(this.labelDescription, 0, 0);
			this.tableLayoutPanel4.Controls.Add(this.labelDescriptionBody, 0, 1);
			this.tableLayoutPanel4.Location = new System.Drawing.Point(12, 220);
			this.tableLayoutPanel4.Name = "tableLayoutPanel4";
			this.tableLayoutPanel4.RowCount = 2;
			this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel4.Size = new System.Drawing.Size(329, 208);
			this.tableLayoutPanel4.TabIndex = 8;
			// 
			// labelDescription
			// 
			this.labelDescription.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelDescription.Location = new System.Drawing.Point(5, 2);
			this.labelDescription.Name = "labelDescription";
			this.labelDescription.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.labelDescription.Text = "Description:";
			// 
			// labelDescriptionBody
			// 
			this.labelDescriptionBody.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelDescriptionBody.Location = new System.Drawing.Point(5, 23);
			this.labelDescriptionBody.Name = "labelDescriptionBody";
			this.labelDescriptionBody.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.labelDescriptionBody.Text = resources.GetString("labelDescriptionBody.Text");
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(74, 23);
			this.label3.Name = "label3";
			this.label3.Text = "X";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(3, 0);
			this.label4.Name = "label4";
			this.label4.Text = "Resize:";
			// 
			// buttonAuto
			// 
			this.buttonAuto.Location = new System.Drawing.Point(89, 42);
			this.buttonAuto.Name = "buttonAuto";
			this.buttonAuto.Size = new System.Drawing.Size(70, 34);
			this.buttonAuto.TabIndex = 14;
			this.buttonAuto.Text = "/\\ Set As Resize";
			this.buttonAuto.UseVisualStyleBackColor = true;
			this.buttonAuto.Click += new System.EventHandler(this.ButtonAuto_Click);
			// 
			// panelSizeSelect
			// 
			this.panelSizeSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.panelSizeSelect.Controls.Add(this.lblSize);
			this.panelSizeSelect.Controls.Add(this.label4);
			this.panelSizeSelect.Controls.Add(this.buttonAuto);
			this.panelSizeSelect.Controls.Add(this.numericTextBoxW);
			this.panelSizeSelect.Controls.Add(this.numericTextBoxH);
			this.panelSizeSelect.Controls.Add(this.label3);
			this.panelSizeSelect.Location = new System.Drawing.Point(347, 58);
			this.panelSizeSelect.Name = "panelSizeSelect";
			this.panelSizeSelect.Size = new System.Drawing.Size(162, 84);
			this.panelSizeSelect.TabIndex = 15;
			// 
			// lblSize
			// 
			this.lblSize.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblSize.Location = new System.Drawing.Point(3, 42);
			this.lblSize.Name = "lblSize";
			this.lblSize.Text = "Size:\r\nTestxTest";
			// 
			// numericTextBoxW
			// 
			this.numericTextBoxW.AllowDecimal = false;
			this.numericTextBoxW.AllowNegative = false;
			this.numericTextBoxW.AllowSpace = false;
			this.numericTextBoxW.Location = new System.Drawing.Point(0, 16);
			this.numericTextBoxW.Name = "numericTextBoxW";
			this.numericTextBoxW.Size = new System.Drawing.Size(70, 20);
			this.numericTextBoxW.TabIndex = 10;
			// 
			// numericTextBoxH
			// 
			this.numericTextBoxH.AllowDecimal = false;
			this.numericTextBoxH.AllowNegative = false;
			this.numericTextBoxH.AllowSpace = false;
			this.numericTextBoxH.Location = new System.Drawing.Point(92, 16);
			this.numericTextBoxH.Name = "numericTextBoxH";
			this.numericTextBoxH.Size = new System.Drawing.Size(70, 20);
			this.numericTextBoxH.TabIndex = 11;
			// 
			// checkBoxPad
			// 
			this.checkBoxPad.AutoSize = true;
			this.checkBoxPad.Location = new System.Drawing.Point(444, 35);
			this.checkBoxPad.Name = "checkBoxPad";
			this.checkBoxPad.Size = new System.Drawing.Size(45, 17);
			this.checkBoxPad.TabIndex = 15;
			this.checkBoxPad.Text = "Pad";
			this.checkBoxPad.UseVisualStyleBackColor = true;
			// 
			// checkBoxASync
			// 
			this.checkBoxASync.AutoSize = true;
			this.checkBoxASync.Location = new System.Drawing.Point(347, 12);
			this.checkBoxASync.Name = "checkBoxASync";
			this.checkBoxASync.Size = new System.Drawing.Size(95, 17);
			this.checkBoxASync.TabIndex = 16;
			this.checkBoxASync.Text = "Sync to Audio";
			this.checkBoxASync.UseVisualStyleBackColor = true;
			toolTip1.SetToolTip(checkBoxASync, "Configures A/V Sync strategy for Variable Frame Rate movies. If checked, drops or repeats frames to match audio sync. If unchecked, stretches audio to match video sync, resulting in pitch issues");
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.lblResolutionWarning);
			this.panel1.Location = new System.Drawing.Point(347, 148);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(162, 93);
			this.panel1.TabIndex = 17;
			// 
			// lblResolutionWarning
			// 
			this.lblResolutionWarning.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblResolutionWarning.Location = new System.Drawing.Point(4, 4);
			this.lblResolutionWarning.Name = "lblResolutionWarning";
			this.lblResolutionWarning.MaximumSize = new System.Drawing.Size(162, 0);
			this.lblResolutionWarning.Text = "Resolution is not a multiple of 4! Odd or non-x4 resolutions breaks many codecs. " +
    "Check your output carefully and adjust the window size or codec settings if need" +
    "ed.";
			// 
			// VideoWriterChooserForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(521, 440);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.checkBoxPad);
			this.Controls.Add(this.checkBoxASync);
			this.Controls.Add(this.panelSizeSelect);
			this.Controls.Add(this.tableLayoutPanel4);
			this.Controls.Add(this.listBox1);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.checkBoxResize);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "VideoWriterChooserForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Choose A/V Writer";
			this.tableLayoutPanel4.ResumeLayout(false);
			this.tableLayoutPanel4.PerformLayout();
			this.panelSizeSelect.ResumeLayout(false);
			this.panelSizeSelect.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxResize;
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
		private BizHawk.WinForms.Controls.LocLabelEx labelDescription;
		private BizHawk.WinForms.Controls.LocLabelEx labelDescriptionBody;
		private NumericTextBox numericTextBoxW;
		private NumericTextBox numericTextBoxH;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private System.Windows.Forms.Button buttonAuto;
		private System.Windows.Forms.Panel panelSizeSelect;
		private System.Windows.Forms.CheckBox checkBoxPad;
		private System.Windows.Forms.CheckBox checkBoxASync;
		private System.Windows.Forms.ToolTip toolTip1;
		private BizHawk.WinForms.Controls.LocLabelEx lblSize;
		private System.Windows.Forms.Panel panel1;
		private BizHawk.WinForms.Controls.LocLabelEx lblResolutionWarning;
	}
}