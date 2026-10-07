namespace BizHawk.Client.EmuHawk
{
	partial class RCheevosAchievementForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RCheevosAchievementForm));
            this.cheevoBadgeBox = new System.Windows.Forms.PictureBox();
            this.titleLabel = new System.Windows.Forms.Label();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.titleBox = new System.Windows.Forms.TextBox();
            this.descriptionBox = new System.Windows.Forms.TextBox();
            this.pointsLabel = new System.Windows.Forms.Label();
            this.pointsBox = new System.Windows.Forms.TextBox();
            this.progressBox = new System.Windows.Forms.TextBox();
            this.progressLabel = new System.Windows.Forms.Label();
            this.unofficialCheckBox = new System.Windows.Forms.CheckBox();
            this.hcUnlockedCheckBox = new System.Windows.Forms.CheckBox();
            this.primedCheckBox = new System.Windows.Forms.CheckBox();
            this.scUnlockedCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.cheevoBadgeBox)).BeginInit();
            this.SuspendLayout();
            // 
            // cheevoBadgeBox
            // 
            resources.ApplyResources(this.cheevoBadgeBox, "cheevoBadgeBox");
            this.cheevoBadgeBox.Name = "cheevoBadgeBox";
            this.cheevoBadgeBox.TabStop = false;
            // 
            // titleLabel
            // 
            resources.ApplyResources(this.titleLabel, "titleLabel");
            this.titleLabel.Name = "titleLabel";
            // 
            // descriptionLabel
            // 
            resources.ApplyResources(this.descriptionLabel, "descriptionLabel");
            this.descriptionLabel.Name = "descriptionLabel";
            // 
            // titleBox
            // 
            resources.ApplyResources(this.titleBox, "titleBox");
            this.titleBox.Name = "titleBox";
            this.titleBox.ReadOnly = true;
            // 
            // descriptionBox
            // 
            resources.ApplyResources(this.descriptionBox, "descriptionBox");
            this.descriptionBox.Name = "descriptionBox";
            this.descriptionBox.ReadOnly = true;
            // 
            // pointsLabel
            // 
            resources.ApplyResources(this.pointsLabel, "pointsLabel");
            this.pointsLabel.Name = "pointsLabel";
            // 
            // pointsBox
            // 
            resources.ApplyResources(this.pointsBox, "pointsBox");
            this.pointsBox.Name = "pointsBox";
            this.pointsBox.ReadOnly = true;
            // 
            // progressBox
            // 
            resources.ApplyResources(this.progressBox, "progressBox");
            this.progressBox.Name = "progressBox";
            this.progressBox.ReadOnly = true;
            // 
            // progressLabel
            // 
            resources.ApplyResources(this.progressLabel, "progressLabel");
            this.progressLabel.Name = "progressLabel";
            // 
            // unofficialCheckBox
            // 
            resources.ApplyResources(this.unofficialCheckBox, "unofficialCheckBox");
            this.unofficialCheckBox.AutoCheck = false;
            this.unofficialCheckBox.Name = "unofficialCheckBox";
            this.unofficialCheckBox.UseVisualStyleBackColor = true;
            // 
            // hcUnlockedCheckBox
            // 
            resources.ApplyResources(this.hcUnlockedCheckBox, "hcUnlockedCheckBox");
            this.hcUnlockedCheckBox.AutoCheck = false;
            this.hcUnlockedCheckBox.Name = "hcUnlockedCheckBox";
            this.hcUnlockedCheckBox.UseVisualStyleBackColor = true;
            // 
            // primedCheckBox
            // 
            resources.ApplyResources(this.primedCheckBox, "primedCheckBox");
            this.primedCheckBox.AutoCheck = false;
            this.primedCheckBox.Name = "primedCheckBox";
            this.primedCheckBox.UseVisualStyleBackColor = true;
            // 
            // scUnlockedCheckBox
            // 
            resources.ApplyResources(this.scUnlockedCheckBox, "scUnlockedCheckBox");
            this.scUnlockedCheckBox.AutoCheck = false;
            this.scUnlockedCheckBox.Name = "scUnlockedCheckBox";
            this.scUnlockedCheckBox.UseVisualStyleBackColor = true;
            // 
            // RCheevosAchievementForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ControlBox = false;
            this.Controls.Add(this.scUnlockedCheckBox);
            this.Controls.Add(this.primedCheckBox);
            this.Controls.Add(this.hcUnlockedCheckBox);
            this.Controls.Add(this.unofficialCheckBox);
            this.Controls.Add(this.progressLabel);
            this.Controls.Add(this.progressBox);
            this.Controls.Add(this.pointsBox);
            this.Controls.Add(this.pointsLabel);
            this.Controls.Add(this.descriptionBox);
            this.Controls.Add(this.titleBox);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.cheevoBadgeBox);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RCheevosAchievementForm";
            this.ShowIcon = false;
            ((System.ComponentModel.ISupportInitialize)(this.cheevoBadgeBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox cheevoBadgeBox;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label descriptionLabel;
		private System.Windows.Forms.TextBox titleBox;
		private System.Windows.Forms.TextBox descriptionBox;
		private System.Windows.Forms.Label pointsLabel;
		private System.Windows.Forms.TextBox pointsBox;
		private System.Windows.Forms.TextBox progressBox;
		private System.Windows.Forms.Label progressLabel;
		private System.Windows.Forms.CheckBox unofficialCheckBox;
		private System.Windows.Forms.CheckBox hcUnlockedCheckBox;
		private System.Windows.Forms.CheckBox primedCheckBox;
		private System.Windows.Forms.CheckBox scUnlockedCheckBox;
	}
}