namespace BizHawk.MultiClient.tools
{
	partial class LuaWriterColorConfig
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
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.panelKeyWord = new System.Windows.Forms.Panel();
			this.lblKeyWords = new System.Windows.Forms.Label();
			this.lblComments = new System.Windows.Forms.Label();
			this.panelComment = new System.Windows.Forms.Panel();
			this.panelString = new System.Windows.Forms.Panel();
			this.lblStrings = new System.Windows.Forms.Label();
			this.panelSymbol = new System.Windows.Forms.Panel();
			this.lblSymbols = new System.Windows.Forms.Label();
			this.panelLibrary = new System.Windows.Forms.Panel();
			this.lblLibraries = new System.Windows.Forms.Label();
			this.KeyWordColorDialog = new System.Windows.Forms.ColorDialog();
			this.CommentColorDialog = new System.Windows.Forms.ColorDialog();
			this.StringColorDialog = new System.Windows.Forms.ColorDialog();
			this.SymbolColorDialog = new System.Windows.Forms.ColorDialog();
			this.LibraryColorDialog = new System.Windows.Forms.ColorDialog();
			this.buttonDefaults = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(116, 227);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 10;
			this.OK.Text = "&OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(197, 227);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 11;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			// 
			// panelKeyWord
			// 
			this.panelKeyWord.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelKeyWord.Location = new System.Drawing.Point(105, 10);
			this.panelKeyWord.Name = "panelKeyWord";
			this.panelKeyWord.Size = new System.Drawing.Size(20, 20);
			this.panelKeyWord.TabIndex = 1;
			this.panelKeyWord.DoubleClick += new System.EventHandler(this.panelKeyWord_DoubleClick);
			// 
			// lblKeyWords
			// 
			this.lblKeyWords.AutoSize = true;
			this.lblKeyWords.Location = new System.Drawing.Point(40, 17);
			this.lblKeyWords.Name = "lblKeyWords";
			this.lblKeyWords.Size = new System.Drawing.Size(59, 13);
			this.lblKeyWords.TabIndex = 0;
			this.lblKeyWords.Text = "Key Words";
			// 
			// lblComments
			// 
			this.lblComments.AutoSize = true;
			this.lblComments.Location = new System.Drawing.Point(43, 57);
			this.lblComments.Name = "lblComments";
			this.lblComments.Size = new System.Drawing.Size(56, 13);
			this.lblComments.TabIndex = 2;
			this.lblComments.Text = "Comments";
			// 
			// panelComment
			// 
			this.panelComment.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelComment.Location = new System.Drawing.Point(105, 50);
			this.panelComment.Name = "panelComment";
			this.panelComment.Size = new System.Drawing.Size(20, 20);
			this.panelComment.TabIndex = 3;
			this.panelComment.DoubleClick += new System.EventHandler(this.panelComment_DoubleClick);
			// 
			// panelString
			// 
			this.panelString.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelString.Location = new System.Drawing.Point(105, 90);
			this.panelString.Name = "panelString";
			this.panelString.Size = new System.Drawing.Size(20, 20);
			this.panelString.TabIndex = 5;
			this.panelString.DoubleClick += new System.EventHandler(this.panelString_DoubleClick);
			// 
			// lblStrings
			// 
			this.lblStrings.AutoSize = true;
			this.lblStrings.Location = new System.Drawing.Point(60, 97);
			this.lblStrings.Name = "lblStrings";
			this.lblStrings.Size = new System.Drawing.Size(39, 13);
			this.lblStrings.TabIndex = 4;
			this.lblStrings.Text = "Strings";
			// 
			// panelSymbol
			// 
			this.panelSymbol.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelSymbol.Location = new System.Drawing.Point(105, 130);
			this.panelSymbol.Name = "panelSymbol";
			this.panelSymbol.Size = new System.Drawing.Size(20, 20);
			this.panelSymbol.TabIndex = 7;
			this.panelSymbol.DoubleClick += new System.EventHandler(this.panelSymbol_DoubleClick);
			// 
			// lblSymbols
			// 
			this.lblSymbols.AutoSize = true;
			this.lblSymbols.Location = new System.Drawing.Point(53, 137);
			this.lblSymbols.Name = "lblSymbols";
			this.lblSymbols.Size = new System.Drawing.Size(46, 13);
			this.lblSymbols.TabIndex = 6;
			this.lblSymbols.Text = "Symbols";
			// 
			// panelLibrary
			// 
			this.panelLibrary.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelLibrary.Location = new System.Drawing.Point(105, 170);
			this.panelLibrary.Name = "panelLibrary";
			this.panelLibrary.Size = new System.Drawing.Size(20, 20);
			this.panelLibrary.TabIndex = 9;
			this.panelLibrary.DoubleClick += new System.EventHandler(this.panelLibrary_DoubleClick);
			// 
			// lblLibraries
			// 
			this.lblLibraries.AutoSize = true;
			this.lblLibraries.Location = new System.Drawing.Point(53, 177);
			this.lblLibraries.Name = "lblLibraries";
			this.lblLibraries.Size = new System.Drawing.Size(46, 13);
			this.lblLibraries.TabIndex = 8;
			this.lblLibraries.Text = "Libraries";
			// 
			// KeyWordColorDialog
			// 
			this.KeyWordColorDialog.Color = System.Drawing.Color.Blue;
			// 
			// CommentColorDialog
			// 
			this.CommentColorDialog.Color = System.Drawing.Color.Green;
			// 
			// StringColorDialog
			// 
			this.StringColorDialog.Color = System.Drawing.Color.Gray;
			// 
			// LibraryColorDialog
			// 
			this.LibraryColorDialog.Color = System.Drawing.Color.Cyan;
			// 
			// buttonDefaults
			// 
			this.buttonDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonDefaults.Location = new System.Drawing.Point(12, 227);
			this.buttonDefaults.Name = "buttonDefaults";
			this.buttonDefaults.Size = new System.Drawing.Size(75, 23);
			this.buttonDefaults.TabIndex = 12;
			this.buttonDefaults.Text = "&Deafults";
			this.buttonDefaults.UseVisualStyleBackColor = true;
			this.buttonDefaults.Click += new System.EventHandler(this.buttonDefaults_Click);
			// 
			// LuaWriterColorConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.buttonDefaults);
			this.Controls.Add(this.panelLibrary);
			this.Controls.Add(this.lblLibraries);
			this.Controls.Add(this.panelSymbol);
			this.Controls.Add(this.lblSymbols);
			this.Controls.Add(this.panelString);
			this.Controls.Add(this.lblStrings);
			this.Controls.Add(this.panelComment);
			this.Controls.Add(this.lblComments);
			this.Controls.Add(this.lblKeyWords);
			this.Controls.Add(this.panelKeyWord);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Name = "LuaWriterColorConfig";
			this.ShowIcon = false;
			this.Text = "Syntax Highlight Config";
			this.Load += new System.EventHandler(this.LuaWriterColorConfig_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Panel panelKeyWord;
        private System.Windows.Forms.Label lblKeyWords;
        private System.Windows.Forms.Label lblComments;
        private System.Windows.Forms.Panel panelComment;
        private System.Windows.Forms.Panel panelString;
        private System.Windows.Forms.Label lblStrings;
        private System.Windows.Forms.Panel panelSymbol;
        private System.Windows.Forms.Label lblSymbols;
        private System.Windows.Forms.Panel panelLibrary;
        private System.Windows.Forms.Label lblLibraries;
        private System.Windows.Forms.ColorDialog KeyWordColorDialog;
        private System.Windows.Forms.ColorDialog CommentColorDialog;
        private System.Windows.Forms.ColorDialog StringColorDialog;
        private System.Windows.Forms.ColorDialog SymbolColorDialog;
        private System.Windows.Forms.ColorDialog LibraryColorDialog;
        private System.Windows.Forms.Button buttonDefaults;
	}
}