using System.Reflection;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Client.DiscoHawk
{
	public class About : Form
	{
		public About(MultiMessageContext i18n)
		{
			SuspendLayout();

			RichTextBox richTextBox1 = new();
			richTextBox1.Location = new(12, 12);
			richTextBox1.Name = "richTextBox1";
			richTextBox1.ReadOnly = true;
			richTextBox1.Size = new(499, 236);
			richTextBox1.TabIndex = 1;
			richTextBox1.Text = i18n["discohawkabout-4584-lbl-explainer"]!.Replace("\n", Environment.NewLine);
			richTextBox1.LinkClicked += (_, clickedArgs) => Util.OpenUrlExternal(clickedArgs.LinkText);

			Button button1 = new();
			button1.DialogResult = DialogResult.Cancel;
			button1.Location = new(436, 254);
			button1.Name = "button1";
			button1.Size = new(75, 23);
			button1.TabIndex = 2;
			button1.Text = i18n.GetWithMnemonic("discohawkabout-9804-btn-dismiss");
			button1.UseVisualStyleBackColor = true;
			button1.Click += (_, _) => Close();

			Label lblVersion = new();
			lblVersion.AutoSize = true;
			lblVersion.Location = new(12, 259);
			lblVersion.Name = "lblVersion";
			lblVersion.Size = new(79, 13);
			lblVersion.TabIndex = 3;
			lblVersion.Text = $"v{Assembly.GetExecutingAssembly().GetName().Version}";

			AcceptButton = button1;
			AutoScaleDimensions = new(6.0f, 13.0f);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = button1;
			ClientSize = new(525, 282);
			ControlBox = false;
			Controls.Add(lblVersion);
			Controls.Add(button1);
			Controls.Add(richTextBox1);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MinimizeBox = false;
			Name = "About";
			Text = i18n["discohawkabout-2822-windowtitlestatic"];
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
