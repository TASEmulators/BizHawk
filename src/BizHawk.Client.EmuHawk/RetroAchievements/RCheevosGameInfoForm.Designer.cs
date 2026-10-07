namespace BizHawk.Client.EmuHawk
{
	partial class RCheevosGameInfoForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RCheevosGameInfoForm));
            this.gameIconBox = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.titleTextBox = new System.Windows.Forms.TextBox();
            this.totalPointsBox = new System.Windows.Forms.TextBox();
            this.currentLboardBox = new System.Windows.Forms.TextBox();
            this.richPresenceBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.gameIconBox)).BeginInit();
            this.SuspendLayout();
            // 
            // gameIconBox
            // 
            resources.ApplyResources(this.gameIconBox, "gameIconBox");
            this.gameIconBox.Name = "gameIconBox";
            this.gameIconBox.TabStop = false;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // titleTextBox
            // 
            resources.ApplyResources(this.titleTextBox, "titleTextBox");
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.ReadOnly = true;
            // 
            // totalPointsBox
            // 
            resources.ApplyResources(this.totalPointsBox, "totalPointsBox");
            this.totalPointsBox.Name = "totalPointsBox";
            this.totalPointsBox.ReadOnly = true;
            // 
            // currentLboardBox
            // 
            resources.ApplyResources(this.currentLboardBox, "currentLboardBox");
            this.currentLboardBox.Name = "currentLboardBox";
            this.currentLboardBox.ReadOnly = true;
            // 
            // richPresenceBox
            // 
            resources.ApplyResources(this.richPresenceBox, "richPresenceBox");
            this.richPresenceBox.Name = "richPresenceBox";
            this.richPresenceBox.ReadOnly = true;
            // 
            // RCheevosGameInfoForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.richPresenceBox);
            this.Controls.Add(this.currentLboardBox);
            this.Controls.Add(this.totalPointsBox);
            this.Controls.Add(this.titleTextBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.gameIconBox);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RCheevosGameInfoForm";
            this.ShowIcon = false;
            ((System.ComponentModel.ISupportInitialize)(this.gameIconBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox gameIconBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox titleTextBox;
		private System.Windows.Forms.TextBox totalPointsBox;
		private System.Windows.Forms.TextBox currentLboardBox;
		private System.Windows.Forms.TextBox richPresenceBox;
	}
}