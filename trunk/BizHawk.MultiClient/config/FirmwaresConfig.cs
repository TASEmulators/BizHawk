using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//notes: eventually, we intend to have a "firmware acquisition interface" exposed to the emulator cores.
//it will be implemented by the multiclient, and use firmware keys to fetch the firmware content.
//however, for now, the cores are using strings from the config class. so we have the `configMember` which is 
//used by reflection to set the configuration for firmwares which were found

//TODO - we may eventually need to add a progress dialog for this. we should have one for other reasons.
//I started making one in Bizhawk.Util as QuickProgressPopup but ran out of time

//IDEA: show current path in tooltip

//TODO - display some kind if [!] if you have a user-specified file which is known but defined as incompatible by the firmware DB

namespace BizHawk.MultiClient
{
	public partial class FirmwaresConfig : Form
	{
		//friendlier names than the system Ids
		static readonly Dictionary<string, string> systemGroupNames = new Dictionary<string, string>()
			{
				{ "NES", "NES" },
				{ "SNES", "SNES" },
				{ "PCECD", "PCE-CD" },
				{ "SAT", "Saturn" },
				{ "A78", "Atari 7800" },
				{ "Coleco", "Colecovision" },
				{ "GBA", "GBA" },
				{ "TI83", "TI-83" },
				{ "INTV", "Intellivision" },
				{ "C64", "C64" },
			};



		private const int idUnsure = 0;
		private const int idMissing = 1;
		private const int idOk = 2;

		Font fixedFont, boldFont, boldFixedFont;

		class ListViewSorter : IComparer
		{
			public FirmwaresConfig dialog;
			public int column;
			public int sign;
			public ListViewSorter(FirmwaresConfig dialog, int column)
			{
				this.dialog = dialog;
				this.column = column;
			}
			public int Compare(object a, object b)
			{
				var lva = (ListViewItem)a;
				var lvb = (ListViewItem)b;
				return sign * string.Compare(lva.SubItems[column].Text, lvb.SubItems[column].Text);
			}
		}

		string currSelectorDir;
		ListViewSorter listviewSorter;

		public FirmwaresConfig()
		{
			InitializeComponent();

			//prep imagelist for listview with 3 item states for {idUnsure, idMissing, idOk}
			imageList1.Images.AddRange(new[] { MultiClient.Properties.Resources.RetroQuestion, MultiClient.Properties.Resources.ExclamationRed, MultiClient.Properties.Resources.GreenCheck });

			listviewSorter = new ListViewSorter(this, -1);

			currSelectorDir = PathManager.MakeAbsolutePath(Global.Config.FirmwaresPath);
		}

		//makes sure that the specified SystemId is selected in the list (and that all the firmwares for it are visible)
		public void WarpToSystemId(string sysid)
		{
			bool selectedFirst = false;
			foreach (ListViewItem lvi in lvFirmwares.Items)
			{
				if (lvi.SubItems[1].Text == sysid)
				{
					if(!selectedFirst) lvi.Selected = true;
					lvi.EnsureVisible();
					selectedFirst = true;
				}
			}
		}

		private void FirmwaresConfig_Load(object sender, EventArgs e)
		{
			//we'll use this font for displaying the hash, so they dont look all jagged in a long list
			fixedFont = new Font(new FontFamily("Courier New"), 8);
			boldFont = new Font(lvFirmwares.Font, FontStyle.Bold);
			boldFixedFont = new Font(fixedFont, FontStyle.Bold);

			//populate listview from firmware DB
			var groups = new Dictionary<string, ListViewGroup>();
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				var lvi = new ListViewItem();
				lvi.Tag = fr;
				lvi.UseItemStyleForSubItems = false;
				lvi.ImageIndex = idUnsure;
				lvi.SubItems.Add(fr.systemId);
				lvi.SubItems.Add(fr.firmwareId);
				lvi.SubItems.Add(fr.descr);
				lvi.SubItems.Add(""); //resolved with
				lvi.SubItems.Add(""); //location
				lvi.SubItems.Add(""); //hash
				lvi.SubItems[6].Font = fixedFont; //would be used for hash
				lvFirmwares.Items.Add(lvi);

				//build the groups in the listview as we go:
				if (!groups.ContainsKey(fr.systemId))
				{
					lvFirmwares.Groups.Add(fr.systemId, systemGroupNames[fr.systemId]);
					var lvg = lvFirmwares.Groups[lvFirmwares.Groups.Count - 1];
					groups[fr.systemId] = lvg;
				}
				lvi.Group = groups[fr.systemId];
			}

			//now that we have some items in the listview, we can size some columns to sensible widths
			lvFirmwares.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);

			DoScan();
		}

		private void FirmwaresConfig_FormClosed(object sender, FormClosedEventArgs e)
		{
			fixedFont.Dispose();
			boldFont.Dispose();
			boldFixedFont.Dispose();
		}

		private void tbbGroup_Click(object sender, EventArgs e)
		{
			//toggle the grouping state
			lvFirmwares.ShowGroups = !lvFirmwares.ShowGroups;
		}

		private void lvFirmwares_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (listviewSorter.column != e.Column)
			{
				listviewSorter.column = e.Column;
				listviewSorter.sign = 1;
			}
			else listviewSorter.sign *= -1;
			lvFirmwares.ListViewItemSorter = listviewSorter;
			lvFirmwares.SetSortIcon(e.Column, listviewSorter.sign == 1 ? SortOrder.Descending : SortOrder.Ascending);
			lvFirmwares.Sort();
		}

		private void tbbScan_Click(object sender, EventArgs e)
		{
			//user-initiated scan
			DoScan();
		}

		FirmwareManager Manager { get { return Global.MainForm.FirmwareManager; } }

		private void DoScan()
		{
			lvFirmwares.BeginUpdate();
			Manager.DoScanAndResolve();

			//for each type of firmware, try resolving and record the result
			foreach (ListViewItem lvi in lvFirmwares.Items)
			{
				var fr = lvi.Tag as FirmwareDatabase.FirmwareRecord;
				var ri = Manager.Resolve(fr);
				for(int i=4;i<=6;i++)
					lvi.SubItems[i].Text = "";

				if (ri == null)
				{
					lvi.ImageIndex = idMissing;
				}
				else
				{
					//lazy substring extraction. really should do a better job
					var basePath = PathManager.MakeAbsolutePath(Global.Config.FirmwaresPath) + Path.DirectorySeparatorChar;
					
					var path = ri.FilePath.Replace(basePath, "");

					//bolden the item if the user has specified a path for it
					bool bolden = ri.UserSpecified;

					//set columns based on whether it was a known file
					if (ri.KnownFirmwareFile == null)
					{
						lvi.ImageIndex = idUnsure;
						lvi.SubItems[4].Text = "-custom-";
					}
					else
					{
						lvi.ImageIndex = idOk;
						lvi.SubItems[4].Text = ri.KnownFirmwareFile.descr;
					}

					//bolden the item if necessary
					if (bolden)
					{
						foreach (ListViewItem.ListViewSubItem lvsi in lvi.SubItems) lvsi.Font = boldFont;
						lvi.SubItems[6].Font = boldFixedFont;
					}
					else
					{
						foreach (ListViewItem.ListViewSubItem lvsi in lvi.SubItems) lvsi.Font = lvFirmwares.Font;
						lvi.SubItems[6].Font = fixedFont;
					}

					//if the user specified a file but its missing, mark it as such
					if(ri.Missing)
						lvi.ImageIndex = idMissing;

					//if the user specified a known firmware file but its for some other firmware, it was probably a mistake. mark it as suspicious
					if (ri.KnownMismatching)
						lvi.ImageIndex = idUnsure;

					lvi.SubItems[5].Text = path;
					if (ri.Hash != null) lvi.SubItems[6].Text = "sha1:" + ri.Hash;
					else lvi.SubItems[6].Text = "";
				}
			}

			lvFirmwares.EndUpdate();
		}

		private void tbbOrganize_Click(object sender, EventArgs e)
		{
			if (System.Windows.Forms.MessageBox.Show(this, "This is going to move/rename every automatically-selected firmware file under your configured firmwares directory to match our recommended organizational scheme (which is not super great right now). Proceed?", "Firmwares Organization Confirm", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
			  return;

			Manager.DoScanAndResolve();

			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				var ri = Manager.Resolve(fr);
				if (ri.KnownFirmwareFile == null) continue;
				if (ri.UserSpecified) continue;

				string fpTarget = PathManager.StandardFirmwareName(ri.KnownFirmwareFile.recommendedName);
				string fpSource = ri.FilePath;

				try
				{
				  File.Move(fpSource, fpTarget);
				}
				catch
				{
				  //sometimes moves fail. especially in newer versions of windows with explorers more fragile than your great-grandma.
				  //I am embarassed that I know that. about windows, not your great-grandma.
				}
			}

			DoScan();
		}

		private void lvFirmwares_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift)
			{
				var str = lvFirmwares.CopyItemsAsText();
				if (str.Length > 0) Clipboard.SetDataObject(str);
			}
		}

		private void lvFirmwares_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Right && lvFirmwares.GetItemAt(e.X, e.Y) != null)
				lvFirmwaresContextMenuStrip.Show(lvFirmwares, e.Location);
		}

		private void tsmiSetCustomization_Click(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.InitialDirectory = currSelectorDir;
				ofd.RestoreDirectory = true;

				if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					//remember the location we selected this firmware from, maybe there are others
					currSelectorDir = Path.GetDirectoryName(ofd.FileName);

					//for each selected item, set the user choice (hey, thats the expected semantic
					foreach (ListViewItem lvi in lvFirmwares.SelectedItems)
					{
						var fr = lvi.Tag as FirmwareDatabase.FirmwareRecord;
						Global.Config.FirmwareUserSpecifications[fr.ConfigKey] = ofd.FileName;
					}

					DoScan();
				}
			}
		}

		private void tsmiClearCustomization_Click(object sender, EventArgs e)
		{
			//for each selected item, clear the user choice
			foreach (ListViewItem lvi in lvFirmwares.SelectedItems)
			{
				var fr = lvi.Tag as FirmwareDatabase.FirmwareRecord;
				Global.Config.FirmwareUserSpecifications.Remove(fr.ConfigKey);
			}

			DoScan();
		}

	}		//class FirmwaresConfig
}
