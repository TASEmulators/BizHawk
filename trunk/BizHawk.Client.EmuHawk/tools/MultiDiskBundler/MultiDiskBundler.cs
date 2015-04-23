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

		private bool Recalculate()
		{
			try
			{
				var fileSelectors = FileSelectorPanel.Controls
					.OfType<GroupBox>()
					.SelectMany(g => g.Controls.OfType<MultiDiskFileSelector>())
					.ToList();

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

				string system = Global.Emulator.SystemId; // TODO: have the user pick this?

				var tagNames = names.Select(n => Path.GetFileNameWithoutExtension(n));

				_currentXml = new XElement("BizHawk-XMLGame",
					new XAttribute("System", system),
					new XAttribute("Name", Path.GetFileNameWithoutExtension(name)),
					new XElement("LoadAssets",
						names.Select(n => new XElement(
								ConvertToTag(Path.GetFileNameWithoutExtension(n)),
								new XAttribute("FileName", n)
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
			var sfd = new SaveFileDialog
			{
				FileName = Path.ChangeExtension(GlobalWin.MainForm.CurrentlyOpenRom, ".xml"),
				InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries["Global_NULL", "ROM"].Path, "Global_NULL"),
				Filter = "xml (*.xml)|*.xml|All Files|*.*"
			};

			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.Cancel)
			{
				NameBox.Text = sfd.FileName;
			}
		}
	}
}
