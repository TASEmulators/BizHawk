namespace BizHawk.Client.EmuHawk
{
	partial class RecorderBox
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
			this.RecorderGroupBox = new System.Windows.Forms.GroupBox();
			this.Player1Radio = new System.Windows.Forms.RadioButton();
			this.AllRadio = new System.Windows.Forms.RadioButton();
			this.RecordingCheckbox = new System.Windows.Forms.CheckBox();
			this.SuperimposeCheckbox = new System.Windows.Forms.CheckBox();
			this.UsePatterncheckbox = new System.Windows.Forms.CheckBox();
			this.RecorderGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// RecorderGroupBox
			// 
			this.RecorderGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.RecorderGroupBox.Controls.Add(this.UsePatterncheckbox);
			this.RecorderGroupBox.Controls.Add(this.SuperimposeCheckbox);
			this.RecorderGroupBox.Controls.Add(this.Player1Radio);
			this.RecorderGroupBox.Controls.Add(this.AllRadio);
			this.RecorderGroupBox.Controls.Add(this.RecordingCheckbox);
			this.RecorderGroupBox.Location = new System.Drawing.Point(0, 0);
			this.RecorderGroupBox.Name = "RecorderGroupBox";
			this.RecorderGroupBox.Size = new System.Drawing.Size(201, 96);
			this.RecorderGroupBox.TabIndex = 0;
			this.RecorderGroupBox.TabStop = false;
			this.RecorderGroupBox.Text = "Recorder";
			// 
			// Player1Radio
			// 
			this.Player1Radio.AutoSize = true;
			this.Player1Radio.Location = new System.Drawing.Point(6, 42);
			this.Player1Radio.Name = "Player1Radio";
			this.Player1Radio.Size = new System.Drawing.Size(38, 17);
			this.Player1Radio.TabIndex = 2;
			this.Player1Radio.TabStop = true;
			this.Player1Radio.Text = "1P";
			this.Player1Radio.UseVisualStyleBackColor = true;
			// 
			// AllRadio
			// 
			this.AllRadio.AutoSize = true;
			this.AllRadio.Location = new System.Drawing.Point(156, 19);
			this.AllRadio.Name = "AllRadio";
			this.AllRadio.Size = new System.Drawing.Size(36, 17);
			this.AllRadio.TabIndex = 1;
			this.AllRadio.TabStop = true;
			this.AllRadio.Text = "All";
			this.AllRadio.UseVisualStyleBackColor = true;
			// 
			// RecordingCheckbox
			// 
			this.RecordingCheckbox.AutoSize = true;
			this.RecordingCheckbox.Location = new System.Drawing.Point(6, 19);
			this.RecordingCheckbox.Name = "RecordingCheckbox";
			this.RecordingCheckbox.Size = new System.Drawing.Size(75, 17);
			this.RecordingCheckbox.TabIndex = 0;
			this.RecordingCheckbox.Text = "Recording";
			this.RecordingCheckbox.UseVisualStyleBackColor = true;
			// 
			// SuperimposeCheckbox
			// 
			this.SuperimposeCheckbox.AutoSize = true;
			this.SuperimposeCheckbox.Location = new System.Drawing.Point(6, 65);
			this.SuperimposeCheckbox.Name = "SuperimposeCheckbox";
			this.SuperimposeCheckbox.Size = new System.Drawing.Size(87, 17);
			this.SuperimposeCheckbox.TabIndex = 3;
			this.SuperimposeCheckbox.Text = "Superimpose";
			this.SuperimposeCheckbox.UseVisualStyleBackColor = true;
			// 
			// UsePatterncheckbox
			// 
			this.UsePatterncheckbox.AutoSize = true;
			this.UsePatterncheckbox.Location = new System.Drawing.Point(99, 65);
			this.UsePatterncheckbox.Name = "UsePatterncheckbox";
			this.UsePatterncheckbox.Size = new System.Drawing.Size(81, 17);
			this.UsePatterncheckbox.TabIndex = 4;
			this.UsePatterncheckbox.Text = "Use pattern";
			this.UsePatterncheckbox.UseVisualStyleBackColor = true;
			// 
			// RecorderBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.RecorderGroupBox);
			this.Name = "RecorderBox";
			this.Size = new System.Drawing.Size(204, 99);
			this.RecorderGroupBox.ResumeLayout(false);
			this.RecorderGroupBox.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox RecorderGroupBox;
		private System.Windows.Forms.RadioButton AllRadio;
		private System.Windows.Forms.CheckBox RecordingCheckbox;
		private System.Windows.Forms.RadioButton Player1Radio;
		private System.Windows.Forms.CheckBox UsePatterncheckbox;
		private System.Windows.Forms.CheckBox SuperimposeCheckbox;
	}
}
