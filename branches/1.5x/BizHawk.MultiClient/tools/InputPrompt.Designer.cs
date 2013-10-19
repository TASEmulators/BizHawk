namespace BizHawk.MultiClient
{
    partial class InputPrompt
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
			this.PromptLabel = new System.Windows.Forms.Label();
			this.PromptBox = new System.Windows.Forms.TextBox();
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// PromptLabel
			// 
			this.PromptLabel.AutoSize = true;
			this.PromptLabel.Location = new System.Drawing.Point(33, 9);
			this.PromptLabel.Name = "PromptLabel";
			this.PromptLabel.Size = new System.Drawing.Size(73, 13);
			this.PromptLabel.TabIndex = 0;
			this.PromptLabel.Text = "Enter a value:";
			// 
			// PromptBox
			// 
			this.PromptBox.Location = new System.Drawing.Point(36, 25);
			this.PromptBox.Name = "PromptBox";
			this.PromptBox.Size = new System.Drawing.Size(164, 20);
			this.PromptBox.TabIndex = 1;
			this.PromptBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.PromptBox_KeyPress);
			// 
			// OK
			// 
			this.OK.Location = new System.Drawing.Point(36, 67);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 2;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(125, 67);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 3;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// InputPrompt
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(235, 106);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Controls.Add(this.PromptBox);
			this.Controls.Add(this.PromptLabel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(241, 133);
			this.Name = "InputPrompt";
			this.ShowIcon = false;
			this.Text = "InputPrompt";
			this.Load += new System.EventHandler(this.InputPrompt_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label PromptLabel;
        private System.Windows.Forms.TextBox PromptBox;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
    }
}