using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public static class ToolHelpers
	{
		public static FileInfo GetTasProjFileFromUser(string currentFile)
		{
			var ofd = new OpenFileDialog();
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
			else if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null);
			}
			else
			{
				sfd.FileName = "NULL";
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
			var ofd = new OpenFileDialog();
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
			else if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPathFragment, null);
			}
			else
			{
				sfd.FileName = "NULL";
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
			var ofd = new OpenFileDialog();
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
			else if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
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
			var ofd = new OpenFileDialog
			{
				Filter = "Code Data Logger Files (*.cdl)|*.cdl|All Files|*.*",
				InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null),
				RestoreDirectory = true
			};

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

		public static ToolStripMenuItem GenerateAutoLoadItem(RecentFiles recent)
		{
			var auto = new ToolStripMenuItem { Text = "&Autoload", Checked = recent.AutoLoad };
			auto.Click += (o, ev) => recent.ToggleAutoLoad();
			return auto;
		}

		public static ToolStripItem[] GenerateRecentMenu(RecentFiles recent, Action<string> loadFileCallback)
		{
			var items = new List<ToolStripItem>();

			if (recent.Empty)
			{
				var none = new ToolStripMenuItem { Enabled = false, Text = "None" };
				items.Add(none);
			}
			else
			{
				foreach (var filename in recent)
				{
					var temp = filename;
					var item = new ToolStripMenuItem { Text = temp };
					item.Click += (o, ev) => loadFileCallback(temp);
					items.Add(item);
				}
			}

			items.Add(new ToolStripSeparator());

			var clearitem = new ToolStripMenuItem { Text = "&Clear" };
			clearitem.Click += (o, ev) => recent.Clear();
			items.Add(clearitem);

			return items.ToArray();
		}

		public static void HandleLoadError(RecentFiles recent, string path)
		{
			GlobalWin.Sound.StopSound();
			var result = MessageBox.Show("Could not open " + path + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
			if (result == DialogResult.Yes)
			{
				recent.Remove(path);
			}

			GlobalWin.Sound.StartSound();
		}

		public static IEnumerable<ToolStripItem> GenerateMemoryDomainMenuItems(Action<string> setCallback, string selectedDomain = "", int? maxSize = null)
		{
			var items = new List<ToolStripMenuItem>();

			foreach (var domain in Global.Emulator.MemoryDomains)
			{
				var name = domain.Name;
				var item = new ToolStripMenuItem { Text = name };
				item.Click += (o, ev) => setCallback(name);
				item.Checked = name == selectedDomain;
				item.Enabled = !(maxSize.HasValue && domain.Size > maxSize.Value);
				items.Add(item);
			}

			return items;
		}

		public static void PopulateMemoryDomainDropdown(ref ComboBox dropdown, MemoryDomain startDomain)
		{
			dropdown.Items.Clear();
			if (Global.Emulator.MemoryDomains.Count > 0)
			{
				foreach (var domain in Global.Emulator.MemoryDomains)
				{
					var result = dropdown.Items.Add(domain.ToString());
					if (domain.Name == startDomain.Name)
					{
						dropdown.SelectedIndex = result;
					}
				}
			}
		}

		public static void UpdateCheatRelatedTools(object sender, CheatCollection.CheatListEventArgs e)
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

		public static void FreezeAddress(IEnumerable<Watch> watches)
		{
			Global.CheatList.AddRange(
				watches
				.Where(w => !w.IsSeparator)
				.Select(w => new Cheat(w, w.Value ?? 0)));

			//foreach (var watch in watches.Where(watch => !watch.IsSeparator))
			//{
			//	Global.CheatList.Add(new Cheat(watch, watch.Value ?? 0));
			//}
		}

		public static void UnfreezeAddress(IEnumerable<Watch> watches)
		{
			Global.CheatList.RemoveRange(watches.Where(watch => !watch.IsSeparator));
		}

		public static void ViewInHexEditor(MemoryDomain domain, IEnumerable<int> addresses)
		{
			GlobalWin.Tools.Load<HexEditor>();
			GlobalWin.Tools.HexEditor.SetToAddresses(addresses, domain);
		}

		public static void AddColumn(ListView listView, string columnName, bool enabled, int columnWidth)
		{
			if (enabled)
			{
				if (listView.Columns[columnName] == null)
				{
					var column = new ColumnHeader
					{
						Name = columnName,
						Text = columnName.Replace("Column", string.Empty),
						Width = columnWidth,
					};

					listView.Columns.Add(column);
				}
			}
		}
	}
}
