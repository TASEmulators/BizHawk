namespace BizHawk.MultiClient
{
	partial class VirtualPadSaturnControl
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
			this.B2 = new System.Windows.Forms.CheckBox();
			this.B1 = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// B2
			// 
			this.B2.Appearance = System.Windows.Forms.Appearance.Button;
			this.B2.AutoSize = true;
			this.B2.Location = new System.Drawing.Point(56, 3);
			this.B2.Name = "B2";
			this.B2.Size = new System.Drawing.Size(45, 23);
			this.B2.TabIndex = 13;
			this.B2.Text = "Reset";
			this.B2.UseVisualStyleBackColor = true;
			this.B2.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// B1
			// 
			this.B1.Appearance = System.Windows.Forms.Appearance.Button;
			this.B1.AutoSize = true;
			this.B1.Location = new System.Drawing.Point(3, 3);
			this.B1.Name = "B1";
			this.B1.Size = new System.Drawing.Size(47, 23);
			this.B1.TabIndex = 12;
			this.B1.Text = "Power";
			this.B1.UseVisualStyleBackColor = true;
			this.B1.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// VirtualPadSaturnControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.B2);
			this.Controls.Add(this.B1);
			this.Name = "VirtualPadSaturnControl";
			this.Size = new System.Drawing.Size(104, 29);
			this.Load += new System.EventHandler(this.VirtualPadSaturnControl_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox B2;
		private System.Windows.Forms.CheckBox B1;
	}
}
