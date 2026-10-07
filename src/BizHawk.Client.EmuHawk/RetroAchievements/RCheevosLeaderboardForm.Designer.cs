namespace BizHawk.Client.EmuHawk
{
	partial class RCheevosLeaderboardForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RCheevosLeaderboardForm));
            this.titleLabel = new System.Windows.Forms.Label();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.titleBox = new System.Windows.Forms.TextBox();
            this.descriptionBox = new System.Windows.Forms.TextBox();
            this.scoreLabel = new System.Windows.Forms.Label();
            this.scoreBox = new System.Windows.Forms.TextBox();
            this.lowerIsBetterBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
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
            // scoreLabel
            // 
            resources.ApplyResources(this.scoreLabel, "scoreLabel");
            this.scoreLabel.Name = "scoreLabel";
            // 
            // scoreBox
            // 
            resources.ApplyResources(this.scoreBox, "scoreBox");
            this.scoreBox.Name = "scoreBox";
            this.scoreBox.ReadOnly = true;
            // 
            // lowerIsBetterBox
            // 
            resources.ApplyResources(this.lowerIsBetterBox, "lowerIsBetterBox");
            this.lowerIsBetterBox.AutoCheck = false;
            this.lowerIsBetterBox.Name = "lowerIsBetterBox";
            this.lowerIsBetterBox.UseVisualStyleBackColor = true;
            // 
            // RCheevosLeaderboardForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ControlBox = false;
            this.Controls.Add(this.lowerIsBetterBox);
            this.Controls.Add(this.scoreBox);
            this.Controls.Add(this.scoreLabel);
            this.Controls.Add(this.descriptionBox);
            this.Controls.Add(this.titleBox);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.titleLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RCheevosLeaderboardForm";
            this.ShowIcon = false;
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label descriptionLabel;
		private System.Windows.Forms.TextBox titleBox;
		private System.Windows.Forms.TextBox descriptionBox;
		private System.Windows.Forms.Label scoreLabel;
		private System.Windows.Forms.TextBox scoreBox;
		private System.Windows.Forms.CheckBox lowerIsBetterBox;
	}
}