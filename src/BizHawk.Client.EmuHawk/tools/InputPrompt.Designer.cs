namespace BizHawk.Client.EmuHawk
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InputPrompt));
            this.PromptLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.PromptBox = new System.Windows.Forms.TextBox();
            this.OK = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // PromptLabel
            // 
            resources.ApplyResources(this.PromptLabel, "PromptLabel");
            this.PromptLabel.Name = "PromptLabel";
            // 
            // PromptBox
            // 
            resources.ApplyResources(this.PromptBox, "PromptBox");
            this.PromptBox.Name = "PromptBox";
            this.PromptBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.PromptBox_KeyPress);
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // Cancel
            // 
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // InputPrompt
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.PromptBox);
            this.Controls.Add(this.PromptLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputPrompt";
            this.ShowIcon = false;
            this.TopMost = true;
            this.Load += new System.EventHandler(this.InputPrompt_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private BizHawk.WinForms.Controls.LocLabelEx PromptLabel;
        private System.Windows.Forms.TextBox PromptBox;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
    }
}