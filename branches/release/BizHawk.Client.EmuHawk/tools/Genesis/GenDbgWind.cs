using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

namespace BizHawk.Client.EmuHawk.tools.Genesis
{
	// see GenDbgHlp.cs for a general overview of this
	public partial class GenDbgWind : Form
	{
		GenDbgHlp dbg;

		public GenDbgWind()
		{
			InitializeComponent();
			for (int i = 0; i < 10; i++)
			{
				listBox1.Items.Add(i.ToString());
				listBox2.Items.Add(i.ToString());
			}

			dbg = new GenDbgHlp();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex != -1)
				dbg.SaveState(int.Parse((string)listBox1.SelectedItem));
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (listBox2.SelectedIndex != -1)
				dbg.SaveState(int.Parse((string)listBox2.SelectedItem));
		}

		private void button3_Click(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex != -1 && listBox2.SelectedIndex != -1)
				dbg.Cmp(int.Parse((string)listBox1.SelectedItem), int.Parse((string)listBox2.SelectedItem));
		}

		private void GenDbgWind_FormClosed(object sender, FormClosedEventArgs e)
		{
			dbg.Dispose();
			dbg = null;
		}
	}
}
