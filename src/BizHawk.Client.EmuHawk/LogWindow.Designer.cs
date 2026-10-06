using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogWindow));
            this.btnClose = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonCopy = new System.Windows.Forms.Button();
            this.buttonCopyAll = new System.Windows.Forms.Button();
            this.AddToGameDbBtn = new System.Windows.Forms.Button();
            this.virtualListView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.MenuStrip = new BizHawk.WinForms.Controls.MenuStripEx();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            resources.ApplyResources(this.btnClose, "btnClose");
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Name = "btnClose";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // btnClear
            // 
            resources.ApplyResources(this.btnClear, "btnClear");
            this.btnClear.Name = "btnClear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.btnClear, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnClose, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonCopy, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonCopyAll, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.AddToGameDbBtn, 3, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // buttonCopy
            // 
            resources.ApplyResources(this.buttonCopy, "buttonCopy");
            this.buttonCopy.Name = "buttonCopy";
            this.buttonCopy.UseVisualStyleBackColor = true;
            this.buttonCopy.Click += new System.EventHandler(this.ButtonCopy_Click);
            // 
            // buttonCopyAll
            // 
            resources.ApplyResources(this.buttonCopyAll, "buttonCopyAll");
            this.buttonCopyAll.Name = "buttonCopyAll";
            this.buttonCopyAll.UseVisualStyleBackColor = true;
            this.buttonCopyAll.Click += new System.EventHandler(this.ButtonCopyAll_Click);
            // 
            // AddToGameDbBtn
            // 
            resources.ApplyResources(this.AddToGameDbBtn, "AddToGameDbBtn");
            this.AddToGameDbBtn.Name = "AddToGameDbBtn";
            this.AddToGameDbBtn.UseVisualStyleBackColor = true;
            this.AddToGameDbBtn.Click += new System.EventHandler(this.AddToGameDbBtn_Click);
            // 
            // virtualListView1
            // 
            resources.ApplyResources(this.virtualListView1, "virtualListView1");
            this.virtualListView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.virtualListView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.virtualListView1.HideSelection = false;
            this.virtualListView1.Name = "virtualListView1";
            this.virtualListView1.UseCompatibleStateImageBehavior = false;
            this.virtualListView1.View = System.Windows.Forms.View.Details;
            this.virtualListView1.VirtualMode = true;
            this.virtualListView1.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.ListView_QueryItemText);
            this.virtualListView1.ClientSizeChanged += new System.EventHandler(this.ListView_ClientSizeChanged);
            this.virtualListView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ListView_KeyDown);
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // MenuStrip
            // 
            resources.ApplyResources(this.MenuStrip, "MenuStrip");
            // 
            // LogWindow
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.Controls.Add(this.virtualListView1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.MenuStrip);
            this.MainMenuStrip = this.MenuStrip;
            this.Name = "LogWindow";
            this.Load += new System.EventHandler(this.LogWindow_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnClear;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.ListView virtualListView1;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.Button buttonCopy;
		private System.Windows.Forms.Button buttonCopyAll;
		private System.Windows.Forms.Button AddToGameDbBtn;
		private MenuStripEx MenuStrip;
	}
}