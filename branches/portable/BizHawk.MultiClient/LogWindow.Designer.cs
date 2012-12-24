namespace BizHawk.MultiClient
{
	partial class LogWindow
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
			this.btnClose = new System.Windows.Forms.Button();
			this.btnClear = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.buttonCopy = new System.Windows.Forms.Button();
			this.virtualListView1 = new BizHawk.VirtualListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.buttonCopyAll = new System.Windows.Forms.Button();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnClose
			// 
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnClose.Location = new System.Drawing.Point(597, 3);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 2;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			// 
			// btnClear
			// 
			this.btnClear.Location = new System.Drawing.Point(3, 3);
			this.btnClear.Name = "btnClear";
			this.btnClear.Size = new System.Drawing.Size(75, 23);
			this.btnClear.TabIndex = 1;
			this.btnClear.Text = "&Clear";
			this.btnClear.UseVisualStyleBackColor = true;
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.AutoSize = true;
			this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel1.ColumnCount = 5;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.btnClear, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.btnClose, 4, 0);
			this.tableLayoutPanel1.Controls.Add(this.buttonCopy, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.buttonCopyAll, 2, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 368);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(675, 29);
			this.tableLayoutPanel1.TabIndex = 5;
			// 
			// buttonCopy
			// 
			this.buttonCopy.Location = new System.Drawing.Point(84, 3);
			this.buttonCopy.Name = "buttonCopy";
			this.buttonCopy.Size = new System.Drawing.Size(75, 23);
			this.buttonCopy.TabIndex = 3;
			this.buttonCopy.Text = "Copy Sel.";
			this.buttonCopy.UseVisualStyleBackColor = true;
			this.buttonCopy.Click += new System.EventHandler(this.buttonCopy_Click);
			// 
			// virtualListView1
			// 
			this.virtualListView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
			this.virtualListView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.virtualListView1.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.virtualListView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.virtualListView1.ItemCount = 0;
			this.virtualListView1.Location = new System.Drawing.Point(0, 0);
			this.virtualListView1.Name = "virtualListView1";
			this.virtualListView1.selectedItem = -1;
			this.virtualListView1.Size = new System.Drawing.Size(675, 368);
			this.virtualListView1.TabIndex = 8;
			this.virtualListView1.UseCompatibleStateImageBehavior = false;
			this.virtualListView1.View = System.Windows.Forms.View.Details;
			this.virtualListView1.VirtualMode = true;
			this.virtualListView1.QueryItemText += new BizHawk.QueryItemTextHandler(this.virtualListView1_QueryItemText);
			this.virtualListView1.ClientSizeChanged += new System.EventHandler(this.virtualListView1_ClientSizeChanged);
			// 
			// buttonCopyAll
			// 
			this.buttonCopyAll.Location = new System.Drawing.Point(165, 3);
			this.buttonCopyAll.Name = "buttonCopyAll";
			this.buttonCopyAll.Size = new System.Drawing.Size(75, 23);
			this.buttonCopyAll.TabIndex = 4;
			this.buttonCopyAll.Text = "Copy All";
			this.buttonCopyAll.UseVisualStyleBackColor = true;
			this.buttonCopyAll.Click += new System.EventHandler(this.buttonCopyAll_Click);
			// 
			// LogWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(675, 397);
			this.Controls.Add(this.virtualListView1);
			this.Controls.Add(this.tableLayoutPanel1);
			this.MinimumSize = new System.Drawing.Size(171, 97);
			this.Name = "LogWindow";
			this.ShowIcon = false;
			this.Text = "Log Window";
			this.Load += new System.EventHandler(this.LogWindow_Load);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnClear;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private VirtualListView virtualListView1;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.Button buttonCopy;
		private System.Windows.Forms.Button buttonCopyAll;
	}
}