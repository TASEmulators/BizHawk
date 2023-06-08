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
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.EmuHawk
{
	public partial class MultiDiskBundler : ToolFormBase, IToolFormAutoConfig
	{
		private static readonly FilesystemFilterSet BundlesFSFilterSet = new(new FilesystemFilter("XML Files", new[] { "xml" }));

		public static Icon ToolIcon
			=> Properties.Resources.DualIcon;

		private XElement _currentXml;

		[RequiredService]
		public IEmulator Emulator { get; set; }

		protected override string WindowTitleStatic => "Multi-disk Bundler";

		public MultiDiskBundler()
		{
			InitializeComponent();
			Icon = ToolIcon;
			SystemDropDown.Items.AddRange(new[]
			{
				VSystemID.Raw.AmstradCPC,
				VSystemID.Raw.AppleII,
				VSystemID.Raw.Arcade,
				VSystemID.Raw.C64,
				VSystemID.Raw.GBL,
				VSystemID.Raw.GEN,
				VSystemID.Raw.GGL,
				VSystemID.Raw.Jaguar,
				VSystemID.Raw.N64,
				VSystemID.Raw.NDS,
				VSystemID.Raw.PCFX,
				VSystemID.Raw.PSX,
				VSystemID.Raw.SAT,
				VSystemID.Raw.TI83,
				VSystemID.Raw.ZXSpectrum,
			});
		}

		private void MultiGameCreator_Load(object sender, EventArgs e) => Restart();

		public override void Restart()
		{
			FileSelectorPanel.Controls.Clear();
			AddButton_Click(null, null);
			AddButton_Click(null, null);

			if (!Game.IsNullInstance() && !MainForm.CurrentlyOpenRom.EndsWithOrdinal(".xml"))
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
					SystemDropDown.SelectedItem = VSystemID.Raw.GGL;
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
				var result = this.ModalMessageBox2("File already exists, overwrite?", "File exists", EMsgBoxIcon.Warning, useOKCancel: true);
				if (!result)
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
			_ = MainForm.LoadRom(fileInfo.FullName, lra);
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

			var mdf = new MultiDiskFileSelector(MainForm, Config.PathEntries,
				() => MainForm.CurrentlyOpenRom, () => SystemDropDown.SelectedItem?.ToString())
			{
				Location = UIHelper.Scale(new Point(7, 12)),
				Width = groupBox.ClientSize.Width - UIHelper.ScaleX(13),
				Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
			};

			mdf.NameChanged += FileSelector_NameChanged;

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

				if (names.Count != 0)
				{
					var name = NameBox.Text;

					if (string.IsNullOrWhiteSpace(name))
					{
						throw new Exception("Xml Filename can not be blank");
					}

					if (names.Exists(string.IsNullOrWhiteSpace)) throw new Exception("Rom Names can not be blank");

					var system = SystemDropDown.SelectedItem?.ToString();

					if (string.IsNullOrWhiteSpace(system))
					{
						throw new Exception("System Id can not be blank");
					}

					var basePath = Path.GetDirectoryName(name.SubstringBefore('|'));
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
			string initialDirectory = Config.PathEntries.UseRecentForRoms
				? string.Empty
				: Config.PathEntries.MultiDiskAbsolutePath();

			if (!Game.IsNullInstance())
			{
				filename = NameBox.Text;
				if (string.IsNullOrWhiteSpace(filename))
				{
					filename = Path.ChangeExtension(Game.FilesystemSafeName(), ".xml");
				}

				initialDirectory = Path.GetDirectoryName(filename) ?? string.Empty;
			}

			var result = this.ShowFileSaveDialog(
				filter: BundlesFSFilterSet,
				initDir: initialDirectory,
				initFileName: filename);
			if (result is not null) NameBox.Text = result;
		}

		private void SystemDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			Recalculate();
		}
	}
}
