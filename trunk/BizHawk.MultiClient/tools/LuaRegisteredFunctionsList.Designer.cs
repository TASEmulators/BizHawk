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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LuaRegisteredFunctionsList));
			this.FunctionView = new System.Windows.Forms.ListView();
			this.FunctionsEvent = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FunctionsName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FunctionsGUID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.OK = new System.Windows.Forms.Button();
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
			// LuaRegisteredFunctionsList
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.OK;
			this.ClientSize = new System.Drawing.Size(521, 319);
			this.Controls.Add(this.FunctionView);
			this.Controls.Add(this.OK);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(200, 50);
			this.Name = "LuaRegisteredFunctionsList";
			this.Text = "Actively Registered Functions";
			this.Load += new System.EventHandler(this.LuaRegisteredFunctionsList_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView FunctionView;
		private System.Windows.Forms.ColumnHeader FunctionsName;
		private System.Windows.Forms.ColumnHeader FunctionsEvent;
		private System.Windows.Forms.ColumnHeader FunctionsGUID;
		private System.Windows.Forms.Button OK;
	}
}