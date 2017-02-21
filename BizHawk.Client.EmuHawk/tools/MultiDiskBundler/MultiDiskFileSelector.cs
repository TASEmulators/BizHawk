using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class MultiDiskFileSelector : UserControl
	{
		public string GetName()
		{
			return PathBox.Text;
		}

		public void SetName(string val)
		{
			PathBox.Text = val;
		}

		public event EventHandler NameChanged;

		private void HandleLabelTextChanged(object sender, EventArgs e)
		{
			this.OnNameChanged(EventArgs.Empty);
		}

		public MultiDiskFileSelector()
		{
			InitializeComponent();
			PathBox.TextChanged += this.HandleLabelTextChanged;
		}

		protected virtual void OnNameChanged(EventArgs e)
		{
			EventHandler handler = this.NameChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		private void PathBox_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
				((string[])e.Data.GetData(DataFormats.FileDrop)).Length == 1)
			{
				e.Effect = DragDropEffects.Copy;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		private void PathBox_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var ff = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (ff.Length == 1)
				{
					PathBox.Text = ff[0];
				}
			}
		}

		private void BrowseButton_Click(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries["Global_NULL", "ROM"].Path, "Global_NULL"),
				Filter = MainForm.RomFilter,
				RestoreDirectory = true
			})
			{
				var result = ofd.ShowHawkDialog();
				if (result == DialogResult.OK)
				{
					PathBox.Text = ofd.FileName;
				}
			}
		}

		private void UseCurrentRomButton_Click(object sender, EventArgs e)
		{
			PathBox.Text = GlobalWin.MainForm.CurrentlyOpenRom;
		}

		private void DualGBFileSelector_Load(object sender, EventArgs e)
		{
			UpdateValues();
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			UseCurrentRomButton.Enabled =
				!string.IsNullOrEmpty(GlobalWin.MainForm.CurrentlyOpenRom)
				&& !GlobalWin.MainForm.CurrentlyOpenRom.Contains(".xml"); // Can't already be an xml
		}

		private void PathBox_TextChanged(object sender, EventArgs e)
		{
			OnNameChanged(e);
		}
	}
}
