using System.Windows.Forms;

// todo - display details on the current resolution status
// todo - check(mark) the one that's selected
// todo - turn top info into textboxes i guess, labels suck
namespace BizHawk.Client.EmuHawk
{
	public partial class FirmwareConfigInfo : Form
	{
		public FirmwareConfigInfo()
		{
			InitializeComponent();

			// prep imagelist for listview
			foreach (var img in FirmwareConfig.StatusIcons.Values) imageList1.Images.Add(img);
		}

		private void LvOptions_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsCtrl(Keys.C))
			{
				PerformListCopy();
			}
		}

		private void PerformListCopy()
		{
			var str = lvOptions.CopyItemsAsText();
			if (str.Length > 0) Clipboard.SetDataObject(str);
		}

		private void TsmiOptionsCopy_Click(object sender, EventArgs e)
		{
			PerformListCopy();
		}

		private void LvOptions_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && lvOptions.GetItemAt(e.X, e.Y) != null)
			{
				lvmiOptionsContextMenuStrip.Show(lvOptions, e.Location);
			}
		}
	}
}
