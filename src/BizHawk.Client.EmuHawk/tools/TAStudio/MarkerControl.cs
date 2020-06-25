using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;

namespace BizHawk.Client.EmuHawk
{
	public partial class MarkerControl : UserControl
	{
		public TAStudio Tastudio { get; set; }
		public TasMovieMarkerList Markers => Tastudio.CurrentTasMovie.Markers;

		public MarkerControl()
		{
			InitializeComponent();
			JumpToMarkerToolStripMenuItem.Image = Resources.JumpTo;
			ScrollToMarkerToolStripMenuItem.Image = Resources.ScrollTo;
			EditMarkerToolStripMenuItem.Image = Resources.pencil;
			AddMarkerToolStripMenuItem.Image = Resources.add;
			RemoveMarkerToolStripMenuItem.Image = Resources.Delete;
			JumpToMarkerButton.Image = Resources.JumpTo;
			EditMarkerButton.Image = Resources.pencil;
			AddMarkerButton.Image = Resources.add;
			RemoveMarkerButton.Image = Resources.Delete;
			ScrollToMarkerButton.Image = Resources.ScrollTo;
			AddMarkerWithTextButton.Image = Resources.AddEdit;
			SetupColumns();
			MarkerView.QueryItemBkColor += MarkerView_QueryItemBkColor;
			MarkerView.QueryItemText += MarkerView_QueryItemText;
		}

		private void SetupColumns()
		{
			MarkerView.AllColumns.Clear();
			MarkerView.AllColumns.AddRange(new[]
			{
				new RollColumn
				{
					Name = "FrameColumn",
					Text = "Frame",
					UnscaledWidth = 52
				},
				new RollColumn
				{
					Name = "LabelColumn",
					Text = "",
					UnscaledWidth = 125
				}
			});
		}

		public InputRoll MarkerInputRoll => MarkerView;

		private void MarkerView_QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			var prev = Markers.PreviousOrCurrent(Tastudio.Emulator.Frame);

			if (prev != null && index == Markers.IndexOf(prev))
			{
				// feos: taseditor doesn't have it, so we're free to set arbitrary color scheme. and I prefer consistency
				color = TAStudio.CurrentFrame_InputLog;
			}
			else if (index < Markers.Count)
			{
				var marker = Markers[index];
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
			}
		}

		private void MarkerView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";

			if (column.Name == "FrameColumn")
			{
				text = Markers[index].Frame.ToString();
			}
			else if (column.Name == "LabelColumn")
			{
				text = Markers[index].Message;
			}
		}

		private void MarkerContextMenu_Opening(object sender, CancelEventArgs e)
		{
			EditMarkerToolStripMenuItem.Enabled =
				RemoveMarkerToolStripMenuItem.Enabled =
				MarkerInputRoll.AnyRowsSelected && MarkerView.SelectedRows.First() != 0;

			JumpToMarkerToolStripMenuItem.Enabled =
				ScrollToMarkerToolStripMenuItem.Enabled =
				MarkerInputRoll.AnyRowsSelected;
		}

		private void ScrollToMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected)
			{
				Tastudio.SetVisibleFrame(SelectedMarkerFrame());
				Tastudio.RefreshDialog();
			}
		}

		private void JumpToMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected)
			{
				var index = MarkerView.SelectedRows.First();
				var marker = Markers[index];
				Tastudio.GoToFrame(marker.Frame);
			}
		}

		private void EditMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected)
			{
				var index = MarkerView.SelectedRows.First();
				var marker = Markers[index];
				EditMarkerPopUp(marker);
			}
		}

		private void AddMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddMarker();
			MarkerView_SelectedIndexChanged(null, null);
		}

		private void AddMarkerWithTextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddMarker(editText: true);
			MarkerView_SelectedIndexChanged(null, null);
		}

		private void RemoveMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected)
			{
				SelectedMarkers.ForEach(i => Markers.Remove(i));
				ShrinkSelection();
				Tastudio.RefreshDialog();
				MarkerView_SelectedIndexChanged(null, null);
			}
		}

		// feos: not the same as InputRoll.TruncateSelection(), since multiple selection of markers is forbidden
		// moreover, when the last marker is removed, we need its selection to move to the previous marker
		// still iterate, so things don't break if multiple selection is allowed someday
		public void ShrinkSelection()
		{
			if (MarkerView.AnyRowsSelected)
			{
				while (MarkerView.SelectedRows.Last() > Markers.Count - 1)
				{
					MarkerView.SelectRow(Markers.Count, false);
					MarkerView.SelectRow(Markers.Count - 1, true);
				}
			}

		}

		public void AddMarker(bool editText = false, int? frame = null)
		{
			// feos: we specify the selected frame if we call this from TasView, otherwise marker should be added to the emulated frame
			var markerFrame = frame ?? Tastudio.Emulator.Frame;

			if (editText)
			{
				var i = new InputPrompt
				{
					Text = $"Marker for frame {markerFrame}",
					TextInputType = InputPrompt.InputType.Text,
					Message = "Enter a message",
					InitialValue =
						Markers.IsMarker(markerFrame) ?
						Markers.PreviousOrCurrent(markerFrame).Message :
						""
				};

				var point = Cursor.Position;
				point.Offset(i.Width / -2, i.Height / -2);

				var result = i.ShowHawkDialog(position: point);
				if (result == DialogResult.OK)
				{
					Markers.Add(new TasMovieMarker(markerFrame, i.PromptText));
					UpdateTextColumnWidth();
					UpdateValues();
				}
			}
			else
			{
				Markers.Add(new TasMovieMarker(markerFrame));
				UpdateValues();
			}

			MarkerView.ScrollToIndex(Markers.Count - 1);
			Tastudio.RefreshDialog();
		}

		public void UpdateTextColumnWidth()
		{
			if (Markers.Any())
			{
				var longestBranchText = Markers
					.OrderBy(b => b.Message?.Length ?? 0)
					.Last()
					.Message;

				MarkerView.ExpandColumnToFitText("LabelColumn", longestBranchText);
			}
		}

		public void EditMarkerPopUp(TasMovieMarker marker, bool followCursor = false)
		{
			var markerFrame = marker.Frame;
			var point = default(Point);
			var i = new InputPrompt
			{
				Text = $"Marker for frame {markerFrame}",
				TextInputType = InputPrompt.InputType.Text,
				Message = "Enter a message",
				InitialValue =
					Markers.IsMarker(markerFrame)
					? Markers.PreviousOrCurrent(markerFrame).Message
					: ""
			};

			if (followCursor)
			{
				point = Cursor.Position;
				point.Offset(i.Width / -2, i.Height / -2);
			}

			var result = i.ShowHawkDialog(position: point);

			if (result == DialogResult.OK)
			{
				marker.Message = i.PromptText;
				UpdateTextColumnWidth();
				UpdateValues();
			}
		}

		public void UpdateValues()
		{
			if (MarkerView != null && Tastudio?.CurrentTasMovie != null && Markers != null)
			{
				MarkerView.RowCount = Markers.Count;
			}
		}

		public void Restart()
		{
			SetupColumns();
			MarkerView.RowCount = Markers.Count;
			MarkerView.Refresh();
		}

		private void MarkerView_SelectedIndexChanged(object sender, EventArgs e)
		{
			EditMarkerButton.Enabled =
				RemoveMarkerButton.Enabled =
				MarkerInputRoll.AnyRowsSelected && MarkerView.SelectedRows.First() != 0;

			JumpToMarkerButton.Enabled =
				ScrollToMarkerButton.Enabled =
				MarkerInputRoll.AnyRowsSelected;
		}

		private List<TasMovieMarker> SelectedMarkers => MarkerView
			.SelectedRows
			.Select(index => Markers[index])
			.ToList();
		

		// SuuperW: Marker renaming can be done with a right-click.
		// A much more useful feature would be to easily jump to it.
		private void MarkerView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (MarkerView.CurrentCell?.RowIndex != null && MarkerView.CurrentCell.RowIndex < MarkerView.RowCount)
			{
				var marker = Markers[MarkerView.CurrentCell.RowIndex.Value];
				Tastudio.GoToFrame(marker.Frame);
			}
		}

		public int SelectedMarkerFrame()
		{
			if (MarkerView.AnyRowsSelected)
			{
				var index = MarkerView.SelectedRows.First();
				var marker = Markers[index];

				return marker.Frame;
			}

			return -1;
		}

		private void MarkerView_MouseClick(object sender, MouseEventArgs e)
		{
			MarkerContextMenu.Close();
		}
	}
}
