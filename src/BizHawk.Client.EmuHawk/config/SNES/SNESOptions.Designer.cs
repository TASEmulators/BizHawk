namespace BizHawk.Client.EmuHawk
{
	partial class SNESOptions
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



		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.cbDoubleSize = new System.Windows.Forms.CheckBox();
			this.lblDoubleSize = new System.Windows.Forms.Label();
			this.radioButton1 = new System.Windows.Forms.RadioButton();
			this.cbCropSGBFrame = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.Bg4Checkbox = new System.Windows.Forms.CheckBox();
			this.Bg3Checkbox = new System.Windows.Forms.CheckBox();
			this.Bg2Checkbox = new System.Windows.Forms.CheckBox();
			this.Bg1Checkbox = new System.Windows.Forms.CheckBox();
			this.Obj4Checkbox = new System.Windows.Forms.CheckBox();
			this.Obj3Checkbox = new System.Windows.Forms.CheckBox();
			this.Obj2Checkbox = new System.Windows.Forms.CheckBox();
			this.Obj1Checkbox = new System.Windows.Forms.CheckBox();
			this.cbRandomizedInitialState = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(136, 303);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 0;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(217, 303);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
			// 
			// cbDoubleSize
			// 
			this.cbDoubleSize.AutoSize = true;
			this.cbDoubleSize.Location = new System.Drawing.Point(18, 20);
			this.cbDoubleSize.Name = "cbDoubleSize";
			this.cbDoubleSize.Size = new System.Drawing.Size(178, 17);
			this.cbDoubleSize.TabIndex = 6;
			this.cbDoubleSize.Text = "Always Double-Size Framebuffer";
			this.cbDoubleSize.UseVisualStyleBackColor = true;
			this.cbDoubleSize.CheckedChanged += new System.EventHandler(this.CbDoubleSize_CheckedChanged);
			// 
			// lblDoubleSize
			// 
			this.lblDoubleSize.Location = new System.Drawing.Point(36, 41);
			this.lblDoubleSize.Name = "lblDoubleSize";
			this.lblDoubleSize.Size = new System.Drawing.Size(254, 57);
			this.lblDoubleSize.TabIndex = 7;
			this.lblDoubleSize.Text = "Some games are changing the resolution constantly (e.g. SD3) so this option can f" +
    "orce the SNES output to stay double-size always. NOTE: The Accuracy core runs as" +
    " if this is selected.\r\n";
			// 
			// radioButton1
			// 
			this.radioButton1.AutoSize = true;
			this.radioButton1.Location = new System.Drawing.Point(37, 46);
			this.radioButton1.Name = "radioButton1";
			this.radioButton1.Size = new System.Drawing.Size(202, 17);
			this.radioButton1.TabIndex = 9;
			this.radioButton1.TabStop = true;
			this.radioButton1.Text = "Performance (only for casual gaming!)";
			this.radioButton1.UseVisualStyleBackColor = true;
			// 
			// cbCropSGBFrame
			// 
			this.cbCropSGBFrame.AutoSize = true;
			this.cbCropSGBFrame.Location = new System.Drawing.Point(15, 110);
			this.cbCropSGBFrame.Name = "cbCropSGBFrame";
			this.cbCropSGBFrame.Size = new System.Drawing.Size(105, 17);
			this.cbCropSGBFrame.TabIndex = 10;
			this.cbCropSGBFrame.Text = "Crop SGB Frame";
			this.cbCropSGBFrame.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.Bg4Checkbox);
			this.groupBox1.Controls.Add(this.Bg3Checkbox);
			this.groupBox1.Controls.Add(this.Bg2Checkbox);
			this.groupBox1.Controls.Add(this.Bg1Checkbox);
			this.groupBox1.Controls.Add(this.Obj4Checkbox);
			this.groupBox1.Controls.Add(this.Obj3Checkbox);
			this.groupBox1.Controls.Add(this.Obj2Checkbox);
			this.groupBox1.Controls.Add(this.Obj1Checkbox);
			this.groupBox1.Location = new System.Drawing.Point(18, 165);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(274, 132);
			this.groupBox1.TabIndex = 11;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Display";
			// 
			// Bg4Checkbox
			// 
			this.Bg4Checkbox.AutoSize = true;
			this.Bg4Checkbox.Location = new System.Drawing.Point(150, 98);
			this.Bg4Checkbox.Name = "Bg4Checkbox";
			this.Bg4Checkbox.Size = new System.Drawing.Size(50, 17);
			this.Bg4Checkbox.TabIndex = 7;
			this.Bg4Checkbox.Text = "BG 4";
			this.Bg4Checkbox.UseVisualStyleBackColor = true;
			// 
			// Bg3Checkbox
			// 
			this.Bg3Checkbox.AutoSize = true;
			this.Bg3Checkbox.Location = new System.Drawing.Point(150, 75);
			this.Bg3Checkbox.Name = "Bg3Checkbox";
			this.Bg3Checkbox.Size = new System.Drawing.Size(50, 17);
			this.Bg3Checkbox.TabIndex = 6;
			this.Bg3Checkbox.Text = "BG 3";
			this.Bg3Checkbox.UseVisualStyleBackColor = true;
			// 
			// Bg2Checkbox
			// 
			this.Bg2Checkbox.AutoSize = true;
			this.Bg2Checkbox.Location = new System.Drawing.Point(150, 52);
			this.Bg2Checkbox.Name = "Bg2Checkbox";
			this.Bg2Checkbox.Size = new System.Drawing.Size(50, 17);
			this.Bg2Checkbox.TabIndex = 5;
			this.Bg2Checkbox.Text = "BG 2";
			this.Bg2Checkbox.UseVisualStyleBackColor = true;
			// 
			// Bg1Checkbox
			// 
			this.Bg1Checkbox.AutoSize = true;
			this.Bg1Checkbox.Location = new System.Drawing.Point(150, 29);
			this.Bg1Checkbox.Name = "Bg1Checkbox";
			this.Bg1Checkbox.Size = new System.Drawing.Size(50, 17);
			this.Bg1Checkbox.TabIndex = 4;
			this.Bg1Checkbox.Text = "BG 1";
			this.Bg1Checkbox.UseVisualStyleBackColor = true;
			// 
			// Obj4Checkbox
			// 
			this.Obj4Checkbox.AutoSize = true;
			this.Obj4Checkbox.Location = new System.Drawing.Point(21, 98);
			this.Obj4Checkbox.Name = "Obj4Checkbox";
			this.Obj4Checkbox.Size = new System.Drawing.Size(55, 17);
			this.Obj4Checkbox.TabIndex = 3;
			this.Obj4Checkbox.Text = "OBJ 4";
			this.Obj4Checkbox.UseVisualStyleBackColor = true;
			// 
			// Obj3Checkbox
			// 
			this.Obj3Checkbox.AutoSize = true;
			this.Obj3Checkbox.Location = new System.Drawing.Point(21, 75);
			this.Obj3Checkbox.Name = "Obj3Checkbox";
			this.Obj3Checkbox.Size = new System.Drawing.Size(55, 17);
			this.Obj3Checkbox.TabIndex = 2;
			this.Obj3Checkbox.Text = "OBJ 3";
			this.Obj3Checkbox.UseVisualStyleBackColor = true;
			// 
			// Obj2Checkbox
			// 
			this.Obj2Checkbox.AutoSize = true;
			this.Obj2Checkbox.Location = new System.Drawing.Point(22, 52);
			this.Obj2Checkbox.Name = "Obj2Checkbox";
			this.Obj2Checkbox.Size = new System.Drawing.Size(55, 17);
			this.Obj2Checkbox.TabIndex = 1;
			this.Obj2Checkbox.Text = "OBJ 2";
			this.Obj2Checkbox.UseVisualStyleBackColor = true;
			// 
			// Obj1Checkbox
			// 
			this.Obj1Checkbox.AutoSize = true;
			this.Obj1Checkbox.Location = new System.Drawing.Point(21, 29);
			this.Obj1Checkbox.Name = "Obj1Checkbox";
			this.Obj1Checkbox.Size = new System.Drawing.Size(55, 17);
			this.Obj1Checkbox.TabIndex = 0;
			this.Obj1Checkbox.Text = "OBJ 1";
			this.Obj1Checkbox.UseVisualStyleBackColor = true;
			// 
			// cbRandomizedInitialState
			// 
			this.cbRandomizedInitialState.AutoSize = true;
			this.cbRandomizedInitialState.Location = new System.Drawing.Point(15, 133);
			this.cbRandomizedInitialState.Name = "cbRandomizedInitialState";
			this.cbRandomizedInitialState.Size = new System.Drawing.Size(140, 17);
			this.cbRandomizedInitialState.TabIndex = 12;
			this.cbRandomizedInitialState.Text = "Randomized Initial State";
			this.cbRandomizedInitialState.UseVisualStyleBackColor = true;
			// 
			// SNESOptions
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(304, 338);
			this.Controls.Add(this.cbRandomizedInitialState);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.cbCropSGBFrame);
			this.Controls.Add(this.lblDoubleSize);
			this.Controls.Add(this.cbDoubleSize);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SNESOptions";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "BSNES Options";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}



		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.CheckBox cbDoubleSize;
		private System.Windows.Forms.Label lblDoubleSize;
		private System.Windows.Forms.RadioButton radioButton1;
		private System.Windows.Forms.CheckBox cbCropSGBFrame;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox Bg4Checkbox;
		private System.Windows.Forms.CheckBox Bg3Checkbox;
		private System.Windows.Forms.CheckBox Bg2Checkbox;
		private System.Windows.Forms.CheckBox Bg1Checkbox;
		private System.Windows.Forms.CheckBox Obj4Checkbox;
		private System.Windows.Forms.CheckBox Obj3Checkbox;
		private System.Windows.Forms.CheckBox Obj2Checkbox;
		private System.Windows.Forms.CheckBox Obj1Checkbox;
		private System.Windows.Forms.CheckBox cbRandomizedInitialState;
	}
}