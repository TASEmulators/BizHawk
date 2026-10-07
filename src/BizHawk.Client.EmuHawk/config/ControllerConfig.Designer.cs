namespace BizHawk.Client.EmuHawk
{
	partial class ControllerConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ControllerConfig));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.NormalControlsTab = new System.Windows.Forms.TabPage();
            this.AutofireControlsTab = new System.Windows.Forms.TabPage();
            this.AnalogControlsTab = new System.Windows.Forms.TabPage();
            this.FeedbacksTab = new System.Windows.Forms.TabPage();
            this.checkBoxAutoTab = new System.Windows.Forms.CheckBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.testToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.loadDefaultsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.clearToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label38 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btnMisc = new BizHawk.Client.EmuHawk.MenuButton();
            this.flpUDLR = new BizHawk.WinForms.Controls.LocSingleRowFLP();
            this.lblUDLR = new BizHawk.WinForms.Controls.LabelEx();
            this.rbUDLRForbid = new BizHawk.WinForms.Controls.RadioButtonEx();
            this.rbUDLRPriority = new BizHawk.WinForms.Controls.RadioButtonEx();
            this.rbUDLRAllow = new BizHawk.WinForms.Controls.RadioButtonEx();
            this.tabControl1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.flpUDLR.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.NormalControlsTab);
            this.tabControl1.Controls.Add(this.AutofireControlsTab);
            this.tabControl1.Controls.Add(this.AnalogControlsTab);
            this.tabControl1.Controls.Add(this.FeedbacksTab);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // NormalControlsTab
            // 
            resources.ApplyResources(this.NormalControlsTab, "NormalControlsTab");
            this.NormalControlsTab.Name = "NormalControlsTab";
            this.NormalControlsTab.UseVisualStyleBackColor = true;
            // 
            // AutofireControlsTab
            // 
            resources.ApplyResources(this.AutofireControlsTab, "AutofireControlsTab");
            this.AutofireControlsTab.Name = "AutofireControlsTab";
            this.AutofireControlsTab.UseVisualStyleBackColor = true;
            // 
            // AnalogControlsTab
            // 
            resources.ApplyResources(this.AnalogControlsTab, "AnalogControlsTab");
            this.AnalogControlsTab.Name = "AnalogControlsTab";
            this.AnalogControlsTab.UseVisualStyleBackColor = true;
            // 
            // FeedbacksTab
            // 
            resources.ApplyResources(this.FeedbacksTab, "FeedbacksTab");
            this.FeedbacksTab.Name = "FeedbacksTab";
            this.FeedbacksTab.UseVisualStyleBackColor = true;
            // 
            // checkBoxAutoTab
            // 
            resources.ApplyResources(this.checkBoxAutoTab, "checkBoxAutoTab");
            this.checkBoxAutoTab.Name = "checkBoxAutoTab";
            this.checkBoxAutoTab.UseVisualStyleBackColor = true;
            this.checkBoxAutoTab.CheckedChanged += new System.EventHandler(this.CheckBoxAutoTab_CheckedChanged);
            // 
            // buttonOK
            // 
            resources.ApplyResources(this.buttonOK, "buttonOK");
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.ButtonOk_Click);
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.tabControl1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.pictureBox1, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // contextMenuStrip1
            // 
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.testToolStripMenuItem,
            this.loadDefaultsToolStripMenuItem,
            this.clearToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            // 
            // testToolStripMenuItem
            // 
            resources.ApplyResources(this.testToolStripMenuItem, "testToolStripMenuItem");
            this.testToolStripMenuItem.Click += new System.EventHandler(this.ButtonSaveDefaults_Click);
            // 
            // loadDefaultsToolStripMenuItem
            // 
            resources.ApplyResources(this.loadDefaultsToolStripMenuItem, "loadDefaultsToolStripMenuItem");
            this.loadDefaultsToolStripMenuItem.Click += new System.EventHandler(this.ButtonLoadDefaults_Click);
            // 
            // clearToolStripMenuItem
            // 
            resources.ApplyResources(this.clearToolStripMenuItem, "clearToolStripMenuItem");
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.ClearBtn_Click);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            this.toolTip1.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
            // 
            // label38
            // 
            resources.ApplyResources(this.label38, "label38");
            this.label38.Name = "label38";
            this.toolTip1.SetToolTip(this.label38, resources.GetString("label38.ToolTip"));
            // 
            // btnMisc
            // 
            resources.ApplyResources(this.btnMisc, "btnMisc");
            this.btnMisc.Menu = this.contextMenuStrip1;
            this.btnMisc.Name = "btnMisc";
            this.toolTip1.SetToolTip(this.btnMisc, resources.GetString("btnMisc.ToolTip"));
            this.btnMisc.UseVisualStyleBackColor = true;
            // 
            // flpUDLR
            // 
            resources.ApplyResources(this.flpUDLR, "flpUDLR");
            this.flpUDLR.Controls.Add(this.lblUDLR);
            this.flpUDLR.Controls.Add(this.rbUDLRForbid);
            this.flpUDLR.Controls.Add(this.rbUDLRPriority);
            this.flpUDLR.Controls.Add(this.rbUDLRAllow);
            this.flpUDLR.Name = "flpUDLR";
            this.toolTip1.SetToolTip(this.flpUDLR, resources.GetString("flpUDLR.ToolTip"));
            // 
            // lblUDLR
            // 
            resources.ApplyResources(this.lblUDLR, "lblUDLR");
            this.lblUDLR.Name = "lblUDLR";
            this.toolTip1.SetToolTip(this.lblUDLR, resources.GetString("lblUDLR.ToolTip"));
            // 
            // rbUDLRForbid
            // 
            resources.ApplyResources(this.rbUDLRForbid, "rbUDLRForbid");
            this.rbUDLRForbid.Name = "rbUDLRForbid";
            this.toolTip1.SetToolTip(this.rbUDLRForbid, resources.GetString("rbUDLRForbid.ToolTip"));
            // 
            // rbUDLRPriority
            // 
            resources.ApplyResources(this.rbUDLRPriority, "rbUDLRPriority");
            this.rbUDLRPriority.Name = "rbUDLRPriority";
            this.toolTip1.SetToolTip(this.rbUDLRPriority, resources.GetString("rbUDLRPriority.ToolTip"));
            // 
            // rbUDLRAllow
            // 
            resources.ApplyResources(this.rbUDLRAllow, "rbUDLRAllow");
            this.rbUDLRAllow.Name = "rbUDLRAllow";
            this.toolTip1.SetToolTip(this.rbUDLRAllow, resources.GetString("rbUDLRAllow.ToolTip"));
            // 
            // ControllerConfig
            // 
            this.AcceptButton = this.buttonOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.Controls.Add(this.flpUDLR);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label38);
            this.Controls.Add(this.btnMisc);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.checkBoxAutoTab);
            this.Name = "ControllerConfig";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ControllerConfig_FormClosed);
            this.Load += new System.EventHandler(this.ControllerConfig_Load);
            this.tabControl1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.flpUDLR.ResumeLayout(false);
            this.flpUDLR.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage NormalControlsTab;
		private System.Windows.Forms.TabPage AutofireControlsTab;
		private System.Windows.Forms.CheckBox checkBoxAutoTab;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.TabPage AnalogControlsTab;
		private System.Windows.Forms.TabPage FeedbacksTab;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolTip toolTip1;
		private MenuButton btnMisc;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx testToolStripMenuItem;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx loadDefaultsToolStripMenuItem;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx clearToolStripMenuItem;
				private BizHawk.WinForms.Controls.LocLabelEx label3;
				private BizHawk.WinForms.Controls.LocLabelEx label2;
				private BizHawk.WinForms.Controls.LocLabelEx label38;
		private WinForms.Controls.LocSingleRowFLP flpUDLR;
		private WinForms.Controls.RadioButtonEx rbUDLRForbid;
		private WinForms.Controls.RadioButtonEx rbUDLRPriority;
		private WinForms.Controls.RadioButtonEx rbUDLRAllow;
		private WinForms.Controls.LabelEx lblUDLR;
	}
}