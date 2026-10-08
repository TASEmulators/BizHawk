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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LuaFunctionsForm));
            this.OK = new System.Windows.Forms.Button();
            this.FilterBox = new System.Windows.Forms.TextBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ToWikiMarkupButton = new System.Windows.Forms.Button();
            this.FunctionView = new System.Windows.Forms.ListView();
            this.LibraryReturn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.LibraryHead = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.LibraryName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.LibraryParameters = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.LibraryDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CopyMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CopyMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // FilterBox
            // 
            resources.ApplyResources(this.FilterBox, "FilterBox");
            this.FilterBox.Name = "FilterBox";
            this.FilterBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FilterBox_KeyUp);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // ToWikiMarkupButton
            // 
            resources.ApplyResources(this.ToWikiMarkupButton, "ToWikiMarkupButton");
            this.ToWikiMarkupButton.Name = "ToWikiMarkupButton";
            this.ToWikiMarkupButton.UseVisualStyleBackColor = true;
            this.ToWikiMarkupButton.Click += new System.EventHandler(this.ToWikiMarkupButton_Click);
            // 
            // FunctionView
            // 
            resources.ApplyResources(this.FunctionView, "FunctionView");
            this.FunctionView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.LibraryReturn,
            this.LibraryHead,
            this.LibraryName,
            this.LibraryParameters,
            this.LibraryDescription});
            this.FunctionView.ContextMenuStrip = this.CopyMenu;
            this.FunctionView.FullRowSelect = true;
            this.FunctionView.GridLines = true;
            this.FunctionView.HideSelection = false;
            this.FunctionView.Name = "FunctionView";
            this.FunctionView.UseCompatibleStateImageBehavior = false;
            this.FunctionView.View = System.Windows.Forms.View.Details;
            this.FunctionView.VirtualMode = true;
            this.FunctionView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.FunctionView_ColumnClick);
            this.FunctionView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FunctionView_KeyDown);
            // 
            // LibraryReturn
            // 
            resources.ApplyResources(this.LibraryReturn, "LibraryReturn");
            // 
            // LibraryHead
            // 
            resources.ApplyResources(this.LibraryHead, "LibraryHead");
            // 
            // LibraryName
            // 
            resources.ApplyResources(this.LibraryName, "LibraryName");
            // 
            // LibraryParameters
            // 
            resources.ApplyResources(this.LibraryParameters, "LibraryParameters");
            // 
            // LibraryDescription
            // 
            resources.ApplyResources(this.LibraryDescription, "LibraryDescription");
            // 
            // CopyMenu
            // 
            resources.ApplyResources(this.CopyMenu, "CopyMenu");
            this.CopyMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyMenuItem});
            this.CopyMenu.Name = "CopyMenu";
            // 
            // CopyMenuItem
            // 
            resources.ApplyResources(this.CopyMenuItem, "CopyMenuItem");
            this.CopyMenuItem.Name = "CopyMenuItem";
            this.CopyMenuItem.Click += new System.EventHandler(this.FunctionView_Copy);
            // 
            // LuaFunctionsForm
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ToWikiMarkupButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.FilterBox);
            this.Controls.Add(this.FunctionView);
            this.Controls.Add(this.OK);
            this.Name = "LuaFunctionsForm";
            this.Load += new System.EventHandler(this.LuaFunctionList_Load);
            this.CopyMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.ListView FunctionView;
		private System.Windows.Forms.ColumnHeader LibraryHead;
		private System.Windows.Forms.ColumnHeader LibraryReturn;
		private System.Windows.Forms.ColumnHeader LibraryName;
		private System.Windows.Forms.ColumnHeader LibraryParameters;
		private System.Windows.Forms.ColumnHeader LibraryDescription;
		private System.Windows.Forms.TextBox FilterBox;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.Button ToWikiMarkupButton;
		private System.Windows.Forms.ContextMenuStrip CopyMenu;
		private System.Windows.Forms.ToolStripMenuItem CopyMenuItem;
	}
}