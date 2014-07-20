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
		public TasMovieMarkerList Markers {get; set; }

		public TAStudio Tastudio { get; set; }

		public MarkerControl()
		{
			InitializeComponent();
			MarkerView.QueryItemBkColor += MarkerView_QueryItemBkColor;
			MarkerView.QueryItemText += MarkerView_QueryItemText;
		}

		private void MarkerControl_Load(object sender, EventArgs e)
		{
			
		}

		public void UpdateValues()
		{
			Refresh();
		}

		private void MarkerView_QueryItemBkColor(int index, int column, ref Color color)
		{
			var prev = Markers.PreviousOrCurrent(Global.Emulator.Frame);

			if (prev != null)
			{
				if (index == Markers.IndexOf(prev))
				{
					color = Color.FromArgb(0xE0FBE0);
				}
			}
		}

		private void MarkerView_QueryItemText(int index, int column, out string text)
		{
			text = "";

			if (column == 0)
			{
				text = Markers[index].Frame.ToString();
			}
			else if (column == 1)
			{
				text = Markers[index].Message;
			}
		}

		private void AddBtn_Click(object sender, EventArgs e)
		{
			Tastudio.CallAddMarkerPopUp();
		}

		public new void Refresh()
		{
			if (MarkerView != null && Markers != null)
			{
				MarkerView.ItemCount = Markers.Count;
			}

			base.Refresh();
		}

		private void MarkerView_SelectedIndexChanged(object sender, EventArgs e)
		{
			RemoveBtn.Enabled = SelectedIndices.Any();
		}

		private void RemoveBtn_Click(object sender, EventArgs e)
		{
			SelectedMarkers.ForEach(i => Markers.Remove(i));
			Tastudio.RefreshDialog();
		}

		private IEnumerable<int> SelectedIndices
		{
			get
			{
				return MarkerView.SelectedIndices
					.OfType<int>();
			}
		}

		private List<TasMovieMarker> SelectedMarkers
		{
			get
			{
				return SelectedIndices
					.Select(index => Markers[index])
					.ToList();
			}
		}
	}
}
