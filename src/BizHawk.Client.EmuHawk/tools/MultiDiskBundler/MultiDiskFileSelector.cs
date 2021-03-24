using System;
using System.IO;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MultiDiskFileSelector : UserControl, IDialogParent
	{
		private readonly Func<string> _getLoadedRomNameCallback;

		private readonly PathEntryCollection _pathEntries;

		public IDialogController DialogController { get; }

		public string SystemString { get; set; } = "";

		public string Path
		{
			get => PathBox.Text;
			set => PathBox.Text = value;
		}

		public event EventHandler NameChanged;

		private void HandleLabelTextChanged(object sender, EventArgs e)
		{
			OnNameChanged(EventArgs.Empty);
		}

		public MultiDiskFileSelector(IDialogController dialogController, PathEntryCollection pathEntries, Func<string> getLoadedRomNameCallback)
		{
			DialogController = dialogController;
			_pathEntries = pathEntries;
			_getLoadedRomNameCallback = getLoadedRomNameCallback;
			InitializeComponent();
			PathBox.TextChanged += HandleLabelTextChanged;
		}

		protected virtual void OnNameChanged(EventArgs e)
		{
			NameChanged?.Invoke(this, e);
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
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = _pathEntries.RomAbsolutePath(),
				Filter = RomLoader.RomFilter,
				RestoreDirectory = true
			};

			if (this.ShowDialogWithTempMute(ofd) != DialogResult.OK) return;

			var hawkPath = ofd.FileName;

			try
			{
				var file = new FileInfo(ofd.FileName);
				var path = EmuHawkUtil.ResolveShortcut(file.FullName);

				using var hf = new HawkFile(path);
				if (hf.IsArchive)
				{
					// archive - run the archive chooser
					if (SystemString == "PSX" || SystemString == "PCFX" || SystemString == "SAT")
					{
						DialogController.ShowMessageBox("Using archives with PSX, PCFX or SATURN is not currently recommended/supported.");
						return;
					}

					using var ac = new ArchiveChooser(new HawkFile(hawkPath));
					int memIdx = -1;

					if (ac.ShowDialog(this) == DialogResult.OK)
					{
						memIdx = ac.SelectedMemberIndex;
					}

					var intName = hf.ArchiveItems[memIdx];
					PathBox.Text = $"{hawkPath}|{intName.Name}";
				}
				else
				{
					// file is not an archive
					PathBox.Text = hawkPath;
				}
			}
			catch
			{
				// Do nothing
			}
		}

		private void UseCurrentRomButton_Click(object sender, EventArgs e)
		{
			PathBox.Text = _getLoadedRomNameCallback();
		}

		private void DualGBFileSelector_Load(object sender, EventArgs e)
		{
			UpdateValues();
		}

		public void UpdateValues()
		{
			var loadedRomName = _getLoadedRomNameCallback();
			UseCurrentRomButton.Enabled =
				!string.IsNullOrEmpty(loadedRomName)
				&& !loadedRomName.Contains(".xml"); // Can't already be an xml
		}

		private void PathBox_TextChanged(object sender, EventArgs e)
		{
			OnNameChanged(e);
		}
	}
}
