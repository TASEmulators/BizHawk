using System;
using System.Drawing;
using System.Linq;
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
		private TasMovieChangeLog Log => _tastudio.CurrentTasMovie.ChangeLog;

		public UndoHistoryForm(TAStudio owner)
		{
			InitializeComponent();
			_tastudio = owner;

			HistoryView.QueryItemText += HistoryView_QueryItemText;
			HistoryView.QueryItemBkColor += HistoryView_QueryItemBkColor;

			HistoryView.AllColumns.Clear();
			HistoryView.AllColumns.AddRange(new[]
			{
				new RollColumn { Name = IdColumnName, Text = IdColumnName, UnscaledWidth = 40, Type = ColumnType.Text },
				new RollColumn { Name = UndoColumnName, Text = UndoColumnName, UnscaledWidth = 280, Type = ColumnType.Text }
			});

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
				color = TAStudio.GreenZone_InputLog;
			}
			else if (index > Log.UndoIndex)
			{
				color = TAStudio.LagZone_InputLog;
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

		private int SelectedItem => HistoryView.SelectedRows.Any()
			? HistoryView.SelectedRows.First()
			: -1;

		private void HistoryView_DoubleClick(object sender, EventArgs e)
		{
			if (Log.UndoIndex <= SelectedItem)
			{
				return;
			}

			do
			{
				Log.Undo();
			}
			while (Log.UndoIndex > SelectedItem);

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
			if (SelectedItem == -1 || Log.UndoIndex < SelectedItem)
			{
				return;
			}

			do
			{
				Log.Undo();
			}
			while (Log.UndoIndex >= SelectedItem);

			UpdateValues();
		}

		private void RedoHereMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedItem == -1 || Log.UndoIndex >= SelectedItem)
			{
				return;
			}

			do
			{
				Log.Redo();
			}
			while (Log.UndoIndex < SelectedItem);

			UpdateValues();
		}

		private void ClearHistoryToHereMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedItem != -1)
			{
				Log.ClearLog(SelectedItem);
			}

			UpdateValues();
		}

		private void MaxStepsNum_ValueChanged(object sender, EventArgs e)
		{
			Log.MaxSteps = (int)MaxStepsNum.Value;
		}
	}
}
