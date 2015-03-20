using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class MacroInputTool : Form, IToolFormAutoConfig
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }
		// Zones
		List<MovieZone> zones = new List<MovieZone>();
		private IMovie CurrentMovie
		{
			get { return Global.MovieSession.Movie; }
		}

		// Still need to make sure the user can't load and use macros that 
		// have options only available for TasMovie

		private bool _initializing = false;
		public MacroInputTool()
		{
			_initializing = true;
			InitializeComponent();
		}

		private void MacroInputTool_Load(object sender, EventArgs e)
		{
			// Movie recording must be active
			if (!Global.MovieSession.Movie.IsActive)
			{
				MessageBox.Show("In order to use this tool you must be recording a movie.");
				this.Close();
				this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
				return;
			}

			ReplaceBox.Enabled = CurrentMovie is TasMovie;
			OverlayBox.Enabled = CurrentMovie is TasMovie;
			PlaceNum.Enabled = CurrentMovie is TasMovie;

			MovieZone main = new MovieZone(CurrentMovie, 0, CurrentMovie.InputLogLength);
			main.Name = "Entire Movie";

			zones.Add(main);
			ZonesList.Items.Add(main.Name + " - length: " + main.Length);
			ZonesList.Items[0] += " [Zones don't change!]";

			SetUpButtonBoxes();

			_initializing = false;
		}

		public void Restart()
		{
			if (_initializing)
				return;

			zones.Clear();
			ZonesList.Items.Clear();

			MacroInputTool_Load(null, null);
		}

		// These do absolutely nothing.
		public void UpdateValues()
		{

		}
		public void FastUpdate()
		{

		}
		public bool UpdateBefore
		{
			get { return true; }
		}

		public bool AskSaveChanges()
		{
			return true;
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void SetZoneButton_Click(object sender, EventArgs e)
		{
			if (StartNum.Value >= CurrentMovie.InputLogLength || EndNum.Value >= CurrentMovie.InputLogLength)
			{
				MessageBox.Show("Start and end frames must be inside the movie.");
				return;
			}

			MovieZone newZone = new MovieZone(CurrentMovie, (int)StartNum.Value, (int)(EndNum.Value - StartNum.Value + 1));
			newZone.Name = "Zone " + zones.Count;
			zones.Add(newZone);
			ZonesList.Items.Add(newZone.Name + " - length: " + newZone.Length);
		}

		private MovieZone selectedZone
		{
			get
			{
				if (ZonesList.SelectedIndex == -1)
					return null;
				return zones[ZonesList.SelectedIndex];
			}
		}
		private bool _selecting = false;
		private void ZonesList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (selectedZone == null)
				return;

			_selecting = true;
			PlaceNum.Value = selectedZone.Start;
			ReplaceBox.Checked = selectedZone.Replace;
			NameTextbox.Text = selectedZone.Name;
			OverlayBox.Checked = selectedZone.Overlay;
			_selecting = false;
		}

		private void NameTextbox_TextChanged(object sender, EventArgs e)
		{
			if (selectedZone == null || _selecting)
				return;

			selectedZone.Name = NameTextbox.Text;
			ZonesList.Items[ZonesList.SelectedIndex] = selectedZone.Name + " - length: " + selectedZone.Length;
		}
		private void PlaceNum_ValueChanged(object sender, EventArgs e)
		{
			if (selectedZone == null || _selecting)
				return;

			selectedZone.Start = (int)PlaceNum.Value;
		}
		private void ReplaceBox_CheckedChanged(object sender, EventArgs e)
		{
			if (selectedZone == null || _selecting)
				return;

			selectedZone.Replace = ReplaceBox.Checked;
		}
		private void OverlayBox_CheckedChanged(object sender, EventArgs e)
		{
			if (selectedZone == null || _selecting)
				return;

			selectedZone.Overlay = OverlayBox.Checked;
		}
		private void CurrentButton_Click(object sender, EventArgs e)
		{
			PlaceNum.Value = Global.Emulator.Frame;
		}

		private void PlaceZoneButton_Click(object sender, EventArgs e)
		{
			if (selectedZone == null)
				return;

			if (!(CurrentMovie is TasMovie))
			{
				selectedZone.Start = Global.Emulator.Frame;
			}
			selectedZone.PlaceZone(CurrentMovie);
		}

		#region "Menu Items"
		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (selectedZone == null)
			{
				MessageBox.Show("Please select a zone first.");
				return;
			}

			SaveMacroAs(selectedZone);
		}

		private void loadMacroToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MovieZone loadZone = LoadMacro();
			if (loadZone != null)
			{
				zones.Add(loadZone);
				ZonesList.Items.Add(loadZone.Name + " - length: " + loadZone.Length);

				// Options only for TasMovie
				if (!(CurrentMovie is TasMovie))
				{
					loadZone.Replace = false;
					loadZone.Overlay = false;
				}
			}
		}

		private void RecentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			RecentToolStripMenuItem.DropDownItems.Clear();
			RecentToolStripMenuItem.DropDownItems.AddRange(
				Global.Config.RecentMacros.RecentMenu(DummyLoadMacro, true));
		}
		private void DummyLoadMacro(string path)
		{
			MovieZone loadZone = new MovieZone(path);
			zones.Add(loadZone);
			ZonesList.Items.Add(loadZone.Name + " - length: " + loadZone.Length);
		}

		private static string SuggestedFolder()
		{
			return PathManager.MakeAbsolutePath(Path.Combine(
				Global.Config.PathEntries["Global", "Macros"].Path,
				Global.Game.Name), null);
		}
		#endregion

		public static void SaveMacroAs(MovieZone macro)
		{
			SaveFileDialog dialog = new SaveFileDialog();
			bool create = false;
			if (!Directory.Exists(SuggestedFolder()))
			{
				Directory.CreateDirectory(SuggestedFolder());
				create = true;
			}
			dialog.InitialDirectory = SuggestedFolder();
			// Create directory?
			dialog.Filter = "Movie Macros (*.bk2m)|*.bk2m|All Files|*.*";

			DialogResult result = dialog.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				if (create)
					Directory.Delete(dialog.InitialDirectory);
				return;
			}

			macro.Save(dialog.FileName);
			Global.Config.RecentMacros.Add(dialog.FileName);
		}
		public static MovieZone LoadMacro()
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.InitialDirectory = SuggestedFolder();
			dialog.Filter = "Movie Macros (*.bk2m)|*.bk2m|All Files|*.*";

			DialogResult result = dialog.ShowHawkDialog();
			if (result != DialogResult.OK)
				return null;

			MovieZone ret = new MovieZone(dialog.FileName);
			if (ret != null)
				Global.Config.RecentMacros.Add(dialog.FileName);

			return ret;
		}
	}
}
