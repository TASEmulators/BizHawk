namespace BizHawk.MultiClient
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
			this.callToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// FunctionView
			// 
			this.FunctionView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FunctionView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.FunctionsEvent,
            this.FunctionsName,
            this.FunctionsGUID});
			this.FunctionView.FullRowSelect = true;
			this.FunctionView.GridLines = true;
			this.FunctionView.Location = new System.Drawing.Point(12, 12);
			this.FunctionView.Name = "FunctionView";
			this.FunctionView.Size = new System.Drawing.Size(498, 266);
			this.FunctionView.TabIndex = 3;
			this.FunctionView.UseCompatibleStateImageBehavior = false;
			this.FunctionView.View = System.Windows.Forms.View.Details;
			this.FunctionView.SelectedIndexChanged += new System.EventHandler(this.FunctionView_SelectedIndexChanged);
			// 
			// FunctionsEvent
			// 
			this.FunctionsEvent.Text = "Event";
			this.FunctionsEvent.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.FunctionsEvent.Width = 111;
			// 
			// FunctionsName
			// 
			this.FunctionsName.Text = "Name";
			this.FunctionsName.Width = 99;
			// 
			// FunctionsGUID
			// 
			this.FunctionsGUID.Text = "Guid";
			this.FunctionsGUID.Width = 284;
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.OK.Location = new System.Drawing.Point(435, 284);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 2;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// CallButton
			// 
			this.CallButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.CallButton.Enabled = false;
			this.CallButton.Location = new System.Drawing.Point(12, 284);
			this.CallButton.Name = "CallButton";
			this.CallButton.Size = new System.Drawing.Size(75, 23);
			this.CallButton.TabIndex = 4;
			this.CallButton.Text = "&Call";
			this.CallButton.UseVisualStyleBackColor = true;
			this.CallButton.Click += new System.EventHandler(this.CallButton_Click);
			// 
			// RemoveButton
			// 
			this.RemoveButton.Enabled = false;
			this.RemoveButton.Location = new System.Drawing.Point(93, 284);
			this.RemoveButton.Name = "RemoveButton";
			this.RemoveButton.Size = new System.Drawing.Size(75, 23);
			this.RemoveButton.TabIndex = 5;
			this.RemoveButton.Text = "&Remove";
			this.RemoveButton.UseVisualStyleBackColor = true;
			this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.callToolStripMenuItem,
            this.removeToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(118, 48);
			// 
			// callToolStripMenuItem
			// 
			this.callToolStripMenuItem.Name = "callToolStripMenuItem";
			this.callToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.callToolStripMenuItem.Text = "&Call";
			this.callToolStripMenuItem.Click += new System.EventHandler(this.CallButton_Click);
			// 
			// removeToolStripMenuItem
			// 
			this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
			this.removeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.removeToolStripMenuItem.Text = "&Remove";
			this.removeToolStripMenuItem.Click += new System.EventHandler(this.RemoveButton_Click);
			// 
			// LuaRegisteredFunctionsList
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.OK;
			this.ClientSize = new System.Drawing.Size(521, 319);
			this.Controls.Add(this.RemoveButton);
			this.Controls.Add(this.CallButton);
			this.Controls.Add(this.FunctionView);
			this.Controls.Add(this.OK);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(200, 50);
			this.Name = "LuaRegisteredFunctionsList";
			this.Text = "Active Registered Functions";
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
		private System.Windows.Forms.ToolStripMenuItem callToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
	}
}