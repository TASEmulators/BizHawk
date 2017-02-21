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

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

//notes: eventually, we intend to have a "firmware acquisition interface" exposed to the emulator cores.
//it will be implemented by emuhawk, and use firmware keys to fetch the firmware content.
//however, for now, the cores are using strings from the config class. so we have the `configMember` which is 
//used by reflection to set the configuration for firmwares which were found

//TODO - we may eventually need to add a progress dialog for this. we should have one for other reasons.
//I started making one in Bizhawk.Util as QuickProgressPopup but ran out of time

//IDEA: show current path in tooltip (esp. for custom resolved)
//IDEA: prepop set customization to dir of current custom

//TODO - display some kind if [!] if you have a user-specified file which is known but defined as incompatible by the firmware DB

namespace BizHawk.Client.EmuHawk
{
	public partial class FirmwaresConfig : Form
	{
		//friendlier names than the system Ids
		public static readonly Dictionary<string, string> SystemGroupNames = new Dictionary<string, string>()
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
				{ "GEN", "Genesis" },
				{ "SMS", "Sega Master System" },
				{ "PSX", "Sony PlayStation" },
				{ "Lynx", "Atari Lynx" },
				{ "AppleII", "Apple II" }
			};

		public string TargetSystem = null;

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

		public FirmwaresConfig(bool retryLoadRom = false, string reloadRomPath = null)
		{
			InitializeComponent();

			//prep imagelist for listview with 3 item states for {idUnsure, idMissing, idOk}
			imageList1.Images.AddRange(new[] { Properties.Resources.RetroQuestion, Properties.Resources.ExclamationRed, Properties.Resources.GreenCheck });

			listviewSorter = new ListViewSorter(this, -1);

			if (retryLoadRom)
			{
				toolStripSeparator1.Visible = true;
				tbbCloseReload.Visible = true;
				tbbCloseReload.Enabled = true;


				if (string.IsNullOrWhiteSpace(reloadRomPath))
				{
					tbbCloseReload.ToolTipText = "Close Firmware Manager and reload ROM";
				}
				else
				{
					tbbCloseReload.ToolTipText = "Close Firmware Manager and reload " + reloadRomPath;
				}

			}
		}

		//makes sure that the specified SystemId is selected in the list (and that all the firmwares for it are visible)
		private void WarpToSystemId(string sysid)
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
				lvi.ToolTipText = null;
				lvi.SubItems.Add(fr.systemId);
				lvi.SubItems.Add(fr.firmwareId);
				lvi.SubItems.Add(fr.descr);
				lvi.SubItems.Add(""); //resolved with
				lvi.SubItems.Add(""); //location
				lvi.SubItems.Add(""); //size
				lvi.SubItems.Add(""); //hash
				lvi.SubItems[6].Font = fixedFont; //would be used for hash and size
				lvi.SubItems[7].Font = fixedFont; //would be used for hash and size
				lvFirmwares.Items.Add(lvi);

				//build the groups in the listview as we go:
				if (!groups.ContainsKey(fr.systemId))
				{
					string name;
					if (!SystemGroupNames.TryGetValue(fr.systemId, out name))
						name = "FIX ME (FirmwaresConfig.cs)";
					lvFirmwares.Groups.Add(fr.systemId, name);
					var lvg = lvFirmwares.Groups[lvFirmwares.Groups.Count - 1];
					groups[fr.systemId] = lvg;
				}
				lvi.Group = groups[fr.systemId];
			}

			//now that we have some items in the listview, we can size some columns to sensible widths
			lvFirmwares.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);

			if (TargetSystem != null)
			{
				WarpToSystemId(TargetSystem);
			}

			RefreshBasePath();
		}


		private void tbbClose_Click(object sender, EventArgs e)
		{
			this.Close();
			DialogResult = DialogResult.Cancel;
		}

		private void tbbCloseReload_Click(object sender, EventArgs e)
		{
			this.Close();
			DialogResult = DialogResult.Retry;
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

		FirmwareManager Manager { get { return Global.FirmwareManager; } }

		private void DoScan()
		{
			lvFirmwares.BeginUpdate();
			Manager.DoScanAndResolve();

			//for each type of firmware, try resolving and record the result
			foreach (ListViewItem lvi in lvFirmwares.Items)
			{
				var fr = lvi.Tag as FirmwareDatabase.FirmwareRecord;
				var ri = Manager.Resolve(fr, true);
				for(int i=4;i<=7;i++)
					lvi.SubItems[i].Text = "";

				if (ri == null)
				{
					lvi.ImageIndex = idMissing;
					lvi.ToolTipText = "Missing!";
				}
				else
				{
					//lazy substring extraction. really should do a better job
					var basePath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null) + Path.DirectorySeparatorChar;
					
					var path = ri.FilePath.Replace(basePath, "");

					//bolden the item if the user has specified a path for it
					bool bolden = ri.UserSpecified;

					//set columns based on whether it was a known file
					if (ri.KnownFirmwareFile == null)
					{
						lvi.ImageIndex = idUnsure;
						lvi.ToolTipText = null;
						lvi.SubItems[4].Text = "-custom-";
					}
					else
					{
						lvi.ImageIndex = idOk;
						lvi.ToolTipText = "Good!";
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
					if (ri.Missing)
					{
						lvi.ImageIndex = idMissing;
						lvi.ToolTipText = "Missing!";
					}

					//if the user specified a known firmware file but its for some other firmware, it was probably a mistake. mark it as suspicious
					if (ri.KnownMismatching)
						lvi.ImageIndex = idUnsure;

					lvi.SubItems[5].Text = path;

					lvi.SubItems[6].Text = ri.Size.ToString();

					if (ri.Hash != null) lvi.SubItems[7].Text = "sha1:" + ri.Hash;
					else lvi.SubItems[7].Text = "";
				}
			}

			lvFirmwares.EndUpdate();
		}

		private void tbbOrganize_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show(this, "This is going to move/rename every automatically-selected firmware file under your configured firmwares directory to match our recommended organizational scheme (which is not super great right now). Proceed?", "Firmwares Organization Confirm", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
			  return;

			Manager.DoScanAndResolve();

			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				var ri = Manager.Resolve(fr);
				if (ri == null) continue;
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
				PerformListCopy();
			}
		}

		void PerformListCopy()
		{
			var str = lvFirmwares.CopyItemsAsText();
			if (str.Length > 0) Clipboard.SetDataObject(str);
		}

		private void lvFirmwares_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && lvFirmwares.GetItemAt(e.X, e.Y) != null)
				lvFirmwaresContextMenuStrip.Show(lvFirmwares, e.Location);
		}

		private void tsmiSetCustomization_Click(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.InitialDirectory = currSelectorDir;
				ofd.RestoreDirectory = true;

				if (ofd.ShowDialog() == DialogResult.OK)
				{
					//remember the location we selected this firmware from, maybe there are others
					currSelectorDir = Path.GetDirectoryName(ofd.FileName);

					//for each selected item, set the user choice (even though multiple selection for this operation is no longer allowed)
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

		private void tsmiInfo_Click(object sender, EventArgs e)
		{
			var lvi = lvFirmwares.SelectedItems[0];
			var fr = lvi.Tag as FirmwareDatabase.FirmwareRecord;

			//get all options for this firmware (in order)
			var options =
				from fo in FirmwareDatabase.FirmwareOptions
				where fo.systemId == fr.systemId && fo.firmwareId == fr.firmwareId
				select fo;

			FirmwaresConfigInfo fciDialog = new FirmwaresConfigInfo();
			fciDialog.lblFirmware.Text = string.Format("{0} : {1} ({2})", fr.systemId, fr.firmwareId, fr.descr);
			foreach (var o in options)
			{
				ListViewItem olvi = new ListViewItem();
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				var ff = FirmwareDatabase.FirmwareFilesByHash[o.hash];
				if (o.status == FirmwareDatabase.FirmwareOptionStatus.Ideal)
					olvi.ImageIndex = FirmwaresConfigInfo.idIdeal;
				if (o.status == FirmwareDatabase.FirmwareOptionStatus.Acceptable)
					olvi.ImageIndex = FirmwaresConfigInfo.idAcceptable;
				if (o.status == FirmwareDatabase.FirmwareOptionStatus.Unacceptable)
					olvi.ImageIndex = FirmwaresConfigInfo.idUnacceptable;
				if (o.status == FirmwareDatabase.FirmwareOptionStatus.Bad)
					olvi.ImageIndex = FirmwaresConfigInfo.idBad;
				olvi.SubItems[0].Text = ff.size.ToString();
				olvi.SubItems[0].Font = this.Font; //why doesnt this work?
				olvi.SubItems[1].Text = "sha1:" + o.hash;
				olvi.SubItems[1].Font = fixedFont;
				olvi.SubItems[2].Text = ff.recommendedName;
				olvi.SubItems[2].Font = this.Font; //why doesnt this work?
				olvi.SubItems[3].Text = ff.descr;
				olvi.SubItems[3].Font = this.Font; //why doesnt this work?
				olvi.SubItems[4].Text = ff.info;
				olvi.SubItems[4].Font = this.Font; //why doesnt this work?
				fciDialog.lvOptions.Items.Add(olvi);
			}

			fciDialog.lvOptions.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent);
			fciDialog.lvOptions.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
			fciDialog.lvOptions.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
			fciDialog.lvOptions.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);

			fciDialog.ShowDialog(this);
		}

		private void lvFirmwaresContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			//hide menu items that arent appropriate for multi-select
			tsmiSetCustomization.Visible = lvFirmwares.SelectedItems.Count == 1;
			tsmiInfo.Visible = lvFirmwares.SelectedItems.Count == 1;
		}

		private void tsmiCopy_Click(object sender, EventArgs e)
		{
			PerformListCopy();
		}

		private void linkBasePath_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (Owner is PathConfig)
			{
				MessageBox.Show("C-C-C-Combo Breaker!", "Nice try, but");
				return;
			}
			new PathConfig().ShowDialog(this);
			RefreshBasePath();
		}

		void RefreshBasePath()
		{
			string oldBasePath = currSelectorDir;
			linkBasePath.Text = currSelectorDir = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null);
			if (oldBasePath != currSelectorDir)
				DoScan();
		}

		private void tbbImport_Click(object sender, EventArgs e)
		{
			using(var ofd = new OpenFileDialog())
			{
				ofd.Multiselect = true;
				if (ofd.ShowDialog() != DialogResult.OK)
				{
					return;
				}

				RunImportJob(ofd.FileNames);
			}
		}

		private bool RunImportJobSingle(string basepath, string f, ref string errors)
		{
			try
			{
				var fi = new FileInfo(f);
				if (!fi.Exists)
				{
					return false;
				}

				string target = Path.Combine(basepath, fi.Name);
				if (new FileInfo(target).Exists)
				{
					// compare the files, if theyre the same. dont do anything
					if (File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(f)))
					{
						return false;
					}

					// hmm theyre different. import but rename it
					string dir = Path.GetDirectoryName(target);
					string ext = Path.GetExtension(target);
					string name = Path.GetFileNameWithoutExtension(target);
					name += " (variant)";
					target = Path.Combine(dir, name) + ext;
				}

				Directory.CreateDirectory(Path.GetDirectoryName(target));
				fi.CopyTo(target, false);
				return true;
			}
			catch
			{
				if (errors != "") errors += "\n";
				errors += f;
				return false;
			}
		}

		private void RunImportJob(IEnumerable<string> files)
		{
			bool didSomething = false;
			var basepath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null);
			string errors = "";
			foreach(var f in files)
			{
				using (var hf = new HawkFile(f))
				{
					if (hf.IsArchive)
					{
						//blech. the worst extraction code in the universe.
						string extractpath = Path.GetTempFileName() + ".dir";
						DirectoryInfo di = Directory.CreateDirectory(extractpath);

						try
						{
							foreach (var ai in hf.ArchiveItems)
							{
								hf.BindArchiveMember(ai);
								var stream = hf.GetStream();
								var ms = new MemoryStream();
								Util.CopyStream(hf.GetStream(), ms, stream.Length);
								string outfile = ai.Name;
								string myname = Path.GetFileName(outfile);
								outfile = Path.Combine(extractpath, myname);
								File.WriteAllBytes(outfile, ms.ToArray());
								hf.Unbind();
								didSomething |= RunImportJobSingle(basepath, outfile, ref errors);
							}
						}
						finally
						{
							di.Delete(true);
						}
					}
					else
					{
						didSomething |= RunImportJobSingle(basepath, f, ref errors);
					}
				}
			}

			if (!string.IsNullOrEmpty(errors))
			{
				MessageBox.Show(errors, "Error importing these files");
			}

			if (didSomething)
			{
				DoScan();
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void lvFirmwares_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.Copy;
			else
				e.Effect = DragDropEffects.None;
		}

		private void lvFirmwares_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				RunImportJob(files);
			}
		}
	}		//class FirmwaresConfig
}
