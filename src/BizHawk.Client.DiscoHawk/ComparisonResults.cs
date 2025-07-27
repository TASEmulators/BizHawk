using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.DiscoHawk
{
	public class ComparisonResults : Form
	{
		public readonly RichTextBox textBox1;

		public ComparisonResults(MultiMessageContext i18n)
		{
			SuspendLayout();

			textBox1 = new();
			textBox1.Dock = DockStyle.Fill;
			textBox1.Font = new("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
			textBox1.Location = new(3, 3);
			textBox1.Name = "textBox1";
			textBox1.ReadOnly = true;
			textBox1.Size = new(757, 394);
			textBox1.TabIndex = 1;
			textBox1.Text = string.Empty; // overwritten by caller as part of object initialisation

			TabControl tabControl1 = new();
			tabControl1.SuspendLayout();
			TabPage tabPage1 = new();
			TabPage tabPage2 = new();
			TabPage tabPage3 = new();
			tabControl1.Controls.Add(tabPage1);
			tabControl1.Controls.Add(tabPage2);
			tabControl1.Controls.Add(tabPage3);
			tabControl1.Dock = DockStyle.Fill;
			tabControl1.Location = new(0, 0);
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			tabControl1.Size = new(771, 426);
			tabControl1.TabIndex = 2;

			tabPage1.SuspendLayout();
			tabPage1.Controls.Add(textBox1);
			tabPage1.Location = new(4, 22);
			tabPage1.Name = "tabPage1";
			tabPage1.Padding = new(all: 3);
			tabPage1.Size = new(763, 400);
			tabPage1.TabIndex = 0;
			tabPage1.Text = i18n.GetWithMnemonic("discohawkcomparereadout-6110-tab-log");
			tabPage1.UseVisualStyleBackColor = true;

			tabPage2.Location = new(4, 22);
			tabPage2.Name = "tabPage2";
			tabPage2.Padding = new(all: 3);
			tabPage2.Size = new(763, 400);
			tabPage2.TabIndex = 1;
			tabPage2.Text = i18n.GetWithMnemonic("discohawkcomparereadout-6110-tab-src");
			tabPage2.UseVisualStyleBackColor = true;

			tabPage3.Location = new(4, 22);
			tabPage3.Name = "tabPage3";
			tabPage3.Padding = new(all: 3);
			tabPage3.Size = new(763, 400);
			tabPage3.TabIndex = 2;
			tabPage3.Text = i18n.GetWithMnemonic("discohawkcomparereadout-6110-tab-dest");
			tabPage3.UseVisualStyleBackColor = true;

			AutoScaleDimensions = new(6.0f, 13.0f);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new(771, 426);
			Controls.Add(tabControl1);
			Name = "ComparisonResults";
			Text = i18n["discohawkcomparereadout-2005-windowtitlestatic"];
			tabControl1.ResumeLayout(performLayout: false);
			tabPage1.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
		}
	}
}
