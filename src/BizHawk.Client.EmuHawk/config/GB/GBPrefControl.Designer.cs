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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GBPrefControl));
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
            resources.ApplyResources(this.propertyGrid1, "propertyGrid1");
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.propertyGrid1.ToolbarVisible = false;
            this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.PropertyGrid1_PropertyValueChanged);
            // 
            // buttonDefaults
            // 
            resources.ApplyResources(this.buttonDefaults, "buttonDefaults");
            this.buttonDefaults.Name = "buttonDefaults";
            this.buttonDefaults.UseVisualStyleBackColor = true;
            this.buttonDefaults.Click += new System.EventHandler(this.ButtonDefaults_Click);
            // 
            // buttonGbPalette
            // 
            resources.ApplyResources(this.buttonGbPalette, "buttonGbPalette");
            this.buttonGbPalette.Name = "buttonGbPalette";
            this.buttonGbPalette.UseVisualStyleBackColor = true;
            this.buttonGbPalette.Click += new System.EventHandler(this.ButtonGbPalette_Click);
            // 
            // cbRgbdsSyntax
            // 
            resources.ApplyResources(this.cbRgbdsSyntax, "cbRgbdsSyntax");
            this.cbRgbdsSyntax.Name = "cbRgbdsSyntax";
            this.cbRgbdsSyntax.UseVisualStyleBackColor = true;
            this.cbRgbdsSyntax.CheckedChanged += new System.EventHandler(this.CbRgbdsSyntax_CheckedChanged);
            // 
            // checkBoxMuted
            // 
            resources.ApplyResources(this.checkBoxMuted, "checkBoxMuted");
            this.checkBoxMuted.Name = "checkBoxMuted";
            this.checkBoxMuted.UseVisualStyleBackColor = true;
            this.checkBoxMuted.CheckedChanged += new System.EventHandler(this.CheckBoxMuted_CheckedChanged);
            // 
            // cbShowBorder
            // 
            resources.ApplyResources(this.cbShowBorder, "cbShowBorder");
            this.cbShowBorder.Name = "cbShowBorder";
            this.cbShowBorder.UseVisualStyleBackColor = true;
            this.cbShowBorder.CheckedChanged += new System.EventHandler(this.CbShowBorder_CheckedChanged);
            // 
            // buttonGbcPalette
            // 
            resources.ApplyResources(this.buttonGbcPalette, "buttonGbcPalette");
            this.buttonGbcPalette.Name = "buttonGbcPalette";
            this.buttonGbcPalette.UseVisualStyleBackColor = true;
            this.buttonGbcPalette.Click += new System.EventHandler(this.ButtonGbcPalette_Click);
            // 
            // GBPrefControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.buttonGbcPalette);
            this.Controls.Add(this.cbRgbdsSyntax);
            this.Controls.Add(this.checkBoxMuted);
            this.Controls.Add(this.cbShowBorder);
            this.Controls.Add(this.buttonGbPalette);
            this.Controls.Add(this.buttonDefaults);
            this.Controls.Add(this.propertyGrid1);
            this.Name = "GBPrefControl";
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
