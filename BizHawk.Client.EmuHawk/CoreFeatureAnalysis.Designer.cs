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
			this.CoreTree = new System.Windows.Forms.TreeView();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.ReleasedCoresLabel = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.TotalCoresLabel = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.CurrentCoreTree = new System.Windows.Forms.TreeView();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
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
			// CoreTree
			// 
			this.CoreTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CoreTree.Location = new System.Drawing.Point(6, 24);
			this.CoreTree.Name = "CoreTree";
			this.CoreTree.Size = new System.Drawing.Size(481, 495);
			this.CoreTree.TabIndex = 0;
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Location = new System.Drawing.Point(15, 12);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(501, 551);
			this.tabControl1.TabIndex = 6;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.CurrentCoreTree);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(493, 525);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Current";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.ReleasedCoresLabel);
			this.tabPage2.Controls.Add(this.label2);
			this.tabPage2.Controls.Add(this.TotalCoresLabel);
			this.tabPage2.Controls.Add(this.label1);
			this.tabPage2.Controls.Add(this.CoreTree);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(493, 525);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "All";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// ReleasedCoresLabel
			// 
			this.ReleasedCoresLabel.AutoSize = true;
			this.ReleasedCoresLabel.Location = new System.Drawing.Point(62, 8);
			this.ReleasedCoresLabel.Name = "ReleasedCoresLabel";
			this.ReleasedCoresLabel.Size = new System.Drawing.Size(19, 13);
			this.ReleasedCoresLabel.TabIndex = 9;
			this.ReleasedCoresLabel.Text = "20";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(10, 8);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(55, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Released:";
			// 
			// TotalCoresLabel
			// 
			this.TotalCoresLabel.AutoSize = true;
			this.TotalCoresLabel.Location = new System.Drawing.Point(130, 8);
			this.TotalCoresLabel.Name = "TotalCoresLabel";
			this.TotalCoresLabel.Size = new System.Drawing.Size(19, 13);
			this.TotalCoresLabel.TabIndex = 7;
			this.TotalCoresLabel.Text = "20";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(97, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(34, 13);
			this.label1.TabIndex = 6;
			this.label1.Text = "Total:";
			// 
			// CurrentCoreTree
			// 
			this.CurrentCoreTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CurrentCoreTree.Location = new System.Drawing.Point(6, 6);
			this.CurrentCoreTree.Name = "CurrentCoreTree";
			this.CurrentCoreTree.Size = new System.Drawing.Size(481, 513);
			this.CurrentCoreTree.TabIndex = 1;
			// 
			// CoreFeatureAnalysis
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.OkBtn;
			this.ClientSize = new System.Drawing.Size(528, 604);
			this.Controls.Add(this.tabControl1);
			this.Controls.Add(this.OkBtn);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "CoreFeatureAnalysis";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Core Features";
			this.Load += new System.EventHandler(this.CoreFeatureAnalysis_Load);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.TreeView CoreTree;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.Label ReleasedCoresLabel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label TotalCoresLabel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TreeView CurrentCoreTree;
	}
}