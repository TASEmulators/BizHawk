namespace BizHawk.MultiClient
{
    partial class RecordMovie
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecordMovie));
			this.Cancel = new System.Windows.Forms.Button();
			this.OK = new System.Windows.Forms.Button();
			this.Browse = new System.Windows.Forms.Button();
			this.RecordBox = new System.Windows.Forms.TextBox();
			this.StartFromCombo = new System.Windows.Forms.ComboBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.DefaultAuthorCheckBox = new System.Windows.Forms.CheckBox();
			this.AuthorBox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(391, 139);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(310, 139);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Browse
			// 
			this.Browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.Browse.Image = global::BizHawk.MultiClient.Properties.Resources.OpenFile;
			this.Browse.Location = new System.Drawing.Point(423, 13);
			this.Browse.Name = "Browse";
			this.Browse.Size = new System.Drawing.Size(25, 23);
			this.Browse.TabIndex = 1;
			this.Browse.UseVisualStyleBackColor = true;
			this.Browse.Click += new System.EventHandler(this.button1_Click);
			// 
			// RecordBox
			// 
			this.RecordBox.AllowDrop = true;
			this.RecordBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.RecordBox.Location = new System.Drawing.Point(83, 13);
			this.RecordBox.Name = "RecordBox";
			this.RecordBox.Size = new System.Drawing.Size(334, 20);
			this.RecordBox.TabIndex = 0;
			this.RecordBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.RecordBox_DragDrop);
			this.RecordBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.RecordBox_DragEnter);
			// 
			// StartFromCombo
			// 
			this.StartFromCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.StartFromCombo.FormattingEnabled = true;
			this.StartFromCombo.Items.AddRange(new object[] {
            "Power-On",
            "Now"});
			this.StartFromCombo.Location = new System.Drawing.Point(83, 65);
			this.StartFromCombo.MaxDropDownItems = 32;
			this.StartFromCombo.Name = "StartFromCombo";
			this.StartFromCombo.Size = new System.Drawing.Size(152, 21);
			this.StartFromCombo.TabIndex = 3;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.DefaultAuthorCheckBox);
			this.groupBox1.Controls.Add(this.AuthorBox);
			this.groupBox1.Controls.Add(this.StartFromCombo);
			this.groupBox1.Controls.Add(this.Browse);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.RecordBox);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(454, 112);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			// 
			// DefaultAuthorCheckBox
			// 
			this.DefaultAuthorCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.DefaultAuthorCheckBox.AutoSize = true;
			this.DefaultAuthorCheckBox.Location = new System.Drawing.Point(327, 64);
			this.DefaultAuthorCheckBox.Name = "DefaultAuthorCheckBox";
			this.DefaultAuthorCheckBox.Size = new System.Drawing.Size(121, 17);
			this.DefaultAuthorCheckBox.TabIndex = 6;
			this.DefaultAuthorCheckBox.Text = "Make default author";
			this.DefaultAuthorCheckBox.UseVisualStyleBackColor = true;
			// 
			// AuthorBox
			// 
			this.AuthorBox.AllowDrop = true;
			this.AuthorBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.AuthorBox.Location = new System.Drawing.Point(83, 39);
			this.AuthorBox.Name = "AuthorBox";
			this.AuthorBox.Size = new System.Drawing.Size(365, 20);
			this.AuthorBox.TabIndex = 2;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(36, 41);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(41, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "Author:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 68);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(71, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Record From:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(51, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(26, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "File:";
			// 
			// RecordMovie
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(478, 174);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.OK);
			this.Controls.Add(this.Cancel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(1440, 201);
			this.MinimizeBox = false;
			this.Name = "RecordMovie";
			this.Text = "Record Movie";
			this.Load += new System.EventHandler(this.RecordMovie_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Browse;
        private System.Windows.Forms.TextBox RecordBox;
		private System.Windows.Forms.ComboBox StartFromCombo;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox AuthorBox;
		private System.Windows.Forms.CheckBox DefaultAuthorCheckBox;
    }
}