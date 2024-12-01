using System.Drawing;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class UndoHistoryForm : Form
	{
		private const string IdColumnName = "ID";
		private const string UndoColumnName = "Undo Step";
		
		private readonly TAStudio _tastudio;
		private string _lastUndoAction;
		private IMovieChangeLog Log => _tastudio.CurrentTasMovie.ChangeLog;

		public UndoHistoryForm(TAStudio owner)
		{
			InitializeComponent();
			_tastudio = owner;

			HistoryView.QueryItemText += HistoryView_QueryItemText;
			HistoryView.QueryItemBkColor += HistoryView_QueryItemBkColor;

			HistoryView.AllColumns.Clear();
			HistoryView.AllColumns.Add(new(name: IdColumnName, widthUnscaled: 40, text: IdColumnName));
			HistoryView.AllColumns.Add(new(name: UndoColumnName, widthUnscaled: 280, text: UndoColumnName));

			MaxStepsNum.Value = Log.MaxSteps;
		}

		private void HistoryView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = column.Name == UndoColumnName
				? Log.Names[index]
				: index.ToString();
		}

		private void HistoryView_QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			if (index == Log.UndoIndex)
			{
				color = _tastudio.Palette.GreenZone_InputLog;
			}
			else if (index > Log.UndoIndex)
			{
				color = _tastudio.Palette.LagZone_InputLog;
			}
		}

		public void UpdateValues()
		{
			HistoryView.RowCount = Log.Names.Count;
			if (AutoScrollCheck.Checked && _lastUndoAction != Log.NextUndoStepName)
			{
				HistoryView.ScrollToIndex(Log.UndoIndex);
				HistoryView.DeselectAll();
				HistoryView.SelectRow(Log.UndoIndex - 1, true);
			}

			_lastUndoAction = Log.NextUndoStepName;

			HistoryView.Refresh();
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			Log.Clear();
			UpdateValues();
		}

		private void UndoButton_Click(object sender, EventArgs e)
		{
			_tastudio.UndoExternal();
			_tastudio.RefreshDialog();
		}

		private void RedoButton_Click(object sender, EventArgs e)
		{
			_tastudio.RedoExternal();
			_tastudio.RefreshDialog();
		}

		private int SelectedItem
			=> HistoryView.AnyRowsSelected ? HistoryView.FirstSelectedRowIndex : -1;

		private void UndoToHere(int index)
		{
			int earliestFrame = int.MaxValue;
			while (Log.UndoIndex > index)
			{
				int frame = Log.Undo();
				if (frame < earliestFrame)
					earliestFrame = frame;
			}

			UpdateValues();

			// potentially rewind, then update display for TAStudio
			if (_tastudio.Emulator.Frame > earliestFrame)
				_tastudio.GoToFrame(earliestFrame);
			_tastudio.RefreshDialog();
		}

		private void HistoryView_DoubleClick(object sender, EventArgs e)
		{
			UndoToHere(SelectedItem);
		}

		private void HistoryView_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				RightClickMenu.Show(HistoryView, e.X, e.Y);
			}
			else if (e.Button == MouseButtons.Left)
			{
				if (SelectedItem == -1)
				{
					HistoryView.SelectRow(_hackSelect, true);
				}
			}
		}

		// Hacky way to select a row by clicking the names row
		private int _hackSelect = -1;

		private void HistoryView_MouseDown(object sender, MouseEventArgs e)
		{
			_hackSelect = SelectedItem;
		}

		private void UndoHereMenuItem_Click(object sender, EventArgs e)
		{
			UndoToHere(SelectedItem);
		}

		private void RedoHereMenuItem_Click(object sender, EventArgs e)
		{
			int earliestFrame = int.MaxValue;
			while (Log.UndoIndex < SelectedItem)
			{
				int frame = Log.Redo();
				if (earliestFrame < frame)
					earliestFrame = frame;
			}

			UpdateValues();

			// potentially rewind, then update display for TAStudio
			if (_tastudio.Emulator.Frame > earliestFrame)
				_tastudio.GoToFrame(earliestFrame);
			_tastudio.RefreshDialog();
		}

		private void ClearHistoryToHereMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedItem != -1)
			{
				Log.Clear(SelectedItem);
			}

			UpdateValues();
		}

		private void MaxStepsNum_ValueChanged(object sender, EventArgs e)
		{
			_tastudio.Settings.MaxUndoSteps = Log.MaxSteps = (int)MaxStepsNum.Value;
		}
	}
}
