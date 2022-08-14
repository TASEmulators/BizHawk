namespace BizHawk.Client.EmuHawk
{
	partial class GBPrefControl
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
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.buttonDefaults = new System.Windows.Forms.Button();
            this.buttonGbPalette = new System.Windows.Forms.Button();
            this.cbRgbdsSyntax = new System.Windows.Forms.CheckBox();
            this.checkBoxMuted = new System.Windows.Forms.CheckBox();
            this.cbShowBorder = new System.Windows.Forms.CheckBox();
            this.buttonGbcPalette = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertyGrid1.Location = new System.Drawing.Point(3, 3);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.propertyGrid1.Size = new System.Drawing.Size(516, 408);
            this.propertyGrid1.TabIndex = 0;
            this.propertyGrid1.ToolbarVisible = false;
            this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.PropertyGrid1_PropertyValueChanged);
            // 
            // buttonDefaults
            // 
            this.buttonDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDefaults.Location = new System.Drawing.Point(444, 417);
            this.buttonDefaults.Name = "buttonDefaults";
            this.buttonDefaults.Size = new System.Drawing.Size(75, 23);
            this.buttonDefaults.TabIndex = 1;
            this.buttonDefaults.Text = "Defaults";
            this.buttonDefaults.UseVisualStyleBackColor = true;
            this.buttonDefaults.Click += new System.EventHandler(this.ButtonDefaults_Click);
            // 
            // buttonGbPalette
            // 
            this.buttonGbPalette.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGbPalette.Location = new System.Drawing.Point(3, 417);
            this.buttonGbPalette.Name = "buttonGbPalette";
            this.buttonGbPalette.Size = new System.Drawing.Size(87, 23);
            this.buttonGbPalette.TabIndex = 2;
            this.buttonGbPalette.Text = "GB Palette...";
            this.buttonGbPalette.UseVisualStyleBackColor = true;
            this.buttonGbPalette.Click += new System.EventHandler(this.ButtonGbPalette_Click);
            // 
            // buttonGbcPalette
            // 
            this.buttonGbcPalette.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGbcPalette.Location = new System.Drawing.Point(96, 417);
            this.buttonGbcPalette.Name = "buttonGbcPalette";
            this.buttonGbcPalette.Size = new System.Drawing.Size(87, 23);
            this.buttonGbcPalette.TabIndex = 6;
            this.buttonGbcPalette.Text = "GBC Palette...";
            this.buttonGbcPalette.UseVisualStyleBackColor = true;
            this.buttonGbcPalette.Click += new System.EventHandler(this.ButtonGbcPalette_Click);
            // 
            // cbRgbdsSyntax
            // 
            this.cbRgbdsSyntax.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbRgbdsSyntax.AutoSize = true;
            this.cbRgbdsSyntax.Location = new System.Drawing.Point(245, 421);
            this.cbRgbdsSyntax.Name = "cbRgbdsSyntax";
            this.cbRgbdsSyntax.Size = new System.Drawing.Size(99, 17);
            this.cbRgbdsSyntax.TabIndex = 3;
            this.cbRgbdsSyntax.Text = "RGBDS Syntax";
            this.cbRgbdsSyntax.UseVisualStyleBackColor = true;
            this.cbRgbdsSyntax.CheckedChanged += new System.EventHandler(this.CbRgbdsSyntax_CheckedChanged);
            // 
            // checkBoxMuted
            // 
            this.checkBoxMuted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxMuted.AutoSize = true;
            this.checkBoxMuted.Location = new System.Drawing.Point(189, 421);
            this.checkBoxMuted.Name = "checkBoxMuted";
            this.checkBoxMuted.Size = new System.Drawing.Size(50, 17);
            this.checkBoxMuted.TabIndex = 4;
            this.checkBoxMuted.Text = "Mute";
            this.checkBoxMuted.UseVisualStyleBackColor = true;
            this.checkBoxMuted.CheckedChanged += new System.EventHandler(this.CheckBoxMuted_CheckedChanged);
            // 
            // cbShowBorder
            // 
            this.cbShowBorder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbShowBorder.AutoSize = true;
            this.cbShowBorder.Location = new System.Drawing.Point(350, 421);
            this.cbShowBorder.Name = "cbShowBorder";
            this.cbShowBorder.Size = new System.Drawing.Size(87, 17);
            this.cbShowBorder.TabIndex = 5;
            this.cbShowBorder.Text = "Show Border";
            this.cbShowBorder.UseVisualStyleBackColor = true;
            this.cbShowBorder.CheckedChanged += new System.EventHandler(this.CbShowBorder_CheckedChanged);
            // 
            // GBPrefControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.buttonGbcPalette);
            this.Controls.Add(this.cbRgbdsSyntax);
            this.Controls.Add(this.checkBoxMuted);
            this.Controls.Add(this.cbShowBorder);
            this.Controls.Add(this.buttonGbPalette);
            this.Controls.Add(this.buttonDefaults);
            this.Controls.Add(this.propertyGrid1);
            this.Name = "GBPrefControl";
            this.Size = new System.Drawing.Size(522, 443);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PropertyGrid propertyGrid1;
		private System.Windows.Forms.Button buttonDefaults;
		private System.Windows.Forms.Button buttonGbPalette;
		private System.Windows.Forms.Button buttonGbcPalette;
		private System.Windows.Forms.CheckBox cbRgbdsSyntax;
		private System.Windows.Forms.CheckBox checkBoxMuted;
		private System.Windows.Forms.CheckBox cbShowBorder;
	}
}
