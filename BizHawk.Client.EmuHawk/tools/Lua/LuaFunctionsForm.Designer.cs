namespace BizHawk.Client.EmuHawk
{
	partial class LuaFunctionsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LuaFunctionsForm));
			this.OK = new System.Windows.Forms.Button();
			this.directoryEntry1 = new System.DirectoryServices.DirectoryEntry();
			this.FilterBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.ToWikiMarkupButton = new System.Windows.Forms.Button();
			this.FunctionView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.LibraryReturn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LibraryHead = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LibraryName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LibraryParameters = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LibraryDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(647, 309);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 10;
			this.OK.Text = "&OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.Ok_Click);
			// 
			// FilterBox
			// 
			this.FilterBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.FilterBox.Location = new System.Drawing.Point(12, 311);
			this.FilterBox.Name = "FilterBox";
			this.FilterBox.Size = new System.Drawing.Size(159, 20);
			this.FilterBox.TabIndex = 1;
			this.FilterBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FilterBox_KeyUp);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(173, 314);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(29, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Filter";
			// 
			// ToWikiMarkupButton
			// 
			this.ToWikiMarkupButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ToWikiMarkupButton.Location = new System.Drawing.Point(493, 309);
			this.ToWikiMarkupButton.Name = "ToWikiMarkupButton";
			this.ToWikiMarkupButton.Size = new System.Drawing.Size(138, 23);
			this.ToWikiMarkupButton.TabIndex = 11;
			this.ToWikiMarkupButton.Text = "Wiki markup to Clipboard";
			this.ToWikiMarkupButton.UseVisualStyleBackColor = true;
			this.ToWikiMarkupButton.Click += new System.EventHandler(this.ToWikiMarkupButton_Click);
			// 
			// FunctionView
			// 
			this.FunctionView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FunctionView.BlazingFast = false;
			this.FunctionView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.LibraryReturn,
            this.LibraryHead,
            this.LibraryName,
            this.LibraryParameters,
            this.LibraryDescription});
			this.FunctionView.FullRowSelect = true;
			this.FunctionView.GridLines = true;
			this.FunctionView.ItemCount = 0;
			this.FunctionView.Location = new System.Drawing.Point(12, 12);
			this.FunctionView.Name = "FunctionView";
			this.FunctionView.SelectAllInProgress = false;
			this.FunctionView.selectedItem = -1;
			this.FunctionView.Size = new System.Drawing.Size(710, 291);
			this.FunctionView.TabIndex = 1;
			this.FunctionView.UseCompatibleStateImageBehavior = false;
			this.FunctionView.View = System.Windows.Forms.View.Details;
			this.FunctionView.VirtualMode = true;
			this.FunctionView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.FunctionView_ColumnClick);
			this.FunctionView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FunctionView_KeyDown);
			// 
			// LibraryReturn
			// 
			this.LibraryReturn.Text = "Return";
			// 
			// LibraryHead
			// 
			this.LibraryHead.Text = "Library";
			this.LibraryHead.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.LibraryHead.Width = 68;
			// 
			// LibraryName
			// 
			this.LibraryName.Text = "Name";
			this.LibraryName.Width = 131;
			// 
			// LibraryParameters
			// 
			this.LibraryParameters.Text = "Parameters";
			this.LibraryParameters.Width = 170;
			// 
			// LibraryDescription
			// 
			this.LibraryDescription.Text = "Description";
			this.LibraryDescription.Width = 296;
			// 
			// LuaFunctionsForm
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(730, 340);
			this.Controls.Add(this.ToWikiMarkupButton);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.FilterBox);
			this.Controls.Add(this.FunctionView);
			this.Controls.Add(this.OK);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(200, 50);
			this.Name = "LuaFunctionsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Lua Functions";
			this.Load += new System.EventHandler(this.LuaFunctionList_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.DirectoryServices.DirectoryEntry directoryEntry1;
		private VirtualListView FunctionView;
		private System.Windows.Forms.ColumnHeader LibraryHead;
		private System.Windows.Forms.ColumnHeader LibraryReturn;
		private System.Windows.Forms.ColumnHeader LibraryName;
		private System.Windows.Forms.ColumnHeader LibraryParameters;
		private System.Windows.Forms.ColumnHeader LibraryDescription;
		private System.Windows.Forms.TextBox FilterBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button ToWikiMarkupButton;
	}
}