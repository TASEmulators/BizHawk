using BizHawk.Client.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MultiDiskBundler : Form, IToolFormAutoConfig
	{
		private XElement _currentXml = null;

		public MultiDiskBundler()
		{
			InitializeComponent();
		}

		private void MultiGameCreator_Load(object sender, EventArgs e)
		{
			AddButton_Click(null, null);
			AddButton_Click(null, null);

			if (!Global.Game.IsNullInstance &&  !GlobalWin.MainForm.CurrentlyOpenRom.EndsWith(".xml"))
			{
				string currentRom = GlobalWin.MainForm.CurrentlyOpenRom;
				if (GlobalWin.MainForm.CurrentlyOpenRom.Contains("|"))
				{
					var pieces = GlobalWin.MainForm.CurrentlyOpenRom.Split('|');

					var directory = Path.GetDirectoryName(pieces[0]);
					var filename = Path.ChangeExtension(pieces[1], ".xml");

					NameBox.Text = Path.Combine(directory, filename);
				}
				else
				{
					NameBox.Text = Path.ChangeExtension(GlobalWin.MainForm.CurrentlyOpenRom, ".xml");
				}

				 if (SystemDropDown.Items.Contains(Global.Emulator.SystemId))
				 {
					 SystemDropDown.SelectedItem = Global.Emulator.SystemId;
				 }

				 FileSelectors.First().SetName(GlobalWin.MainForm.CurrentlyOpenRom);
			}
		}

		#region IToolForm

		public void UpdateValues()
		{

		}

		public void FastUpdate()
		{

		}

		public void Restart()
		{

		}

		public bool AskSaveChanges()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return true; }
		}

		#endregion

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void SaveRunButton_Click(object sender, EventArgs e)
		{
			if (Recalculate())
			{
				var fileInfo = new FileInfo(NameBox.Text);
				if (fileInfo.Exists)
				{
					var result = MessageBox.Show(this, "File already exists, overwrite?", "File exists", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
					if (result != DialogResult.OK)
					{
						return;
					}
				}

				File.WriteAllText(fileInfo.FullName, _currentXml.ToString());

				DialogResult = DialogResult.OK;
				Close();

				GlobalWin.MainForm.LoadRom(fileInfo.FullName);
			}
		}

		private void AddButton_Click(object sender, EventArgs e)
		{
			int start = 5 + (FileSelectorPanel.Controls.Count * 43);

			var groupBox = new GroupBox
			{
				Text = "",
				Location = new Point(5, start),
				Size = new Size(435, 38),
				Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
			};

			var mdf = new MultiDiskFileSelector
			{
				Location = new Point(5, 8),
				Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
			};

			mdf.NameChanged += FileSelector_NameChanged;

			groupBox.Controls.Add(mdf);

			FileSelectorPanel.Controls.Add(groupBox);
		}

		private void FileSelector_NameChanged(object sender, EventArgs e)
		{
			Recalculate();
		}


		private IEnumerable<MultiDiskFileSelector> FileSelectors
		{
			get
			{
				return FileSelectorPanel.Controls
					.OfType<GroupBox>()
					.SelectMany(g => g.Controls.OfType<MultiDiskFileSelector>());
			}
		}

		private bool Recalculate()
		{
			try
			{
				var fileSelectors = FileSelectors.ToList();

				var names = fileSelectors.Select(f => f.GetName());

				var name = NameBox.Text;

				if (string.IsNullOrWhiteSpace(name))
				{
					throw new Exception("Blank Names");
				}

				if (names.Any(n => string.IsNullOrWhiteSpace(n)))
				{
					throw new Exception("Blank Names");
				}

				var system = SystemDropDown.SelectedItem.ToString();

				if (system == null)
				{
					throw new Exception("Blank System Id");
				}

				var basePath = Path.GetDirectoryName(name.Split('|').First());

				_currentXml = new XElement("BizHawk-XMLGame",
					new XAttribute("System", system),
					new XAttribute("Name", name),
					new XElement("LoadAssets",
						names.Select(n => new XElement(
							"Asset",
							new XAttribute("FileName", GetRelativePath(basePath, n))
						))
					) 
				);

				SaveRunButton.Enabled = true;
				return true;
			}
			catch (Exception)
			{
				_currentXml = null;
				SaveRunButton.Enabled = false;
				return false;
			}
		}

		private static string ConvertToTag(string name)
		{
			return new Regex("[^A-Za-z0-9]").Replace(name, string.Empty);
		}

		private void NameBox_TextChanged(object sender, EventArgs e)
		{
			Recalculate();
		}

		private void BrowseBtn_Click(object sender, EventArgs e)
		{
			string filename = string.Empty;
			string initialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries["Global_NULL", "Multi-Disk Bundles"].Path, "Global_NULL");

			if (!Global.Game.IsNullInstance)
			{
				filename = NameBox.Text;
				if (string.IsNullOrWhiteSpace(filename))
				{
					filename = Path.ChangeExtension(PathManager.FilesystemSafeName(Global.Game), ".xml");
				}

				initialDirectory = Path.GetDirectoryName(filename);
			}

			var sfd = new SaveFileDialog
			{
				FileName = filename,
				InitialDirectory = initialDirectory,
				Filter = "xml (*.xml)|*.xml|All Files|*.*"
			};

			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.Cancel)
			{
				NameBox.Text = sfd.FileName;
			}
		}

		// http://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
		public static string GetRelativePath(string fromPath, string toPath)
		{
			Win32.FileAttributes fromAttr = GetPathAttribute(fromPath);
			Win32.FileAttributes toAttr = GetPathAttribute(toPath);

			var path = new StringBuilder(260); // MAX_PATH
			if (Win32.PathRelativePathTo(
				path,
				fromPath,
				fromAttr,
				toPath,
				toAttr) == false)
			{
				throw new ArgumentException("Paths must have a common prefix");
			}

			return path.ToString();
		}

		private static Win32.FileAttributes GetPathAttribute(string path)
		{
			var di = new DirectoryInfo(path.Split('|').First());
			if (di.Exists)
			{
				return Win32.FileAttributes.Directory;
			}

			var fi = new FileInfo(path.Split('|').First());
			if (fi.Exists)
			{
				return Win32.FileAttributes.Normal;
			}

			throw new FileNotFoundException();
		}
	}
}
