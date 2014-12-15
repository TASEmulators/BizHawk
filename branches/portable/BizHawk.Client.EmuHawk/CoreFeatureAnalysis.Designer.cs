namespace BizHawk.Client.EmuHawk
{
	partial class CoreFeatureAnalysis
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CoreFeatureAnalysis));
			this.OkBtn = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.TotalCoresLabel = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.ReleasedCoresLabel = new System.Windows.Forms.Label();
			this.CoreTree = new System.Windows.Forms.TreeView();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.OkBtn.Location = new System.Drawing.Point(456, 569);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 0;
			this.OkBtn.Text = "&Ok";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(99, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(34, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Total:";
			// 
			// TotalCoresLabel
			// 
			this.TotalCoresLabel.AutoSize = true;
			this.TotalCoresLabel.Location = new System.Drawing.Point(132, 19);
			this.TotalCoresLabel.Name = "TotalCoresLabel";
			this.TotalCoresLabel.Size = new System.Drawing.Size(19, 13);
			this.TotalCoresLabel.TabIndex = 3;
			this.TotalCoresLabel.Text = "20";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 19);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(55, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Released:";
			// 
			// ReleasedCoresLabel
			// 
			this.ReleasedCoresLabel.AutoSize = true;
			this.ReleasedCoresLabel.Location = new System.Drawing.Point(64, 19);
			this.ReleasedCoresLabel.Name = "ReleasedCoresLabel";
			this.ReleasedCoresLabel.Size = new System.Drawing.Size(19, 13);
			this.ReleasedCoresLabel.TabIndex = 5;
			this.ReleasedCoresLabel.Text = "20";
			// 
			// CoreTree
			// 
			this.CoreTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CoreTree.Location = new System.Drawing.Point(12, 35);
			this.CoreTree.Name = "CoreTree";
			this.CoreTree.Size = new System.Drawing.Size(504, 528);
			this.CoreTree.TabIndex = 0;
			// 
			// CoreFeatureAnalysis
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.OkBtn;
			this.ClientSize = new System.Drawing.Size(528, 604);
			this.Controls.Add(this.CoreTree);
			this.Controls.Add(this.ReleasedCoresLabel);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.TotalCoresLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.OkBtn);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "CoreFeatureAnalysis";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Core Features";
			this.Load += new System.EventHandler(this.CoreFeatureAnalysis_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label TotalCoresLabel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label ReleasedCoresLabel;
		private System.Windows.Forms.TreeView CoreTree;
	}
}