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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SNESOptions));
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbDoubleSize = new System.Windows.Forms.CheckBox();
            this.lblDoubleSize = new BizHawk.WinForms.Controls.LocSzLabelEx();
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
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.Name = "btnOk";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // cbDoubleSize
            // 
            resources.ApplyResources(this.cbDoubleSize, "cbDoubleSize");
            this.cbDoubleSize.Name = "cbDoubleSize";
            this.cbDoubleSize.UseVisualStyleBackColor = true;
            this.cbDoubleSize.CheckedChanged += new System.EventHandler(this.CbDoubleSize_CheckedChanged);
            // 
            // lblDoubleSize
            // 
            resources.ApplyResources(this.lblDoubleSize, "lblDoubleSize");
            this.lblDoubleSize.Name = "lblDoubleSize";
            // 
            // radioButton1
            // 
            resources.ApplyResources(this.radioButton1, "radioButton1");
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.TabStop = true;
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // cbCropSGBFrame
            // 
            resources.ApplyResources(this.cbCropSGBFrame, "cbCropSGBFrame");
            this.cbCropSGBFrame.Name = "cbCropSGBFrame";
            this.cbCropSGBFrame.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.Bg4Checkbox);
            this.groupBox1.Controls.Add(this.Bg3Checkbox);
            this.groupBox1.Controls.Add(this.Bg2Checkbox);
            this.groupBox1.Controls.Add(this.Bg1Checkbox);
            this.groupBox1.Controls.Add(this.Obj4Checkbox);
            this.groupBox1.Controls.Add(this.Obj3Checkbox);
            this.groupBox1.Controls.Add(this.Obj2Checkbox);
            this.groupBox1.Controls.Add(this.Obj1Checkbox);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // Bg4Checkbox
            // 
            resources.ApplyResources(this.Bg4Checkbox, "Bg4Checkbox");
            this.Bg4Checkbox.Name = "Bg4Checkbox";
            this.Bg4Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg3Checkbox
            // 
            resources.ApplyResources(this.Bg3Checkbox, "Bg3Checkbox");
            this.Bg3Checkbox.Name = "Bg3Checkbox";
            this.Bg3Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg2Checkbox
            // 
            resources.ApplyResources(this.Bg2Checkbox, "Bg2Checkbox");
            this.Bg2Checkbox.Name = "Bg2Checkbox";
            this.Bg2Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg1Checkbox
            // 
            resources.ApplyResources(this.Bg1Checkbox, "Bg1Checkbox");
            this.Bg1Checkbox.Name = "Bg1Checkbox";
            this.Bg1Checkbox.UseVisualStyleBackColor = true;
            // 
            // Obj4Checkbox
            // 
            resources.ApplyResources(this.Obj4Checkbox, "Obj4Checkbox");
            this.Obj4Checkbox.Name = "Obj4Checkbox";
            this.Obj4Checkbox.UseVisualStyleBackColor = true;
            // 
            // Obj3Checkbox
            // 
            resources.ApplyResources(this.Obj3Checkbox, "Obj3Checkbox");
            this.Obj3Checkbox.Name = "Obj3Checkbox";
            this.Obj3Checkbox.UseVisualStyleBackColor = true;
            // 
            // Obj2Checkbox
            // 
            resources.ApplyResources(this.Obj2Checkbox, "Obj2Checkbox");
            this.Obj2Checkbox.Name = "Obj2Checkbox";
            this.Obj2Checkbox.UseVisualStyleBackColor = true;
            // 
            // Obj1Checkbox
            // 
            resources.ApplyResources(this.Obj1Checkbox, "Obj1Checkbox");
            this.Obj1Checkbox.Name = "Obj1Checkbox";
            this.Obj1Checkbox.UseVisualStyleBackColor = true;
            // 
            // cbRandomizedInitialState
            // 
            resources.ApplyResources(this.cbRandomizedInitialState, "cbRandomizedInitialState");
            this.cbRandomizedInitialState.Name = "cbRandomizedInitialState";
            this.cbRandomizedInitialState.UseVisualStyleBackColor = true;
            // 
            // SNESOptions
            // 
            this.AcceptButton = this.btnOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
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
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.CheckBox cbDoubleSize;
		private BizHawk.WinForms.Controls.LocSzLabelEx lblDoubleSize;
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