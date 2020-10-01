using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.EmuHawk
{
	public partial class MultiDiskBundler : ToolFormBase, IToolFormAutoConfig
	{
		private XElement _currentXml;

		[RequiredService]
		public IEmulator Emulator { get; set; }

		protected override string WindowTitleStatic => "Multi-disk Bundler";

		public MultiDiskBundler()
		{
			InitializeComponent();
			Icon = Properties.Resources.DualIcon;
		}

		private void MultiGameCreator_Load(object sender, EventArgs e) => Restart();

		public override void Restart()
		{
			FileSelectorPanel.Controls.Clear();
			AddButton_Click(null, null);
			AddButton_Click(null, null);

			if (!Game.IsNullInstance() &&  !MainForm.CurrentlyOpenRom.EndsWith(".xml"))
			{
				if (MainForm.CurrentlyOpenRom.Contains("|"))
				{
					var pieces = MainForm.CurrentlyOpenRom.Split('|');

					var directory = Path.GetDirectoryName(pieces[0]) ?? "";
					var filename = Path.ChangeExtension(pieces[1], ".xml");

					NameBox.Text = Path.Combine(directory, filename);
				}
				else
				{
					NameBox.Text = Path.ChangeExtension(MainForm.CurrentlyOpenRom, ".xml");
				}

				if (SystemDropDown.Items.Contains(Emulator.SystemId))
				{
					SystemDropDown.SelectedItem = Emulator.SystemId;
				}
				else if (Emulator is SMS sms && sms.IsGameGear)
				{
					SystemDropDown.SelectedItem = "Game Gear";
				}

				FileSelectors.First().Path = MainForm.CurrentlyOpenRom;
				Recalculate();
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private bool DoSave(out FileInfo fileInfo)
		{
			fileInfo = null;

			if (!Recalculate())
				return false;

			fileInfo = new FileInfo(NameBox.Text);
			if (fileInfo.Exists)
			{
				var result = MessageBox.Show(this, "File already exists, overwrite?", "File exists", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
				if (result != DialogResult.OK)
				{
					return false;
				}
			}

			File.WriteAllText(fileInfo.FullName, _currentXml.ToString());
			return true;
		}

		private void SaveButton_Click(object sender, EventArgs e)
		{
			FileInfo dummy;
			DoSave(out dummy);
		}

		private void SaveRunButton_Click(object sender, EventArgs e)
		{
			FileInfo fileInfo;

			if (!DoSave(out fileInfo))
				return;

			DialogResult = DialogResult.OK;
			Close();

			var lra = new LoadRomArgs { OpenAdvanced = new OpenAdvanced_OpenRom { Path = fileInfo.FullName } };
			MainForm.LoadRom(fileInfo.FullName, lra);
		}

		private void AddButton_Click(object sender, EventArgs e)
		{
			int start = 3 + (FileSelectorPanel.Controls.Count * 43);

			var groupBox = new GroupBox
			{
				Text = "",
				Location = UIHelper.Scale(new Point(6, start)),
				Size = new Size(FileSelectorPanel.ClientSize.Width - UIHelper.ScaleX(12), UIHelper.ScaleY(41)),
				Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
			};

			var mdf = new MultiDiskFileSelector(this)
			{
				Location = UIHelper.Scale(new Point(7, 12)),
				Width = groupBox.ClientSize.Width - UIHelper.ScaleX(13),
				Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
			};

			mdf.NameChanged += FileSelector_NameChanged;
			mdf.SystemString = SystemDropDown.SelectedText;

			groupBox.Controls.Add(mdf);
			FileSelectorPanel.Controls.Add(groupBox);
		}

		private void btnRemove_Click(object sender, EventArgs e)
		{
			//ToDo:
			//Make this better?
			//We need to have i at 1 and not zero because Controls Count doesn't start at zero (sort of)
			var i = 1;
			//For Each Control box we have, loop
			foreach (Control ctrl in FileSelectorPanel.Controls)
			{
				//if we are at the last box, then remove it.
				if ((i == FileSelectorPanel.Controls.Count))
				{
					ctrl.Dispose();
				}
				//One to our looper
				i++;
			}

			Recalculate();
		}

		private void FileSelector_NameChanged(object sender, EventArgs e)
		{
			Recalculate();
		}

		private IEnumerable<MultiDiskFileSelector> FileSelectors =>
			FileSelectorPanel.Controls
				.OfType<GroupBox>()
				.SelectMany(g => g.Controls.OfType<MultiDiskFileSelector>());

		private bool Recalculate()
		{
			try
			{
				var names = FileSelectors.Select(f => f.Path).ToList();

				if (names.Count == 0)
				{
					throw new Exception("No selectors");
				}

				var name = NameBox.Text;

				if (string.IsNullOrWhiteSpace(name))
				{
					throw new Exception("Xml Filename can not be blank");
				}

				if (names.Any(string.IsNullOrWhiteSpace))
				{
					throw new Exception("Rom Names can not be blank");
				}

				var system = SystemDropDown.SelectedItem?.ToString();

				if (string.IsNullOrWhiteSpace(system))
				{
					throw new Exception("System Id can not be blank");
				}

				var basePath = Path.GetDirectoryName(name.Split('|').First());

				if (string.IsNullOrEmpty(basePath))
				{
					var fileInfo = new FileInfo(name);
					basePath = Path.GetDirectoryName(fileInfo.FullName);
				}

				_currentXml = new XElement("BizHawk-XMLGame",
					new XAttribute("System", system),
					new XAttribute("Name", Path.GetFileNameWithoutExtension(name)),
					new XElement("LoadAssets",
						names.Select(n => new XElement(
							"Asset",
							new XAttribute("FileName", PathExtensions.GetRelativePath(basePath, n))
						))
					)
				);

				SaveRunButton.Enabled = true;
				SaveButton.Enabled = true;
				return true;
			}
			catch (Exception)
			{
				//swallow exceptions, since this is just validation logic
			}

			_currentXml = null;
			SaveRunButton.Enabled = false;
			SaveButton.Enabled = false;
			return false;
		}

		private void NameBox_TextChanged(object sender, EventArgs e)
		{
			Recalculate();
		}

		private void BrowseBtn_Click(object sender, EventArgs e)
		{
			string filename = "";
			string initialDirectory = Config.PathEntries.MultiDiskAbsolutePath();

			if (!Game.IsNullInstance())
			{
				filename = NameBox.Text;
				if (string.IsNullOrWhiteSpace(filename))
				{
					filename = Path.ChangeExtension(Game.FilesystemSafeName(), ".xml");
				}

				initialDirectory = Path.GetDirectoryName(filename);
			}

			using var sfd = new SaveFileDialog
			{
				FileName = filename,
				InitialDirectory = initialDirectory,
				Filter = new FilesystemFilterSet(new FilesystemFilter("XML Files", new[] { "xml" })).ToString()
			};

			var result = sfd.ShowHawkDialog(this);
			if (result != DialogResult.Cancel)
			{
				NameBox.Text = sfd.FileName;
			}
		}

		private void SystemDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			Recalculate();
			do
			{
				foreach (Control ctrl in FileSelectorPanel.Controls)
				{
					ctrl.Dispose();
				}
			} while (FileSelectorPanel.Controls.Count != 0);

			if (SystemDropDown.SelectedItem.ToString() == "GB")
			{
				AddButton.Enabled = false;
				btnRemove.Enabled = false;
			}
			else
			{
				AddButton.Enabled = true;
				btnRemove.Enabled = true;
			}
			AddButton_Click(null, null);
			AddButton_Click(null, null);
		}

	}
}
