using BizHawk.Core;

namespace BizHawk.Emulation.Consoles.Gameboy
{
	partial class Debugger
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
			this.btnRun = new System.Windows.Forms.Button();
			this.btnStepInto = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.txtRegAF = new System.Windows.Forms.TextBox();
			this.txtRegDE = new System.Windows.Forms.TextBox();
			this.txtRegPC = new System.Windows.Forms.TextBox();
			this.txtRegSP = new System.Windows.Forms.TextBox();
			this.txtRegHL = new System.Windows.Forms.TextBox();
			this.txtRegBC = new System.Windows.Forms.TextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.checkFlag_Z = new System.Windows.Forms.CheckBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.checkFlag_H = new System.Windows.Forms.CheckBox();
			this.checkFlag_N = new System.Windows.Forms.CheckBox();
			this.checkFlag_C = new System.Windows.Forms.CheckBox();
			this.label12 = new System.Windows.Forms.Label();
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.vScrollBar1 = new System.Windows.Forms.VScrollBar();
			this.btnSeekPC = new System.Windows.Forms.Button();
			this.btnSeekUser = new System.Windows.Forms.Button();
			this.txtSeekUser = new System.Windows.Forms.TextBox();
			this.listBreakpoints = new System.Windows.Forms.ListBox();
			this.menuContextBreakpoints = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.miBreakpointAdd = new System.Windows.Forms.ToolStripMenuItem();
			this.miBreakpointDelete = new System.Windows.Forms.ToolStripMenuItem();
			this.label10 = new System.Windows.Forms.Label();
			this.timerRunUpdate = new System.Windows.Forms.Timer(this.components);
			this.btnBreak = new System.Windows.Forms.Button();
			this.txtFrame = new System.Windows.Forms.TextBox();
			this.label13 = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.txtLine = new System.Windows.Forms.TextBox();
			this.txtDot = new System.Windows.Forms.TextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.checkViewBg = new System.Windows.Forms.CheckBox();
			this.checkViewObj = new System.Windows.Forms.CheckBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.checkViewObjNoLimit = new System.Windows.Forms.CheckBox();
			this.lblInputActive = new System.Windows.Forms.Label();
			this.viewTiles0x9000 = new ViewportPanel();
			this.viewTiles0x8000 = new ViewportPanel();
			this.panelMemory = new ScrollableViewportPanel();
			this.viewDisassembly = new ViewportPanel();
			this.viewBG = new ViewportPanel();
			this.groupBox1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.menuContextBreakpoints.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnRun
			// 
			this.btnRun.Location = new System.Drawing.Point(377, 2);
			this.btnRun.Name = "btnRun";
			this.btnRun.Size = new System.Drawing.Size(75, 23);
			this.btnRun.TabIndex = 1;
			this.btnRun.Text = "Run";
			this.btnRun.UseVisualStyleBackColor = true;
			this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
			// 
			// btnStepInto
			// 
			this.btnStepInto.Location = new System.Drawing.Point(458, 2);
			this.btnStepInto.Name = "btnStepInto";
			this.btnStepInto.Size = new System.Drawing.Size(75, 23);
			this.btnStepInto.TabIndex = 2;
			this.btnStepInto.Text = "Step Into";
			this.btnStepInto.UseVisualStyleBackColor = true;
			this.btnStepInto.Click += new System.EventHandler(this.btnStepInto_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(369, 63);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(23, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "AF:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(428, 63);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(24, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "BC:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(367, 83);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(25, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "DE:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(428, 83);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(24, 13);
			this.label4.TabIndex = 6;
			this.label4.Text = "HL:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(428, 104);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(24, 13);
			this.label5.TabIndex = 7;
			this.label5.Text = "PC:";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(367, 104);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(24, 13);
			this.label6.TabIndex = 8;
			this.label6.Text = "SP:";
			// 
			// txtRegAF
			// 
			this.txtRegAF.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtRegAF.Location = new System.Drawing.Point(392, 60);
			this.txtRegAF.MaxLength = 4;
			this.txtRegAF.Name = "txtRegAF";
			this.txtRegAF.Size = new System.Drawing.Size(34, 20);
			this.txtRegAF.TabIndex = 10;
			this.txtRegAF.Text = "FFF0";
			this.txtRegAF.Validating += new System.ComponentModel.CancelEventHandler(this.reg16_Validating);
			// 
			// txtRegDE
			// 
			this.txtRegDE.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtRegDE.Location = new System.Drawing.Point(392, 80);
			this.txtRegDE.MaxLength = 4;
			this.txtRegDE.Name = "txtRegDE";
			this.txtRegDE.Size = new System.Drawing.Size(34, 20);
			this.txtRegDE.TabIndex = 11;
			this.txtRegDE.Text = "FFF0";
			this.txtRegDE.Validating += new System.ComponentModel.CancelEventHandler(this.reg16_Validating);
			// 
			// txtRegPC
			// 
			this.txtRegPC.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtRegPC.Location = new System.Drawing.Point(452, 100);
			this.txtRegPC.MaxLength = 4;
			this.txtRegPC.Name = "txtRegPC";
			this.txtRegPC.Size = new System.Drawing.Size(34, 20);
			this.txtRegPC.TabIndex = 12;
			this.txtRegPC.Text = "FFF0";
			this.txtRegPC.Validating += new System.ComponentModel.CancelEventHandler(this.reg16_Validating);
			// 
			// txtRegSP
			// 
			this.txtRegSP.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtRegSP.Location = new System.Drawing.Point(392, 100);
			this.txtRegSP.MaxLength = 4;
			this.txtRegSP.Name = "txtRegSP";
			this.txtRegSP.Size = new System.Drawing.Size(34, 20);
			this.txtRegSP.TabIndex = 15;
			this.txtRegSP.Text = "FFF0";
			this.txtRegSP.Validating += new System.ComponentModel.CancelEventHandler(this.reg16_Validating);
			// 
			// txtRegHL
			// 
			this.txtRegHL.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtRegHL.Location = new System.Drawing.Point(452, 80);
			this.txtRegHL.MaxLength = 4;
			this.txtRegHL.Name = "txtRegHL";
			this.txtRegHL.Size = new System.Drawing.Size(34, 20);
			this.txtRegHL.TabIndex = 14;
			this.txtRegHL.Text = "FFF0";
			this.txtRegHL.Validating += new System.ComponentModel.CancelEventHandler(this.reg16_Validating);
			// 
			// txtRegBC
			// 
			this.txtRegBC.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtRegBC.Location = new System.Drawing.Point(452, 60);
			this.txtRegBC.MaxLength = 4;
			this.txtRegBC.Name = "txtRegBC";
			this.txtRegBC.Size = new System.Drawing.Size(34, 20);
			this.txtRegBC.TabIndex = 13;
			this.txtRegBC.Text = "FFF0";
			this.txtRegBC.Validating += new System.ComponentModel.CancelEventHandler(this.reg16_Validating);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.tableLayoutPanel1);
			this.groupBox1.Location = new System.Drawing.Point(370, 149);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(96, 59);
			this.groupBox1.TabIndex = 16;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "CPU Flags";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.AutoSize = true;
			this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel1.ColumnCount = 4;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Controls.Add(this.checkFlag_Z, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label8, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label11, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.label9, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this.checkFlag_H, 2, 1);
			this.tableLayoutPanel1.Controls.Add(this.checkFlag_N, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.checkFlag_C, 3, 1);
			this.tableLayoutPanel1.Controls.Add(this.label12, 3, 0);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 19);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(80, 33);
			this.tableLayoutPanel1.TabIndex = 17;
			// 
			// checkFlag_Z
			// 
			this.checkFlag_Z.AutoSize = true;
			this.checkFlag_Z.Location = new System.Drawing.Point(3, 16);
			this.checkFlag_Z.Name = "checkFlag_Z";
			this.checkFlag_Z.Size = new System.Drawing.Size(14, 14);
			this.checkFlag_Z.TabIndex = 8;
			this.checkFlag_Z.UseVisualStyleBackColor = true;
			this.checkFlag_Z.CheckedChanged += new System.EventHandler(this.cpuflag_checkChanged);
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(3, 0);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(14, 13);
			this.label8.TabIndex = 2;
			this.label8.Text = "Z";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(23, 0);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(14, 13);
			this.label11.TabIndex = 5;
			this.label11.Text = "N";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(43, 0);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(14, 13);
			this.label9.TabIndex = 3;
			this.label9.Text = "H";
			// 
			// checkFlag_H
			// 
			this.checkFlag_H.AutoSize = true;
			this.checkFlag_H.Location = new System.Drawing.Point(43, 16);
			this.checkFlag_H.Name = "checkFlag_H";
			this.checkFlag_H.Size = new System.Drawing.Size(14, 14);
			this.checkFlag_H.TabIndex = 0;
			this.checkFlag_H.UseVisualStyleBackColor = true;
			this.checkFlag_H.CheckedChanged += new System.EventHandler(this.cpuflag_checkChanged);
			// 
			// checkFlag_N
			// 
			this.checkFlag_N.AutoSize = true;
			this.checkFlag_N.Location = new System.Drawing.Point(23, 16);
			this.checkFlag_N.Name = "checkFlag_N";
			this.checkFlag_N.Size = new System.Drawing.Size(14, 14);
			this.checkFlag_N.TabIndex = 10;
			this.checkFlag_N.UseVisualStyleBackColor = true;
			this.checkFlag_N.CheckedChanged += new System.EventHandler(this.cpuflag_checkChanged);
			// 
			// checkFlag_C
			// 
			this.checkFlag_C.AutoSize = true;
			this.checkFlag_C.Location = new System.Drawing.Point(63, 16);
			this.checkFlag_C.Name = "checkFlag_C";
			this.checkFlag_C.Size = new System.Drawing.Size(14, 14);
			this.checkFlag_C.TabIndex = 11;
			this.checkFlag_C.UseVisualStyleBackColor = true;
			this.checkFlag_C.CheckedChanged += new System.EventHandler(this.cpuflag_checkChanged);
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(63, 0);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(14, 13);
			this.label12.TabIndex = 6;
			this.label12.Text = "C";
			// 
			// tableLayoutPanel2
			// 
			this.tableLayoutPanel2.ColumnCount = 2;
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel2.Controls.Add(this.vScrollBar1, 1, 0);
			this.tableLayoutPanel2.Controls.Add(this.viewDisassembly, 0, 0);
			this.tableLayoutPanel2.Location = new System.Drawing.Point(2, 2);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			this.tableLayoutPanel2.RowCount = 1;
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel2.Size = new System.Drawing.Size(350, 205);
			this.tableLayoutPanel2.TabIndex = 19;
			// 
			// vScrollBar1
			// 
			this.vScrollBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.vScrollBar1.Location = new System.Drawing.Point(334, 0);
			this.vScrollBar1.Maximum = 65535;
			this.vScrollBar1.Name = "vScrollBar1";
			this.vScrollBar1.Size = new System.Drawing.Size(16, 205);
			this.vScrollBar1.TabIndex = 20;
			this.vScrollBar1.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBar1_Scroll);
			// 
			// btnSeekPC
			// 
			this.btnSeekPC.Location = new System.Drawing.Point(489, 100);
			this.btnSeekPC.Name = "btnSeekPC";
			this.btnSeekPC.Size = new System.Drawing.Size(52, 20);
			this.btnSeekPC.TabIndex = 20;
			this.btnSeekPC.Text = "Seek";
			this.btnSeekPC.UseVisualStyleBackColor = true;
			this.btnSeekPC.Click += new System.EventHandler(this.btnSeekPC_Click);
			// 
			// btnSeekUser
			// 
			this.btnSeekUser.Location = new System.Drawing.Point(489, 124);
			this.btnSeekUser.Name = "btnSeekUser";
			this.btnSeekUser.Size = new System.Drawing.Size(52, 20);
			this.btnSeekUser.TabIndex = 21;
			this.btnSeekUser.Text = "Seek";
			this.btnSeekUser.UseVisualStyleBackColor = true;
			this.btnSeekUser.Click += new System.EventHandler(this.btnSeekUser_Click);
			// 
			// txtSeekUser
			// 
			this.txtSeekUser.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtSeekUser.Location = new System.Drawing.Point(452, 123);
			this.txtSeekUser.MaxLength = 4;
			this.txtSeekUser.Name = "txtSeekUser";
			this.txtSeekUser.Size = new System.Drawing.Size(34, 20);
			this.txtSeekUser.TabIndex = 22;
			// 
			// listBreakpoints
			// 
			this.listBreakpoints.ContextMenuStrip = this.menuContextBreakpoints;
			this.listBreakpoints.FormattingEnabled = true;
			this.listBreakpoints.Location = new System.Drawing.Point(915, 245);
			this.listBreakpoints.Name = "listBreakpoints";
			this.listBreakpoints.Size = new System.Drawing.Size(120, 95);
			this.listBreakpoints.TabIndex = 25;
			// 
			// menuContextBreakpoints
			// 
			this.menuContextBreakpoints.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miBreakpointAdd,
            this.miBreakpointDelete});
			this.menuContextBreakpoints.Name = "menuContextBreakpoints";
			this.menuContextBreakpoints.Size = new System.Drawing.Size(117, 48);
			this.menuContextBreakpoints.Opening += new System.ComponentModel.CancelEventHandler(this.menuContextBreakpoints_Opening);
			// 
			// miBreakpointAdd
			// 
			this.miBreakpointAdd.Name = "miBreakpointAdd";
			this.miBreakpointAdd.Size = new System.Drawing.Size(116, 22);
			this.miBreakpointAdd.Text = "Add";
			// 
			// miBreakpointDelete
			// 
			this.miBreakpointDelete.Name = "miBreakpointDelete";
			this.miBreakpointDelete.Size = new System.Drawing.Size(116, 22);
			this.miBreakpointDelete.Text = "Delete";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(912, 227);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(63, 13);
			this.label10.TabIndex = 26;
			this.label10.Text = "Breakpoints";
			// 
			// timerRunUpdate
			// 
			this.timerRunUpdate.Enabled = true;
			this.timerRunUpdate.Tick += new System.EventHandler(this.timerRunUpdate_Tick);
			// 
			// btnBreak
			// 
			this.btnBreak.Location = new System.Drawing.Point(377, 27);
			this.btnBreak.Name = "btnBreak";
			this.btnBreak.Size = new System.Drawing.Size(75, 23);
			this.btnBreak.TabIndex = 29;
			this.btnBreak.Text = "Break";
			this.btnBreak.UseVisualStyleBackColor = true;
			this.btnBreak.Click += new System.EventHandler(this.btnBreak_Click);
			// 
			// txtFrame
			// 
			this.txtFrame.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtFrame.Location = new System.Drawing.Point(775, 21);
			this.txtFrame.MaxLength = 4;
			this.txtFrame.Name = "txtFrame";
			this.txtFrame.ReadOnly = true;
			this.txtFrame.Size = new System.Drawing.Size(48, 20);
			this.txtFrame.TabIndex = 31;
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(730, 24);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(39, 13);
			this.label13.TabIndex = 32;
			this.label13.Text = "Frame:";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(739, 44);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(30, 13);
			this.label14.TabIndex = 33;
			this.label14.Text = "Line:";
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(742, 66);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(27, 13);
			this.label15.TabIndex = 34;
			this.label15.Text = "Dot:";
			// 
			// txtLine
			// 
			this.txtLine.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtLine.Location = new System.Drawing.Point(775, 43);
			this.txtLine.MaxLength = 4;
			this.txtLine.Name = "txtLine";
			this.txtLine.ReadOnly = true;
			this.txtLine.Size = new System.Drawing.Size(48, 20);
			this.txtLine.TabIndex = 35;
			// 
			// txtDot
			// 
			this.txtDot.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtDot.Location = new System.Drawing.Point(775, 65);
			this.txtDot.MaxLength = 4;
			this.txtDot.Name = "txtDot";
			this.txtDot.ReadOnly = true;
			this.txtDot.Size = new System.Drawing.Size(48, 20);
			this.txtDot.TabIndex = 36;
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel1.Controls.Add(this.panelMemory);
			this.panel1.Location = new System.Drawing.Point(5, 225);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(545, 181);
			this.panel1.TabIndex = 38;
			// 
			// checkViewBg
			// 
			this.checkViewBg.AutoSize = true;
			this.checkViewBg.Checked = true;
			this.checkViewBg.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkViewBg.Location = new System.Drawing.Point(564, 3);
			this.checkViewBg.Name = "checkViewBg";
			this.checkViewBg.Size = new System.Drawing.Size(41, 17);
			this.checkViewBg.TabIndex = 39;
			this.checkViewBg.Text = "BG";
			this.checkViewBg.UseVisualStyleBackColor = true;
			this.checkViewBg.CheckedChanged += new System.EventHandler(this.checkViewBg_CheckedChanged);
			// 
			// checkViewObj
			// 
			this.checkViewObj.AutoSize = true;
			this.checkViewObj.Checked = true;
			this.checkViewObj.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkViewObj.Location = new System.Drawing.Point(611, 3);
			this.checkViewObj.Name = "checkViewObj";
			this.checkViewObj.Size = new System.Drawing.Size(46, 17);
			this.checkViewObj.TabIndex = 40;
			this.checkViewObj.Text = "OBJ";
			this.checkViewObj.UseVisualStyleBackColor = true;
			this.checkViewObj.CheckedChanged += new System.EventHandler(this.checkViewObj_CheckedChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(561, 195);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(45, 13);
			this.label7.TabIndex = 42;
			this.label7.Text = "0x8000:";
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(701, 197);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(45, 13);
			this.label16.TabIndex = 44;
			this.label16.Text = "0x8800:";
			// 
			// checkViewObjNoLimit
			// 
			this.checkViewObjNoLimit.AutoSize = true;
			this.checkViewObjNoLimit.Checked = true;
			this.checkViewObjNoLimit.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkViewObjNoLimit.Location = new System.Drawing.Point(663, 3);
			this.checkViewObjNoLimit.Name = "checkViewObjNoLimit";
			this.checkViewObjNoLimit.Size = new System.Drawing.Size(44, 17);
			this.checkViewObjNoLimit.TabIndex = 45;
			this.checkViewObjNoLimit.Text = ">10";
			this.checkViewObjNoLimit.UseVisualStyleBackColor = true;
			this.checkViewObjNoLimit.CheckedChanged += new System.EventHandler(this.checkViewObjNoLimit_CheckedChanged);
			// 
			// lblInputActive
			// 
			this.lblInputActive.AutoSize = true;
			this.lblInputActive.Location = new System.Drawing.Point(711, 10);
			this.lblInputActive.Name = "lblInputActive";
			this.lblInputActive.Size = new System.Drawing.Size(13, 13);
			this.lblInputActive.TabIndex = 46;
			this.lblInputActive.Text = "o";
			// 
			// viewTiles0x9000
			// 
			this.viewTiles0x9000.Location = new System.Drawing.Point(704, 215);
			this.viewTiles0x9000.Name = "viewTiles0x9000";
			this.viewTiles0x9000.Size = new System.Drawing.Size(128, 128);
			this.viewTiles0x9000.TabIndex = 43;
			this.viewTiles0x9000.Paint += new System.Windows.Forms.PaintEventHandler(this.viewTiles0x9000_Paint);
			// 
			// viewTiles0x8000
			// 
			this.viewTiles0x8000.Location = new System.Drawing.Point(561, 215);
			this.viewTiles0x8000.Name = "viewTiles0x8000";
			this.viewTiles0x8000.Size = new System.Drawing.Size(128, 128);
			this.viewTiles0x8000.TabIndex = 41;
			this.viewTiles0x8000.Paint += new System.Windows.Forms.PaintEventHandler(this.viewTiles0x8000_Paint);
			// 
			// panelMemory
			// 
			this.panelMemory.AutoSize = true;
			this.panelMemory.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelMemory.Location = new System.Drawing.Point(0, 0);
			this.panelMemory.Name = "panelMemory";
			this.panelMemory.ScrollLargeChange = 10;
			this.panelMemory.ScrollMax = 4095;
			this.panelMemory.Size = new System.Drawing.Size(541, 177);
			this.panelMemory.TabIndex = 37;
			this.panelMemory.Paint += new System.Windows.Forms.PaintEventHandler(this.panelMemory_Paint);
			this.panelMemory.Scroll += new System.Windows.Forms.ScrollEventHandler(this.panelMemory_Scroll);
			// 
			// viewDisassembly
			// 
			this.viewDisassembly.Dock = System.Windows.Forms.DockStyle.Fill;
			this.viewDisassembly.Location = new System.Drawing.Point(3, 3);
			this.viewDisassembly.Name = "viewDisassembly";
			this.viewDisassembly.Size = new System.Drawing.Size(328, 199);
			this.viewDisassembly.TabIndex = 0;
			this.viewDisassembly.Paint += new System.Windows.Forms.PaintEventHandler(this.viewDisassembly_Paint);
			// 
			// viewBG
			// 
			this.viewBG.Location = new System.Drawing.Point(564, 27);
			this.viewBG.Name = "viewBG";
			this.viewBG.Size = new System.Drawing.Size(160, 144);
			this.viewBG.TabIndex = 23;
			this.viewBG.Paint += new System.Windows.Forms.PaintEventHandler(this.viewBG_Paint);
			this.viewBG.Leave += new System.EventHandler(this.viewBG_Leave);
			this.viewBG.KeyUp += new System.Windows.Forms.KeyEventHandler(this.viewBG_KeyUp);
			this.viewBG.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.viewBG_KeyPress);
			this.viewBG.Enter += new System.EventHandler(this.viewBG_Enter);
			this.viewBG.KeyDown += new System.Windows.Forms.KeyEventHandler(this.viewBG_KeyDown);
			// 
			// Debugger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1065, 483);
			this.Controls.Add(this.lblInputActive);
			this.Controls.Add(this.checkViewObjNoLimit);
			this.Controls.Add(this.viewTiles0x9000);
			this.Controls.Add(this.label16);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.viewTiles0x8000);
			this.Controls.Add(this.checkViewObj);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.checkViewBg);
			this.Controls.Add(this.label13);
			this.Controls.Add(this.txtDot);
			this.Controls.Add(this.txtLine);
			this.Controls.Add(this.label15);
			this.Controls.Add(this.label14);
			this.Controls.Add(this.btnBreak);
			this.Controls.Add(this.txtFrame);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.tableLayoutPanel2);
			this.Controls.Add(this.listBreakpoints);
			this.Controls.Add(this.txtSeekUser);
			this.Controls.Add(this.viewBG);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnSeekUser);
			this.Controls.Add(this.btnSeekPC);
			this.Controls.Add(this.btnStepInto);
			this.Controls.Add(this.txtRegAF);
			this.Controls.Add(this.txtRegSP);
			this.Controls.Add(this.txtRegHL);
			this.Controls.Add(this.txtRegBC);
			this.Controls.Add(this.txtRegPC);
			this.Controls.Add(this.txtRegDE);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.btnRun);
			this.Controls.Add(this.label1);
			this.KeyPreview = true;
			this.Name = "Debugger";
			this.Text = "Debugger";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.tableLayoutPanel2.ResumeLayout(false);
			this.menuContextBreakpoints.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private ViewportPanel viewDisassembly;
		private System.Windows.Forms.Button btnRun;
		private System.Windows.Forms.Button btnStepInto;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox txtRegAF;
		private System.Windows.Forms.TextBox txtRegDE;
		private System.Windows.Forms.TextBox txtRegPC;
		private System.Windows.Forms.TextBox txtRegSP;
		private System.Windows.Forms.TextBox txtRegHL;
		private System.Windows.Forms.TextBox txtRegBC;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox checkFlag_H;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.CheckBox checkFlag_Z;
		private System.Windows.Forms.CheckBox checkFlag_N;
		private System.Windows.Forms.CheckBox checkFlag_C;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
		private System.Windows.Forms.VScrollBar vScrollBar1;
		private System.Windows.Forms.Button btnSeekPC;
		private System.Windows.Forms.Button btnSeekUser;
		private System.Windows.Forms.TextBox txtSeekUser;
		private ViewportPanel viewBG;
		private System.Windows.Forms.ListBox listBreakpoints;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.ContextMenuStrip menuContextBreakpoints;
		private System.Windows.Forms.ToolStripMenuItem miBreakpointAdd;
		private System.Windows.Forms.ToolStripMenuItem miBreakpointDelete;
		private System.Windows.Forms.Timer timerRunUpdate;
		private System.Windows.Forms.Button btnBreak;
		private System.Windows.Forms.TextBox txtFrame;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.TextBox txtLine;
		private System.Windows.Forms.TextBox txtDot;
		private ScrollableViewportPanel panelMemory;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.CheckBox checkViewBg;
		private System.Windows.Forms.CheckBox checkViewObj;
		private ViewportPanel viewTiles0x8000;
		private System.Windows.Forms.Label label7;
		private ViewportPanel viewTiles0x9000;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.CheckBox checkViewObjNoLimit;
		private System.Windows.Forms.Label lblInputActive;

	}
}