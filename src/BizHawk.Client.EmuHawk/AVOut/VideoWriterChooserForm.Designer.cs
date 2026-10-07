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
            this.components = new System.ComponentModel.Container();
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
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel4.SuspendLayout();
            this.panelSizeSelect.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkBoxResize
            // 
            resources.ApplyResources(this.checkBoxResize, "checkBoxResize");
            this.checkBoxResize.Name = "checkBoxResize";
            this.toolTip1.SetToolTip(this.checkBoxResize, resources.GetString("checkBoxResize.ToolTip"));
            this.checkBoxResize.UseVisualStyleBackColor = true;
            this.checkBoxResize.CheckedChanged += new System.EventHandler(this.CheckBoxResize_CheckedChanged);
            // 
            // listBox1
            // 
            resources.ApplyResources(this.listBox1, "listBox1");
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Name = "listBox1";
            this.toolTip1.SetToolTip(this.listBox1, resources.GetString("listBox1.ToolTip"));
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.ListBox1_SelectedIndexChanged);
            // 
            // buttonOK
            // 
            resources.ApplyResources(this.buttonOK, "buttonOK");
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Name = "buttonOK";
            this.toolTip1.SetToolTip(this.buttonOK, resources.GetString("buttonOK.ToolTip"));
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            this.toolTip1.SetToolTip(this.buttonCancel, resources.GetString("buttonCancel.ToolTip"));
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel4
            // 
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.labelDescription, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.labelDescriptionBody, 0, 1);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.toolTip1.SetToolTip(this.tableLayoutPanel4, resources.GetString("tableLayoutPanel4.ToolTip"));
            // 
            // labelDescription
            // 
            resources.ApplyResources(this.labelDescription, "labelDescription");
            this.labelDescription.Name = "labelDescription";
            this.toolTip1.SetToolTip(this.labelDescription, resources.GetString("labelDescription.ToolTip"));
            // 
            // labelDescriptionBody
            // 
            resources.ApplyResources(this.labelDescriptionBody, "labelDescriptionBody");
            this.labelDescriptionBody.Name = "labelDescriptionBody";
            this.toolTip1.SetToolTip(this.labelDescriptionBody, resources.GetString("labelDescriptionBody.ToolTip"));
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            this.toolTip1.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            this.toolTip1.SetToolTip(this.label4, resources.GetString("label4.ToolTip"));
            // 
            // buttonAuto
            // 
            resources.ApplyResources(this.buttonAuto, "buttonAuto");
            this.buttonAuto.Name = "buttonAuto";
            this.toolTip1.SetToolTip(this.buttonAuto, resources.GetString("buttonAuto.ToolTip"));
            this.buttonAuto.UseVisualStyleBackColor = true;
            this.buttonAuto.Click += new System.EventHandler(this.ButtonAuto_Click);
            // 
            // panelSizeSelect
            // 
            resources.ApplyResources(this.panelSizeSelect, "panelSizeSelect");
            this.panelSizeSelect.Controls.Add(this.lblSize);
            this.panelSizeSelect.Controls.Add(this.label4);
            this.panelSizeSelect.Controls.Add(this.buttonAuto);
            this.panelSizeSelect.Controls.Add(this.numericTextBoxW);
            this.panelSizeSelect.Controls.Add(this.numericTextBoxH);
            this.panelSizeSelect.Controls.Add(this.label3);
            this.panelSizeSelect.Name = "panelSizeSelect";
            this.toolTip1.SetToolTip(this.panelSizeSelect, resources.GetString("panelSizeSelect.ToolTip"));
            // 
            // lblSize
            // 
            resources.ApplyResources(this.lblSize, "lblSize");
            this.lblSize.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblSize.Name = "lblSize";
            this.toolTip1.SetToolTip(this.lblSize, resources.GetString("lblSize.ToolTip"));
            // 
            // numericTextBoxW
            // 
            resources.ApplyResources(this.numericTextBoxW, "numericTextBoxW");
            this.numericTextBoxW.AllowDecimal = false;
            this.numericTextBoxW.AllowNegative = false;
            this.numericTextBoxW.AllowSpace = false;
            this.numericTextBoxW.Name = "numericTextBoxW";
            this.toolTip1.SetToolTip(this.numericTextBoxW, resources.GetString("numericTextBoxW.ToolTip"));
            // 
            // numericTextBoxH
            // 
            resources.ApplyResources(this.numericTextBoxH, "numericTextBoxH");
            this.numericTextBoxH.AllowDecimal = false;
            this.numericTextBoxH.AllowNegative = false;
            this.numericTextBoxH.AllowSpace = false;
            this.numericTextBoxH.Name = "numericTextBoxH";
            this.toolTip1.SetToolTip(this.numericTextBoxH, resources.GetString("numericTextBoxH.ToolTip"));
            // 
            // checkBoxPad
            // 
            resources.ApplyResources(this.checkBoxPad, "checkBoxPad");
            this.checkBoxPad.Name = "checkBoxPad";
            this.toolTip1.SetToolTip(this.checkBoxPad, resources.GetString("checkBoxPad.ToolTip"));
            this.checkBoxPad.UseVisualStyleBackColor = true;
            // 
            // checkBoxASync
            // 
            resources.ApplyResources(this.checkBoxASync, "checkBoxASync");
            this.checkBoxASync.Name = "checkBoxASync";
            this.toolTip1.SetToolTip(this.checkBoxASync, resources.GetString("checkBoxASync.ToolTip"));
            this.checkBoxASync.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Controls.Add(this.lblResolutionWarning);
            this.panel1.Name = "panel1";
            this.toolTip1.SetToolTip(this.panel1, resources.GetString("panel1.ToolTip"));
            // 
            // lblResolutionWarning
            // 
            resources.ApplyResources(this.lblResolutionWarning, "lblResolutionWarning");
            this.lblResolutionWarning.Name = "lblResolutionWarning";
            this.toolTip1.SetToolTip(this.lblResolutionWarning, resources.GetString("lblResolutionWarning.ToolTip"));
            // 
            // VideoWriterChooserForm
            // 
            this.AcceptButton = this.buttonOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
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
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.panelSizeSelect.ResumeLayout(false);
            this.panelSizeSelect.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
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