using System;
using Eto.Forms;
using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using System.Collections.Generic;

namespace BizHawk.Client.EtoHawk
{
	internal class FirmwareRow
	{
		public FirmwareDatabase.FirmwareRecord Record { get; set; }
		public string SystemId { get { return Record.systemId; } }
		public string FirmwareId { get { return Record.firmwareId; } }
		public string Description { get { return Record.descr; } }
		public string ResolvedWith { get; set; }
		public string Location { get; set; }
		public long Size { get; set; }
		public string Hash { get; set; }
	}

	public partial class FirmwaresConfig  : Dialog<bool>
	{
		public FirmwaresConfig ()
		{
			InitializeComponent();
			FirmwaresConfig_Load ();
		}

		private void FirmwaresConfig_Load()
		{
			//we'll use this font for displaying the hash, so they dont look all jagged in a long list
			/*fixedFont = new Font(new FontFamily("Courier New"), 8);
			boldFont = new Font(lvFirmwares.Font, FontStyle.Bold);
			boldFixedFont = new Font(fixedFont, FontStyle.Bold);*/

			//populate listview from firmware DB
			//var groups = new Dictionary<string, ListViewGroup>();
			List<FirmwareRow> firmwareList = new List<FirmwareRow>();
			foreach (FirmwareDatabase.FirmwareRecord fr in FirmwareDatabase.FirmwareRecords)
			{
				var lvi = new FirmwareRow();
				lvi.Record = fr;
				firmwareList.Add(lvi);

				//build the groups in the listview as we go:
				/*if (!groups.ContainsKey(fr.systemId))
				{
					string name;
					if (!SystemGroupNames.TryGetValue(fr.systemId, out name))
						name = "FIX ME (FirmwaresConfig.cs)";
					lvFirmwares.Groups.Add(fr.systemId, name);
					var lvg = lvFirmwares.Groups[lvFirmwares.Groups.Count - 1];
					groups[fr.systemId] = lvg;
				}
				lvi.Group = groups[fr.systemId];*/
			}
			gvFirmwares.DataStore = firmwareList;

			//now that we have some items in the listview, we can size some columns to sensible widths
			/*lvFirmwares.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);

			if (TargetSystem != null)
			{
				WarpToSystemId(TargetSystem);
			}

			RefreshBasePath();*/
		}

		private void SetCustom_Click (object sender, EventArgs e)
		{
			//Unfortunately, Eto seems to have no way to tell where you right-clicked on a grid via the context menu.
			//I will probably get rid of the context menu and just add set/clear buttons to a toolbar, but not right now.
			if (gvFirmwares.SelectedItem != null && gvFirmwares.SelectedItem is FirmwareRow)
			{
				OpenFileDialog ofd = new OpenFileDialog();
				if (ofd.ShowDialog(this) == DialogResult.Ok) 
				{
					FirmwareRow fr = (FirmwareRow)gvFirmwares.SelectedItem;
					Global.Config.FirmwareUserSpecifications[fr.Record.ConfigKey] = ofd.FileName;
					DoScan();
				}
			}
		}

		private void ClearCustom_Click (object sender, EventArgs e)
		{
			if (gvFirmwares.SelectedItem != null && gvFirmwares.SelectedItem is FirmwareRow) 
			{
				FirmwareRow fr = (FirmwareRow)gvFirmwares.SelectedItem;
				Global.Config.FirmwareUserSpecifications.Remove(fr.Record.ConfigKey);
				DoScan();
			}
		}

		FirmwareManager Manager { get { return Global.FirmwareManager; } }
		private void DoScan()
		{
			//lvFirmwares.BeginUpdate();
			Manager.DoScanAndResolve();

			//for each type of firmware, try resolving and record the result
			foreach (FirmwareRow lvi in gvFirmwares.DataStore)
			{
				var fr = lvi.Record;
				var ri = Manager.Resolve(fr, true);

				if (ri == null)
				{
					//lvi.Description = "Missing!";
				}
				else
				{
					//lazy substring extraction. really should do a better job
					var basePath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null) + System.IO.Path.DirectorySeparatorChar;

					var path = ri.FilePath.Replace(basePath, "");

					//bolden the item if the user has specified a path for it
					bool bolden = ri.UserSpecified;

					//set columns based on whether it was a known file
					if (ri.KnownFirmwareFile == null)
					{
						//lvi.ImageIndex = idUnsure;
						//lvi.Description = "-custom-";
					}
					else
					{
						//lvi.ImageIndex = idOk;
						//lvi.Description = ri.KnownFirmwareFile.descr;
					}

					//if the user specified a file but its missing, mark it as such
					/*if (ri.Missing)
					{
						lvi.ImageIndex = idMissing;
						lvi.ToolTipText = "Missing!";
					}*/

					//if the user specified a known firmware file but its for some other firmware, it was probably a mistake. mark it as suspicious
					/*if (ri.KnownMismatching)
						lvi.ImageIndex = idUnsure;*/

					lvi.Location = path;

					lvi.Size = ri.Size;

					if (ri.Hash != null) lvi.Hash = "sha1:" + ri.Hash;
					else lvi.Hash = "";
				}
			}

			//lvFirmwares.EndUpdate();
		}
	}
}

