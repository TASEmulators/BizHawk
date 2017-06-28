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
			gvFirmwares.GridLines = GridLines.Both;
			GridColumnCollection gridCols = gvFirmwares.Columns;
			gridCols.Add (new GridColumn(){ HeaderText = " " }); //TBD - Status column
			gridCols.Add (new GridColumn(){ HeaderText = "System", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,string>(r=>r.SystemId) } });
			gridCols.Add (new GridColumn(){ HeaderText = "ID", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,string>(r=>r.FirmwareId) } });
			gridCols.Add (new GridColumn(){ HeaderText = "Description", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,string>(r=>r.Description) } });
			gridCols.Add (new GridColumn(){ HeaderText = "Resolved With", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,string>(r=>r.ResolvedWith) } });
			gridCols.Add (new GridColumn(){ HeaderText = "Location", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,string>(r=>r.Location) } });
			gridCols.Add (new GridColumn(){ HeaderText = "Size", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,long>(r=>r.Size).Convert(r=>r.ToString(),v=>int.Parse(v)) } });
			gridCols.Add (new GridColumn(){ HeaderText = "Hash", DataCell = new TextBoxCell() { Binding=Binding.Property<FirmwareRow,string>(r=>r.Hash) } });
			ContextMenu gridContext = new ContextMenu();
			ButtonMenuItem setCustom = new ButtonMenuItem(){ Text = "Set Customization" };
			setCustom.Click += SetCustom_Click;
			ButtonMenuItem clearCustom = new ButtonMenuItem (){ Text = "Clear Customization" };
			clearCustom.Click += ClearCustom_Click;
			gridContext.Items.Add(setCustom);
			gridContext.Items.Add(clearCustom);
			gvFirmwares.ContextMenu = gridContext;
			this.Content = gvFirmwares;
		}
		private GridView gvFirmwares;
	}
}

