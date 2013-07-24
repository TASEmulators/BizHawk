namespace BizHawk.MultiClient.config.ControllerConfig
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
			this.labelButtonName = new System.Windows.Forms.Label();
			this.trackBarSensitivity = new System.Windows.Forms.TrackBar();
			this.labelSensitivity = new System.Windows.Forms.Label();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.buttonBind = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.trackBarSensitivity)).BeginInit();
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
			// labelButtonName
			// 
			this.labelButtonName.AutoSize = true;
			this.labelButtonName.Location = new System.Drawing.Point(109, 6);
			this.labelButtonName.Name = "labelButtonName";
			this.labelButtonName.Size = new System.Drawing.Size(54, 13);
			this.labelButtonName.TabIndex = 1;
			this.labelButtonName.Text = "Bindname";
			// 
			// trackBarSensitivity
			// 
			this.trackBarSensitivity.LargeChange = 2000;
			this.trackBarSensitivity.Location = new System.Drawing.Point(169, 3);
			this.trackBarSensitivity.Maximum = 2000;
			this.trackBarSensitivity.Minimum = -2000;
			this.trackBarSensitivity.Name = "trackBarSensitivity";
			this.trackBarSensitivity.Size = new System.Drawing.Size(104, 42);
			this.trackBarSensitivity.SmallChange = 1000;
			this.trackBarSensitivity.TabIndex = 2;
			this.trackBarSensitivity.TickFrequency = 1000;
			this.trackBarSensitivity.ValueChanged += new System.EventHandler(this.trackBarSensitivity_ValueChanged);
			// 
			// labelSensitivity
			// 
			this.labelSensitivity.AutoSize = true;
			this.labelSensitivity.Location = new System.Drawing.Point(3, 26);
			this.labelSensitivity.Name = "labelSensitivity";
			this.labelSensitivity.Size = new System.Drawing.Size(95, 13);
			this.labelSensitivity.TabIndex = 3;
			this.labelSensitivity.Text = "Sensitivity: 5 billion";
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// buttonBind
			// 
			this.buttonBind.Location = new System.Drawing.Point(279, 1);
			this.buttonBind.Name = "buttonBind";
			this.buttonBind.Size = new System.Drawing.Size(75, 23);
			this.buttonBind.TabIndex = 4;
			this.buttonBind.Text = "Bind!";
			this.buttonBind.UseVisualStyleBackColor = true;
			this.buttonBind.Click += new System.EventHandler(this.buttonBind_Click);
			// 
			// AnalogBindControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.buttonBind);
			this.Controls.Add(this.labelSensitivity);
			this.Controls.Add(this.trackBarSensitivity);
			this.Controls.Add(this.labelButtonName);
			this.Controls.Add(this.textBox1);
			this.Name = "AnalogBindControl";
			this.Size = new System.Drawing.Size(387, 43);
			((System.ComponentModel.ISupportInitialize)(this.trackBarSensitivity)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label labelButtonName;
		private System.Windows.Forms.TrackBar trackBarSensitivity;
		private System.Windows.Forms.Label labelSensitivity;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Button buttonBind;
	}
}
