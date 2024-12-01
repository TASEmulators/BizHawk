using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Libretro;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

// these match strings from OpenAdvance. should we make them constants in there?
namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// The Advanced ROM Loader type in MainForm/RomLoader/OpenAdvancedChooser
	/// </summary>
	public enum AdvancedRomLoaderType
	{
		None,
		LibretroLaunchNoGame,
		LibretroLaunchGame,
		ClassicLaunchGame,
		MameLaunchGame
	}

	public partial class OpenAdvancedChooser : Form, IDialogParent
	{
		private readonly Config _config;

		private readonly Func<CoreComm> _createCoreComm;

		private RetroDescription _currentDescription;

		private readonly IGameInfo _game;

		private readonly Func<bool> _libretroCoreChooserCallback;

		public IDialogController DialogController { get; }

		public AdvancedRomLoaderType Result;

		public FilesystemFilterSet SuggestedExtensionFilter = null;

		public OpenAdvancedChooser(IDialogController dialogController, Config config, Func<CoreComm> createCoreComm, IGameInfo game, Func<bool> libretroCoreChooserCallback)
		{
			_config = config;
			_createCoreComm = createCoreComm;
			_game = game;
			_libretroCoreChooserCallback = libretroCoreChooserCallback;
			DialogController = dialogController;

			InitializeComponent();

			RefreshLibretroCore(true);
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void btnSetLibretroCore_Click(object sender, EventArgs e)
		{
			if (_libretroCoreChooserCallback()) RefreshLibretroCore(false);
		}

		private void RefreshLibretroCore(bool bootstrap)
		{
			txtLibretroCore.Text = "";
			btnLibretroLaunchNoGame.Enabled = false;
			btnLibretroLaunchGame.Enabled = false;

			var core = _config.LibretroCore;
			if (string.IsNullOrEmpty(core))
			{
				return;
			}

			txtLibretroCore.Text = core;
			_currentDescription = null;

			// scan the current libretro core to see if it can be launched with NoGame,and other stuff
			try
			{
				var coreComm = _createCoreComm();
				using var retro = new LibretroHost(coreComm, _game, core, true);
				btnLibretroLaunchGame.Enabled = true;
				if (retro.Description.SupportsNoGame)
					btnLibretroLaunchNoGame.Enabled = true;

				//print descriptive information
				var descr = retro.Description;
				_currentDescription = descr;
				Console.WriteLine($"core name: {descr.LibraryName} version {descr.LibraryVersion}");
				Console.WriteLine($"extensions: {descr.ValidExtensions}");
				Console.WriteLine($"{nameof(descr.NeedsRomAsPath)}: {descr.NeedsRomAsPath}");
				Console.WriteLine($"{nameof(descr.NeedsArchives)}: {descr.NeedsArchives}");
				Console.WriteLine($"{nameof(descr.SupportsNoGame)}: {descr.SupportsNoGame}");
					
				foreach (var v in descr.Variables.Values)
					Console.WriteLine(v);
			}
			catch (Exception ex)
			{
				if (!bootstrap)
				{
					DialogController.ShowMessageBox($"Couldn't load the selected Libretro core for analysis. It won't be available.\n\nError:\n\n{ex}");
				}
			}
		}

		private void btnLibretroLaunchGame_Click(object sender, EventArgs e)
		{
			var entries = new List<FilesystemFilter> { new FilesystemFilter("ROMs", _currentDescription.ValidExtensions.Split('|')) };
			if (!_currentDescription.NeedsArchives) entries.Add(FilesystemFilter.Archives); // "needs archives" means the relevant archive extensions are already in the list, and we shouldn't scan archives for roms
			SuggestedExtensionFilter = new(entries.ToArray());
			Result = AdvancedRomLoaderType.LibretroLaunchGame;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnMAMELaunchGame_Click(object sender, EventArgs e)
		{
			Result = AdvancedRomLoaderType.MameLaunchGame;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnClassicLaunchGame_Click(object sender, EventArgs e)
		{
			Result = AdvancedRomLoaderType.ClassicLaunchGame;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnLibretroLaunchNoGame_Click(object sender, EventArgs e)
		{
			Result = AdvancedRomLoaderType.LibretroLaunchNoGame;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void txtLibretroCore_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
				var ext = Path.GetExtension(filePaths[0]).ToUpperInvariant();
				if (OSTailoredCode.IsUnixHost ? ext == ".SO" : ext == ".DLL")
				{
					e.Effect = DragDropEffects.Copy;
					return;
				}
			}

			e.Effect = DragDropEffects.None;
		}

		private void txtLibretroCore_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			_config.LibretroCore = filePaths[0];
			RefreshLibretroCore(false);
		}
	}
}
