using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MarkerControl : UserControl
	{
		public TAStudio Tastudio { get; set; }
		public IEmulator Emulator { get; set; }

		public MarkerControl()
		{
			InitializeComponent();

			MarkerView.AllColumns.AddRange(new InputRoll.RollColumn[]
			{
				new InputRoll.RollColumn
				{
					Name = "FrameColumn",
					Text = "Frame",
					Width = 52
				},
				new InputRoll.RollColumn
				{
					Name = "LabelColumn",
					Text = "",
					Width = 139
				}
			});

			MarkerView.QueryItemBkColor += MarkerView_QueryItemBkColor;
			MarkerView.QueryItemText += MarkerView_QueryItemText;
		}

		private void MarkerControl_Load(object sender, EventArgs e)
		{

		}

		public InputRoll MarkerInputRoll { get { return MarkerView; } }

		private void MarkerView_QueryItemBkColor(int index, InputRoll.RollColumn column, ref Color color)
		{
			var prev = Tastudio.CurrentTasMovie.Markers.PreviousOrCurrent(Global.Emulator.Frame);//Temp fix

			if (prev != null && index == Tastudio.CurrentTasMovie.Markers.IndexOf(prev))
			{
				color = TAStudio.Marker_FrameCol;
			}
			else if (index < Tastudio.CurrentTasMovie.Markers.Count)
			{
				var marker = Tastudio.CurrentTasMovie.Markers[index];
				var record = Tastudio.CurrentTasMovie[marker.Frame];

				if (record.Lagged.HasValue)
				{
					if (record.Lagged.Value)
					{
						color = column.Name == "FrameColumn" ? TAStudio.LagZone_FrameCol : TAStudio.LagZone_InputLog;
					}
					else
					{
						color = column.Name == "LabelColumn" ? TAStudio.GreenZone_FrameCol : TAStudio.GreenZone_InputLog;
					}
				}
				else
				{
					color = Color.White;
				}
			}
			else
				color = Color.White;
		}

		private void MarkerView_QueryItemText(int index, InputRoll.RollColumn column, out string text)
		{
			text = "";

			if (column.Name == "FrameColumn")
			{
				text = Tastudio.CurrentTasMovie.Markers[index].Frame.ToString();
			}
			else if (column.Name == "LabelColumn")
			{
				text = Tastudio.CurrentTasMovie.Markers[index].Message;
			}
		}

		private void AddBtn_Click(object sender, EventArgs e)
		{
			Tastudio.CallAddMarkerPopUp();
			MarkerView_SelectedIndexChanged(sender, e);
		}

		public void UpdateValues()
		{
			if (MarkerView != null &&
				Tastudio != null &&
				Tastudio.CurrentTasMovie != null &&
				Tastudio.CurrentTasMovie.Markers != null)
			{
				MarkerView.RowCount = Tastudio.CurrentTasMovie.Markers.Count;
			}

			MarkerView.Refresh();
		}

		private void MarkerView_SelectedIndexChanged(object sender, EventArgs e)
		{
			RemoveBtn.Enabled = MarkerView.SelectedRows.Any(i => i < Tastudio.CurrentTasMovie.Markers.Count);
		}

		private void RemoveBtn_Click(object sender, EventArgs e)
		{
			SelectedMarkers.ForEach(i => Tastudio.RemoveMarker(i));
			MarkerInputRoll.DeselectAll();
			Tastudio.RefreshDialog();
			MarkerView_SelectedIndexChanged(sender, e);
		}

		private List<TasMovieMarker> SelectedMarkers
		{
			get
			{
				return MarkerView.SelectedRows
					.Select(index => Tastudio.CurrentTasMovie.Markers[index])
					.ToList();
			}
		}

		private void MarkerView_ItemActivate(object sender, EventArgs e)
		{
			Tastudio.GoToMarker(SelectedMarkers.First());
		}

		// SuuperW: Marker renaming can be done with a right-click.
		// A much more useful feature would be to easily jump to it.
		private void MarkerView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (MarkerView.CurrentCell != null && MarkerView.CurrentCell.RowIndex.HasValue &&
				MarkerView.CurrentCell.RowIndex < MarkerView.RowCount)
			{
				var marker = Tastudio.CurrentTasMovie.Markers[MarkerView.CurrentCell.RowIndex.Value];
				Tastudio.GoToFrame(marker.Frame);
			}
		}

		public void EditMarker()
		{
			if (MarkerView.SelectedRows.Any())
			{
				var index = MarkerView.SelectedRows.First();
				var marker = Tastudio.CurrentTasMovie.Markers[index];
				Tastudio.CallEditMarkerPopUp(marker);
			}
		}

		public void AddMarker()
		{
			AddBtn_Click(null, null);
		}

		public void RemoveMarker()
		{
			if (RemoveBtn.Enabled)
			{
				RemoveBtn_Click(null, null);
			}
		}
	}
}
