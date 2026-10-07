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
			
			_dropdownMenu.Dispose();
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
            resources.ApplyResources(this.btnSpecial, "btnSpecial");
            this.btnSpecial.Name = "btnSpecial";
            this.btnSpecial.UseVisualStyleBackColor = true;
            this.btnSpecial.Click += new System.EventHandler(this.BtnSpecial_Click);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.widget, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnSpecial, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // widget
            // 
            resources.ApplyResources(this.widget, "widget");
            this.widget.AutoTab = true;
            this.widget.Bindings = "按钮1";
            this.widget.CompositeWidget = null;
            this.widget.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.widget.Name = "widget";
            this.widget.WidgetName = null;
            // 
            // InputCompositeWidget
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "InputCompositeWidget";
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
