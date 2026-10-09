namespace BizHawk.Client.EmuHawk
{
	partial class AnalogBindControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AnalogBindControl));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.labelAxisName = new BizHawk.WinForms.Controls.LocLabelEx();
            this.trackBarSensitivity = new System.Windows.Forms.TrackBar();
            this.labelSensitivity = new BizHawk.WinForms.Controls.LocLabelEx();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.buttonBind = new System.Windows.Forms.Button();
            this.trackBarDeadzone = new System.Windows.Forms.TrackBar();
            this.labelDeadzone = new BizHawk.WinForms.Controls.LocLabelEx();
            this.buttonFlip = new System.Windows.Forms.Button();
            this.buttonUnbind = new System.Windows.Forms.Button();
            this.iwPositiveButton = new BizHawk.Client.EmuHawk.InputWidget();
            this.iwNegativeButton = new BizHawk.Client.EmuHawk.InputWidget();
            this.labelPositiveButtonName = new BizHawk.WinForms.Controls.LocLabelEx();
            this.labelNegativeButtonName = new BizHawk.WinForms.Controls.LocLabelEx();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarSensitivity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDeadzone)).BeginInit();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            resources.ApplyResources(this.textBox1, "textBox1");
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            // 
            // labelAxisName
            // 
            resources.ApplyResources(this.labelAxisName, "labelAxisName");
            this.labelAxisName.Name = "labelAxisName";
            // 
            // trackBarSensitivity
            // 
            resources.ApplyResources(this.trackBarSensitivity, "trackBarSensitivity");
            this.trackBarSensitivity.LargeChange = 4;
            this.trackBarSensitivity.Maximum = 40;
            this.trackBarSensitivity.Minimum = -40;
            this.trackBarSensitivity.Name = "trackBarSensitivity";
            this.trackBarSensitivity.TickFrequency = 10;
            this.trackBarSensitivity.ValueChanged += new System.EventHandler(this.TrackBarSensitivity_ValueChanged);
            // 
            // labelSensitivity
            // 
            resources.ApplyResources(this.labelSensitivity, "labelSensitivity");
            this.labelSensitivity.Name = "labelSensitivity";
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // buttonBind
            // 
            resources.ApplyResources(this.buttonBind, "buttonBind");
            this.buttonBind.Name = "buttonBind";
            this.buttonBind.UseVisualStyleBackColor = true;
            this.buttonBind.Click += new System.EventHandler(this.ButtonBind_Click);
            // 
            // trackBarDeadzone
            // 
            resources.ApplyResources(this.trackBarDeadzone, "trackBarDeadzone");
            this.trackBarDeadzone.Maximum = 25;
            this.trackBarDeadzone.Name = "trackBarDeadzone";
            this.trackBarDeadzone.TickFrequency = 5;
            this.trackBarDeadzone.ValueChanged += new System.EventHandler(this.TrackBarDeadzone_ValueChanged);
            // 
            // labelDeadzone
            // 
            resources.ApplyResources(this.labelDeadzone, "labelDeadzone");
            this.labelDeadzone.Name = "labelDeadzone";
            // 
            // buttonFlip
            // 
            resources.ApplyResources(this.buttonFlip, "buttonFlip");
            this.buttonFlip.Name = "buttonFlip";
            this.buttonFlip.UseVisualStyleBackColor = true;
            this.buttonFlip.Click += new System.EventHandler(this.ButtonFlip_Click);
            // 
            // buttonUnbind
            // 
            resources.ApplyResources(this.buttonUnbind, "buttonUnbind");
            this.buttonUnbind.Name = "buttonUnbind";
            this.buttonUnbind.UseVisualStyleBackColor = true;
            this.buttonUnbind.Click += new System.EventHandler(this.Unbind_Click);
            // 
            // iwPositiveButton
            // 
            resources.ApplyResources(this.iwPositiveButton, "iwPositiveButton");
            this.iwPositiveButton.AutoTab = true;
            this.iwPositiveButton.Bindings = "";
            this.iwPositiveButton.CompositeWidget = null;
            this.iwPositiveButton.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.iwPositiveButton.Name = "iwPositiveButton";
            this.iwPositiveButton.WidgetName = null;
            // 
            // iwNegativeButton
            // 
            resources.ApplyResources(this.iwNegativeButton, "iwNegativeButton");
            this.iwNegativeButton.AutoTab = true;
            this.iwNegativeButton.Bindings = "";
            this.iwNegativeButton.CompositeWidget = null;
            this.iwNegativeButton.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.iwNegativeButton.Name = "iwNegativeButton";
            this.iwNegativeButton.WidgetName = null;
            // 
            // labelPositiveButtonName
            // 
            resources.ApplyResources(this.labelPositiveButtonName, "labelPositiveButtonName");
            this.labelPositiveButtonName.Name = "labelPositiveButtonName";
            // 
            // labelNegativeButtonName
            // 
            resources.ApplyResources(this.labelNegativeButtonName, "labelNegativeButtonName");
            this.labelNegativeButtonName.Name = "labelNegativeButtonName";
            // 
            // AnalogBindControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelNegativeButtonName);
            this.Controls.Add(this.labelPositiveButtonName);
            this.Controls.Add(this.iwNegativeButton);
            this.Controls.Add(this.iwPositiveButton);
            this.Controls.Add(this.buttonUnbind);
            this.Controls.Add(this.buttonFlip);
            this.Controls.Add(this.labelDeadzone);
            this.Controls.Add(this.trackBarDeadzone);
            this.Controls.Add(this.buttonBind);
            this.Controls.Add(this.labelSensitivity);
            this.Controls.Add(this.trackBarSensitivity);
            this.Controls.Add(this.labelAxisName);
            this.Controls.Add(this.textBox1);
            this.Name = "AnalogBindControl";
            ((System.ComponentModel.ISupportInitialize)(this.trackBarSensitivity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDeadzone)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBox1;
		private BizHawk.WinForms.Controls.LocLabelEx labelAxisName;
		private System.Windows.Forms.TrackBar trackBarSensitivity;
		private BizHawk.WinForms.Controls.LocLabelEx labelSensitivity;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Button buttonBind;
		private System.Windows.Forms.TrackBar trackBarDeadzone;
		private BizHawk.WinForms.Controls.LocLabelEx labelDeadzone;
		private System.Windows.Forms.Button buttonFlip;
		private System.Windows.Forms.Button buttonUnbind;
		private InputWidget iwPositiveButton;
		private InputWidget iwNegativeButton;
		private WinForms.Controls.LocLabelEx labelPositiveButtonName;
		private WinForms.Controls.LocLabelEx labelNegativeButtonName;
	}
}
