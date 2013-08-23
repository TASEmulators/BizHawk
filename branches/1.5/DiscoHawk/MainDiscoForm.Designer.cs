namespace BizHawk
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
			this.lblMagicDragArea.SuspendLayout();
			this.lblMp3ExtractMagicArea.SuspendLayout();
			this.SuspendLayout();
			// 
			// ExitButton
			// 
			this.ExitButton.Location = new System.Drawing.Point(146, 245);
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
			this.lblMagicDragArea.Location = new System.Drawing.Point(21, 12);
			this.lblMagicDragArea.Name = "lblMagicDragArea";
			this.lblMagicDragArea.Size = new System.Drawing.Size(200, 100);
			this.lblMagicDragArea.TabIndex = 1;
			this.lblMagicDragArea.DragDrop += new System.Windows.Forms.DragEventHandler(this.lblMagicDragArea_DragDrop);
			this.lblMagicDragArea.DragEnter += new System.Windows.Forms.DragEventHandler(this.lblMagicDragArea_DragEnter);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(47, 10);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(106, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Drag here for MAGIC";
			// 
			// lblMp3ExtractMagicArea
			// 
			this.lblMp3ExtractMagicArea.AllowDrop = true;
			this.lblMp3ExtractMagicArea.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblMp3ExtractMagicArea.Controls.Add(this.label2);
			this.lblMp3ExtractMagicArea.Location = new System.Drawing.Point(21, 127);
			this.lblMp3ExtractMagicArea.Name = "lblMp3ExtractMagicArea";
			this.lblMp3ExtractMagicArea.Size = new System.Drawing.Size(200, 100);
			this.lblMp3ExtractMagicArea.TabIndex = 2;
			this.lblMp3ExtractMagicArea.DragDrop += new System.Windows.Forms.DragEventHandler(this.lblMp3ExtractMagicArea_DragDrop);
			this.lblMp3ExtractMagicArea.DragEnter += new System.Windows.Forms.DragEventHandler(this.lblMagicDragArea_DragEnter);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(17, 25);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(151, 39);
			this.label2.TabIndex = 0;
			this.label2.Text = "Drag cue here to extract audio\ntracks to MP3 for listening\npleasure!";
			// 
			// btnAbout
			// 
			this.btnAbout.Location = new System.Drawing.Point(21, 245);
			this.btnAbout.Name = "btnAbout";
			this.btnAbout.Size = new System.Drawing.Size(75, 23);
			this.btnAbout.TabIndex = 3;
			this.btnAbout.Text = "&About";
			this.btnAbout.UseVisualStyleBackColor = true;
			this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
			// 
			// MainDiscoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(247, 285);
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
			this.lblMagicDragArea.PerformLayout();
			this.lblMp3ExtractMagicArea.ResumeLayout(false);
			this.lblMp3ExtractMagicArea.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button ExitButton;
		private System.Windows.Forms.Panel lblMagicDragArea;
		private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel lblMp3ExtractMagicArea;
        private System.Windows.Forms.Label label2;
				private System.Windows.Forms.Button btnAbout;
	}
}