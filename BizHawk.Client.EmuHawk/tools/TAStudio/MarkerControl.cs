using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MarkerControl : UserControl
	{
		public TAStudio Tastudio { get; set; }

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

		private void MarkerView_QueryItemBkColor(int index, InputRoll.RollColumn column, ref Color color)
		{
			var prev = Tastudio.CurrentTasMovie.Markers.PreviousOrCurrent(Global.Emulator.Frame);

			if (prev != null && index == Tastudio.CurrentTasMovie.Markers.IndexOf(prev))
			{
				color = TAStudio.Marker_FrameCol;
			}
			else if (index < Tastudio.CurrentTasMovie.InputLogLength)
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
			RemoveBtn.Enabled = SelectedIndices.Any(i => i < Tastudio.CurrentTasMovie.Markers.Count);
		}

		private void RemoveBtn_Click(object sender, EventArgs e)
		{
			SelectedMarkers.ForEach(i => Tastudio.CurrentTasMovie.Markers.Remove(i));
			Tastudio.RefreshDialog();
			MarkerView_SelectedIndexChanged(sender, e);
		}

		private IEnumerable<int> SelectedIndices
		{
			get
			{
				return MarkerView.SelectedRows
					.OfType<int>();
			}
		}

		private List<TasMovieMarker> SelectedMarkers
		{
			get
			{
				return SelectedIndices
					.Select(index => Tastudio.CurrentTasMovie.Markers[index])
					.ToList();
			}
		}

		private void MarkerView_ItemActivate(object sender, EventArgs e)
		{
			Tastudio.GoToMarker(SelectedMarkers.First());
		}
	}
}
