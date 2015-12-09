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
		public int Size { get; set; }
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
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
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

	}
}

