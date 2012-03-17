namespace BizHawk.MultiClient
{
	partial class LuaFunctionList
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LuaFunctionList));
			this.OK = new System.Windows.Forms.Button();
			this.directoryEntry1 = new System.DirectoryServices.DirectoryEntry();
			this.FunctionBox = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(174, 287);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// FunctionBox
			// 
			this.FunctionBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FunctionBox.Location = new System.Drawing.Point(12, 12);
			this.FunctionBox.Name = "FunctionBox";
			this.FunctionBox.Size = new System.Drawing.Size(237, 269);
			this.FunctionBox.TabIndex = 1;
			this.FunctionBox.Text = "";
			// 
			// LuaFunctionList
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(261, 322);
			this.Controls.Add(this.FunctionBox);
			this.Controls.Add(this.OK);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "LuaFunctionList";
			this.Text = "Lua Functions";
			this.Load += new System.EventHandler(this.LuaFunctionList_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.DirectoryServices.DirectoryEntry directoryEntry1;
		private System.Windows.Forms.RichTextBox FunctionBox;
	}
}