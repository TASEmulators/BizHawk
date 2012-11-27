namespace BizHawk.MultiClient.GBAtools
{
	partial class GBAGPUView
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
			this.listBoxWidgets = new System.Windows.Forms.ListBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonShowWidget = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// listBoxWidgets
			// 
			this.listBoxWidgets.Location = new System.Drawing.Point(12, 25);
			this.listBoxWidgets.Name = "listBoxWidgets";
			this.listBoxWidgets.Size = new System.Drawing.Size(120, 160);
			this.listBoxWidgets.TabIndex = 0;
			this.listBoxWidgets.DoubleClick += new System.EventHandler(this.listBoxWidgets_DoubleClick);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel1.Location = new System.Drawing.Point(138, 12);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(486, 381);
			this.panel1.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(92, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Available widgets:";
			// 
			// buttonShowWidget
			// 
			this.buttonShowWidget.Location = new System.Drawing.Point(29, 191);
			this.buttonShowWidget.Name = "buttonShowWidget";
			this.buttonShowWidget.Size = new System.Drawing.Size(75, 23);
			this.buttonShowWidget.TabIndex = 3;
			this.buttonShowWidget.Text = "Show >>";
			this.buttonShowWidget.UseVisualStyleBackColor = true;
			this.buttonShowWidget.Click += new System.EventHandler(this.buttonShowWidget_Click);
			// 
			// GBAGPUView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(636, 405);
			this.Controls.Add(this.buttonShowWidget);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.listBoxWidgets);
			this.Name = "GBAGPUView";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "GBA GPU Viewer";
			this.Load += new System.EventHandler(this.GBAGPUView_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxWidgets;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonShowWidget;

	}
}