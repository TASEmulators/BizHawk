using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class UndoHistoryForm : Form
	{
		private TAStudio tastudio;

		public UndoHistoryForm(TAStudio owner)
		{
			InitializeComponent();
			tastudio = owner;

			HistoryView.QueryItemText += HistoryView_QueryItemText;
			HistoryView.QueryItemBkColor += HistoryView_QueryItemBkColor;

			HistoryView.Columns[1].Width = 280;

			MaxStepsNum.Value = log.MaxSteps;
		}

		private Common.TasMovieChangeLog log
		{
			get { return tastudio.CurrentTasMovie.ChangeLog; }
		}

		private void HistoryView_QueryItemText(int row, int column, out string text)
		{
			if (column == 1)
				text = log.Names[row];
			else
				text = row.ToString();
		}
		private void HistoryView_QueryItemBkColor(int row, int column, ref Color color)
		{
			if (column == 0)
				return;

			if (row == log.UndoIndex)
				color = TAStudio.GreenZone_InputLog;
			else if (row > log.UndoIndex)
				color = TAStudio.LagZone_InputLog;
		}

		private string _lastUndoAction = null;
		public void UpdateValues()
		{
			HistoryView.ItemCount = log.Names.Count;
			if (AutoScrollCheck.Checked && _lastUndoAction != log.NextUndoStepName)
			{
				HistoryView.ensureVisible(log.UndoIndex);
				HistoryView.clearSelection();
				HistoryView.SelectItem(log.UndoIndex - 1, true);
			}
			_lastUndoAction = log.NextUndoStepName;

			HistoryView.Refresh();
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			log.ClearLog();
			UpdateValues();
		}
		private void UndoButton_Click(object sender, EventArgs e)
		{
			log.Undo();
			tastudio.RefreshDialog();
		}
		private void RedoButton_Click(object sender, EventArgs e)
		{
			log.Redo();
			tastudio.RefreshDialog();
		}


		private void HistoryView_DoubleClick(object sender, EventArgs e)
		{
			if (log.UndoIndex <= HistoryView.selectedItem)
				return;

			do
			{
				log.Undo();
			} while (log.UndoIndex > HistoryView.selectedItem);
			UpdateValues();
		}
		private void HistoryView_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Right)
				RightClickMenu.Show(HistoryView, e.X, e.Y);
			else if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				if (HistoryView.selectedItem == -1)
					HistoryView.SelectItem(_hackSelect, true);
			}
		}
		// Hacky way to select a row by clicking the names row
		int _hackSelect = -1;
		private void HistoryView_MouseDown(object sender, MouseEventArgs e)
		{
			HistoryView.SelectItem(e.Y / HistoryView.LineHeight + HistoryView.VScrollPos - 1, true);
			_hackSelect = HistoryView.selectedItem;
		}

		private void undoHereToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (HistoryView.selectedItem == -1 || log.UndoIndex < HistoryView.selectedItem)
				return;

			do
			{
				log.Undo();
			} while (log.UndoIndex >= HistoryView.selectedItem);
			UpdateValues();
		}
		private void redoHereToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (HistoryView.selectedItem == -1 || log.UndoIndex >= HistoryView.selectedItem)
				return;

			do
			{
				log.Redo();
			} while (log.UndoIndex < HistoryView.selectedItem);
			UpdateValues();
		}
		private void clearHistoryToHereToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (HistoryView.selectedItem != -1)
				log.ClearLog(HistoryView.selectedItem);
			UpdateValues();
		}

		private void MaxStepsNum_ValueChanged(object sender, EventArgs e)
		{
			log.MaxSteps = (int)MaxStepsNum.Value;
		}



	}
}
