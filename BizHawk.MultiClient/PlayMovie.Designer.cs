namespace BizHawk.MultiClient
{
    partial class PlayMovie
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
            this.Cancel = new System.Windows.Forms.Button();
            this.OK = new System.Windows.Forms.Button();
            this.BrowseMovies = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(234, 250);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 0;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.Location = new System.Drawing.Point(143, 250);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 1;
            this.OK.Text = "&Ok";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // BrowseMovies
            // 
            this.BrowseMovies.Location = new System.Drawing.Point(234, 34);
            this.BrowseMovies.Name = "BrowseMovies";
            this.BrowseMovies.Size = new System.Drawing.Size(75, 23);
            this.BrowseMovies.TabIndex = 2;
            this.BrowseMovies.Text = "Browse...";
            this.BrowseMovies.UseVisualStyleBackColor = true;
            this.BrowseMovies.Click += new System.EventHandler(this.BrowseMovies_Click);
            // 
            // PlayMovie
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(321, 290);
            this.Controls.Add(this.BrowseMovies);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.Cancel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlayMovie";
            this.Text = "Play Movie";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button BrowseMovies;
    }
}