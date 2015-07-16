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
			System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("BizHawk");
			System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("Mednafen");
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
			this.checkEnableOutput = new System.Windows.Forms.CheckBox();
			this.radioButton4 = new System.Windows.Forms.RadioButton();
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
			this.ExitButton.Location = new System.Drawing.Point(411, 401);
			this.ExitButton.Name = "ExitButton";
			this.ExitButton.Size = new System.Drawing.Size(75, 23);
			this.ExitButton.TabIndex = 0;
			this.ExitButton.Text = "E&xit";
			this.ExitButton.UseVisualStyleBackColor = true;
			this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
			// 
			// lblMagicDragArea
			// 
			this.lblMagicDragArea.AllowDrop = true;
			this.lblMagicDragArea.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblMagicDragArea.Controls.Add(this.label1);
			this.lblMagicDragArea.Location = new System.Drawing.Point(286, 31);
			this.lblMagicDragArea.Name = "lblMagicDragArea";
			this.lblMagicDragArea.Size = new System.Drawing.Size(200, 100);
			this.lblMagicDragArea.TabIndex = 1;
			this.lblMagicDragArea.DragDrop += new System.Windows.Forms.DragEventHandler(this.lblMagicDragArea_DragDrop);
			this.lblMagicDragArea.DragEnter += new System.Windows.Forms.DragEventHandler(this.lblMagicDragArea_DragEnter);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(17, 25);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(166, 47);
			this.label1.TabIndex = 0;
			this.label1.Text = "Drag here to HAWK your disc - dump it out as a clean CCD";
			// 
			// lblMp3ExtractMagicArea
			// 
			this.lblMp3ExtractMagicArea.AllowDrop = true;
			this.lblMp3ExtractMagicArea.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblMp3ExtractMagicArea.Controls.Add(this.label2);
			this.lblMp3ExtractMagicArea.Location = new System.Drawing.Point(286, 146);
			this.lblMp3ExtractMagicArea.Name = "lblMp3ExtractMagicArea";
			this.lblMp3ExtractMagicArea.Size = new System.Drawing.Size(200, 100);
			this.lblMp3ExtractMagicArea.TabIndex = 2;
			this.lblMp3ExtractMagicArea.DragDrop += new System.Windows.Forms.DragEventHandler(this.lblMp3ExtractMagicArea_DragDrop);
			this.lblMp3ExtractMagicArea.DragEnter += new System.Windows.Forms.DragEventHandler(this.lblMagicDragArea_DragEnter);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(20, 25);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(163, 39);
			this.label2.TabIndex = 0;
			this.label2.Text = "Drag a disc here to extract the audio tracks to MP3";
			// 
			// btnAbout
			// 
			this.btnAbout.Location = new System.Drawing.Point(319, 401);
			this.btnAbout.Name = "btnAbout";
			this.btnAbout.Size = new System.Drawing.Size(75, 23);
			this.btnAbout.TabIndex = 3;
			this.btnAbout.Text = "&About";
			this.btnAbout.UseVisualStyleBackColor = true;
			this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
			// 
			// radioButton1
			// 
			this.radioButton1.AutoSize = true;
			this.radioButton1.Checked = true;
			this.radioButton1.Location = new System.Drawing.Point(6, 19);
			this.radioButton1.Name = "radioButton1";
			this.radioButton1.Size = new System.Drawing.Size(67, 17);
			this.radioButton1.TabIndex = 4;
			this.radioButton1.TabStop = true;
			this.radioButton1.Text = "BizHawk";
			this.radioButton1.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.radioButton2);
			this.groupBox1.Controls.Add(this.radioButton1);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(253, 206);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Disc Reading Engine";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(20, 95);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(216, 43);
			this.label4.TabIndex = 8;
			this.label4.Text = "- Doesn\'t support audio decoding yet\r\n(even though Mednafen proper can do it)\r\n- " +
    "Loads ISO, CUE, and CCD";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(20, 39);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(216, 33);
			this.label3.TabIndex = 7;
			this.label3.Text = "- Uses FFMPEG for audio decoding\r\n- Loads ISO, CUE, and CCD";
			// 
			// radioButton2
			// 
			this.radioButton2.AutoSize = true;
			this.radioButton2.Location = new System.Drawing.Point(6, 75);
			this.radioButton2.Name = "radioButton2";
			this.radioButton2.Size = new System.Drawing.Size(73, 17);
			this.radioButton2.TabIndex = 5;
			this.radioButton2.Text = "Mednafen";
			this.radioButton2.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.checkEnableOutput);
			this.groupBox2.Controls.Add(this.radioButton4);
			this.groupBox2.Location = new System.Drawing.Point(12, 224);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(253, 69);
			this.groupBox2.TabIndex = 6;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Output Format";
			// 
			// checkEnableOutput
			// 
			this.checkEnableOutput.AutoSize = true;
			this.checkEnableOutput.Checked = true;
			this.checkEnableOutput.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkEnableOutput.Location = new System.Drawing.Point(177, 19);
			this.checkEnableOutput.Name = "checkEnableOutput";
			this.checkEnableOutput.Size = new System.Drawing.Size(59, 17);
			this.checkEnableOutput.TabIndex = 7;
			this.checkEnableOutput.Text = "Enable";
			this.checkEnableOutput.UseVisualStyleBackColor = true;
			// 
			// radioButton4
			// 
			this.radioButton4.AutoSize = true;
			this.radioButton4.Checked = true;
			this.radioButton4.Location = new System.Drawing.Point(12, 19);
			this.radioButton4.Name = "radioButton4";
			this.radioButton4.Size = new System.Drawing.Size(47, 17);
			this.radioButton4.TabIndex = 5;
			this.radioButton4.TabStop = true;
			this.radioButton4.Text = "CCD";
			this.radioButton4.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(9, 305);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(111, 13);
			this.label6.TabIndex = 2;
			this.label6.Text = "Compare Reading To:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(343, 12);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(70, 13);
			this.label7.TabIndex = 10;
			this.label7.Text = "- Operations -";
			// 
			// lvCompareTargets
			// 
			this.lvCompareTargets.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
			this.lvCompareTargets.FullRowSelect = true;
			this.lvCompareTargets.GridLines = true;
			this.lvCompareTargets.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.lvCompareTargets.HideSelection = false;
			this.lvCompareTargets.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2});
			this.lvCompareTargets.Location = new System.Drawing.Point(12, 321);
			this.lvCompareTargets.Name = "lvCompareTargets";
			this.lvCompareTargets.Size = new System.Drawing.Size(121, 97);
			this.lvCompareTargets.TabIndex = 11;
			this.lvCompareTargets.UseCompatibleStateImageBehavior = false;
			this.lvCompareTargets.View = System.Windows.Forms.View.Details;
			// 
			// MainDiscoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(510, 436);
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
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MainDiscoForm";
			this.Text = "DiscoHawk";
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
				private System.Windows.Forms.RadioButton radioButton4;
				private System.Windows.Forms.ListView lvCompareTargets;
				private System.Windows.Forms.ColumnHeader columnHeader1;
				private System.Windows.Forms.CheckBox checkEnableOutput;
	}
}