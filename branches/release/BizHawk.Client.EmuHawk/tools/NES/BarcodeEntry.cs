using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BarcodeEntry : Form, IToolForm
	{
		private DatachBarcode reader;

		public BarcodeEntry()
		{
			InitializeComponent();

			Restart();
		}

		#region IToolForm

		public void UpdateValues()
		{
		}

		public void FastUpdate()
		{
		}

		public static bool HasReader()
		{
			return GetReader() != null;
		}

		private static DatachBarcode GetReader()
		{
			var nes = Global.Emulator as NES;
			if (nes == null)
				return null;
			var board = nes.GetBoard() as BANDAI_FCG_1;
			if (board == null)
				return null;
			var reader = board.reader; // will be null for non-datach FCG-1 games
			return reader;
		}

		public void Restart()
		{
			reader = GetReader();
			if (reader == null)
				Close();
			textBox1_TextChanged(null, null);
		}

		public bool AskSaveChanges()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}

		#endregion

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			string why;
			if (!DatachBarcode.ValidString(textBox1.Text, out why))
			{
				label3.Text = "Invalid: " + why;
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
			reader.Transfer(textBox1.Text);
		}
	}
}
