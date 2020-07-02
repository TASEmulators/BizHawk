using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class ToolFormBase : Form
	{
		public ToolManager Tools { get; set; }
		public Config Config { get; set; }
		public IMainFormForTools MainForm { get; set; }

		public IMovieSession MovieSession { get; set; }
		public IGameInfo Game { get; set; }

		public virtual bool AskSaveChanges() => true;

		public virtual void UpdateValues(ToolFormUpdateType type)
		{
			switch (type)
			{
				case ToolFormUpdateType.PreFrame:
					UpdateBefore();
					break;
				case ToolFormUpdateType.PostFrame:
					UpdateAfter();
					break;
				case ToolFormUpdateType.General:
					GeneralUpdate();
					break;
				case ToolFormUpdateType.FastPreFrame:
					FastUpdateBefore();
					break;
				case ToolFormUpdateType.FastPostFrame:
					FastUpdateAfter();
					break;
			}
		}

		protected virtual void UpdateBefore() { }
		protected virtual void UpdateAfter() { }
		protected virtual void GeneralUpdate() { }
		protected virtual void FastUpdateBefore() { }
		protected virtual void FastUpdateAfter() { }

		public FileInfo OpenFileDialog(string currentFile, string path, string fileType, string fileExt)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			using var ofd = new OpenFileDialog
			{
				FileName = !string.IsNullOrWhiteSpace(currentFile)
					? Path.GetFileName(currentFile)
					: $"{Game.FilesystemSafeName()}.{fileExt}",
				InitialDirectory = path,
				Filter = new FilesystemFilterSet(new FilesystemFilter(fileType, new[] { fileExt })).ToString(),
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

			using var sfd = new SaveFileDialog
			{
				FileName = !string.IsNullOrWhiteSpace(currentFile)
					? Path.GetFileName(currentFile)
					: $"{GlobalWin.Game.FilesystemSafeName()}.{fileExt}",
				InitialDirectory = path,
				Filter = new FilesystemFilterSet(new FilesystemFilter(fileType, new[] { fileExt })).ToString(),
				RestoreDirectory = true
			};

			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(sfd.FileName);
		}

		public FileInfo GetWatchFileFromUser(string currentFile)
		{
			return OpenFileDialog(currentFile, Config.PathEntries.WatchAbsolutePath(), "Watch Files", "wch");
		}

		public FileInfo GetWatchSaveFileFromUser(string currentFile)
		{
			return SaveFileDialog(currentFile, Config.PathEntries.WatchAbsolutePath(), "Watch Files", "wch");
		}

		public void ViewInHexEditor(MemoryDomain domain, IEnumerable<long> addresses, WatchSize size)
		{
			Tools.Load<HexEditor>();
			Tools.HexEditor.SetToAddresses(addresses, domain, size);
		}

		protected void GenericDragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Copy);
		}

		protected void RefreshFloatingWindowControl(bool floatingWindow)
		{
			Owner = floatingWindow ? null : (MainForm) MainForm;
		}

		protected bool IsOnScreen(Point topLeft)
		{
			return Tools.IsOnScreen(topLeft);
		}
	}
}
