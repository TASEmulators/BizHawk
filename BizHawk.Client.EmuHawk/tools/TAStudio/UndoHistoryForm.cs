using System;
using System.Drawing;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class UndoHistoryForm : Form
	{
		private readonly TAStudio _tastudio;
		private string _lastUndoAction;
		private TasMovieChangeLog Log => _tastudio.CurrentTasMovie.ChangeLog;

		public UndoHistoryForm(TAStudio owner)
		{
			InitializeComponent();
			_tastudio = owner;

			HistoryView.QueryItemText += HistoryView_QueryItemText;
			HistoryView.QueryItemBkColor += HistoryView_QueryItemBkColor;

			HistoryView.Columns[1].Width = 280;

			MaxStepsNum.Value = Log.MaxSteps;
		}

		private void HistoryView_QueryItemText(int row, int column, out string text)
		{
			text = column == 1
				? Log.Names[row]
				: row.ToString();
		}

		private void HistoryView_QueryItemBkColor(int row, int column, ref Color color)
		{
			if (column == 0)
			{
				return;
			}

			if (row == Log.UndoIndex)
			{
				color = TAStudio.GreenZone_InputLog;
			}
			else if (row > Log.UndoIndex)
			{
				color = TAStudio.LagZone_InputLog;
			}
		}

		public void UpdateValues()
		{
			HistoryView.ItemCount = Log.Names.Count;
			if (AutoScrollCheck.Checked && _lastUndoAction != Log.NextUndoStepName)
			{
				HistoryView.ensureVisible(Log.UndoIndex);
				HistoryView.clearSelection();
				HistoryView.SelectItem(Log.UndoIndex - 1, true);
			}

			_lastUndoAction = Log.NextUndoStepName;

			HistoryView.Refresh();
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			Log.ClearLog();
			UpdateValues();
		}

		private void UndoButton_Click(object sender, EventArgs e)
		{
			Log.Undo();
			_tastudio.RefreshDialog();
		}

		private void RedoButton_Click(object sender, EventArgs e)
		{
			Log.Redo();
			_tastudio.RefreshDialog();
		}

		private void HistoryView_DoubleClick(object sender, EventArgs e)
		{
			if (Log.UndoIndex <= HistoryView.selectedItem)
			{
				return;
			}

			do
			{
				Log.Undo();
			}
			while (Log.UndoIndex > HistoryView.selectedItem);

			UpdateValues();
		}

		private void HistoryView_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				RightClickMenu.Show(HistoryView, e.X, e.Y);
			}
			else if (e.Button == MouseButtons.Left)
			{
				if (HistoryView.selectedItem == -1)
				{
					HistoryView.SelectItem(_hackSelect, true);
				}
			}
		}

		// Hacky way to select a row by clicking the names row
		private int _hackSelect = -1;

		private void HistoryView_MouseDown(object sender, MouseEventArgs e)
		{
			HistoryView.SelectItem((e.Y / HistoryView.LineHeight) + HistoryView.VScrollPos - 1, true);
			_hackSelect = HistoryView.selectedItem;
		}

		private void UndoHereMenuItem_Click(object sender, EventArgs e)
		{
			if (HistoryView.selectedItem == -1 || Log.UndoIndex < HistoryView.selectedItem)
			{
				return;
			}

			do
			{
				Log.Undo();
			}
			while (Log.UndoIndex >= HistoryView.selectedItem);

			UpdateValues();
		}

		private void RedoHereMenuItem_Click(object sender, EventArgs e)
		{
			if (HistoryView.selectedItem == -1 || Log.UndoIndex >= HistoryView.selectedItem)
			{
				return;
			}

			do
			{
				Log.Redo();
			}
			while (Log.UndoIndex < HistoryView.selectedItem);

			UpdateValues();
		}

		private void ClearHistoryToHereMenuItem_Click(object sender, EventArgs e)
		{
			if (HistoryView.selectedItem != -1)
			{
				Log.ClearLog(HistoryView.selectedItem);
			}

			UpdateValues();
		}

		private void MaxStepsNum_ValueChanged(object sender, EventArgs e)
		{
			Log.MaxSteps = (int)MaxStepsNum.Value;
		}
	}
}
