using System.IO;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Arcades.MAME;

namespace BizHawk.Client.EmuHawk
{
	public partial class MultiDiskFileSelector : UserControl, IDialogParent
	{
		private readonly Func<string> _getLoadedRomNameCallback;

		private readonly PathEntryCollection _pathEntries;

		public IDialogController DialogController { get; }

		private readonly Func<string> _getSystemNameCallback;

		public string Path
		{
			get => PathBox.Text;
			set => PathBox.Text = value;
		}

		public event EventHandler NameChanged;

		public MultiDiskFileSelector(IDialogController dialogController, PathEntryCollection pathEntries,
			Func<string> getLoadedRomNameCallback, Func<string> getSystemNameCallback)
		{
			DialogController = dialogController;
			_pathEntries = pathEntries;
			_getLoadedRomNameCallback = getLoadedRomNameCallback;
			_getSystemNameCallback = getSystemNameCallback;
			InitializeComponent();
		}

		protected virtual void OnNameChanged(EventArgs e)
		{
			NameChanged?.Invoke(this, e);
		}

		private void PathBox_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop)
				&& ((string[]) e.Data.GetData(DataFormats.FileDrop)).Length is 1)
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
			var systemName = _getSystemNameCallback();
			var hawkPath = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: RomLoader.RomFilter,
				initDir: _pathEntries.UseRecentForRoms ? string.Empty : _pathEntries.RomAbsolutePath(systemName));
			if (hawkPath is null) return;
			try
			{
				FileInfo file = new(hawkPath);
				var path = EmuHawkUtil.ResolveShortcut(file.FullName);

				using HawkFile hf = new(path, allowArchives: !MAMEMachineDB.IsMAMEMachine(hawkPath));
				if (!hf.IsArchive)
				{
					// file is not an archive
					PathBox.Text = hawkPath;
					return;
				}
				// else archive - run the archive chooser

				if (systemName is VSystemID.Raw.PSX or VSystemID.Raw.PCFX or VSystemID.Raw.SAT)
				{
					DialogController.ShowMessageBox("Using archives with PSX, PCFX or SATURN is not currently recommended/supported.");
					return;
				}

				using ArchiveChooser ac = new(new(hawkPath)); //TODO can we pass hf here instead of instantiating a new HawkFile?
				if (!this.ShowDialogAsChild(ac).IsOk()
					|| ac.SelectedMemberIndex < 0 || hf.ArchiveItems.Count <= ac.SelectedMemberIndex)
				{
					return;
				}

				PathBox.Text = $"{hawkPath}|{hf.ArchiveItems[ac.SelectedMemberIndex].Name}";
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
