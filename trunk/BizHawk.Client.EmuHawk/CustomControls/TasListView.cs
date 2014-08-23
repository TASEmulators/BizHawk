using System;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class TasListView : VirtualListView
	{
		public class Cell
		{
			public int? RowIndex;
			public string Column;

			// Convenience hack
			public override string ToString()
			{
				return string.IsNullOrEmpty(Column) ? "?" : Column + " - " + (RowIndex.HasValue ? RowIndex.ToString() : "?");
			}
		}

		public bool RightButtonHeld { get; set; }

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
		public Cell CurrentCell
		{
			get { return _currentPointedCell; }
		}

		private Cell _lastPointedCell = new Cell();
		public Cell LastCell
		{
			get { return _lastPointedCell; }
		}

		public bool InputPaintingMode { get; set; }
		public bool IsPaintDown { get; private set; }

		/// <summary>
		/// Calculates the column name and row number that the point (x, y) lies in.
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
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

			var rowHeight = this.LineHeight;// 5 (in VirtualListView) and 6 work here for me, but are they always dependable, how can I get the padding?
			var headerHeight = rowHeight + 6;

			newRow = ((y - headerHeight) / rowHeight) + this.VScrollPos;
			if (newRow >= ItemCount)
			{
				newRow = null;
			}

			if (newColumn != CurrentCell.Column || newRow != CurrentCell.RowIndex)
			{
				LastCell.Column = CurrentCell.Column;
				LastCell.RowIndex = CurrentCell.RowIndex;

				CurrentCell.Column = newColumn;
				CurrentCell.RowIndex = newRow;

				CellChanged(LastCell, CurrentCell);
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
			_currentPointedCell.RowIndex = null;
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
			if (e.Button == MouseButtons.Left && InputPaintingMode)
			{
				IsPaintDown = true;
			}

			if (e.Button == MouseButtons.Right)
			{
				RightButtonHeld = true;
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			IsPaintDown = false;
			RightButtonHeld = false;

			base.OnMouseUp(e);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (RightButtonHeld)
			{
				DoRightMouseScroll(this, e);
			}
			else
			{
				base.OnMouseWheel(e);
			}
		}

		public delegate void RightMouseScrollEventHandler(object sender, MouseEventArgs e);
		public event RightMouseScrollEventHandler RightMouseScrolled;

		private void DoRightMouseScroll(object sender, MouseEventArgs e)
		{
			if (RightMouseScrolled != null)
			{
				RightMouseScrolled(sender, e);
			}
		}
	}
}
