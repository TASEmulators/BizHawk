using System;
using System.Windows.Forms;

using BizHawk.Client.EmuHawk.WinFormExtensions;

//todo - display details on the current resolution status
//todo - check(mark) the one thats selected
//todo - turn top info into textboxes i guess, labels suck

namespace BizHawk.Client.EmuHawk
{
	public partial class FirmwaresConfigInfo : Form
	{

		public const int idIdeal = 0;
		public const int idAcceptable = 1;
		public const int idUnacceptable = 2;
		public const int idBad = 3;

		public FirmwaresConfigInfo()
		{
			InitializeComponent();

			//prep imagelist for listview with 4 item states for (ideal, acceptable, unacceptable, bad)
			imageList1.Images.AddRange(new[] { Properties.Resources.GreenCheck, Properties.Resources.Freeze, Properties.Resources.thumbsdown, Properties.Resources.ExclamationRed });
		}

		private void lvOptions_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift)
			{
				PerformListCopy();
			}
		}

		void PerformListCopy()
		{
			var str = lvOptions.CopyItemsAsText();
			if (str.Length > 0) Clipboard.SetDataObject(str);
		}

		private void tsmiOptionsCopy_Click(object sender, EventArgs e)
		{
			PerformListCopy();
		}

		private void lvOptions_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && lvOptions.GetItemAt(e.X, e.Y) != null)
				lvmiOptionsContextMenuStrip.Show(lvOptions, e.Location);
		}
	}
}
