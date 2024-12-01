using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;

namespace BizHawk.Client.EmuHawk
{
	public partial class MarkerControl : UserControl, IDialogParent
	{
		public TAStudio Tastudio { get; set; }
		public TasMovieMarkerList Markers => Tastudio.CurrentTasMovie.Markers;

		public IDialogController DialogController => Tastudio.MainForm;

		private TasMovieMarker FirstSelectedMarker
			=> Markers[MarkerView.FirstSelectedRowIndex];

		public MarkerControl()
		{
			InitializeComponent();
			JumpToMarkerToolStripMenuItem.Image = Resources.JumpTo;
			ScrollToMarkerToolStripMenuItem.Image = Resources.ScrollTo;
			EditMarkerToolStripMenuItem.Image = Resources.Pencil;
			EditMarkerFrameToolStripMenuItem.Image = Resources.Clock;
			AddMarkerToolStripMenuItem.Image = Resources.Add;
			RemoveMarkerToolStripMenuItem.Image = Resources.Delete;
			JumpToMarkerButton.Image = Resources.JumpTo;
			EditMarkerButton.Image = Resources.Pencil;
			EditMarkerFrameButton.Image = Resources.Clock;
			AddMarkerButton.Image = Resources.Add;
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
			MarkerView.AllColumns.Add(new(name: "FrameColumn", widthUnscaled: 52, text: "Frame"));
			MarkerView.AllColumns.Add(new(name: "LabelColumn", widthUnscaled: 125, text: string.Empty));
		}

		public InputRoll MarkerInputRoll => MarkerView;

		private void MarkerView_QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			// This could happen if the control is told to redraw while Tastudio is rebooting, as we would not have a TasMovie just yet
			if (Tastudio.CurrentTasMovie is null) return;

			if (index >= Markers.Count) return; // this should never happen

			var marker = Markers[index];
			var prev = Markers.PreviousOrCurrent(Tastudio.Emulator.Frame);

			if (ReferenceEquals(marker, prev))
			{
				// feos: taseditor doesn't have it, so we're free to set arbitrary color scheme. and I prefer consistency
				color = Tastudio.Palette.CurrentFrame_InputLog;
			}
			else if (Tastudio.CurrentTasMovie.LagLog[marker.Frame + 1] is bool lagged)
			{
				if (lagged)
				{
					color = column.Name == "FrameColumn"
						? Tastudio.Palette.LagZone_FrameCol
						: Tastudio.Palette.LagZone_InputLog;
				}
				else
				{
					color = column.Name == "LabelColumn"
						? Tastudio.Palette.GreenZone_FrameCol
						: Tastudio.Palette.GreenZone_InputLog;
				}
			}
		}

		private void MarkerView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";

			// This could happen if the control is told to redraw while Tastudio is rebooting, as we would not have a TasMovie just yet
			if (Tastudio.CurrentTasMovie == null)
			{
				return;
			}

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
				EditMarkerFrameToolStripMenuItem.Enabled =
				RemoveMarkerToolStripMenuItem.Enabled =
					MarkerInputRoll.AnyRowsSelected && MarkerView.FirstSelectedRowIndex is not 0;

			JumpToMarkerToolStripMenuItem.Enabled =
				ScrollToMarkerToolStripMenuItem.Enabled =
				MarkerInputRoll.AnyRowsSelected;
		}

		private void ScrollToMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected)
			{
				Tastudio.SetVisibleFrame(FirstSelectedMarker.Frame);
				Tastudio.RefreshDialog();
			}
		}

		private void JumpToMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected) Tastudio.GoToFrame(FirstSelectedMarker.Frame);
		}

		private void EditMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected) EditMarkerPopUp(FirstSelectedMarker);
		}

		private void EditMarkerFrameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected) EditMarkerFramePopUp(FirstSelectedMarker);
		}

		private void AddMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddMarker(Tastudio.Emulator.Frame);
		}

		private void AddMarkerWithTextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddMarker(Tastudio.Emulator.Frame, true);
		}

		private void RemoveMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!MarkerView.AnyRowsSelected) return;
			foreach (var i in MarkerView.SelectedRows.Select(index => Markers[index]).ToList()) Markers.Remove(i);
			MarkerView.RowCount = Markers.Count;
			Tastudio.RefreshDialog();
		}

		public void UpdateMarkerCount()
		{
			MarkerView.RowCount = Markers.Count;
		}

		public void AddMarker(int frame, bool editText = false)
		{
			TasMovieMarker marker;
			if (editText)
			{
				var i = new InputPrompt
				{
					Text = $"Marker for frame {frame}",
					TextInputType = InputPrompt.InputType.Text,
					Message = "Enter a message",
					InitialValue =
						Markers.IsMarker(frame) ?
						Markers.PreviousOrCurrent(frame).Message :
						""
				};

				i.FollowMousePointer();
				if (!i.ShowDialogOnScreen().IsOk()) return;

				UpdateTextColumnWidth();
				marker = new TasMovieMarker(frame, i.PromptText);
			}
			else
			{
				marker = new TasMovieMarker(frame);
			}

			UpdateValues();
			Markers.Add(marker);
			var index = Markers.IndexOf(marker);
			MarkerView.MakeIndexVisible(index);
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

		public void EditMarkerPopUp(TasMovieMarker marker)
		{
			var markerFrame = marker.Frame;
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

			i.FollowMousePointer();
			if (!i.ShowDialogOnScreen().IsOk()) return;

			marker.Message = i.PromptText;
			UpdateTextColumnWidth();
			UpdateValues();
		}

		public void EditMarkerFramePopUp(TasMovieMarker marker)
		{
			var markerFrame = marker.Frame;
			var i = new InputPrompt
			{
				Text = $"Marker for frame {markerFrame}",
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Enter a frame number",
				InitialValue = Markers.IsMarker(markerFrame)
					? Markers.PreviousOrCurrent(markerFrame).Frame.ToString()
					: "0",
			};

			i.FollowMousePointer();
			if (!i.ShowDialogOnScreen().IsOk()
				|| !int.TryParse(i.PromptText, out var promptValue)
				|| Markers.IsMarker(promptValue)) // don't move to frame with an existing marker
			{
				return;
			}
			Markers.Move(marker.Frame, promptValue);
			UpdateTextColumnWidth();
			UpdateValues();
			Tastudio.RefreshDialog();
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
				EditMarkerFrameButton.Enabled =
				RemoveMarkerButton.Enabled =
					MarkerInputRoll.AnyRowsSelected && MarkerView.FirstSelectedRowIndex is not 0;

			JumpToMarkerButton.Enabled =
				ScrollToMarkerButton.Enabled =
				MarkerInputRoll.AnyRowsSelected;
		}

		// SuuperW: Marker renaming can be done with a right-click.
		// A much more useful feature would be to easily jump to it.
		private void MarkerView_MouseDoubleClick(object sender, EventArgs e)
		{
			if (MarkerView.AnyRowsSelected) Tastudio.GoToFrame(FirstSelectedMarker.Frame);
		}
	}
}
