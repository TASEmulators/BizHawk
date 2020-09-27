using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	[Tool(false, null)]
	public partial class MacroInputTool : ToolFormBase, IToolFormAutoConfig
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		private static readonly FilesystemFilterSet MacrosFSFilterSet = new FilesystemFilterSet(new FilesystemFilter("Movie Macros", new[] { "bk2m" }));

		private readonly List<MovieZone> _zones = new List<MovieZone>();
		private readonly List<int> _unsavedZones = new List<int>();
		private bool _selecting;

		private IMovie CurrentMovie => MovieSession.Movie;

		// Still need to make sure the user can't load and use macros that 
		// have options only available for TasMovie

		private bool _initializing;

		protected override string WindowTitleStatic => "Macro Input";

		public MacroInputTool()
		{
			_initializing = true;
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;
		}

		private void MacroInputTool_Load(object sender, EventArgs e)
		{
			// Movie recording must be active (check TAStudio because opening a project re-loads the ROM,
			// which resets tools before the movie session becomes active)
			if (!CurrentMovie.IsActive() && !Tools.IsLoaded<TAStudio>())
			{
				MessageBox.Show("In order to use this tool you must be recording a movie.");
				Close();
				DialogResult = DialogResult.Cancel;
				return;
			}

			ReplaceBox.Enabled = OverlayBox.Enabled = PlaceNum.Enabled = CurrentMovie is ITasMovie;

			var main = new MovieZone(CurrentMovie, Emulator, Tools, MovieSession, 0, CurrentMovie.InputLogLength)
			{
				Name = "Entire Movie"
			};

			_zones.Add(main);
			ZonesList.Items.Add($"{main.Name} - length: {main.Length}");
			ZonesList.Items[0] += " [Zones don't change!]";

			SetUpButtonBoxes();

			_initializing = false;
		}

		private void MacroInputTool_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (_initializing)
			{
				return;
			}

			if (!AskSaveChanges())
			{
				e.Cancel = true;
			}
		}

		public override void Restart()
		{
			if (_initializing)
			{
				return;
			}

			_zones.Clear();
			ZonesList.Items.Clear();

			MacroInputTool_Load(null, null);
		}

		public override bool AskSaveChanges()
		{
			if (_unsavedZones.Count == 0 || IsDisposed)
			{
				return true;
			}

			DialogResult result = MessageBox.Show("You have unsaved macro(s). Do you wish to save them?", "Save?", MessageBoxButtons.YesNoCancel);
			if (result == DialogResult.Cancel)
			{
				return false;
			}

			if (result == DialogResult.No)
			{
				return true;
			}

			foreach (var zone in _unsavedZones)
			{
				SaveMacroAs(_zones[zone]);
			}

			return true;
		}

		private void SetZoneButton_Click(object sender, EventArgs e)
		{
			if (StartNum.Value >= CurrentMovie.InputLogLength || EndNum.Value >= CurrentMovie.InputLogLength)
			{
				MessageBox.Show("Start and end frames must be inside the movie.");
				return;
			}

			var newZone = new MovieZone(CurrentMovie, Emulator, Tools, MovieSession, (int) StartNum.Value, (int) (EndNum.Value - StartNum.Value + 1))
			{
				Name = $"Zone {_zones.Count}"
			};
			_zones.Add(newZone);
			ZonesList.Items.Add($"{newZone.Name} - length: {newZone.Length}");

			_unsavedZones.Add(ZonesList.Items.Count - 1);
		}

		private MovieZone SelectedZone => ZonesList.SelectedIndex == -1 ? null : _zones[ZonesList.SelectedIndex];

		private void ZonesList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (SelectedZone == null)
			{
				return;
			}

			_selecting = true;
			PlaceNum.Value = SelectedZone.Start;
			ReplaceBox.Checked = SelectedZone.Replace;
			NameTextbox.Text = SelectedZone.Name;
			OverlayBox.Checked = SelectedZone.Overlay;
			_selecting = false;
		}

		private void NameTextBox_TextChanged(object sender, EventArgs e)
		{
			if (SelectedZone == null || _selecting)
			{
				return;
			}

			SelectedZone.Name = NameTextbox.Text;
			ZonesList.Items[ZonesList.SelectedIndex] = $"{SelectedZone.Name} - length: {SelectedZone.Length}";
		}

		private void PlaceNum_ValueChanged(object sender, EventArgs e)
		{
			if (SelectedZone == null || _selecting)
			{
				return;
			}

			SelectedZone.Start = (int)PlaceNum.Value;
		}

		private void ReplaceBox_CheckedChanged(object sender, EventArgs e)
		{
			if (SelectedZone == null || _selecting)
			{
				return;
			}

			SelectedZone.Replace = ReplaceBox.Checked;
		}

		private void OverlayBox_CheckedChanged(object sender, EventArgs e)
		{
			if (SelectedZone == null || _selecting)
			{
				return;
			}

			SelectedZone.Overlay = OverlayBox.Checked;
		}

		private void CurrentButton_Click(object sender, EventArgs e)
		{
			PlaceNum.Value = Emulator.Frame;
		}

		private void PlaceZoneButton_Click(object sender, EventArgs e)
		{
			if (SelectedZone == null)
			{
				return;
			}

			if (!(CurrentMovie is ITasMovie))
			{
				SelectedZone.Start = Emulator.Frame;
			}

			SelectedZone.PlaceZone(CurrentMovie);
		}

		private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedZone == null)
			{
				MessageBox.Show("Please select a zone first.");
				return;
			}

			if (SaveMacroAs(SelectedZone))
				_unsavedZones.Remove(ZonesList.SelectedIndex);
		}

		private void LoadMacroToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MovieZone loadZone = LoadMacro();
			if (loadZone != null)
			{
				_zones.Add(loadZone);
				ZonesList.Items.Add($"{loadZone.Name} - length: {loadZone.Length}");

				// Options only for TasMovie
				if (!(CurrentMovie is ITasMovie))
				{
					loadZone.Replace = false;
					loadZone.Overlay = false;
				}
			}
		}

		private void RecentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			RecentToolStripMenuItem.DropDownItems.Clear();
			RecentToolStripMenuItem.DropDownItems.AddRange(Config.RecentMacros.RecentMenu(DummyLoadMacro, "Macro"));
		}

		private void DummyLoadMacro(string path)
		{
			MovieZone loadZone = new MovieZone(path, Emulator, MovieSession, Tools);
			_zones.Add(loadZone);
			ZonesList.Items.Add($"{loadZone.Name} - length: {loadZone.Length}");
		}

		private string SuggestedFolder()
		{
			return Config.PathEntries.AbsolutePathFor(Path.Combine(
				Config.PathEntries["Global", "Macros"].Path,
				Game.FilesystemSafeName()), null);
		}

		public bool SaveMacroAs(MovieZone macro)
		{
			using var dialog = new SaveFileDialog
			{
				InitialDirectory = SuggestedFolder(),
				FileName = macro.Name,
				Filter = MacrosFSFilterSet.ToString()
			};

			// Create directory?
			bool create = false;
			if (!Directory.Exists(SuggestedFolder()))
			{
				Directory.CreateDirectory(SuggestedFolder());
				create = true;
			}

			DialogResult result = dialog.ShowHawkDialog(this);
			if (result != DialogResult.OK)
			{
				if (create)
				{
					Directory.Delete(dialog.InitialDirectory);
				}

				return false;
			}

			macro.Save(dialog.FileName);
			Config.RecentMacros.Add(dialog.FileName);

			return true;
		}

		public MovieZone LoadMacro(IEmulator emulator = null, ToolManager tools = null)
		{
			using var dialog = new OpenFileDialog
			{
				InitialDirectory = SuggestedFolder(),
				Filter = MacrosFSFilterSet.ToString()
			};
			
			DialogResult result = dialog.ShowHawkDialog(this);
			if (result != DialogResult.OK)
			{
				return null;
			}

			Config.RecentMacros.Add(dialog.FileName);
			return new MovieZone(dialog.FileName, emulator ?? Emulator, MovieSession, tools ?? Tools);
		}
	}
}
