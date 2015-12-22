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
			this.checkBoxAutoTab = new System.Windows.Forms.CheckBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadDefaultsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label38 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.rbUDLRForbid = new System.Windows.Forms.RadioButton();
			this.rbUDLRPriority = new System.Windows.Forms.RadioButton();
			this.rbUDLRAllow = new System.Windows.Forms.RadioButton();
			this.btnMisc = new BizHawk.Client.EmuHawk.MenuButton();
			this.tabControl1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.NormalControlsTab);
			this.tabControl1.Controls.Add(this.AutofireControlsTab);
			this.tabControl1.Controls.Add(this.AnalogControlsTab);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(3, 3);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(562, 521);
			this.tabControl1.TabIndex = 1;
			// 
			// NormalControlsTab
			// 
			this.NormalControlsTab.Location = new System.Drawing.Point(4, 22);
			this.NormalControlsTab.Name = "NormalControlsTab";
			this.NormalControlsTab.Padding = new System.Windows.Forms.Padding(3);
			this.NormalControlsTab.Size = new System.Drawing.Size(554, 495);
			this.NormalControlsTab.TabIndex = 0;
			this.NormalControlsTab.Text = "Normal Controls";
			this.NormalControlsTab.UseVisualStyleBackColor = true;
			// 
			// AutofireControlsTab
			// 
			this.AutofireControlsTab.Location = new System.Drawing.Point(4, 22);
			this.AutofireControlsTab.Name = "AutofireControlsTab";
			this.AutofireControlsTab.Padding = new System.Windows.Forms.Padding(3);
			this.AutofireControlsTab.Size = new System.Drawing.Size(554, 495);
			this.AutofireControlsTab.TabIndex = 1;
			this.AutofireControlsTab.Text = "Autofire Controls";
			this.AutofireControlsTab.UseVisualStyleBackColor = true;
			// 
			// AnalogControlsTab
			// 
			this.AnalogControlsTab.Location = new System.Drawing.Point(4, 22);
			this.AnalogControlsTab.Name = "AnalogControlsTab";
			this.AnalogControlsTab.Size = new System.Drawing.Size(554, 495);
			this.AnalogControlsTab.TabIndex = 2;
			this.AnalogControlsTab.Text = "Analog Controls";
			this.AnalogControlsTab.UseVisualStyleBackColor = true;
			// 
			// checkBoxAutoTab
			// 
			this.checkBoxAutoTab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxAutoTab.AutoSize = true;
			this.checkBoxAutoTab.Location = new System.Drawing.Point(371, 548);
			this.checkBoxAutoTab.Name = "checkBoxAutoTab";
			this.checkBoxAutoTab.Size = new System.Drawing.Size(70, 17);
			this.checkBoxAutoTab.TabIndex = 3;
			this.checkBoxAutoTab.Text = "Auto Tab";
			this.checkBoxAutoTab.UseVisualStyleBackColor = true;
			this.checkBoxAutoTab.CheckedChanged += new System.EventHandler(this.CheckBoxAutoTab_CheckedChanged);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(764, 542);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 5;
			this.buttonOK.Text = "&Save";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.ButtonOk_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(845, 542);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 6;
			this.buttonCancel.Text = "&Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 340F));
			this.tableLayoutPanel1.Controls.Add(this.tabControl1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.pictureBox1, 1, 0);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(908, 527);
			this.tableLayoutPanel1.TabIndex = 7;
			// 
			// pictureBox1
			// 
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBox1.Location = new System.Drawing.Point(571, 23);
			this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 23, 3, 3);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(334, 501);
			this.pictureBox1.TabIndex = 2;
			this.pictureBox1.TabStop = false;
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.testToolStripMenuItem,
            this.loadDefaultsToolStripMenuItem,
            this.clearToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(142, 70);
			// 
			// testToolStripMenuItem
			// 
			this.testToolStripMenuItem.Name = "testToolStripMenuItem";
			this.testToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
			this.testToolStripMenuItem.Text = "Save Defaults";
			this.testToolStripMenuItem.Click += new System.EventHandler(this.ButtonSaveDefaults_Click);
			// 
			// loadDefaultsToolStripMenuItem
			// 
			this.loadDefaultsToolStripMenuItem.Name = "loadDefaultsToolStripMenuItem";
			this.loadDefaultsToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
			this.loadDefaultsToolStripMenuItem.Text = "Load Defaults";
			this.loadDefaultsToolStripMenuItem.Click += new System.EventHandler(this.ButtonLoadDefaults_Click);
			// 
			// clearToolStripMenuItem
			// 
			this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
			this.clearToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
			this.clearToolStripMenuItem.Text = "Clear";
			this.clearToolStripMenuItem.Click += new System.EventHandler(this.ClearBtn_Click);
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(11, 550);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(30, 13);
			this.label3.TabIndex = 112;
			this.label3.Text = "Tips:";
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(197, 550);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(168, 13);
			this.label2.TabIndex = 111;
			this.label2.Text = "* Disable Auto Tab to multiply bind";
			// 
			// label38
			// 
			this.label38.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label38.AutoSize = true;
			this.label38.Location = new System.Drawing.Point(41, 550);
			this.label38.Name = "label38";
			this.label38.Size = new System.Drawing.Size(153, 13);
			this.label38.TabIndex = 110;
			this.label38.Text = "* Escape clears a key mapping";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(442, 550);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(57, 13);
			this.label1.TabIndex = 113;
			this.label1.Text = "U+D/L+R:";
			// 
			// rbUDLRForbid
			// 
			this.rbUDLRForbid.AutoSize = true;
			this.rbUDLRForbid.Location = new System.Drawing.Point(559, 548);
			this.rbUDLRForbid.Name = "rbUDLRForbid";
			this.rbUDLRForbid.Size = new System.Drawing.Size(54, 17);
			this.rbUDLRForbid.TabIndex = 114;
			this.rbUDLRForbid.TabStop = true;
			this.rbUDLRForbid.Text = "Forbid";
			this.rbUDLRForbid.UseVisualStyleBackColor = true;
			// 
			// rbUDLRPriority
			// 
			this.rbUDLRPriority.AutoSize = true;
			this.rbUDLRPriority.Location = new System.Drawing.Point(622, 548);
			this.rbUDLRPriority.Name = "rbUDLRPriority";
			this.rbUDLRPriority.Size = new System.Drawing.Size(56, 17);
			this.rbUDLRPriority.TabIndex = 115;
			this.rbUDLRPriority.TabStop = true;
			this.rbUDLRPriority.Text = "Priority";
			this.rbUDLRPriority.UseVisualStyleBackColor = true;
			// 
			// rbUDLRAllow
			// 
			this.rbUDLRAllow.AutoSize = true;
			this.rbUDLRAllow.Location = new System.Drawing.Point(503, 548);
			this.rbUDLRAllow.Name = "rbUDLRAllow";
			this.rbUDLRAllow.Size = new System.Drawing.Size(50, 17);
			this.rbUDLRAllow.TabIndex = 116;
			this.rbUDLRAllow.TabStop = true;
			this.rbUDLRAllow.Text = "Allow";
			this.rbUDLRAllow.UseVisualStyleBackColor = true;
			// 
			// btnMisc
			// 
			this.btnMisc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnMisc.Location = new System.Drawing.Point(683, 542);
			this.btnMisc.Menu = this.contextMenuStrip1;
			this.btnMisc.Name = "btnMisc";
			this.btnMisc.Size = new System.Drawing.Size(75, 23);
			this.btnMisc.TabIndex = 11;
			this.btnMisc.Text = "Misc...";
			this.btnMisc.UseVisualStyleBackColor = true;
			// 
			// ControllerConfig
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(932, 572);
			this.Controls.Add(this.rbUDLRAllow);
			this.Controls.Add(this.rbUDLRPriority);
			this.Controls.Add(this.rbUDLRForbid);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label38);
			this.Controls.Add(this.btnMisc);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.checkBoxAutoTab);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ControllerConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Controller Config";
			this.Load += new System.EventHandler(this.NewControllerConfig_Load);
			this.tabControl1.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.contextMenuStrip1.ResumeLayout(false);
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
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolTip toolTip1;
		private MenuButton btnMisc;
				private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
				private System.Windows.Forms.ToolStripMenuItem loadDefaultsToolStripMenuItem;
				private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
				private System.Windows.Forms.Label label3;
				private System.Windows.Forms.Label label2;
				private System.Windows.Forms.Label label38;
				private System.Windows.Forms.Label label1;
				private System.Windows.Forms.RadioButton rbUDLRForbid;
				private System.Windows.Forms.RadioButton rbUDLRPriority;
				private System.Windows.Forms.RadioButton rbUDLRAllow;
	}
}