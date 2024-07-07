#nullable disable

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BizHawk.DBManTool
{
	public partial class DBMan : Form
	{
		string[] Systems = { "SMS", "GG", "SG", "PCE", "PCECD", "SGX", "NES", "GEN" };
		Rom SelectedRom;

		public DBMan()
		{
			InitializeComponent();

			nameBox.GotFocus += (o, e) => { nameBox.SelectionLength = 0; nameBox.SelectionStart = nameBox.Text.Length; };
			regionBox.GotFocus += (o, e) => { regionBox.SelectionLength = 0; regionBox.SelectionStart = versionBox.Text.Length; };
			versionBox.GotFocus += (o, e) => { versionBox.SelectionLength = 0; versionBox.SelectionStart = versionBox.Text.Length; };
			gameMetaBox.GotFocus += (o, e) => { gameMetaBox.SelectionLength = 0; gameMetaBox.SelectionStart = gameMetaBox.Text.Length; };
			romMetaBox.GotFocus += (o, e) => { romMetaBox.SelectionLength = 0; romMetaBox.SelectionStart = romMetaBox.Text.Length; };
			tagsBox.GotFocus += (o, e) => { tagsBox.SelectionLength = 0; tagsBox.SelectionStart = tagsBox.Text.Length; };
			developerBox.GotFocus += (o, e) => { developerBox.SelectionLength = 0; developerBox.SelectionStart = developerBox.Text.Length; };
			publisherBox.GotFocus += (o, e) => { publisherBox.SelectionLength = 0; publisherBox.SelectionStart = publisherBox.Text.Length; };
			releaseDateBox.GotFocus += (o, e) => { releaseDateBox.SelectionLength = 0; releaseDateBox.SelectionStart = releaseDateBox.Text.Length; };
			playersBox.GotFocus += (o, e) => { playersBox.SelectionLength = 0; playersBox.SelectionStart = playersBox.Text.Length; };
			catalogBox.GotFocus += (o, e) => { catalogBox.SelectionLength = 0; catalogBox.SelectionStart = catalogBox.Text.Length; };
			altNamesBox.GotFocus += (o, e) => { altNamesBox.SelectionLength = 0; altNamesBox.SelectionStart = altNamesBox.Text.Length; };
			notesBox.GotFocus += (o, e) => { notesBox.SelectionLength = 0; notesBox.SelectionStart = notesBox.Text.Length; };

			configSystemBox();
			loadRomsForSelectedSystem();
		}

		void configSystemBox()
		{
			systemBox.Items.AddRange(Systems);
			systemBox.Items.Add("Unassigned");
			systemBox.SelectedIndex = 0;
			gameSystemBox.Items.AddRange(Systems);
			gameSystemBox.Items.Add("Unassigned");
		}

		void loadRomsForSelectedSystem()
		{
			DB.LoadDbForSystem(systemBox.SelectedItem.ToString());
			var names = DB.GetDeveloperPublisherNames().ToArray();
			
			romListView.Items.Clear();
			foreach (var rom in DB.Roms)
			{
				var lvi = new ListViewItem(new string[] { rom.DisplayName, rom.Region, rom.VersionTags, rom.CombinedMetaData, rom.Game.Tags }); 
				lvi.Tag = rom;
				lvi.BackColor = rom.New ? Color.LightGreen : Color.White;
				romListView.Items.Add(lvi);
			}
			detailPanel.Visible = false;
			SelectedRom = null;

			developerBox.AutoCompleteCustomSource.Clear();
			developerBox.AutoCompleteCustomSource.AddRange(names);
			publisherBox.AutoCompleteCustomSource.Clear();
			publisherBox.AutoCompleteCustomSource.AddRange(names);
		}

		void systemBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			loadRomsForSelectedSystem();
		}

		void directoryScanToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var ds = new FolderBrowserDialog { ShowNewFolderButton = false };
			var result = ds.ShowDialog();
			if (result == DialogResult.OK)
			{
				var infos = DirectoryScan.GetRomInfos(ds.SelectedPath);
				DirectoryScan.MergeRomInfosWithDatabase(infos);
				MessageBox.Show("Directory Import complete!");
			}
		}

		void selectedRomChanged(object sender, EventArgs e)
		{
			if (RomChangesMade())
			{
				var result = MessageBox.Show("Save changes?", "Save or Cancel Changes", MessageBoxButtons.YesNo);
				if (result == DialogResult.Yes)
					saveButton_Click(null, null);
				SelectedRom = null;
			}

			if (romListView.SelectedItems.Count == 0)
			{
				detailPanel.Visible = false;
				return;
			}

			var rom = (Rom)romListView.SelectedItems[0].Tag;
			SelectedRom = rom;

			gameSystemBox.Text = rom.System;
			nameBox.Text = rom.Name;
			crcBox.Text = rom.CRC32;
			md5Box.Text = rom.MD5;
			sha1Box.Text = rom.SHA1;
			sizeBox.Text = rom.SizeFriendly;
			regionBox.Text = rom.Region;
			versionBox.Text = rom.VersionTags;
			gameMetaBox.Text = rom.Game.GameMetadata;
			romMetaBox.Text = rom.RomMetadata;
			tagsBox.Text = rom.Game.Tags;
			romStatusBox.Text = rom.RomStatus;
			developerBox.Text = rom.Game.Developer;
			publisherBox.Text = rom.Game.Publisher;
			classificationBox.Text = rom.Game.Classification;
			releaseDateBox.Text = rom.Game.ReleaseDate;
			playersBox.Text = rom.Game.Players;
			catalogBox.Text = rom.Catalog;
			altNamesBox.Text = rom.Game.AltNames;
			notesBox.Text = rom.Game.Notes;

			detailPanel.Visible = true;
		}

		void cancelButton_Click(object sender, EventArgs e)
		{
			gameSystemBox.Text = SelectedRom.System;
			nameBox.Text = SelectedRom.Name;
			crcBox.Text = SelectedRom.CRC32;
			md5Box.Text = SelectedRom.MD5;
			sha1Box.Text = SelectedRom.SHA1;
			sizeBox.Text = SelectedRom.SizeFriendly;
			regionBox.Text = SelectedRom.Region;
			versionBox.Text = SelectedRom.VersionTags;
			gameMetaBox.Text = SelectedRom.Game.GameMetadata;
			romMetaBox.Text = SelectedRom.RomMetadata;
			tagsBox.Text = SelectedRom.Game.Tags;
			romStatusBox.Text = SelectedRom.RomStatus;
			developerBox.Text = SelectedRom.Game.Developer;
			publisherBox.Text = SelectedRom.Game.Publisher;
			classificationBox.Text = SelectedRom.Game.Classification;
			releaseDateBox.Text = SelectedRom.Game.ReleaseDate;
			playersBox.Text = SelectedRom.Game.Players;
			catalogBox.Text = SelectedRom.Catalog;
			altNamesBox.Text = SelectedRom.Game.AltNames;
			notesBox.Text = SelectedRom.Game.Notes;
		}

		void saveButton_Click(object sender, EventArgs e)
		{
			// Check if any changes were made
			if (!SelectedRom.New && !RomChangesMade())
				return;
			
			int saveMode = 0;
			string origSystem = SelectedRom.System;
			string origName = SelectedRom.Name;

			// Did we change System or Name?
			if (KeyChangesMade())
			{
				var rslt = MessageBox.Show("Change all instances of this system/name?\n\nClicking Yes will change all roms to point to the new game info.\nClicking No will create a new Game instance.", "Confirm game change action", MessageBoxButtons.YesNo);
				saveMode = (rslt == DialogResult.Yes) ? 1 : 2;
			}

			// Actually save the stuff
			SelectedRom.System = fmt(gameSystemBox.Text);
			SelectedRom.Name = fmt(nameBox.Text);
			SelectedRom.Region = fmt(regionBox.Text);
			SelectedRom.VersionTags = fmt(versionBox.Text);
			SelectedRom.Game.GameMetadata = fmt(gameMetaBox.Text);
			SelectedRom.RomMetadata = fmt(romMetaBox.Text);
			SelectedRom.Game.Tags = fmt(tagsBox.Text);
			SelectedRom.RomStatus = fmt(romStatusBox.Text);
			SelectedRom.Game.Developer = fmt(developerBox.Text);
			SelectedRom.Game.Publisher = fmt(publisherBox.Text);
			SelectedRom.Game.Classification = fmt(classificationBox.Text);
			SelectedRom.Game.ReleaseDate = fmt(releaseDateBox.Text);
			SelectedRom.Game.Players = fmt(playersBox.Text);
			SelectedRom.Catalog = fmt(catalogBox.Text);
			SelectedRom.Game.AltNames = fmt(altNamesBox.Text);
			SelectedRom.Game.Notes = fmt(notesBox.Text);
			SelectedRom.Modified = DateTime.Now;

			if (saveMode == 0) DB.SaveRom(SelectedRom);
			if (saveMode == 1) DB.SaveRom1(SelectedRom, origSystem, origName);
			if (saveMode == 2) DB.SaveRom2(SelectedRom);

			if (romListView.SelectedItems.Count > 0)
			{
				// Update the side listing
				var romListItem = romListView.SelectedItems[0];
				romListItem.SubItems[0] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.DisplayName);
				romListItem.SubItems[1] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.Region);
				romListItem.SubItems[2] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.VersionTags);
				romListItem.SubItems[3] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.CombinedMetaData);
				romListItem.SubItems[4] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.Game.Tags);
				romListItem.BackColor = SelectedRom.New ? Color.LightGreen : Color.White;
			}

			if (saveMode > 0) loadRomsForSelectedSystem();
		}

		bool RomChangesMade()
		{
			if (SelectedRom == null) 
				return false;
			
			if (!streq(SelectedRom.System, gameSystemBox.Text)) return true;
			if (!streq(SelectedRom.Name, nameBox.Text)) return true;
			if (!streq(SelectedRom.Region, regionBox.Text)) return true;
			if (!streq(SelectedRom.VersionTags, versionBox.Text)) return true;
			if (!streq(SelectedRom.Game.GameMetadata, gameMetaBox.Text)) return true;
			if (!streq(SelectedRom.RomMetadata, romMetaBox.Text)) return true;
			if (!streq(SelectedRom.Game.Tags, tagsBox.Text)) return true;
			if (!streq(SelectedRom.RomStatus, romStatusBox.Text)) return true;
			if (!streq(SelectedRom.Game.Developer, developerBox.Text)) return true;
			if (!streq(SelectedRom.Game.Publisher, publisherBox.Text)) return true;
			if (!streq(SelectedRom.Game.Classification, classificationBox.Text)) return true;
			if (!streq(SelectedRom.Game.ReleaseDate, releaseDateBox.Text)) return true;
			if (!streq(SelectedRom.Game.Players, playersBox.Text)) return true;
			if (!streq(SelectedRom.Catalog, catalogBox.Text)) return true;
			if (!streq(SelectedRom.Game.AltNames, altNamesBox.Text)) return true;
			if (!streq(SelectedRom.Game.Notes, notesBox.Text)) return true;

			return false;
		}

		bool KeyChangesMade()
		{
			if (SelectedRom == null)
				return false;

			if (!streq(SelectedRom.System, gameSystemBox.Text)) return true;
			if (!streq(SelectedRom.Name, nameBox.Text)) return true;
			return false;
		}


		static bool streq(string s1, string s2)
		{
			if (string.IsNullOrWhiteSpace(s1) && string.IsNullOrWhiteSpace(s2)) return true;
			if (s1 == null || s2 == null) return false;
			return s1.Trim() == s2.Trim();
		}

		static string fmt(string s)
		{
			var trimmed = s.Trim();
			if (trimmed.Length == 0)
				return null;
			return trimmed;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == (Keys.F5))
			{
				loadRomsForSelectedSystem();
				return true;
			}
			if (keyData == (Keys.S | Keys.Control) && SelectedRom != null) 
			{
				saveButton_Click(null, null);
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		void cleanupDBToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DB.Cleanup();
			MessageBox.Show("Orphaned GAME records deleted and Sqlite VACUUM performed.");
		}

		void exportGameDBToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var sfd = new SaveFileDialog();
			sfd.DefaultExt = ".txt";
			sfd.AddExtension = true;
			var result = sfd.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.Cancel)
				return;

			var tw = new StreamWriter(sfd.FileName);

			loadRomsForSelectedSystem();
			foreach (var rom in DB.Roms)
			{
				string romCode = "";
				if (rom.Game.Classification == "Homebrew") romCode = "D";
				if (rom.RomStatus == "Overdump") romCode = "O";
				if (rom.RomStatus == "Bad Dump") romCode = "V";
				
				string regionStr = "";
				if (rom.Region != null)
				{
					if (rom.Region.Contains("Japan")) regionStr += "J";
					if (rom.Region.Contains("USA")) regionStr += "U";
					if (rom.Region.Contains("Europe")) regionStr += "E";
					if (rom.Region.Contains("Brazil")) regionStr += "B";
					if (rom.Region.Contains("Taiwan")) regionStr += "T";
					if (rom.Region.Contains("Korea")) regionStr += "K";
					if (rom.Region.Contains("Australia")) regionStr += "Aus";
					if (rom.Region.Contains("World")) regionStr += "W";
				}

				string romName = rom.NameWithTheFlipped;
				if (regionStr.Length > 0)
					romName += " ("+regionStr+")";

				if (rom.VersionTags != null) 
				{
					var versions = rom.VersionTags.Split(';');
					foreach (var version in versions)
					{
						if (version.Trim().Length == 0)
							continue;
						romName += " (" + version + ")";
					}
				}

				tw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", rom.MD5, romCode, romName, rom.System, rom.Game.Tags, rom.CombinedMetaData, rom.Region);
			}

			tw.Close();
		}

		void deleteButton_Click(object sender, EventArgs e)
		{
			var rslt = MessageBox.Show("Confirm deletion for ROM: "+SelectedRom.Name+" "+SelectedRom.Region+" "+SelectedRom.VersionTags+"?", "Confirm ROM Delete", MessageBoxButtons.YesNo);
			if (rslt != System.Windows.Forms.DialogResult.Yes)
				return;

			DB.DeleteRom(SelectedRom);
			loadRomsForSelectedSystem();
		}
	}
}
