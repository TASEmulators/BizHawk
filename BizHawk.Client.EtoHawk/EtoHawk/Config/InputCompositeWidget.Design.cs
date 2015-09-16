using System;
using System.Collections.Generic;
using System.Text;
using Eto;
using Eto.Forms;
using Eto.Drawing;

namespace EtoHawk.Config
{
    partial class InputCompositeWidget
    {
        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            DropdownMenu.Dispose();
        }

        #region Component Designer generated code

        /// <summary> 
        /// This was not actually generated with a designer, it's just an edited copy/paste job of the WinForms version
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSpecial = new Button();
            this.tableLayoutPanel1 = new TableLayout(2,2);
            this.widget = new InputWidget(_parent);
            this.tableLayoutPanel1.SuspendLayout();
            //this.SuspendLayout();
            // 
            // btnSpecial
            // 
            //this.btnSpecial.Image = ((System.Drawing.Image)(resources.GetObject("btnSpecial.Image")));
            //this.btnSpecial.Location = new Point(472, 0);
            //this.btnSpecial.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            //this.btnSpecial.Name = "btnSpecial";
            this.btnSpecial.Size = new Size(20, 20);
            //this.btnSpecial.TabIndex = 2;
            //this.btnSpecial.UseVisualStyleBackColor = true;
            this.btnSpecial.Click += this.btnSpecial_Click;
            // 
            // tableLayoutPanel1
            // 
            //this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            //this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Add(this.widget, 0, 0);
            this.tableLayoutPanel1.Add(this.btnSpecial, 1, 0);
            //this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            //this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            //this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            //this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            //this.tableLayoutPanel1.TabIndex = 8;
            // 
            // widget
            // 
            this.widget.AutoTab = true;
            //this.widget.Dock = System.Windows.Forms.DockStyle.Fill;
            //this.widget.Location = new Point(0, 0);
            //this.widget.Margin = new Forms.Padding(0);
            //this.widget.Name = "widget";
            this.widget.Size = new Size(470, 20);
            //this.widget.TabIndex = 1;
            this.widget.Text = "button1";
            this.widget.WidgetName = null;
            // 
            // InputCompositeWidget
            // 
            //this.AutoScaleDimensions = new System.Drawing.SizeF(6, 13F);
            //this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Content = tableLayoutPanel1;
            //this.Name = "InputCompositeWidget";
            //this.Size = new Size(492, 20);
        }

        #endregion

        private Button btnSpecial;
        private TableLayout tableLayoutPanel1;
        private InputWidget widget;
        
    }
}
