namespace BizHawk.Client.EmuHawk
{
	partial class HexFind
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HexFind));
            this.FindBox = new System.Windows.Forms.TextBox();
            this.Find_Prev = new System.Windows.Forms.Button();
            this.Find_Next = new System.Windows.Forms.Button();
            this.HexRadio = new System.Windows.Forms.RadioButton();
            this.TextRadio = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // FindBox
            // 
            resources.ApplyResources(this.FindBox, "FindBox");
            this.FindBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.FindBox.Name = "FindBox";
            this.FindBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FindBox_KeyDown);
            // 
            // Find_Prev
            // 
            resources.ApplyResources(this.Find_Prev, "Find_Prev");
            this.Find_Prev.Name = "Find_Prev";
            this.Find_Prev.UseVisualStyleBackColor = true;
            this.Find_Prev.Click += new System.EventHandler(this.Find_Prev_Click);
            // 
            // Find_Next
            // 
            resources.ApplyResources(this.Find_Next, "Find_Next");
            this.Find_Next.Name = "Find_Next";
            this.Find_Next.UseVisualStyleBackColor = true;
            this.Find_Next.Click += new System.EventHandler(this.Find_Next_Click);
            // 
            // HexRadio
            // 
            resources.ApplyResources(this.HexRadio, "HexRadio");
            this.HexRadio.Checked = true;
            this.HexRadio.Name = "HexRadio";
            this.HexRadio.TabStop = true;
            this.HexRadio.UseVisualStyleBackColor = true;
            this.HexRadio.CheckedChanged += new System.EventHandler(this.HexRadio_CheckedChanged);
            // 
            // TextRadio
            // 
            resources.ApplyResources(this.TextRadio, "TextRadio");
            this.TextRadio.Name = "TextRadio";
            this.TextRadio.UseVisualStyleBackColor = true;
            this.TextRadio.CheckedChanged += new System.EventHandler(this.TextRadio_CheckedChanged);
            // 
            // HexFind
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.TextRadio);
            this.Controls.Add(this.HexRadio);
            this.Controls.Add(this.Find_Next);
            this.Controls.Add(this.Find_Prev);
            this.Controls.Add(this.FindBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.Name = "HexFind";
            this.ShowIcon = false;
            this.Load += new System.EventHandler(this.HexFind_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HexFind_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox FindBox;
		private System.Windows.Forms.Button Find_Prev;
		private System.Windows.Forms.Button Find_Next;
		private System.Windows.Forms.RadioButton HexRadio;
		private System.Windows.Forms.RadioButton TextRadio;
	}
}