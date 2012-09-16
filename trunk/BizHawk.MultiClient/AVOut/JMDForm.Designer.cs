namespace BizHawk.MultiClient
{
    partial class JMDForm
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
			this.okButton = new System.Windows.Forms.Button();
			this.threadsBar = new System.Windows.Forms.TrackBar();
			this.compressionBar = new System.Windows.Forms.TrackBar();
			this.threadLeft = new System.Windows.Forms.Label();
			this.threadRight = new System.Windows.Forms.Label();
			this.compressionLeft = new System.Windows.Forms.Label();
			this.compressionRight = new System.Windows.Forms.Label();
			this.threadTop = new System.Windows.Forms.Label();
			this.compressionTop = new System.Windows.Forms.Label();
			this.cancelButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.threadsBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.compressionBar)).BeginInit();
			this.SuspendLayout();
			// 
			// okButton
			// 
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Location = new System.Drawing.Point(23, 224);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(117, 30);
			this.okButton.TabIndex = 0;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// threadsBar
			// 
			this.threadsBar.Location = new System.Drawing.Point(88, 68);
			this.threadsBar.Name = "threadsBar";
			this.threadsBar.Size = new System.Drawing.Size(104, 42);
			this.threadsBar.TabIndex = 5;
			this.threadsBar.Scroll += new System.EventHandler(this.threadsBar_Scroll);
			// 
			// compressionBar
			// 
			this.compressionBar.Location = new System.Drawing.Point(88, 138);
			this.compressionBar.Name = "compressionBar";
			this.compressionBar.Size = new System.Drawing.Size(104, 42);
			this.compressionBar.TabIndex = 9;
			this.compressionBar.Scroll += new System.EventHandler(this.compressionBar_Scroll);
			// 
			// threadLeft
			// 
			this.threadLeft.AutoSize = true;
			this.threadLeft.Location = new System.Drawing.Point(47, 68);
			this.threadLeft.Name = "threadLeft";
			this.threadLeft.Size = new System.Drawing.Size(35, 13);
			this.threadLeft.TabIndex = 2;
			this.threadLeft.Text = "label1";
			// 
			// threadRight
			// 
			this.threadRight.AutoSize = true;
			this.threadRight.Location = new System.Drawing.Point(198, 68);
			this.threadRight.Name = "threadRight";
			this.threadRight.Size = new System.Drawing.Size(35, 13);
			this.threadRight.TabIndex = 4;
			this.threadRight.Text = "label2";
			// 
			// compressionLeft
			// 
			this.compressionLeft.AutoSize = true;
			this.compressionLeft.Location = new System.Drawing.Point(47, 148);
			this.compressionLeft.Name = "compressionLeft";
			this.compressionLeft.Size = new System.Drawing.Size(35, 13);
			this.compressionLeft.TabIndex = 6;
			this.compressionLeft.Text = "label3";
			// 
			// compressionRight
			// 
			this.compressionRight.AutoSize = true;
			this.compressionRight.Location = new System.Drawing.Point(198, 148);
			this.compressionRight.Name = "compressionRight";
			this.compressionRight.Size = new System.Drawing.Size(35, 13);
			this.compressionRight.TabIndex = 8;
			this.compressionRight.Text = "label4";
			// 
			// threadTop
			// 
			this.threadTop.AutoSize = true;
			this.threadTop.Location = new System.Drawing.Point(94, 52);
			this.threadTop.Name = "threadTop";
			this.threadTop.Size = new System.Drawing.Size(98, 13);
			this.threadTop.TabIndex = 3;
			this.threadTop.Text = "Number of Threads";
			this.threadTop.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// compressionTop
			// 
			this.compressionTop.AutoSize = true;
			this.compressionTop.Location = new System.Drawing.Point(96, 122);
			this.compressionTop.Name = "compressionTop";
			this.compressionTop.Size = new System.Drawing.Size(96, 13);
			this.compressionTop.TabIndex = 7;
			this.compressionTop.Text = "Compression Level";
			this.compressionTop.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// cancelButton
			// 
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(158, 224);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(122, 30);
			this.cancelButton.TabIndex = 1;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// JMDForm
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.compressionTop);
			this.Controls.Add(this.threadTop);
			this.Controls.Add(this.compressionRight);
			this.Controls.Add(this.compressionLeft);
			this.Controls.Add(this.threadRight);
			this.Controls.Add(this.threadLeft);
			this.Controls.Add(this.compressionBar);
			this.Controls.Add(this.threadsBar);
			this.Controls.Add(this.okButton);
			this.MaximumSize = new System.Drawing.Size(300, 300);
			this.MinimumSize = new System.Drawing.Size(300, 300);
			this.Name = "JMDForm";
			this.Text = "JMD Compression Options";
			((System.ComponentModel.ISupportInitialize)(this.threadsBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.compressionBar)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.TrackBar threadsBar;
        private System.Windows.Forms.TrackBar compressionBar;
        private System.Windows.Forms.Label threadLeft;
        private System.Windows.Forms.Label threadRight;
        private System.Windows.Forms.Label compressionLeft;
        private System.Windows.Forms.Label compressionRight;
        private System.Windows.Forms.Label threadTop;
        private System.Windows.Forms.Label compressionTop;
        private System.Windows.Forms.Button cancelButton;
    }
}