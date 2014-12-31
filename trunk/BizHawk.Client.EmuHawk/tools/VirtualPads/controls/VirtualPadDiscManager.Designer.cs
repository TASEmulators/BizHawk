namespace BizHawk.Client.EmuHawk
{
	partial class VirtualPadDiscManager
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lvDiscs = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.btnInsert = new BizHawk.Client.EmuHawk.VirtualPadButton();
			this.btnEject = new BizHawk.Client.EmuHawk.VirtualPadButton();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.lvDiscs);
			this.groupBox1.Location = new System.Drawing.Point(3, 32);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(279, 207);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Discs";
			this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
			// 
			// lvDiscs
			// 
			this.lvDiscs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.lvDiscs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvDiscs.FullRowSelect = true;
			this.lvDiscs.GridLines = true;
			this.lvDiscs.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lvDiscs.HideSelection = false;
			this.lvDiscs.Location = new System.Drawing.Point(3, 16);
			this.lvDiscs.MultiSelect = false;
			this.lvDiscs.Name = "lvDiscs";
			this.lvDiscs.Size = new System.Drawing.Size(273, 188);
			this.lvDiscs.TabIndex = 0;
			this.lvDiscs.UseCompatibleStateImageBehavior = false;
			this.lvDiscs.View = System.Windows.Forms.View.Details;
			this.lvDiscs.SelectedIndexChanged += new System.EventHandler(this.lvDiscs_SelectedIndexChanged);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "#";
			this.columnHeader1.Width = 27;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Name";
			this.columnHeader2.Width = 228;
			// 
			// btnInsert
			// 
			this.btnInsert.Appearance = System.Windows.Forms.Appearance.Button;
			this.btnInsert.AutoSize = true;
			this.btnInsert.ForeColor = System.Drawing.SystemColors.ControlText;
			this.btnInsert.Location = new System.Drawing.Point(50, 3);
			this.btnInsert.Name = "btnInsert";
			this.btnInsert.ReadOnly = false;
			this.btnInsert.RightClicked = false;
			this.btnInsert.Size = new System.Drawing.Size(43, 23);
			this.btnInsert.TabIndex = 2;
			this.btnInsert.Text = "Insert";
			this.btnInsert.UseVisualStyleBackColor = true;
			// 
			// btnEject
			// 
			this.btnEject.Appearance = System.Windows.Forms.Appearance.Button;
			this.btnEject.AutoSize = true;
			this.btnEject.ForeColor = System.Drawing.SystemColors.ControlText;
			this.btnEject.Location = new System.Drawing.Point(3, 3);
			this.btnEject.Name = "btnEject";
			this.btnEject.ReadOnly = false;
			this.btnEject.RightClicked = false;
			this.btnEject.Size = new System.Drawing.Size(41, 23);
			this.btnEject.TabIndex = 0;
			this.btnEject.Text = "Eject";
			this.btnEject.UseVisualStyleBackColor = true;
			// 
			// VirtualPadDiscManager
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.Controls.Add(this.btnInsert);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnEject);
			this.Name = "VirtualPadDiscManager";
			this.Size = new System.Drawing.Size(286, 244);
			this.groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private VirtualPadButton btnEject;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.ListView lvDiscs;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private VirtualPadButton btnInsert;
	}
}
