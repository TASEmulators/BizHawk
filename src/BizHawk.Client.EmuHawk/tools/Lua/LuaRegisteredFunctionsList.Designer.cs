namespace BizHawk.Client.EmuHawk
{
	partial class LuaRegisteredFunctionsList
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LuaRegisteredFunctionsList));
            this.FunctionView = new System.Windows.Forms.ListView();
            this.FunctionsEvent = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.FunctionsName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.FunctionsGUID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.OK = new System.Windows.Forms.Button();
            this.CallButton = new System.Windows.Forms.Button();
            this.RemoveButton = new System.Windows.Forms.Button();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.callToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.removeToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RemoveAllBtn = new System.Windows.Forms.Button();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // FunctionView
            // 
            resources.ApplyResources(this.FunctionView, "FunctionView");
            this.FunctionView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.FunctionsEvent,
            this.FunctionsName,
            this.FunctionsGUID});
            this.FunctionView.FullRowSelect = true;
            this.FunctionView.GridLines = true;
            this.FunctionView.HideSelection = false;
            this.FunctionView.Name = "FunctionView";
            this.FunctionView.UseCompatibleStateImageBehavior = false;
            this.FunctionView.View = System.Windows.Forms.View.Details;
            this.FunctionView.SelectedIndexChanged += new System.EventHandler(this.FunctionView_SelectedIndexChanged);
            this.FunctionView.DoubleClick += new System.EventHandler(this.FunctionView_DoubleClick);
            this.FunctionView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FunctionView_KeyDown);
            // 
            // FunctionsEvent
            // 
            resources.ApplyResources(this.FunctionsEvent, "FunctionsEvent");
            // 
            // FunctionsName
            // 
            resources.ApplyResources(this.FunctionsName, "FunctionsName");
            // 
            // FunctionsGUID
            // 
            resources.ApplyResources(this.FunctionsGUID, "FunctionsGUID");
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // CallButton
            // 
            resources.ApplyResources(this.CallButton, "CallButton");
            this.CallButton.Name = "CallButton";
            this.CallButton.UseVisualStyleBackColor = true;
            this.CallButton.Click += new System.EventHandler(this.CallButton_Click);
            // 
            // RemoveButton
            // 
            resources.ApplyResources(this.RemoveButton, "RemoveButton");
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.UseVisualStyleBackColor = true;
            this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // contextMenuStrip1
            // 
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.callToolStripMenuItem,
            this.removeToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            // 
            // callToolStripMenuItem
            // 
            resources.ApplyResources(this.callToolStripMenuItem, "callToolStripMenuItem");
            this.callToolStripMenuItem.Click += new System.EventHandler(this.CallButton_Click);
            // 
            // removeToolStripMenuItem
            // 
            resources.ApplyResources(this.removeToolStripMenuItem, "removeToolStripMenuItem");
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // RemoveAllBtn
            // 
            resources.ApplyResources(this.RemoveAllBtn, "RemoveAllBtn");
            this.RemoveAllBtn.Name = "RemoveAllBtn";
            this.RemoveAllBtn.UseVisualStyleBackColor = true;
            this.RemoveAllBtn.Click += new System.EventHandler(this.RemoveAllBtn_Click);
            // 
            // LuaRegisteredFunctionsList
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.OK;
            this.Controls.Add(this.RemoveAllBtn);
            this.Controls.Add(this.RemoveButton);
            this.Controls.Add(this.CallButton);
            this.Controls.Add(this.FunctionView);
            this.Controls.Add(this.OK);
            this.Name = "LuaRegisteredFunctionsList";
            this.Load += new System.EventHandler(this.LuaRegisteredFunctionsList_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView FunctionView;
		private System.Windows.Forms.ColumnHeader FunctionsName;
		private System.Windows.Forms.ColumnHeader FunctionsEvent;
		private System.Windows.Forms.ColumnHeader FunctionsGUID;
		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button CallButton;
		private System.Windows.Forms.Button RemoveButton;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx callToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx removeToolStripMenuItem;
		private System.Windows.Forms.Button RemoveAllBtn;
	}
}