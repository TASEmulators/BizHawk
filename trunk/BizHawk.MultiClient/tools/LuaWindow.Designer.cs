namespace BizHawk.MultiClient.tools
{
    partial class LuaWindow
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
            this.IDT_SCRIPTFILE = new System.Windows.Forms.TextBox();
            this.IDB_BROWSE = new System.Windows.Forms.Button();
            this.IDB_EDIT = new System.Windows.Forms.Button();
            this.IDB_RUN = new System.Windows.Forms.Button();
            this.IDB_STOP = new System.Windows.Forms.Button();
            this.IDT_OUTPUT = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // IDT_SCRIPTFILE
            // 
            this.IDT_SCRIPTFILE.Location = new System.Drawing.Point(12, 21);
            this.IDT_SCRIPTFILE.Name = "IDT_SCRIPTFILE";
            this.IDT_SCRIPTFILE.Size = new System.Drawing.Size(349, 20);
            this.IDT_SCRIPTFILE.TabIndex = 0;
            // 
            // IDB_BROWSE
            // 
            this.IDB_BROWSE.Location = new System.Drawing.Point(12, 47);
            this.IDB_BROWSE.Name = "IDB_BROWSE";
            this.IDB_BROWSE.Size = new System.Drawing.Size(75, 23);
            this.IDB_BROWSE.TabIndex = 1;
            this.IDB_BROWSE.Text = "Browse";
            this.IDB_BROWSE.UseVisualStyleBackColor = true;
            this.IDB_BROWSE.Click += new System.EventHandler(this.IDB_BROWSE_Click);
            // 
            // IDB_EDIT
            // 
            this.IDB_EDIT.Location = new System.Drawing.Point(93, 47);
            this.IDB_EDIT.Name = "IDB_EDIT";
            this.IDB_EDIT.Size = new System.Drawing.Size(75, 23);
            this.IDB_EDIT.TabIndex = 2;
            this.IDB_EDIT.Text = "Edit";
            this.IDB_EDIT.UseVisualStyleBackColor = true;
            // 
            // IDB_RUN
            // 
            this.IDB_RUN.Location = new System.Drawing.Point(286, 47);
            this.IDB_RUN.Name = "IDB_RUN";
            this.IDB_RUN.Size = new System.Drawing.Size(75, 23);
            this.IDB_RUN.TabIndex = 4;
            this.IDB_RUN.Text = "Run";
            this.IDB_RUN.UseVisualStyleBackColor = true;
            this.IDB_RUN.Click += new System.EventHandler(this.IDB_RUN_Click);
            // 
            // IDB_STOP
            // 
            this.IDB_STOP.Location = new System.Drawing.Point(205, 47);
            this.IDB_STOP.Name = "IDB_STOP";
            this.IDB_STOP.Size = new System.Drawing.Size(75, 23);
            this.IDB_STOP.TabIndex = 3;
            this.IDB_STOP.Text = "Stop";
            this.IDB_STOP.UseVisualStyleBackColor = true;
            // 
            // IDT_OUTPUT
            // 
            this.IDT_OUTPUT.Location = new System.Drawing.Point(12, 91);
            this.IDT_OUTPUT.Multiline = true;
            this.IDT_OUTPUT.Name = "IDT_OUTPUT";
            this.IDT_OUTPUT.ReadOnly = true;
            this.IDT_OUTPUT.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.IDT_OUTPUT.Size = new System.Drawing.Size(349, 151);
            this.IDT_OUTPUT.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 75);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Output Console:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Script File";
            // 
            // LuaWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(365, 251);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.IDT_OUTPUT);
            this.Controls.Add(this.IDB_RUN);
            this.Controls.Add(this.IDB_STOP);
            this.Controls.Add(this.IDB_EDIT);
            this.Controls.Add(this.IDB_BROWSE);
            this.Controls.Add(this.IDT_SCRIPTFILE);
            this.Name = "LuaWindow";
            this.Text = "Lua Script";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox IDT_SCRIPTFILE;
        private System.Windows.Forms.Button IDB_BROWSE;
        private System.Windows.Forms.Button IDB_EDIT;
        private System.Windows.Forms.Button IDB_RUN;
        private System.Windows.Forms.Button IDB_STOP;
        private System.Windows.Forms.TextBox IDT_OUTPUT;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}