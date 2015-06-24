using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public static class ToolHelpers
	{
		public static FileInfo GetTasProjFileFromUser(string currentFile)
		{
			var ofd = HawkDialogFactory.CreateOpenFileDialog();
			if (!string.IsNullOrWhiteSpace(currentFile))
			{
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			}

			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null);
			ofd.Filter = "Tas Project Files (*.tasproj)|*.tasproj|All Files|*.*";
			ofd.RestoreDirectory = true;

			var result = ofd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(ofd.FileName);
		}

		public static FileInfo GetTasProjSaveFileFromUser(string currentFile)
		{
			var sfd = new SaveFileDialog();
			if (!string.IsNullOrWhiteSpace(currentFile))
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(currentFile);
				sfd.InitialDirectory = Path.GetDirectoryName(currentFile);
			}
			else
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null);
			}

			sfd.Filter = "Tas Project Files (*.tasproj)|*.tasproj|All Files|*.*";
			sfd.RestoreDirectory = true;
			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(sfd.FileName);
		}

		public static FileInfo GetWatchFileFromUser(string currentFile)
		{
			var ofd = HawkDialogFactory.CreateOpenFileDialog();
			if (!string.IsNullOrWhiteSpace(currentFile))
			{
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			}

			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPathFragment, null);
			ofd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
			ofd.RestoreDirectory = true;

			var result = ofd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(ofd.FileName);
		}

		public static FileInfo GetWatchSaveFileFromUser(string currentFile)
		{
			var sfd = new SaveFileDialog();
			if (!string.IsNullOrWhiteSpace(currentFile))
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(currentFile);
				sfd.InitialDirectory = Path.GetDirectoryName(currentFile);
			}
			else
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPathFragment, null);
			}

			sfd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
			sfd.RestoreDirectory = true;
			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(sfd.FileName);
		}

		public static FileInfo GetCheatFileFromUser(string currentFile)
		{
			var ofd = HawkDialogFactory.CreateOpenFileDialog();
			if (!string.IsNullOrWhiteSpace(currentFile))
			{
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			}

			ofd.InitialDirectory = PathManager.GetCheatsPath(Global.Game);
			ofd.Filter = "Cheat Files (*.cht)|*.cht|All Files|*.*";
			ofd.RestoreDirectory = true;

			var result = ofd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(ofd.FileName);
		}

		public static FileInfo GetCheatSaveFileFromUser(string currentFile)
		{
			var sfd = new SaveFileDialog();
			if (!string.IsNullOrWhiteSpace(currentFile))
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			}

			sfd.InitialDirectory = PathManager.GetCheatsPath(Global.Game);
			sfd.Filter = "Cheat Files (*.cht)|*.cht|All Files|*.*";
			sfd.RestoreDirectory = true;
			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(sfd.FileName);
		}

		public static FileInfo GetCdlFileFromUser(string currentFile)
		{
			var ofd = HawkDialogFactory.CreateOpenFileDialog();
			ofd.Filter = "Code Data Logger Files (*.cdl)|*.cdl|All Files|*.*";
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null);
			ofd.RestoreDirectory = true;

			if (!string.IsNullOrWhiteSpace(currentFile))
			{
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			}

			var result = ofd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(ofd.FileName);
		}

		public static FileInfo GetCdlSaveFileFromUser(string currentFile)
		{
			var sfd = new SaveFileDialog
			{
				Filter = "Code Data Logger Files (*.cdl)|*.cdl|All Files|*.*",
				InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null),
				RestoreDirectory = true
			};

			if (!string.IsNullOrWhiteSpace(currentFile))
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			}

			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(sfd.FileName);
		}

		public static void UpdateCheatRelatedTools(object sender, CheatCollection.CheatListEventArgs e)
		{
			if (Global.Emulator.HasMemoryDomains())
			{
				GlobalWin.Tools.UpdateValues<RamWatch>();
				GlobalWin.Tools.UpdateValues<RamSearch>();
				GlobalWin.Tools.UpdateValues<HexEditor>();

				if (GlobalWin.Tools.Has<Cheats>())
				{
					GlobalWin.Tools.Cheats.UpdateDialog();
				}

				GlobalWin.MainForm.UpdateCheatStatus();
			}
		}

		public static void ViewInHexEditor(MemoryDomain domain, IEnumerable<long> addresses, Watch.WatchSize size)
		{
			GlobalWin.Tools.Load<HexEditor>();
			GlobalWin.Tools.HexEditor.SetToAddresses(addresses, domain, size);
		}
	}
}
