using System;
using Eto.Forms;

namespace BizHawk.Client.EtoHawk
{
	public partial class FirmwaresConfig
	{
		private void InitializeComponent()
		{
			gvFirmwares = new GridView();
			gvFirmwares.Size = new Eto.Drawing.Size (600, 400);
			GridColumnCollection gridCols = gvFirmwares.Columns;
			gridCols.Add (new GridColumn (){ HeaderText = " " }); //TBD - Status column
			gridCols.Add (new GridColumn (){ HeaderText = "System", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,string>(r=>r.SystemId) } });
			gridCols.Add (new GridColumn (){ HeaderText = "ID", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,string>(r=>r.FirmwareId) } });
			gridCols.Add (new GridColumn (){ HeaderText = "Description", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,string>(r=>r.Description) } });
			gridCols.Add (new GridColumn (){ HeaderText = "Resolved With" });
			gridCols.Add (new GridColumn (){ HeaderText = "Location" });
			gridCols.Add (new GridColumn (){ HeaderText = "Size" });
			gridCols.Add (new GridColumn (){ HeaderText = "Hash" });

			this.Content = gvFirmwares;
		}
		private GridView gvFirmwares;
	}
}

