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
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class MarkerControl : UserControl
	{
		public TAStudio Tastudio { get; set; }
		public IEmulator Emulator { get; set; }
		public TasMovieMarkerList Markers { get { return Tastudio.CurrentTasMovie.Markers; } }

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
					Width = 125
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
			var prev = Markers.PreviousOrCurrent(0);

			if (this.Emulator!=null) //Temp fix
				prev = Markers.PreviousOrCurrent(Emulator.Frame);
				
			if (prev != null && index == Markers.IndexOf(prev))
			{
				color = TAStudio.Marker_FrameCol;
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
				else
				{
					color = Color.White;
				}
			}
			else
				color = Color.White;
		}

		private void MarkerView_QueryItemText(int index, InputRoll.RollColumn column, out string text, ref int offsetX, ref int offsetY)
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
			JumpToMarkerToolStripMenuItem.Enabled =
			ScrollToMarkerToolStripMenuItem.Enabled =
				MarkerInputRoll.AnyRowsSelected && MarkerView.SelectedRows.First() != 0;
		}

		private void ScrollToMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected)
			{
				Tastudio.SetVisibleIndex(SelectedMarkerFrame());
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
				MarkerInputRoll.DeselectAll();
				Tastudio.RefreshDialog();
				MarkerView_SelectedIndexChanged(null, null);
			}
		}

		public void AddMarker(bool editText = false, int? frame = null)
		{
			// feos: we specify the selected frame if we call this from TasView, otherwise marker should be added to the emulated frame
			var markerFrame = frame ?? Emulator.Frame;

			if (editText)
			{
				InputPrompt i = new InputPrompt
				{
					Text = "Marker for frame " + markerFrame,
					TextInputType = InputPrompt.InputType.Text,
					Message = "Enter a message",
					InitialValue =
						Markers.IsMarker(markerFrame) ?
						Markers.PreviousOrCurrent(markerFrame).Message :
						""
				};
				var result = i.ShowHawkDialog();
				if (result == DialogResult.OK)
				{
					Markers.Add(new TasMovieMarker(markerFrame, i.PromptText));
					UpdateValues();
				}
			}
			else
			{
				Markers.Add(new TasMovieMarker(markerFrame, ""));
				UpdateValues();
			}
			Tastudio.RefreshDialog();
		}

		public void EditMarkerPopUp(TasMovieMarker marker)
		{
			var markerFrame = marker.Frame;
			InputPrompt i = new InputPrompt
			{
				Text = "Marker for frame " + markerFrame,
				TextInputType = InputPrompt.InputType.Text,
				Message = "Enter a message",
				InitialValue =
					Markers.IsMarker(markerFrame) ?
					Markers.PreviousOrCurrent(markerFrame).Message :
					""
			};

			var result = i.ShowHawkDialog();

			if (result == DialogResult.OK)
			{
				marker.Message = i.PromptText;
				UpdateValues();
			}
		}

		public void UpdateValues()
		{
			if (MarkerView != null &&
				Tastudio != null &&
				Tastudio.CurrentTasMovie != null &&
				Markers != null)
			{
				MarkerView.RowCount = Markers.Count;
			}

			MarkerView.Refresh();
		}

		public void Restart()
		{
			MarkerView.DeselectAll();
			UpdateValues();
		}

		private void MarkerView_SelectedIndexChanged(object sender, EventArgs e)
		{
			EditMarkerButton.Enabled =
			RemoveMarkerButton.Enabled =
			JumpToMarkerButton.Enabled =
			ScrollToMarkerButton.Enabled =
				MarkerInputRoll.AnyRowsSelected && MarkerView.SelectedRows.First() != 0;
		}

		private List<TasMovieMarker> SelectedMarkers
		{
			get
			{
				return MarkerView.SelectedRows
					.Select(index => Markers[index])
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
