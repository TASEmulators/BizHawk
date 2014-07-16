using System;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class TasListView : VirtualListView
	{
		public class Cell
		{
			public int? Row;
			public string Column;

			// Convenience hack
			public override string ToString()
			{
				return string.IsNullOrEmpty(Column) ? "?" : Column + " - " + (Row.HasValue ? Row.ToString() : "?");
			}
		}

		public int? LastSelectedIndex
		{
			get
			{
				if (SelectedIndices.Count > 0)
				{
					return SelectedIndices
						.OfType<int>()
						.OrderBy(x => x)
						.Last();
				}

				return null;
			}
		}

		private Cell _currentPointedCell = new Cell();
		public Cell PointedCell
		{
			get { return _currentPointedCell; }
		}

		private Cell _lastPointedCell = new Cell();
		public Cell LastPointedCell
		{
			get { return _lastPointedCell; }
		}

		public bool InputPaintingMode { get; set; }
		public bool IsPaintDown { get; private set; }

		private void CalculatePointedCell(int x, int y)
		{
			int? newRow;
			string newColumn = String.Empty;

			var accumulator = 0;
			foreach (ColumnHeader column in Columns)
			{
				accumulator += column.Width;
				if (accumulator < x)
				{
					continue;
				}

				newColumn = column.Name;
				break;
			}

			var headerHeight = 24; //Are these always true? Don't know, is there a way to programmatically determine them?
			var rowHeight = 18;

			newRow = ((y - headerHeight) / rowHeight) + this.VScrollPos;
			if (newRow >= ItemCount)
			{
				newRow = null;
			}

			if (newColumn != PointedCell.Column || newRow != PointedCell.Row)
			{
				LastPointedCell.Column = PointedCell.Column;
				LastPointedCell.Row = PointedCell.Row;

				PointedCell.Column = newColumn;
				PointedCell.Row = newRow;

				CellChanged(LastPointedCell, PointedCell);
			}
		}

		public class CellEventArgs
		{
			public CellEventArgs(Cell oldCell, Cell newCell)
			{
				OldCell = oldCell;
				NewCell = newCell;
			}

			public Cell OldCell { get; private set; }
			public Cell NewCell { get; private set; }
		}

		public delegate void CellChangeEventHandler(object sender, CellEventArgs e);
		public event CellChangeEventHandler PointedCellChanged;

		private void CellChanged(Cell oldCell, Cell newCell)
		{
			if (PointedCellChanged != null)
			{
				PointedCellChanged(this, new CellEventArgs(oldCell, newCell));
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			_currentPointedCell.Column = String.Empty;
			_currentPointedCell.Row = null;
			IsPaintDown = false;
			base.OnMouseLeave(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			CalculatePointedCell(e.X, e.Y);
			base.OnMouseMove(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (InputPaintingMode)
			{
				IsPaintDown = true;
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			IsPaintDown = false;
			base.OnMouseUp(e);
		}
	}
}
