namespace BizHawk.Client.EmuHawk
{
	partial class PSXOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PSXOptions));
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.lblTweakedMednafen = new BizHawk.WinForms.Controls.LocLabelEx();
            this.rbTweakedMednafenMode = new System.Windows.Forms.RadioButton();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.rbDebugMode = new System.Windows.Forms.RadioButton();
            this.btnNiceDisplayConfig = new System.Windows.Forms.Button();
            this.lblMednafen = new BizHawk.WinForms.Controls.LocLabelEx();
            this.rbMednafenMode = new System.Windows.Forms.RadioButton();
            this.lblPixelPro = new BizHawk.WinForms.Controls.LocLabelEx();
            this.rbPixelPro = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.rbClipNone = new System.Windows.Forms.RadioButton();
            this.rbClipToFramebuffer = new System.Windows.Forms.RadioButton();
            this.rbClipBasic = new System.Windows.Forms.RadioButton();
            this.lblPAL = new BizHawk.WinForms.Controls.LocLabelEx();
            this.PAL_LastLineNumeric = new System.Windows.Forms.NumericUpDown();
            this.PAL_FirstLineNumeric = new System.Windows.Forms.NumericUpDown();
            this.lblNTSC = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btnAreaFull = new System.Windows.Forms.Button();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.NTSC_LastLineNumeric = new System.Windows.Forms.NumericUpDown();
            this.NTSC_FirstLineNumeric = new System.Windows.Forms.NumericUpDown();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.rbWeave = new System.Windows.Forms.RadioButton();
            this.rbBobOffset = new System.Windows.Forms.RadioButton();
            this.rbBob = new System.Windows.Forms.RadioButton();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.cbLEC = new System.Windows.Forms.CheckBox();
            this.cbGpuLag = new System.Windows.Forms.CheckBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PAL_LastLineNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PAL_FirstLineNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.toolTip1.SetToolTip(this.btnCancel, resources.GetString("btnCancel.ToolTip"));
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.Name = "btnOk";
            this.toolTip1.SetToolTip(this.btnOk, resources.GetString("btnOk.ToolTip"));
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.linkLabel1);
            this.groupBox1.Controls.Add(this.lblTweakedMednafen);
            this.groupBox1.Controls.Add(this.rbTweakedMednafenMode);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.rbDebugMode);
            this.groupBox1.Controls.Add(this.btnNiceDisplayConfig);
            this.groupBox1.Controls.Add(this.lblMednafen);
            this.groupBox1.Controls.Add(this.rbMednafenMode);
            this.groupBox1.Controls.Add(this.lblPixelPro);
            this.groupBox1.Controls.Add(this.rbPixelPro);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox1, resources.GetString("groupBox1.ToolTip"));
            // 
            // linkLabel1
            // 
            resources.ApplyResources(this.linkLabel1, "linkLabel1");
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.TabStop = true;
            this.toolTip1.SetToolTip(this.linkLabel1, resources.GetString("linkLabel1.ToolTip"));
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel1_LinkClicked);
            // 
            // lblTweakedMednafen
            // 
            resources.ApplyResources(this.lblTweakedMednafen, "lblTweakedMednafen");
            this.lblTweakedMednafen.Name = "lblTweakedMednafen";
            this.toolTip1.SetToolTip(this.lblTweakedMednafen, resources.GetString("lblTweakedMednafen.ToolTip"));
            // 
            // rbTweakedMednafenMode
            // 
            resources.ApplyResources(this.rbTweakedMednafenMode, "rbTweakedMednafenMode");
            this.rbTweakedMednafenMode.Name = "rbTweakedMednafenMode";
            this.rbTweakedMednafenMode.TabStop = true;
            this.toolTip1.SetToolTip(this.rbTweakedMednafenMode, resources.GetString("rbTweakedMednafenMode.ToolTip"));
            this.rbTweakedMednafenMode.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            this.toolTip1.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // rbDebugMode
            // 
            resources.ApplyResources(this.rbDebugMode, "rbDebugMode");
            this.rbDebugMode.Name = "rbDebugMode";
            this.rbDebugMode.TabStop = true;
            this.toolTip1.SetToolTip(this.rbDebugMode, resources.GetString("rbDebugMode.ToolTip"));
            this.rbDebugMode.UseVisualStyleBackColor = true;
            // 
            // btnNiceDisplayConfig
            // 
            resources.ApplyResources(this.btnNiceDisplayConfig, "btnNiceDisplayConfig");
            this.btnNiceDisplayConfig.Name = "btnNiceDisplayConfig";
            this.toolTip1.SetToolTip(this.btnNiceDisplayConfig, resources.GetString("btnNiceDisplayConfig.ToolTip"));
            this.btnNiceDisplayConfig.UseVisualStyleBackColor = true;
            this.btnNiceDisplayConfig.Click += new System.EventHandler(this.BtnNiceDisplayConfig_Click);
            // 
            // lblMednafen
            // 
            resources.ApplyResources(this.lblMednafen, "lblMednafen");
            this.lblMednafen.Name = "lblMednafen";
            this.toolTip1.SetToolTip(this.lblMednafen, resources.GetString("lblMednafen.ToolTip"));
            // 
            // rbMednafenMode
            // 
            resources.ApplyResources(this.rbMednafenMode, "rbMednafenMode");
            this.rbMednafenMode.Name = "rbMednafenMode";
            this.rbMednafenMode.TabStop = true;
            this.toolTip1.SetToolTip(this.rbMednafenMode, resources.GetString("rbMednafenMode.ToolTip"));
            this.rbMednafenMode.UseVisualStyleBackColor = true;
            // 
            // lblPixelPro
            // 
            resources.ApplyResources(this.lblPixelPro, "lblPixelPro");
            this.lblPixelPro.Name = "lblPixelPro";
            this.toolTip1.SetToolTip(this.lblPixelPro, resources.GetString("lblPixelPro.ToolTip"));
            // 
            // rbPixelPro
            // 
            resources.ApplyResources(this.rbPixelPro, "rbPixelPro");
            this.rbPixelPro.Name = "rbPixelPro";
            this.rbPixelPro.TabStop = true;
            this.toolTip1.SetToolTip(this.rbPixelPro, resources.GetString("rbPixelPro.ToolTip"));
            this.rbPixelPro.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.groupBox3);
            this.groupBox2.Controls.Add(this.lblPAL);
            this.groupBox2.Controls.Add(this.PAL_LastLineNumeric);
            this.groupBox2.Controls.Add(this.PAL_FirstLineNumeric);
            this.groupBox2.Controls.Add(this.lblNTSC);
            this.groupBox2.Controls.Add(this.btnAreaFull);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.NTSC_LastLineNumeric);
            this.groupBox2.Controls.Add(this.NTSC_FirstLineNumeric);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox2, resources.GetString("groupBox2.ToolTip"));
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.rbClipNone);
            this.groupBox3.Controls.Add(this.rbClipToFramebuffer);
            this.groupBox3.Controls.Add(this.rbClipBasic);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox3, resources.GetString("groupBox3.ToolTip"));
            // 
            // rbClipNone
            // 
            resources.ApplyResources(this.rbClipNone, "rbClipNone");
            this.rbClipNone.Name = "rbClipNone";
            this.rbClipNone.TabStop = true;
            this.toolTip1.SetToolTip(this.rbClipNone, resources.GetString("rbClipNone.ToolTip"));
            this.rbClipNone.UseVisualStyleBackColor = true;
            this.rbClipNone.CheckedChanged += new System.EventHandler(this.RbClipNone_CheckedChanged);
            // 
            // rbClipToFramebuffer
            // 
            resources.ApplyResources(this.rbClipToFramebuffer, "rbClipToFramebuffer");
            this.rbClipToFramebuffer.Name = "rbClipToFramebuffer";
            this.rbClipToFramebuffer.TabStop = true;
            this.toolTip1.SetToolTip(this.rbClipToFramebuffer, resources.GetString("rbClipToFramebuffer.ToolTip"));
            this.rbClipToFramebuffer.UseVisualStyleBackColor = true;
            this.rbClipToFramebuffer.CheckedChanged += new System.EventHandler(this.RbClipToFramebuffer_CheckedChanged);
            // 
            // rbClipBasic
            // 
            resources.ApplyResources(this.rbClipBasic, "rbClipBasic");
            this.rbClipBasic.Name = "rbClipBasic";
            this.rbClipBasic.TabStop = true;
            this.toolTip1.SetToolTip(this.rbClipBasic, resources.GetString("rbClipBasic.ToolTip"));
            this.rbClipBasic.UseVisualStyleBackColor = true;
            this.rbClipBasic.CheckedChanged += new System.EventHandler(this.RbClipHorizontal_CheckedChanged);
            // 
            // lblPAL
            // 
            resources.ApplyResources(this.lblPAL, "lblPAL");
            this.lblPAL.Name = "lblPAL";
            this.toolTip1.SetToolTip(this.lblPAL, resources.GetString("lblPAL.ToolTip"));
            // 
            // PAL_LastLineNumeric
            // 
            resources.ApplyResources(this.PAL_LastLineNumeric, "PAL_LastLineNumeric");
            this.PAL_LastLineNumeric.Maximum = new decimal(new int[] {
            287,
            0,
            0,
            0});
            this.PAL_LastLineNumeric.Name = "PAL_LastLineNumeric";
            this.toolTip1.SetToolTip(this.PAL_LastLineNumeric, resources.GetString("PAL_LastLineNumeric.ToolTip"));
            this.PAL_LastLineNumeric.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
            this.PAL_LastLineNumeric.ValueChanged += new System.EventHandler(this.DrawingArea_ValueChanged);
            // 
            // PAL_FirstLineNumeric
            // 
            resources.ApplyResources(this.PAL_FirstLineNumeric, "PAL_FirstLineNumeric");
            this.PAL_FirstLineNumeric.Maximum = new decimal(new int[] {
            287,
            0,
            0,
            0});
            this.PAL_FirstLineNumeric.Name = "PAL_FirstLineNumeric";
            this.toolTip1.SetToolTip(this.PAL_FirstLineNumeric, resources.GetString("PAL_FirstLineNumeric.ToolTip"));
            this.PAL_FirstLineNumeric.ValueChanged += new System.EventHandler(this.DrawingArea_ValueChanged);
            // 
            // lblNTSC
            // 
            resources.ApplyResources(this.lblNTSC, "lblNTSC");
            this.lblNTSC.Name = "lblNTSC";
            this.toolTip1.SetToolTip(this.lblNTSC, resources.GetString("lblNTSC.ToolTip"));
            // 
            // btnAreaFull
            // 
            resources.ApplyResources(this.btnAreaFull, "btnAreaFull");
            this.btnAreaFull.Name = "btnAreaFull";
            this.toolTip1.SetToolTip(this.btnAreaFull, resources.GetString("btnAreaFull.ToolTip"));
            this.btnAreaFull.UseVisualStyleBackColor = true;
            this.btnAreaFull.Click += new System.EventHandler(this.BtnAreaFull_Click);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            this.toolTip1.SetToolTip(this.label4, resources.GetString("label4.ToolTip"));
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // NTSC_LastLineNumeric
            // 
            resources.ApplyResources(this.NTSC_LastLineNumeric, "NTSC_LastLineNumeric");
            this.NTSC_LastLineNumeric.Maximum = new decimal(new int[] {
            239,
            0,
            0,
            0});
            this.NTSC_LastLineNumeric.Name = "NTSC_LastLineNumeric";
            this.toolTip1.SetToolTip(this.NTSC_LastLineNumeric, resources.GetString("NTSC_LastLineNumeric.ToolTip"));
            this.NTSC_LastLineNumeric.Value = new decimal(new int[] {
            239,
            0,
            0,
            0});
            this.NTSC_LastLineNumeric.ValueChanged += new System.EventHandler(this.DrawingArea_ValueChanged);
            // 
            // NTSC_FirstLineNumeric
            // 
            resources.ApplyResources(this.NTSC_FirstLineNumeric, "NTSC_FirstLineNumeric");
            this.NTSC_FirstLineNumeric.Maximum = new decimal(new int[] {
            239,
            0,
            0,
            0});
            this.NTSC_FirstLineNumeric.Name = "NTSC_FirstLineNumeric";
            this.toolTip1.SetToolTip(this.NTSC_FirstLineNumeric, resources.GetString("NTSC_FirstLineNumeric.ToolTip"));
            this.NTSC_FirstLineNumeric.ValueChanged += new System.EventHandler(this.DrawingArea_ValueChanged);
            // 
            // rbWeave
            // 
            resources.ApplyResources(this.rbWeave, "rbWeave");
            this.rbWeave.Name = "rbWeave";
            this.rbWeave.TabStop = true;
            this.toolTip1.SetToolTip(this.rbWeave, resources.GetString("rbWeave.ToolTip"));
            this.rbWeave.UseVisualStyleBackColor = true;
            // 
            // rbBobOffset
            // 
            resources.ApplyResources(this.rbBobOffset, "rbBobOffset");
            this.rbBobOffset.Name = "rbBobOffset";
            this.rbBobOffset.TabStop = true;
            this.toolTip1.SetToolTip(this.rbBobOffset, resources.GetString("rbBobOffset.ToolTip"));
            this.rbBobOffset.UseVisualStyleBackColor = true;
            // 
            // rbBob
            // 
            resources.ApplyResources(this.rbBob, "rbBob");
            this.rbBob.Name = "rbBob";
            this.rbBob.TabStop = true;
            this.toolTip1.SetToolTip(this.rbBob, resources.GetString("rbBob.ToolTip"));
            this.rbBob.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Controls.Add(this.rbWeave);
            this.groupBox4.Controls.Add(this.rbBobOffset);
            this.groupBox4.Controls.Add(this.rbBob);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox4, resources.GetString("groupBox4.ToolTip"));
            // 
            // groupBox5
            // 
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Controls.Add(this.cbLEC);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox5, resources.GetString("groupBox5.ToolTip"));
            // 
            // cbLEC
            // 
            resources.ApplyResources(this.cbLEC, "cbLEC");
            this.cbLEC.Name = "cbLEC";
            this.toolTip1.SetToolTip(this.cbLEC, resources.GetString("cbLEC.ToolTip"));
            this.cbLEC.UseVisualStyleBackColor = true;
            // 
            // cbGpuLag
            // 
            resources.ApplyResources(this.cbGpuLag, "cbGpuLag");
            this.cbGpuLag.Name = "cbGpuLag";
            this.toolTip1.SetToolTip(this.cbGpuLag, resources.GetString("cbGpuLag.ToolTip"));
            this.cbGpuLag.UseVisualStyleBackColor = true;
            // 
            // groupBox6
            // 
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.Controls.Add(this.cbGpuLag);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox6, resources.GetString("groupBox6.ToolTip"));
            // 
            // PSXOptions
            // 
            this.AcceptButton = this.btnOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PSXOptions";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PAL_LastLineNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PAL_FirstLineNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton rbPixelPro;
		private System.Windows.Forms.Button btnNiceDisplayConfig;
		private BizHawk.WinForms.Controls.LocLabelEx lblMednafen;
		private System.Windows.Forms.RadioButton rbMednafenMode;
		private BizHawk.WinForms.Controls.LocLabelEx lblPixelPro;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.RadioButton rbDebugMode;
		private BizHawk.WinForms.Controls.LocLabelEx lblTweakedMednafen;
		private System.Windows.Forms.RadioButton rbTweakedMednafenMode;
		private System.Windows.Forms.GroupBox groupBox2;
		private BizHawk.WinForms.Controls.LocLabelEx lblPAL;
		private System.Windows.Forms.NumericUpDown PAL_LastLineNumeric;
		private System.Windows.Forms.NumericUpDown PAL_FirstLineNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx lblNTSC;
		private System.Windows.Forms.Button btnAreaFull;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.NumericUpDown NTSC_LastLineNumeric;
		private System.Windows.Forms.NumericUpDown NTSC_FirstLineNumeric;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.RadioButton rbClipNone;
		private System.Windows.Forms.RadioButton rbClipToFramebuffer;
		private System.Windows.Forms.RadioButton rbClipBasic;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.RadioButton rbWeave;
		private System.Windows.Forms.RadioButton rbBobOffset;
		private System.Windows.Forms.RadioButton rbBob;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.CheckBox cbLEC;
		private System.Windows.Forms.CheckBox cbGpuLag;
		private System.Windows.Forms.GroupBox groupBox6;
	}
}