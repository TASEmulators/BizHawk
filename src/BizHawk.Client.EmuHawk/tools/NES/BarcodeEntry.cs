using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BarcodeEntry : ToolFormBase, IToolForm
	{
		[RequiredService]
		private DatachBarcode Reader { get; set; }

		public BarcodeEntry()
		{
			InitializeComponent();
		}

		public void Restart()
		{
			textBox1_TextChanged(null, null);
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			if (!DatachBarcode.ValidString(textBox1.Text, out var why))
			{
				label3.Text = $"Invalid: {why}";
				label3.Visible = true;
				button1.Enabled = false;
			}
			else
			{
				label3.Visible = false;
				button1.Enabled = true;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Reader.Transfer(textBox1.Text);
		}
	}
}
