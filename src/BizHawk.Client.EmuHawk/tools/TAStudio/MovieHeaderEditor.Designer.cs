using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class MovieHeaderEditor
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

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MovieHeaderEditor));
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OkBtn = new System.Windows.Forms.Button();
            this.AuthorTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.DefaultAuthorButton = new System.Windows.Forms.Button();
            this.MakeDefaultCheckbox = new System.Windows.Forms.CheckBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.EmulatorVersionTextBox = new System.Windows.Forms.TextBox();
            this.CoreTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.BoardNameTextBox = new System.Windows.Forms.TextBox();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.GameNameTextBox = new System.Windows.Forms.TextBox();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SuspendLayout();
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // OkBtn
            // 
            resources.ApplyResources(this.OkBtn, "OkBtn");
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // AuthorTextBox
            // 
            resources.ApplyResources(this.AuthorTextBox, "AuthorTextBox");
            this.AuthorTextBox.Name = "AuthorTextBox";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // DefaultAuthorButton
            // 
            resources.ApplyResources(this.DefaultAuthorButton, "DefaultAuthorButton");
            this.DefaultAuthorButton.Name = "DefaultAuthorButton";
            this.DefaultAuthorButton.UseVisualStyleBackColor = true;
            this.DefaultAuthorButton.Click += new System.EventHandler(this.DefaultAuthorButton_Click);
            // 
            // MakeDefaultCheckbox
            // 
            resources.ApplyResources(this.MakeDefaultCheckbox, "MakeDefaultCheckbox");
            this.MakeDefaultCheckbox.Name = "MakeDefaultCheckbox";
            this.MakeDefaultCheckbox.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // EmulatorVersionTextBox
            // 
            resources.ApplyResources(this.EmulatorVersionTextBox, "EmulatorVersionTextBox");
            this.EmulatorVersionTextBox.Name = "EmulatorVersionTextBox";
            // 
            // CoreTextBox
            // 
            resources.ApplyResources(this.CoreTextBox, "CoreTextBox");
            this.CoreTextBox.Name = "CoreTextBox";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // BoardNameTextBox
            // 
            resources.ApplyResources(this.BoardNameTextBox, "BoardNameTextBox");
            this.BoardNameTextBox.Name = "BoardNameTextBox";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // GameNameTextBox
            // 
            resources.ApplyResources(this.GameNameTextBox, "GameNameTextBox");
            this.GameNameTextBox.Name = "GameNameTextBox";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // MovieHeaderEditor
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.label6);
            this.Controls.Add(this.GameNameTextBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.BoardNameTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.CoreTextBox);
            this.Controls.Add(this.EmulatorVersionTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.MakeDefaultCheckbox);
            this.Controls.Add(this.DefaultAuthorButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.AuthorTextBox);
            this.Controls.Add(this.OkBtn);
            this.Controls.Add(this.CancelBtn);
            this.Name = "MovieHeaderEditor";
            this.Load += new System.EventHandler(this.MovieHeaderEditor_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		private Button CancelBtn;
		private Button OkBtn;
		private TextBox AuthorTextBox;
		private Button DefaultAuthorButton;
		private CheckBox MakeDefaultCheckbox;
		private TextBox EmulatorVersionTextBox;
		private TextBox CoreTextBox;
		private TextBox BoardNameTextBox;
		private TextBox GameNameTextBox;
		private WinForms.Controls.LocLabelEx label1;
		private WinForms.Controls.LocLabelEx label2;
		private WinForms.Controls.LocLabelEx label4;
		private WinForms.Controls.LocLabelEx label5;
		private WinForms.Controls.LocLabelEx label6;
	}
}
