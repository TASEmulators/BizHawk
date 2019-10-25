using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public class ToolFormBase : Form
	{
		public static FileInfo OpenFileDialog(string currentFile, string path, string fileType, string fileExt)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			var ofd = new OpenFileDialog
			{
				FileName = !string.IsNullOrWhiteSpace(currentFile)
					? Path.GetFileName(currentFile)
					: $"{PathManager.FilesystemSafeName(Global.Game)}.{fileExt}",
				InitialDirectory = path,
				Filter = string.Format("{0} (*.{1})|*.{1}|All Files|*.*", fileType, fileExt),
				RestoreDirectory = true
			};

			var result = ofd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(ofd.FileName);
		}

		public static FileInfo SaveFileDialog(string currentFile, string path, string fileType, string fileExt)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			var sfd = new SaveFileDialog
			{
				FileName = !string.IsNullOrWhiteSpace(currentFile)
					? Path.GetFileName(currentFile)
					: $"{PathManager.FilesystemSafeName(Global.Game)}.{fileExt}",
				InitialDirectory = path,
				Filter = string.Format("{0} (*.{1})|*.{1}|All Files|*.*", fileType, fileExt),
				RestoreDirectory = true,
			};

			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(sfd.FileName);
		}

		public static FileInfo GetWatchFileFromUser(string currentFile)
		{
			return OpenFileDialog(currentFile, PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPathFragment, null), "Watch Files", "wch");
		}

		public static FileInfo GetWatchSaveFileFromUser(string currentFile)
		{
			return SaveFileDialog(currentFile, PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPathFragment, null), "Watch Files", "wch");
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

		public static void ViewInHexEditor(MemoryDomain domain, IEnumerable<long> addresses, WatchSize size)
		{
			GlobalWin.Tools.Load<HexEditor>();
			GlobalWin.Tools.HexEditor.SetToAddresses(addresses, domain, size);
		}

		protected void GenericDragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		protected void LoadColumnInfo(VirtualListView listView, ToolDialogSettings.ColumnList columns)
		{
			listView.Columns.Clear();

			var cl = columns
				.Where(c => c.Visible)
				.OrderBy(c => c.Index);

			foreach (var column in cl)
			{
				listView.AddColumn(column);
			}
		}

		protected void LoadColumnInfo(PlatformAgnosticVirtualListView listView, ToolDialogSettings.ColumnList columns)
		{
			listView.AllColumns.Clear();

			var cl = columns
				.Where(c => c.Visible)
				.OrderBy(c => c.Index);

			foreach (var column in cl)
			{
				string colText = column.Name.Replace("Column", "");
				listView.AddColumn(column.Name, colText, column.Width, PlatformAgnosticVirtualListView.ListColumn.InputType.Text);
			}
		}

		protected void SaveColumnInfo(VirtualListView listview, ToolDialogSettings.ColumnList columns)
		{
			foreach (ColumnHeader column in listview.Columns)
			{
				columns[column.Name].Index = column.DisplayIndex;
				columns[column.Name].Width = column.Width;
			}
		}

		protected void SaveColumnInfo(PlatformAgnosticVirtualListView listview, ToolDialogSettings.ColumnList columns)
		{
			foreach (var column in listview.AllColumns)
			{
				columns[column.Name].Index = listview.AllColumns.IndexOf(column);
				columns[column.Name].Width = column.Width.Value;
			}
		}

		protected void RefreshFloatingWindowControl(bool floatingWindow)
		{
			Owner = floatingWindow ? null : GlobalWin.MainForm;
		}

		protected bool IsOnScreen(Point topLeft)
		{
			return ToolManager.IsOnScreen(topLeft);
		}
	}
}
