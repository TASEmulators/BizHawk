namespace BizHawk.Client.DiscoHawk
{
	partial class MainDiscoForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainDiscoForm));
            this.ExitButton = new System.Windows.Forms.Button();
            this.lblMagicDragArea = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.lblMp3ExtractMagicArea = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.btnAbout = new System.Windows.Forms.Button();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ccdOutputButton = new System.Windows.Forms.RadioButton();
            this.chdOutputButton = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lvCompareTargets = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lblMagicDragArea.SuspendLayout();
            this.lblMp3ExtractMagicArea.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // ExitButton
            // 
            resources.ApplyResources(this.ExitButton, "ExitButton");
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // lblMagicDragArea
            // 
            resources.ApplyResources(this.lblMagicDragArea, "lblMagicDragArea");
            this.lblMagicDragArea.AllowDrop = true;
            this.lblMagicDragArea.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblMagicDragArea.Controls.Add(this.label1);
            this.lblMagicDragArea.Name = "lblMagicDragArea";
            this.lblMagicDragArea.DragDrop += new System.Windows.Forms.DragEventHandler(this.lblMagicDragArea_DragDrop);
            this.lblMagicDragArea.DragEnter += new System.Windows.Forms.DragEventHandler(this.LblMagicDragArea_DragEnter);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // lblMp3ExtractMagicArea
            // 
            resources.ApplyResources(this.lblMp3ExtractMagicArea, "lblMp3ExtractMagicArea");
            this.lblMp3ExtractMagicArea.AllowDrop = true;
            this.lblMp3ExtractMagicArea.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblMp3ExtractMagicArea.Controls.Add(this.label2);
            this.lblMp3ExtractMagicArea.Name = "lblMp3ExtractMagicArea";
            this.lblMp3ExtractMagicArea.DragDrop += new System.Windows.Forms.DragEventHandler(this.LblMp3ExtractMagicArea_DragDrop);
            this.lblMp3ExtractMagicArea.DragEnter += new System.Windows.Forms.DragEventHandler(this.LblMagicDragArea_DragEnter);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // btnAbout
            // 
            resources.ApplyResources(this.btnAbout, "btnAbout");
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.UseVisualStyleBackColor = true;
            this.btnAbout.Click += new System.EventHandler(this.BtnAbout_Click);
            // 
            // radioButton1
            // 
            resources.ApplyResources(this.radioButton1, "radioButton1");
            this.radioButton1.Checked = true;
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.TabStop = true;
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.radioButton2);
            this.groupBox1.Controls.Add(this.radioButton1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // radioButton2
            // 
            resources.ApplyResources(this.radioButton2, "radioButton2");
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.ccdOutputButton);
            this.groupBox2.Controls.Add(this.chdOutputButton);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // ccdOutputButton
            // 
            resources.ApplyResources(this.ccdOutputButton, "ccdOutputButton");
            this.ccdOutputButton.Checked = true;
            this.ccdOutputButton.Name = "ccdOutputButton";
            this.ccdOutputButton.TabStop = true;
            this.ccdOutputButton.UseVisualStyleBackColor = true;
            // 
            // chdOutputButton
            // 
            resources.ApplyResources(this.chdOutputButton, "chdOutputButton");
            this.chdOutputButton.Name = "chdOutputButton";
            this.chdOutputButton.TabStop = true;
            this.chdOutputButton.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // lvCompareTargets
            // 
            resources.ApplyResources(this.lvCompareTargets, "lvCompareTargets");
            this.lvCompareTargets.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.lvCompareTargets.FullRowSelect = true;
            this.lvCompareTargets.GridLines = true;
            this.lvCompareTargets.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvCompareTargets.HideSelection = false;
            this.lvCompareTargets.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvCompareTargets.Items"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvCompareTargets.Items1")))});
            this.lvCompareTargets.Name = "lvCompareTargets";
            this.lvCompareTargets.UseCompatibleStateImageBehavior = false;
            this.lvCompareTargets.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // MainDiscoForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lvCompareTargets);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnAbout);
            this.Controls.Add(this.lblMp3ExtractMagicArea);
            this.Controls.Add(this.lblMagicDragArea);
            this.Controls.Add(this.ExitButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainDiscoForm";
            this.Load += new System.EventHandler(this.MainDiscoForm_Load);
            this.lblMagicDragArea.ResumeLayout(false);
            this.lblMp3ExtractMagicArea.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button ExitButton;
		private System.Windows.Forms.Panel lblMagicDragArea;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel lblMp3ExtractMagicArea;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btnAbout;
		private System.Windows.Forms.RadioButton radioButton1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton radioButton2;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.RadioButton ccdOutputButton;
		private System.Windows.Forms.RadioButton chdOutputButton;
		private System.Windows.Forms.ListView lvCompareTargets;
		private System.Windows.Forms.ColumnHeader columnHeader1;
	}
}