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
			this.FunctionView = new System.Windows.Forms.ListView();
			this.LibraryReturn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LibraryHead = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LibraryName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LibraryParameters = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(423, 284);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// FunctionView
			// 
			this.FunctionView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FunctionView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.LibraryReturn,
            this.LibraryHead,
            this.LibraryName,
            this.LibraryParameters});
			this.FunctionView.GridLines = true;
			this.FunctionView.Location = new System.Drawing.Point(12, 12);
			this.FunctionView.Name = "FunctionView";
			this.FunctionView.Size = new System.Drawing.Size(486, 266);
			this.FunctionView.TabIndex = 1;
			this.FunctionView.UseCompatibleStateImageBehavior = false;
			this.FunctionView.View = System.Windows.Forms.View.Details;
			this.FunctionView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.FunctionView_ColumnClick);
			// 
			// LibraryReturn
			// 
			this.LibraryReturn.Text = "Return";
			// 
			// LibraryHead
			// 
			this.LibraryHead.Text = "Library";
			this.LibraryHead.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.LibraryHead.Width = 75;
			// 
			// LibraryName
			// 
			this.LibraryName.Text = "Name";
			this.LibraryName.Width = 135;
			// 
			// LibraryParameters
			// 
			this.LibraryParameters.Text = "Parameters";
			this.LibraryParameters.Width = 210;
			// 
			// LuaFunctionList
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(510, 319);
			this.Controls.Add(this.FunctionView);
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
		private System.Windows.Forms.ListView FunctionView;
		private System.Windows.Forms.ColumnHeader LibraryHead;
		private System.Windows.Forms.ColumnHeader LibraryReturn;
		private System.Windows.Forms.ColumnHeader LibraryName;
		private System.Windows.Forms.ColumnHeader LibraryParameters;
	}
}