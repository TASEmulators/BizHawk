using System.Reflection;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Client.DiscoHawk
{
	public class About : Form
	{
		public About()
		{
			SuspendLayout();

			RichTextBox richTextBox1 = new();
			richTextBox1.Location = new(12, 12);
			richTextBox1.Name = "richTextBox1";
			richTextBox1.ReadOnly = true;
			richTextBox1.Size = new(499, 236);
			richTextBox1.TabIndex = 1;
			richTextBox1.Text = ("DiscoHawk converts bolloxed-up crusty disc images to totally tidy CCD."
				+ "\n\nDiscoHawk is part of the BizHawk project ( https://github.com/TASEmulators/BizHawk )."
				+ "\n\nBizHawk is a .net-based multi-system emulator brought to you by some of the rerecording emulator principals. We wrote our own cue parsing/generating code to be able to handle any kind of junk we threw at it. Instead of trapping it in the emulator, we liberated it in the form of this tool, to be useful in other environments."
				+ "\n\nTo use, drag a disc (.cue, .iso, .ccd, .cdi, .mds, .nrg) into the top area. DiscoHawk will dump a newly cleaned up CCD file set to the same directory as the original disc image, and call it _hawked."
				+ "\n\nThis is beta software. You are invited to report problems to our bug tracker or IRC. Problems consist of: crusty disc images that crash DiscoHawk or that cause DiscoHawk to produce a _hawked.ccd which fails to serve your particular purposes (which we will need to be informed of, in case we are outputting wrongly.)")
					.Replace("\n", Environment.NewLine);
			richTextBox1.LinkClicked += (_, clickedArgs) => Util.OpenUrlExternal(clickedArgs.LinkText);

			Button button1 = new();
			button1.DialogResult = DialogResult.Cancel;
			button1.Location = new(436, 254);
			button1.Name = "button1";
			button1.Size = new(75, 23);
			button1.TabIndex = 2;
			button1.Text = "OK";
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
			Text = "About DiscoHawk";
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
