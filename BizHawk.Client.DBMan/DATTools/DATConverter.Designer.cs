namespace BizHawk.Client.DBMan
{
	partial class DATConverter
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
			this.comboBoxSystemSelect = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.groupImportTypes = new System.Windows.Forms.GroupBox();
			this.radioTOSEC = new System.Windows.Forms.RadioButton();
			this.listBoxFiles = new System.Windows.Forms.ListBox();
			this.buttonAddFiles = new System.Windows.Forms.Button();
			this.buttonRemove = new System.Windows.Forms.Button();
			this.buttonStartProcessing = new System.Windows.Forms.Button();
			this.textBoxOutputFolder = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.button2 = new System.Windows.Forms.Button();
			this.radioNOINTRO = new System.Windows.Forms.RadioButton();
			this.groupImportTypes.SuspendLayout();
			this.SuspendLayout();
			// 
			// comboBoxSystemSelect
			// 
			this.comboBoxSystemSelect.FormattingEnabled = true;
			this.comboBoxSystemSelect.Location = new System.Drawing.Point(13, 13);
			this.comboBoxSystemSelect.Name = "comboBoxSystemSelect";
			this.comboBoxSystemSelect.Size = new System.Drawing.Size(121, 21);
			this.comboBoxSystemSelect.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(141, 20);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(74, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Select System";
			// 
			// groupImportTypes
			// 
			this.groupImportTypes.Controls.Add(this.radioNOINTRO);
			this.groupImportTypes.Controls.Add(this.radioTOSEC);
			this.groupImportTypes.Location = new System.Drawing.Point(13, 50);
			this.groupImportTypes.Name = "groupImportTypes";
			this.groupImportTypes.Size = new System.Drawing.Size(200, 100);
			this.groupImportTypes.TabIndex = 2;
			this.groupImportTypes.TabStop = false;
			this.groupImportTypes.Text = "Import Type";
			// 
			// radioTOSEC
			// 
			this.radioTOSEC.AutoSize = true;
			this.radioTOSEC.Location = new System.Drawing.Point(7, 20);
			this.radioTOSEC.Name = "radioTOSEC";
			this.radioTOSEC.Size = new System.Drawing.Size(61, 17);
			this.radioTOSEC.TabIndex = 0;
			this.radioTOSEC.TabStop = true;
			this.radioTOSEC.Text = "TOSEC";
			this.radioTOSEC.UseVisualStyleBackColor = true;
			// 
			// listBoxFiles
			// 
			this.listBoxFiles.FormattingEnabled = true;
			this.listBoxFiles.HorizontalScrollbar = true;
			this.listBoxFiles.Location = new System.Drawing.Point(13, 180);
			this.listBoxFiles.Name = "listBoxFiles";
			this.listBoxFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.listBoxFiles.Size = new System.Drawing.Size(328, 121);
			this.listBoxFiles.TabIndex = 3;
			this.listBoxFiles.UseTabStops = false;
			// 
			// buttonAddFiles
			// 
			this.buttonAddFiles.Location = new System.Drawing.Point(348, 180);
			this.buttonAddFiles.Name = "buttonAddFiles";
			this.buttonAddFiles.Size = new System.Drawing.Size(107, 23);
			this.buttonAddFiles.TabIndex = 4;
			this.buttonAddFiles.Text = "Browse";
			this.buttonAddFiles.UseVisualStyleBackColor = true;
			this.buttonAddFiles.Click += new System.EventHandler(this.buttonAddFiles_Click);
			// 
			// buttonRemove
			// 
			this.buttonRemove.Location = new System.Drawing.Point(348, 210);
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.Size = new System.Drawing.Size(107, 23);
			this.buttonRemove.TabIndex = 5;
			this.buttonRemove.Text = "Remove";
			this.buttonRemove.UseVisualStyleBackColor = true;
			this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
			// 
			// buttonStartProcessing
			// 
			this.buttonStartProcessing.Location = new System.Drawing.Point(13, 356);
			this.buttonStartProcessing.Name = "buttonStartProcessing";
			this.buttonStartProcessing.Size = new System.Drawing.Size(101, 23);
			this.buttonStartProcessing.TabIndex = 6;
			this.buttonStartProcessing.Text = "Start Processing";
			this.buttonStartProcessing.UseVisualStyleBackColor = true;
			this.buttonStartProcessing.Click += new System.EventHandler(this.buttonStartProcessing_Click);
			// 
			// textBoxOutputFolder
			// 
			this.textBoxOutputFolder.Location = new System.Drawing.Point(13, 330);
			this.textBoxOutputFolder.Name = "textBoxOutputFolder";
			this.textBoxOutputFolder.Size = new System.Drawing.Size(328, 20);
			this.textBoxOutputFolder.TabIndex = 7;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 164);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(75, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Files to Import:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 314);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(87, 13);
			this.label3.TabIndex = 9;
			this.label3.Text = "Output Directory:";
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(348, 328);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(107, 23);
			this.button2.TabIndex = 10;
			this.button2.Text = "Select Output DIR";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// radioNOINTRO
			// 
			this.radioNOINTRO.AutoSize = true;
			this.radioNOINTRO.Location = new System.Drawing.Point(7, 44);
			this.radioNOINTRO.Name = "radioNOINTRO";
			this.radioNOINTRO.Size = new System.Drawing.Size(172, 17);
			this.radioNOINTRO.TabIndex = 1;
			this.radioNOINTRO.TabStop = true;
			this.radioNOINTRO.Text = "NOINTRO (standard DAT only)";
			this.radioNOINTRO.UseVisualStyleBackColor = true;
			// 
			// DATConverter
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(469, 391);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.textBoxOutputFolder);
			this.Controls.Add(this.buttonStartProcessing);
			this.Controls.Add(this.buttonRemove);
			this.Controls.Add(this.buttonAddFiles);
			this.Controls.Add(this.listBoxFiles);
			this.Controls.Add(this.groupImportTypes);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBoxSystemSelect);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "DATConverter";
			this.Text = "DATConverter";
			this.groupImportTypes.ResumeLayout(false);
			this.groupImportTypes.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox comboBoxSystemSelect;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupImportTypes;
		private System.Windows.Forms.RadioButton radioTOSEC;
		private System.Windows.Forms.ListBox listBoxFiles;
		private System.Windows.Forms.Button buttonAddFiles;
		private System.Windows.Forms.Button buttonRemove;
		private System.Windows.Forms.Button buttonStartProcessing;
		private System.Windows.Forms.TextBox textBoxOutputFolder;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.RadioButton radioNOINTRO;
	}
}