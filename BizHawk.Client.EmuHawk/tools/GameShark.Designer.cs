namespace BizHawk.Client.EmuHawk
{
	partial class GameShark
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameShark));
			this.mnuGameShark = new System.Windows.Forms.MenuStrip();
			this.btnClear = new System.Windows.Forms.Button();
			this.lblCheat = new System.Windows.Forms.Label();
			this.txtCheat = new System.Windows.Forms.TextBox();
			this.btnGo = new System.Windows.Forms.Button();
			this.lblDescription = new System.Windows.Forms.Label();
			this.txtDescription = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// mnuGameShark
			// 
			this.mnuGameShark.Location = new System.Drawing.Point(0, 0);
			this.mnuGameShark.Name = "mnuGameShark";
			this.mnuGameShark.Size = new System.Drawing.Size(284, 24);
			this.mnuGameShark.TabIndex = 0;
			// 
			// btnClear
			// 
			this.btnClear.Location = new System.Drawing.Point(141, 132);
			this.btnClear.Name = "btnClear";
			this.btnClear.Size = new System.Drawing.Size(75, 23);
			this.btnClear.TabIndex = 16;
			this.btnClear.Text = "Clear";
			this.btnClear.UseVisualStyleBackColor = true;
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// lblCheat
			// 
			this.lblCheat.AutoSize = true;
			this.lblCheat.Location = new System.Drawing.Point(147, 91);
			this.lblCheat.Name = "lblCheat";
			this.lblCheat.Size = new System.Drawing.Size(63, 13);
			this.lblCheat.TabIndex = 11;
			this.lblCheat.Text = "Cheat Code";
			// 
			// txtCheat
			// 
			this.txtCheat.Location = new System.Drawing.Point(128, 106);
			this.txtCheat.Name = "txtCheat";
			this.txtCheat.Size = new System.Drawing.Size(100, 20);
			this.txtCheat.TabIndex = 10;
			// 
			// btnGo
			// 
			this.btnGo.Location = new System.Drawing.Point(35, 131);
			this.btnGo.Name = "btnGo";
			this.btnGo.Size = new System.Drawing.Size(75, 23);
			this.btnGo.TabIndex = 9;
			this.btnGo.Text = "Convert";
			this.btnGo.UseVisualStyleBackColor = true;
			this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
			// 
			// lblDescription
			// 
			this.lblDescription.AutoSize = true;
			this.lblDescription.Location = new System.Drawing.Point(42, 90);
			this.lblDescription.Name = "lblDescription";
			this.lblDescription.Size = new System.Drawing.Size(60, 13);
			this.lblDescription.TabIndex = 17;
			this.lblDescription.Text = "Description";
			// 
			// txtDescription
			// 
			this.txtDescription.Location = new System.Drawing.Point(22, 106);
			this.txtDescription.Name = "txtDescription";
			this.txtDescription.Size = new System.Drawing.Size(100, 20);
			this.txtDescription.TabIndex = 18;
			// 
			// GameShark
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.txtDescription);
			this.Controls.Add(this.lblDescription);
			this.Controls.Add(this.btnClear);
			this.Controls.Add(this.lblCheat);
			this.Controls.Add(this.txtCheat);
			this.Controls.Add(this.btnGo);
			this.Controls.Add(this.mnuGameShark);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.mnuGameShark;
			this.MaximizeBox = false;
			this.Name = "GameShark";
			this.Text = "GameShark Converter";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip mnuGameShark;
		internal System.Windows.Forms.Button btnClear;
		internal System.Windows.Forms.Label lblCheat;
		internal System.Windows.Forms.TextBox txtCheat;
		internal System.Windows.Forms.Button btnGo;
		private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.TextBox txtDescription;
	}
}