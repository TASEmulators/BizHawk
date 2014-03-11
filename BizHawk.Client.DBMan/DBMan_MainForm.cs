using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.DBMan
{
	public partial class DBMan_MainForm : Form
	{
		string[] Systems = { "SMS", "GG", "SG", "PCE", "PCECD", "SGX", "NES", "GEN" };
		Rom SelectedRom;

		public DBMan_MainForm()
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
				var lvi = new ListViewItem(new string[] { rom.Name, rom.Region, rom.VersionTags, rom.CombinedMetaData, rom.Game.Tags }); 
				lvi.Tag = rom;
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

		bool RomChanged;

		void selectedRomChanged(object sender, EventArgs e)
		{
			if (RomChangesMade())
			{
				var result = MessageBox.Show("Save changes?", "Save or Cancel Changes", MessageBoxButtons.YesNo);
				if (result == DialogResult.Yes)
					saveButton_Click(null, null);
				SelectedRom = null;
			}
			
			RomChanged = true;
		}

		void selectedRomMouseUp(object sender, MouseEventArgs e)
		{
			if (RomChanged == false) return;
			RomChanged = false;

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
	//		nameBox.Focus();
		}

		void cancelButton_Click(object sender, EventArgs e)
		{
			RomChanged = true;
			selectedRomMouseUp(null, null);
		}

		void saveButton_Click(object sender, EventArgs e)
		{
			// Check if any changes were made
			if (RomChangesMade() == false)
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

			if (saveMode == 0) DB.SaveRom(SelectedRom);
			if (saveMode == 1) DB.SaveRom1(SelectedRom, origSystem, origName);
			if (saveMode == 2) DB.SaveRom2(SelectedRom);


			if (romListView.SelectedItems.Count > 0)
			{
				// Update the side listing
				var romListItem = (ListViewItem)romListView.SelectedItems[0];
				romListItem.SubItems[0] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.Name);
				romListItem.SubItems[1] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.Region);
				romListItem.SubItems[2] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.VersionTags);
				romListItem.SubItems[3] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.CombinedMetaData);
				romListItem.SubItems[4] = new ListViewItem.ListViewSubItem(romListItem, SelectedRom.Game.Tags);
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
	}
}