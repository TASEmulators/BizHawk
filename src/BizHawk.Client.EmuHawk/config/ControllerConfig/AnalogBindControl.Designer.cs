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
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.trackBarSensitivity = new System.Windows.Forms.TrackBar();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.buttonBind = new System.Windows.Forms.Button();
			this.trackBarDeadzone = new System.Windows.Forms.TrackBar();
			this.buttonFlip = new System.Windows.Forms.Button();
			this.buttonUnbind = new System.Windows.Forms.Button();
			this.labelDeadzone = new BizHawk.WinForms.Controls.LocLabelEx();
			this.labelSensitivity = new BizHawk.WinForms.Controls.LocLabelEx();
			this.labelAxisName = new BizHawk.WinForms.Controls.LocLabelEx();
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
			this.textBox1.Location = new System.Drawing.Point(3, 3);
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(100, 20);
			this.textBox1.TabIndex = 0;
			// 
			// trackBarSensitivity
			// 
			this.trackBarSensitivity.LargeChange = 4;
			this.trackBarSensitivity.Location = new System.Drawing.Point(307, 31);
			this.trackBarSensitivity.Maximum = 40;
			this.trackBarSensitivity.Minimum = -40;
			this.trackBarSensitivity.Name = "trackBarSensitivity";
			this.trackBarSensitivity.Size = new System.Drawing.Size(160, 45);
			this.trackBarSensitivity.TabIndex = 2;
			this.trackBarSensitivity.TickFrequency = 10;
			this.trackBarSensitivity.ValueChanged += new System.EventHandler(this.TrackBarSensitivity_ValueChanged);
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
			// 
			// buttonBind
			// 
			this.buttonBind.Location = new System.Drawing.Point(207, 3);
			this.buttonBind.Name = "buttonBind";
			this.buttonBind.Size = new System.Drawing.Size(75, 23);
			this.buttonBind.TabIndex = 4;
			this.buttonBind.Text = "Bind!";
			this.buttonBind.UseVisualStyleBackColor = true;
			this.buttonBind.Click += new System.EventHandler(this.ButtonBind_Click);
			// 
			// trackBarDeadzone
			// 
			this.trackBarDeadzone.Location = new System.Drawing.Point(307, 61);
			this.trackBarDeadzone.Maximum = 25;
			this.trackBarDeadzone.Name = "trackBarDeadzone";
			this.trackBarDeadzone.Size = new System.Drawing.Size(160, 45);
			this.trackBarDeadzone.TabIndex = 5;
			this.trackBarDeadzone.TickFrequency = 5;
			this.trackBarDeadzone.ValueChanged += new System.EventHandler(this.TrackBarDeadzone_ValueChanged);
			// 
			// buttonFlip
			// 
			this.buttonFlip.Location = new System.Drawing.Point(387, 3);
			this.buttonFlip.Name = "buttonFlip";
			this.buttonFlip.Size = new System.Drawing.Size(75, 23);
			this.buttonFlip.TabIndex = 7;
			this.buttonFlip.Text = "Flip Axis";
			this.buttonFlip.UseVisualStyleBackColor = true;
			this.buttonFlip.Click += new System.EventHandler(this.ButtonFlip_Click);
			// 
			// buttonUnbind
			// 
			this.buttonUnbind.Location = new System.Drawing.Point(297, 3);
			this.buttonUnbind.Name = "buttonUnbind";
			this.buttonUnbind.Size = new System.Drawing.Size(75, 23);
			this.buttonUnbind.TabIndex = 8;
			this.buttonUnbind.Text = "Unbind!";
			this.buttonUnbind.UseVisualStyleBackColor = true;
			this.buttonUnbind.Click += new System.EventHandler(this.Unbind_Click);
			// 
			// labelDeadzone
			// 
			this.labelDeadzone.Location = new System.Drawing.Point(206, 65);
			this.labelDeadzone.Name = "labelDeadzone";
			this.labelDeadzone.Text = "Deadzone: 5 billion";
			// 
			// labelSensitivity
			// 
			this.labelSensitivity.Location = new System.Drawing.Point(206, 35);
			this.labelSensitivity.Name = "labelSensitivity";
			this.labelSensitivity.Text = "Sensitivity: 5 billion";
			// 
			// labelAxisName
			// 
			this.labelAxisName.Location = new System.Drawing.Point(109, 6);
			this.labelAxisName.Name = "labelAxisName";
			this.labelAxisName.Text = "Bindname";
			// 
			// iwPositiveButton
			// 
			this.iwPositiveButton.AutoTab = true;
			this.iwPositiveButton.Bindings = "";
			this.iwPositiveButton.CompositeWidget = null;
			this.iwPositiveButton.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.iwPositiveButton.Location = new System.Drawing.Point(3, 33);
			this.iwPositiveButton.Name = "iwPositiveButton";
			this.iwPositiveButton.Size = new System.Drawing.Size(100, 20);
			this.iwPositiveButton.TabIndex = 12;
			this.iwPositiveButton.WidgetName = null;
			// 
			// iwNegativeButton
			// 
			this.iwNegativeButton.AutoTab = true;
			this.iwNegativeButton.Bindings = "";
			this.iwNegativeButton.CompositeWidget = null;
			this.iwNegativeButton.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.iwNegativeButton.Location = new System.Drawing.Point(3, 63);
			this.iwNegativeButton.Name = "iwNegativeButton";
			this.iwNegativeButton.Size = new System.Drawing.Size(100, 20);
			this.iwNegativeButton.TabIndex = 13;
			this.iwNegativeButton.WidgetName = null;
			// 
			// labelPositiveButtonName
			// 
			this.labelPositiveButtonName.Location = new System.Drawing.Point(109, 36);
			this.labelPositiveButtonName.Name = "labelPositiveButtonName";
			this.labelPositiveButtonName.Text = "Bindname +";
			// 
			// labelNegativeButtonName
			// 
			this.labelNegativeButtonName.Location = new System.Drawing.Point(109, 66);
			this.labelNegativeButtonName.Name = "labelNegativeButtonName";
			this.labelNegativeButtonName.Text = "Bindname -";
			// 
			// AnalogBindControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
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
			this.Size = new System.Drawing.Size(474, 99);
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
