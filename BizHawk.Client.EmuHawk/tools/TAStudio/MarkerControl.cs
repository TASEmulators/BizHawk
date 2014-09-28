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

			MarkerView.Columns.AddRange(new InputRoll.RollColumn[]
			{
				new InputRoll.RollColumn
				{
					Name = "FrameColumn",
					Text = "Frame",
					Width = 64
				},
				new InputRoll.RollColumn
				{
					Name = "LabelColumn",
					Text = "Label",
					Width = 133
				}
			});

			MarkerView.QueryItemBkColor += MarkerView_QueryItemBkColor;
			MarkerView.QueryItemText += MarkerView_QueryItemText;
		}

		private void MarkerControl_Load(object sender, EventArgs e)
		{
			
		}

		private void MarkerView_QueryItemBkColor(int index, int column, ref Color color)
		{
			var prev = Tastudio.CurrentMovie.Markers.PreviousOrCurrent(Global.Emulator.Frame);

			if (prev != null && index == Tastudio.CurrentMovie.Markers.IndexOf(prev))
			{
				color = TAStudio.Marker_FrameCol;
			}
			else if (index < Tastudio.CurrentMovie.InputLogLength)
			{
				var record = Tastudio.CurrentMovie[index];
				if (record.HasState && record.Lagged.HasValue)
				{
					if (record.Lagged.Value)
					{
						color = column == 0 ? TAStudio.LagZone_FrameCol : TAStudio.LagZone_InputLog;
					}
					else
					{
						color = column == 0 ? TAStudio.GreenZone_FrameCol : TAStudio.GreenZone_InputLog;
					}
				}
				else
				{
					color = Color.White;
				}
			}
			
		}

		private void MarkerView_QueryItemText(int index, int column, out string text)
		{
			text = "";

			if (column == 0)
			{
				text = Tastudio.CurrentMovie.Markers[index].Frame.ToString();
			}
			else if (column == 1)
			{
				text = Tastudio.CurrentMovie.Markers[index].Message;
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
				Tastudio.CurrentMovie != null &&
				Tastudio.CurrentMovie.Markers != null)
			{
				MarkerView.RowCount = Tastudio.CurrentMovie.Markers.Count;
			}

			MarkerView.Refresh();
		}

		private void MarkerView_SelectedIndexChanged(object sender, EventArgs e)
		{
			RemoveBtn.Enabled = SelectedIndices.Any(i => i < Tastudio.CurrentMovie.Markers.Count);
		}

		private void RemoveBtn_Click(object sender, EventArgs e)
		{
			SelectedMarkers.ForEach(i => Tastudio.CurrentMovie.Markers.Remove(i));
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
					.Select(index => Tastudio.CurrentMovie.Markers[index])
					.ToList();
			}
		}

		private void MarkerView_ItemActivate(object sender, EventArgs e)
		{
			Tastudio.GoToMarker(SelectedMarkers.First());
		}
	}
}
