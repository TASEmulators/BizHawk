using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

// notes: eventually, we intend to have a "firmware acquisition interface" exposed to the emulator cores.
// it will be implemented by EmuHawk, and use firmware keys to fetch the firmware content.
// however, for now, the cores are using strings from the config class. so we have the `configMember` which is 
// used by reflection to set the configuration for firmwares which were found

// TODO - we may eventually need to add a progress dialog for this. we should have one for other reasons.
// I started making one in BizHawk.Util as QuickProgressPopup but ran out of time

// IDEA: show current path in tooltip (esp. for custom resolved)
// IDEA: prepop set customization to dir of current custom

// TODO - display some kind if [!] if you have a user-specified file which is known but defined as incompatible by the firmware DB
namespace BizHawk.Client.EmuHawk
{
	public partial class FirmwaresConfig : Form
	{
		private readonly MainForm _mainForm;

		// friendlier names than the system Ids
		// Redundant with SystemLookup? Not so fast. That datadrives things. This is one step abstracted. Don't be such a smart guy. Keep this redundant list up to date.
		private static readonly Dictionary<string, string> SystemGroupNames = new Dictionary<string, string>
		{
			["NES"] = "NES",
			["SNES"] = "SNES",
			["PCECD"] = "PCE-CD",
			["SAT"] = "Saturn",
			["A78"] = "Atari 7800",
			["Coleco"] = "Colecovision",
			["GBA"] = "GBA",
			["TI83"] = "TI-83",
			["INTV"] = "Intellivision",
			["C64"] = "C64",
			["GEN"] = "Genesis",
			["SMS"] = "Sega Master System",
			["PSX"] = "PlayStation",
			["Lynx"] = "Lynx",
			["AppleII"] = "Apple II",
			["O2"] = "Odyssey 2",
			["GB"] = "Game Boy",
			["GBC"] = "Game Boy Color",
			["PCFX"] = "PC-FX",
			["32X"] = "32X",
			["ZXSpectrum"] = "ZX Spectrum",
			["AmstradCPC"] = "Amstrad CPC",
			["ChannelF"] = "Channel F",
			["Vectrex"] = "Vectrex",
			["MSX"] = "MSX",
		};

		public string TargetSystem { get; set; }

		private CheckBox _cbAllowImport;

		private const int IdUnsure = 0;
		private const int IdMissing = 1;
		private const int IdOk = 2;

		private Font _fixedFont, _boldFont, _boldFixedFont;

		private class ListViewSorter : IComparer
		{
			public int Column { get; set; }
			public int Sign { get; set; }

			public ListViewSorter(int column)
			{
				Column = column;
			}

			public int Compare(object a, object b)
			{
				var lva = (ListViewItem)a;
				var lvb = (ListViewItem)b;
				return Sign * string.Compare(lva.SubItems[Column].Text, lvb.SubItems[Column].Text);
			}
		}

		private string _currSelectorDir;
		private readonly ListViewSorter _listViewSorter;

		public FirmwaresConfig(MainForm mainForm, bool retryLoadRom = false, string reloadRomPath = null)
		{
			_mainForm = mainForm;
			InitializeComponent();

			// prep ImageList for ListView with 3 item states for {idUnsure, idMissing, idOk}
			imageList1.Images.AddRange(new[] { Properties.Resources.RetroQuestion, Properties.Resources.ExclamationRed, Properties.Resources.GreenCheck });

			_listViewSorter = new ListViewSorter(-1);

			if (retryLoadRom)
			{
				toolStripSeparator1.Visible = true;
				tbbCloseReload.Visible = true;
				tbbCloseReload.Enabled = true;


				tbbCloseReload.ToolTipText = string.IsNullOrWhiteSpace(reloadRomPath)
					? "Close Firmware Manager and reload ROM"
					: $"Close Firmware Manager and reload {reloadRomPath}";
			}
		}

		// makes sure that the specified SystemId is selected in the list (and that all the firmwares for it are visible)
		private void WarpToSystemId(string sysId)
		{
			bool selectedFirst = false;
			foreach (ListViewItem lvi in lvFirmwares.Items)
			{
				if (lvi.SubItems[1].Text == sysId)
				{
					if(!selectedFirst) lvi.Selected = true;
					lvi.EnsureVisible();
					selectedFirst = true;
				}
			}
		}

		private void FirmwaresConfig_Load(object sender, EventArgs e)
		{
			// we'll use this font for displaying the hash, so they don't look all jagged in a long list
			_fixedFont = new Font(new FontFamily("Courier New"), 8);
			_boldFont = new Font(lvFirmwares.Font, FontStyle.Bold);
			_boldFixedFont = new Font(_fixedFont, FontStyle.Bold);

			// populate ListView from firmware DB
			var groups = new Dictionary<string, ListViewGroup>();
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				var lvi = new ListViewItem
				{
					Tag = fr,
					UseItemStyleForSubItems = false,
					ImageIndex = IdUnsure,
					ToolTipText = null
				};
				lvi.SubItems.Add(fr.SystemId);
				lvi.SubItems.Add(fr.FirmwareId);
				lvi.SubItems.Add(fr.Descr);
				lvi.SubItems.Add(""); // resolved with
				lvi.SubItems.Add(""); // location
				lvi.SubItems.Add(""); // size
				lvi.SubItems.Add(""); // hash
				lvi.SubItems[6].Font = _fixedFont; // would be used for hash and size
				lvi.SubItems[7].Font = _fixedFont; // would be used for hash and size
				lvFirmwares.Items.Add(lvi);

				// build the groups in the ListView as we go:
				if (!groups.ContainsKey(fr.SystemId))
				{
					if (!SystemGroupNames.TryGetValue(fr.SystemId, out var name))
						name = "FIX ME (FirmwaresConfig.cs)";
					lvFirmwares.Groups.Add(fr.SystemId, name);
					var lvg = lvFirmwares.Groups[lvFirmwares.Groups.Count - 1];
					groups[fr.SystemId] = lvg;
				}
				lvi.Group = groups[fr.SystemId];
			}

			// now that we have some items in the ListView, we can size some columns to sensible widths
			lvFirmwares.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);

			if (TargetSystem != null)
			{
				WarpToSystemId(TargetSystem);
			}

			RefreshBasePath();

			_cbAllowImport = new CheckBox
			{
				Text = "Allow Importing of Unknown Files"
				, BackColor = Color.Transparent
				, CheckAlign = ContentAlignment.MiddleLeft
				, TextAlign = ContentAlignment.MiddleLeft
				, Font = new Font("Segeo UI", 9, FontStyle.Regular, GraphicsUnit.Point, 1, false)
				, Checked = false
			};
			ToolStripControlHost host = new ToolStripControlHost(_cbAllowImport);
			toolStrip1.Items.Add(host);
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
			_fixedFont.Dispose();
			_boldFont.Dispose();
			_boldFixedFont.Dispose();
		}

		private void tbbGroup_Click(object sender, EventArgs e)
		{
			// toggle the grouping state
			lvFirmwares.ShowGroups = !lvFirmwares.ShowGroups;
		}

		private void lvFirmwares_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (_listViewSorter.Column != e.Column)
			{
				_listViewSorter.Column = e.Column;
				_listViewSorter.Sign = 1;
			}
			else _listViewSorter.Sign *= -1;
			lvFirmwares.ListViewItemSorter = _listViewSorter;
			lvFirmwares.SetSortIcon(e.Column, _listViewSorter.Sign == 1 ? SortOrder.Descending : SortOrder.Ascending);
			lvFirmwares.Sort();
		}

		private void tbbScan_Click(object sender, EventArgs e)
		{
			// user-initiated scan
			DoScan();
		}

		private FirmwareManager Manager => Global.FirmwareManager;

		private void DoScan()
		{
			lvFirmwares.BeginUpdate();
			Manager.DoScanAndResolve();

			// for each type of firmware, try resolving and record the result
			foreach (ListViewItem lvi in lvFirmwares.Items)
			{
				var fr = lvi.Tag as FirmwareDatabase.FirmwareRecord;
				var ri = Manager.Resolve(fr, true);
				for(int i=4;i<=7;i++)
					lvi.SubItems[i].Text = "";

				if (ri == null)
				{
					lvi.ImageIndex = IdMissing;
					lvi.ToolTipText = "No file bound for this firmware!";
				}
				else
				{
					// lazy substring extraction. really should do a better job
					var basePath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null) + Path.DirectorySeparatorChar;
					
					var path = ri.FilePath.Replace(basePath, "");

					// bolden the item if the user has specified a path for it
					bool bolden = ri.UserSpecified;

					// set columns based on whether it was a known file
					if (ri.KnownFirmwareFile == null)
					{
						lvi.ImageIndex = IdUnsure;
						lvi.ToolTipText = "You've bound a custom choice here. Hope you know what you're doing.";
						lvi.SubItems[4].Text = "-custom-";
					}
					else
					{
						lvi.ImageIndex = IdOk;
						lvi.ToolTipText = "Good! This file has been bound to some kind of a decent choice";
						lvi.SubItems[4].Text = ri.KnownFirmwareFile.Description;
					}

					// bolden the item if necessary
					if (bolden)
					{
						foreach (ListViewItem.ListViewSubItem lvsi in lvi.SubItems) lvsi.Font = _boldFont;
						lvi.SubItems[6].Font = _boldFixedFont;
					}
					else
					{
						foreach (ListViewItem.ListViewSubItem lvsi in lvi.SubItems) lvsi.Font = lvFirmwares.Font;
						lvi.SubItems[6].Font = _fixedFont;
					}

					// if the user specified a file but its missing, mark it as such
					if (ri.Missing)
					{
						lvi.ImageIndex = IdMissing;
						lvi.ToolTipText = "The file that's specified is missing!";
					}

					// if the user specified a known firmware file but its for some other firmware, it was probably a mistake. mark it as suspicious
					if (ri.KnownMismatching)
					{
						lvi.ImageIndex = IdUnsure;
						lvi.ToolTipText = "You've manually specified a firmware file, and we're sure it's wrong. Hope you know what you're doing.";
					}


					lvi.SubItems[5].Text = path;

					lvi.SubItems[6].Text = ri.Size.ToString();

					if (ri.Hash != null) lvi.SubItems[7].Text = $"sha1:{ri.Hash}";
					else lvi.SubItems[7].Text = "";
				}
			}

			lvFirmwares.EndUpdate();
		}

		private void tbbOrganize_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show(this, "This is going to move/rename every automatically-selected firmware file under your configured firmwares directory to match our recommended organizational scheme (which is not super great right now). Proceed?", "Firmwares Organization Confirm", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
			{
				return;
			}

			Manager.DoScanAndResolve();

			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				var ri = Manager.Resolve(fr);
				if (ri == null) continue;
				if (ri.KnownFirmwareFile == null) continue;
				if (ri.UserSpecified) continue;

				string fpTarget = PathManager.StandardFirmwareName(ri.KnownFirmwareFile.RecommendedName);
				string fpSource = ri.FilePath;

				try
				{
				  File.Move(fpSource, fpTarget);
				}
				catch
				{
				  // sometimes moves fail. especially in newer versions of windows with explorers more fragile than your great-grandma.
				  // I am embarrassed that I know that. about windows, not your great-grandma.
				}
			}

			DoScan();
		}

		private void tbbOpenFolder_Click(object sender, EventArgs e)
		{
			var frmWares = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null);
			if (!Directory.Exists(frmWares))
			{
				Directory.CreateDirectory(frmWares);
			}

			System.Diagnostics.Process.Start(frmWares);
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
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = _currSelectorDir,
				RestoreDirectory = true
			};
			string firmwarePath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null);

			if (ofd.ShowDialog() == DialogResult.OK)
			{
				// remember the location we selected this firmware from, maybe there are others
				_currSelectorDir = Path.GetDirectoryName(ofd.FileName);

				try
				{
					using var hf = new HawkFile(ofd.FileName);
					// for each selected item, set the user choice (even though multiple selection for this operation is no longer allowed)
					foreach (ListViewItem lvi in lvFirmwares.SelectedItems)
					{
						var fr = lvi.Tag as FirmwareDatabase.FirmwareRecord;
						string filePath = ofd.FileName;

						// if the selected file is an archive, allow the user to pick the inside file
						// to always be copied to the global firmwares directory
						if (hf.IsArchive)
						{
							var ac = new ArchiveChooser(new HawkFile(filePath));
							int memIdx;

							if (ac.ShowDialog(this) == DialogResult.OK)
							{
								memIdx = ac.SelectedMemberIndex;
							}
							else
							{
								return;
							}

							var insideFile = hf.BindArchiveMember(memIdx);
							var fileData = insideFile.ReadAllBytes();

							// write to file in the firmwares folder
							File.WriteAllBytes(Path.Combine(firmwarePath, insideFile.Name), fileData);
							filePath = Path.Combine(firmwarePath, insideFile.Name);
						}
						else
						{
							// selected file is not an archive
							// check whether this file is currently outside of the global firmware directory
							if (_currSelectorDir != firmwarePath)
							{
								var askMoveResult = MessageBox.Show(this, "The selected custom firmware does not reside in the root of the global firmware directory.\nDo you want to copy it there?", "Import Custom Firmware", MessageBoxButtons.YesNo);
								if (askMoveResult == DialogResult.Yes)
								{
									try
									{
										var fi = new FileInfo(filePath);
										filePath = Path.Combine(firmwarePath, fi.Name);
										File.Copy(ofd.FileName, filePath);
									}
									catch (Exception ex)
									{
										MessageBox.Show(this, $"There was an issue copying the file. The customization has NOT been set.\n\n{ex.StackTrace}");
										continue;
									}
								}
							}
						}

						Global.Config.FirmwareUserSpecifications[fr.ConfigKey] = filePath;
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, $"There was an issue during the process. The customization has NOT been set.\n\n{ex.StackTrace}");
					return;
				}

				DoScan();
			}
		}

		private void tsmiClearCustomization_Click(object sender, EventArgs e)
		{
			// for each selected item, clear the user choice
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
			var fr = (FirmwareDatabase.FirmwareRecord)lvi.Tag;

			// get all options for this firmware (in order)
			var options =
				from fo in FirmwareDatabase.FirmwareOptions
				where fo.SystemId == fr.SystemId && fo.FirmwareId == fr.FirmwareId
				select fo;

			var fciDialog = new FirmwaresConfigInfo
			{
				lblFirmware =
				{
					Text = $"{fr.SystemId} : {fr.FirmwareId} ({fr.Descr})"
				}
			};

			foreach (var o in options)
			{
				ListViewItem olvi = new ListViewItem();
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				var ff = FirmwareDatabase.FirmwareFilesByHash[o.Hash];
				if (o.Status == FirmwareDatabase.FirmwareOptionStatus.Ideal)
				{
					olvi.ImageIndex = FirmwaresConfigInfo.idIdeal;
					olvi.ToolTipText = FirmwaresConfigInfo.ttIdeal;
				}
				if (o.Status == FirmwareDatabase.FirmwareOptionStatus.Acceptable)
				{
					olvi.ImageIndex = FirmwaresConfigInfo.idAcceptable;
					olvi.ToolTipText = FirmwaresConfigInfo.ttAcceptable;
				}
				if (o.Status == FirmwareDatabase.FirmwareOptionStatus.Unacceptable)
				{
					olvi.ImageIndex = FirmwaresConfigInfo.idUnacceptable;
					olvi.ToolTipText = FirmwaresConfigInfo.ttUnacceptable;
				}
				if (o.Status == FirmwareDatabase.FirmwareOptionStatus.Bad)
				{
					olvi.ImageIndex = FirmwaresConfigInfo.idBad;
					olvi.ToolTipText = FirmwaresConfigInfo.ttBad;
				}
				olvi.SubItems[0].Text = ff.Size.ToString();
				olvi.SubItems[0].Font = Font; // why doesn't this work?
				olvi.SubItems[1].Text = $"sha1:{o.Hash}";
				olvi.SubItems[1].Font = _fixedFont;
				olvi.SubItems[2].Text = ff.RecommendedName;
				olvi.SubItems[2].Font = Font; // why doesn't this work?
				olvi.SubItems[3].Text = ff.Description;
				olvi.SubItems[3].Font = Font; // why doesn't this work?
				olvi.SubItems[4].Text = ff.Info;
				olvi.SubItems[4].Font = Font; // why doesn't this work?
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
			// hide menu items that aren't appropriate for multi-select
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

			using var pathConfig = new PathConfig(_mainForm, Global.Config);
			pathConfig.ShowDialog(this);
			RefreshBasePath();
		}

		private void RefreshBasePath()
		{
			string oldBasePath = _currSelectorDir;
			linkBasePath.Text = _currSelectorDir = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null);
			if (oldBasePath != _currSelectorDir)
			{
				DoScan();
			}
		}

		private void tbbImport_Click(object sender, EventArgs e)
		{
			using var ofd = new OpenFileDialog { Multiselect = true };
			if (ofd.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			RunImportJob(ofd.FileNames);
		}

		private bool RunImportJobSingle(string basePath, string f, ref string errors)
		{
			try
			{
				var fi = new FileInfo(f);
				if (!fi.Exists)
				{
					return false;
				}

				string target = Path.Combine(basePath, fi.Name);
				if (new FileInfo(target).Exists)
				{
					// compare the files, if they're the same. don't do anything
					if (File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(f)))
					{
						return false;
					}

					// hmm they're different. import but rename it
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
				if (errors != "")
				{
					errors += "\n";
				}

				errors += f;
				return false;
			}
		}

		private void RunImportJob(IEnumerable<string> files)
		{
			bool didSomething = false;
			var basePath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null);
			string errors = "";
			foreach(var f in files)
			{
				using var hf = new HawkFile(f);
				if (hf.IsArchive)
				{
					// blech. the worst extraction code in the universe.
					string extractPath = $"{Path.GetTempFileName()}.dir";
					DirectoryInfo di = Directory.CreateDirectory(extractPath);

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
							outfile = Path.Combine(extractPath, myname);
							File.WriteAllBytes(outfile, ms.ToArray());
							hf.Unbind();

							if (_cbAllowImport.Checked || Manager.CanFileBeImported(outfile))
							{
								didSomething |= RunImportJobSingle(basePath, outfile, ref errors);
							}
						}
					}
					finally
					{
						di.Delete(true);
					}
				}
				else
				{
					if (_cbAllowImport.Checked || Manager.CanFileBeImported(hf.CanonicalFullPath))
					{
						didSomething |= RunImportJobSingle(basePath, f, ref errors);
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
			e.Set(DragDropEffects.Copy);
		}

		private void lvFirmwares_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				RunImportJob(files);
			}
		}
	}
}
