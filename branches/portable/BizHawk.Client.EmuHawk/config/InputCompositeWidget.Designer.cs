namespace BizHawk.Client.EmuHawk
{
	partial class InputCompositeWidget
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
			
			DropdownMenu.Dispose();
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InputCompositeWidget));
			this.btnSpecial = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.widget = new BizHawk.Client.EmuHawk.InputWidget();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnSpecial
			// 
			this.btnSpecial.Image = ((System.Drawing.Image)(resources.GetObject("btnSpecial.Image")));
			this.btnSpecial.Location = new System.Drawing.Point(472, 0);
			this.btnSpecial.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
			this.btnSpecial.Name = "btnSpecial";
			this.btnSpecial.Size = new System.Drawing.Size(20, 20);
			this.btnSpecial.TabIndex = 2;
			this.btnSpecial.UseVisualStyleBackColor = true;
			this.btnSpecial.Click += new System.EventHandler(this.btnSpecial_Click);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.widget, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.btnSpecial, 1, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(492, 20);
			this.tableLayoutPanel1.TabIndex = 8;
			// 
			// widget
			// 
			this.widget.AutoTab = true;
			this.widget.Dock = System.Windows.Forms.DockStyle.Fill;
			this.widget.Location = new System.Drawing.Point(0, 0);
			this.widget.Margin = new System.Windows.Forms.Padding(0);
			this.widget.Name = "widget";
			this.widget.Size = new System.Drawing.Size(470, 20);
			this.widget.TabIndex = 1;
			this.widget.Text = "button1";
			this.widget.WidgetName = null;
			// 
			// InputCompositeWidget
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel1);
			this.Name = "InputCompositeWidget";
			this.Size = new System.Drawing.Size(492, 20);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnSpecial;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private InputWidget widget;


	}
}
