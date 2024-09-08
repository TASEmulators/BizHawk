using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class InputRoll
	{
		protected override void OnPaint(PaintEventArgs e)
		{
			using (_renderer.LockGraphics(e.Graphics))
			{
				// White Background
				_renderer.SetBrush(_backColor);
				_renderer.SetSolidPen(_backColor);
				_renderer.FillRectangle(e.ClipRectangle);

				// Lag frame calculations
				SetLagFramesArray();

				List<RollColumn> visibleColumns;

				if (HorizontalOrientation)
				{
					CalculateHorizontalColumnPositions(VisibleColumns.ToList());
					visibleColumns = VisibleColumns
						.Take(_horizontalColumnTops.Count(c => c < e.ClipRectangle.Height))
						.ToList();
				}
				else
				{
					visibleColumns = _columns.VisibleColumns
						.Where(c => c.Right > _hBar.Value && c.Left - _hBar.Value < e.ClipRectangle.Width)
						.ToList();
				}

				var firstVisibleRow = Math.Max(FirstVisibleRow, 0);
				var visibleRows = HorizontalOrientation
					? e.ClipRectangle.Width / CellWidth
					: e.ClipRectangle.Height / CellHeight;

				var lastVisibleRow = firstVisibleRow + visibleRows;

				var needsColumnRedraw = HorizontalOrientation || e.ClipRectangle.Y < ColumnHeight;
				if (visibleColumns.Any() && needsColumnRedraw)
				{
					DrawColumnBg(visibleColumns, e.ClipRectangle);
					DrawColumnText(visibleColumns);
				}

				// Background
				DrawBg(visibleColumns, e.ClipRectangle, firstVisibleRow, lastVisibleRow);

				// Foreground
				DrawData(visibleColumns, firstVisibleRow, lastVisibleRow);

				DrawColumnDrag(visibleColumns);
				DrawCellDrag(visibleColumns);
			}
		}

		private void DrawString(string text, Rectangle rect)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				_renderer.DrawString(text, rect);
			}
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			// Do nothing, and this should never be called
		}

		private void CalculateHorizontalColumnPositions(List<RollColumn> visibleColumns)
		{
			if (_horizontalColumnTops == null || _horizontalColumnTops.Length != visibleColumns.Count + 1)
			{
				_horizontalColumnTops = new int[visibleColumns.Count + 1];
			}

			int top = 0;
			int startRow = FirstVisibleRow;
			for (int j = 0; j < visibleColumns.Count; j++)
			{
				RollColumn col = visibleColumns[j];
				int height = CellHeight;
				if (col.Rotatable)
				{
					int strOffsetX = 0;
					int strOffsetY = 0;
					QueryItemText(startRow, col, out var text, ref strOffsetX, ref strOffsetY);
					int textWidth = (int)_renderer.MeasureString(text, Font).Width;
					height = Math.Max(height, textWidth + (CellWidthPadding * 2));
				}
				_horizontalColumnTops[j] = top;
				top += height;
			}
			_horizontalColumnTops[visibleColumns.Count] = top;
		}

		private void DrawColumnDrag(List<RollColumn> visibleColumns)
		{
			if (_columnDown is not { Width: > 0 }
				|| !_columnDownMoved
				|| !_currentX.HasValue
				|| !_currentY.HasValue
				|| !IsHoveringOnColumnCell)
			{
				return;
			}

			int columnWidth = _columnDown.Width;
			int columnHeight = CellHeight;
			if (HorizontalOrientation)
			{
				int columnIndex = visibleColumns.IndexOf(_columnDown);
				columnWidth = MaxColumnWidth;
				columnHeight = GetHColHeight(columnIndex);
			}

			int x1 = _currentX.Value - (columnWidth / 2);
			int y1 = _currentY.Value - (columnHeight / 2);
			int textOffsetY = CellHeightPadding;
			if (HorizontalOrientation)
			{
				int textHeight = (int)_renderer.MeasureString(_columnDown.Text, Font).Height;
				textOffsetY = (columnHeight - textHeight) / 2;
			}

			_renderer.SetSolidPen(_backColor);
			_renderer.DrawRectangle(new Rectangle(x1, y1, columnWidth, columnHeight));
			_renderer.PrepDrawString(Font, _foreColor);
			_renderer.DrawString(_columnDown.Text, new Rectangle(x1 + CellWidthPadding, y1 + textOffsetY, columnWidth, columnHeight));
		}

		private void DrawCellDrag(List<RollColumn> visibleColumns)
		{
			if (_draggingCell is { RowIndex: int targetRow, Column: { Width: > 0 } targetCol }
				&& _currentX.HasValue
				&& _currentY.HasValue)
			{
				var text = "";
				int offsetX = 0;
				int offsetY = 0;
				QueryItemText?.Invoke(targetRow, targetCol, out text, ref offsetX, ref offsetY);

				Color bgColor = _backColor;
				QueryItemBkColor?.Invoke(targetRow, targetCol, ref bgColor);

				int columnHeight = CellHeight;
				if (HorizontalOrientation)
				{
					var columnIndex = visibleColumns.IndexOf(targetCol);
					columnHeight = GetHColHeight(columnIndex);
				}
				var x1 = _currentX.Value - targetCol.Width / 2;
				int y1 = _currentY.Value - (columnHeight / 2);
				var x2 = x1 + targetCol.Width;
				int y2 = y1 + columnHeight;

				_renderer.SetBrush(bgColor);
				_renderer.FillRectangle(new Rectangle(x1, y1, x2 - x1, y2 - y1));
				_renderer.PrepDrawString(Font, _foreColor);
				_renderer.DrawString(text, new Rectangle(x1 + CellWidthPadding + offsetX, y1 + CellHeightPadding + offsetY, x2 - x1, y2 - y1));
			}
		}

		private void DrawColumnText(List<RollColumn> visibleColumns)
		{
			_renderer.PrepDrawString(Font, _foreColor);

			int h = ColumnHeight;
			int yOffset = HorizontalOrientation ? -_vBar.Value : 0;

			for (int j = 0; j < visibleColumns.Count; j++)
			{
				var column = visibleColumns[j];
				var w = column.Width;
				int x, y;

				if (HorizontalOrientation)
				{
					var columnHeight = GetHColHeight(j);
					var textSize = _renderer.MeasureString(column.Text, Font);
					x = MaxColumnWidth - CellWidthPadding - (int)textSize.Width;
					y = yOffset + ((columnHeight - (int)textSize.Height) / 2);
					yOffset += columnHeight;
				}
				else
				{
					x = 1 + column.Left + CellWidthPadding - _hBar.Value;
					y = CellHeightPadding;
				}

				if (IsHoveringOnColumnCell && column == CurrentCell.Column)
				{
					_renderer.PrepDrawString(Font, SystemColors.HighlightText);
					DrawString(column.Text, new Rectangle(x, y,  column.Width, h));
					_renderer.PrepDrawString(Font, _foreColor);
				}
				else
				{
					DrawString(column.Text, new Rectangle(x, y, w, h));
				}
			}
		}

		private void DrawData(List<RollColumn> visibleColumns, int firstVisibleRow, int lastVisibleRow)
		{
			if (QueryItemText == null)
			{
				return;
			}

			if (!visibleColumns.Any())
			{
				return;
			}

			int startRow = firstVisibleRow;
			int range = Math.Min(lastVisibleRow, RowCount - 1) - startRow + 1;
			_renderer.PrepDrawString(Font, _foreColor);

			if (HorizontalOrientation)
			{
				for (int j = 0; j < visibleColumns.Count; j++)
				{
					RollColumn col = visibleColumns[j];
					int colHeight = GetHColHeight(j);

					for (int i = 0, f = 0; f < range; i++, f++)
					{
						f += _lagFrames[i];

						int baseX = RowsToPixels(i) + (col.Rotatable ? CellWidth : 0);
						int baseY = GetHColTop(j) - _vBar.Value;

						if (!col.Rotatable)
						{
							Bitmap image = null;
							int bitmapOffsetX = 0;
							int bitmapOffsetY = 0;

							QueryItemIcon?.Invoke(f + startRow, col, ref image, ref bitmapOffsetX, ref bitmapOffsetY);

							if (image != null)
							{
								int x = baseX + CellWidthPadding + bitmapOffsetX;
								int y = baseY + CellHeightPadding + bitmapOffsetY;
								_renderer.DrawBitmap(image, new Point(x, y));
							}
						}

						int strOffsetX = 0;
						int strOffsetY = 0;
						QueryItemText(f + startRow, col, out var text, ref strOffsetX, ref strOffsetY);

						int textWidth = (int)_renderer.MeasureString(text, Font).Width;
						if (col.Rotatable)
						{
							// Center Text
							int textX = Math.Max(((colHeight - textWidth) / 2), CellWidthPadding) + strOffsetX;
							int textY = CellHeightPadding + strOffsetY;

							_renderer.PrepDrawString(Font, _foreColor, rotate: true);
							DrawString(text, new Rectangle(baseX - textY, baseY + textX, 999, CellHeight));
							_renderer.PrepDrawString(Font, _foreColor, rotate: false);
						}
						else
						{
							// Center Text
							int textX = Math.Max(((CellWidth - textWidth) / 2), CellWidthPadding) + strOffsetX;
							int textY = CellHeightPadding + strOffsetY;

							DrawString(text, new Rectangle(baseX + textX, baseY + textY, MaxColumnWidth, CellHeight));
						}
					}
				}
			}
			else
			{
				int xPadding = CellWidthPadding + 1 - _hBar.Value;
				var currentCell = new Cell();
				for (int i = 0, f = 0; f < range; i++, f++) // Vertical
				{
					f += _lagFrames[i];
					foreach (var column in visibleColumns)
					{
						int strOffsetX = 0;
						int strOffsetY = 0;
						Point point = new Point(column.Left + xPadding, RowsToPixels(i) + CellHeightPadding);

						Bitmap image = null;
						int bitmapOffsetX = 0;
						int bitmapOffsetY = 0;

						QueryItemIcon?.Invoke(f + startRow, column, ref image, ref bitmapOffsetX, ref bitmapOffsetY);

						if (image != null)
						{
							_renderer.DrawBitmap(image, new Point(point.X + bitmapOffsetX, point.Y + bitmapOffsetY + CellHeightPadding));
						}

						QueryItemText(f + startRow, column, out var text, ref strOffsetX, ref strOffsetY);

						bool rePrep = false;
						currentCell.Column = column;
						currentCell.RowIndex = f + startRow;
						if (_selectedItems.Contains(currentCell))
						{
							_renderer.PrepDrawString(Font, SystemColors.HighlightText);
							rePrep = true;
						}

						DrawString(text, new Rectangle(point.X + strOffsetX, point.Y + strOffsetY, column.Width, ColumnHeight));

						if (rePrep)
						{
							_renderer.PrepDrawString(Font, _foreColor);
						}
					}
				}
			}
		}

		private void DrawColumnBg(List<RollColumn> visibleColumns, Rectangle rect)
		{
			_renderer.SetBrush(SystemColors.ControlLight);
			_renderer.SetSolidPen(Color.Black);

			if (HorizontalOrientation)
			{
				_renderer.FillRectangle(new Rectangle(0, 0, MaxColumnWidth + 1, rect.Height));

				int y = -_vBar.Value;
				for (int j = 0; j < visibleColumns.Count; j++)
				{
					_renderer.Line(1, y, MaxColumnWidth, y);
					y += GetHColHeight(j);
				}

				if (visibleColumns.Any())
				{
					_renderer.Line(1, y, MaxColumnWidth, y);
				}

				_renderer.Line(0, 0, 0, y);
				_renderer.Line(MaxColumnWidth, 0, MaxColumnWidth, y);
			}
			else
			{
				int bottomEdge = RowsToPixels(0);

				// Gray column box and black line underneath
				_renderer.FillRectangle(new Rectangle(0, 0, rect.Width, bottomEdge + 1));
				_renderer.Line(0, 0, rect.Width, 0);
				_renderer.Line(0, bottomEdge, rect.Width, bottomEdge);

				// Vertical black separators
				foreach (var column in visibleColumns)
				{
					int pos = column.Left - _hBar.Value;
					_renderer.Line(pos, 0, pos, bottomEdge);
				}

				// Draw right most line
				if (visibleColumns.Any())
				{
					int right = TotalColWidth - _hBar.Value;
					if (right <= rect.Left + rect.Width)
					{
						_renderer.Line(right, 0, right, bottomEdge);
					}
				}
			}

			// Emphasis
			foreach (var column in visibleColumns.Where(c => c.Emphasis))
			{
				_renderer.SetBrush(SystemColors.ActiveBorder);
				if (HorizontalOrientation)
				{
					int columnIndex = visibleColumns.IndexOf(column);
					_renderer.FillRectangle(new Rectangle(1, GetHColTop(columnIndex) + 1, MaxColumnWidth - 1, GetHColHeight(columnIndex) - 1));
				}
				else
				{
					_renderer.FillRectangle(new Rectangle(column.Left + 1 - _hBar.Value, 1, column.Width - 1, ColumnHeight - 1));
				}
			}

			// If the user is hovering over a column
			if (IsHoveringOnColumnCell)
			{
				if (HorizontalOrientation)
				{
					for (int i = 0; i < visibleColumns.Count; i++)
					{
						if (visibleColumns[i] != CurrentCell.Column)
						{
							continue;
						}

						int top = GetHColTop(i) - _vBar.Value;
						int height = GetHColHeight(i);

						_renderer.SetBrush(CurrentCell.Column!.Emphasis
							? SystemColors.Highlight.Add(0x00222222)
							: SystemColors.Highlight);

						_renderer.FillRectangle(new Rectangle(1, top + 1, MaxColumnWidth - 1, height - 1));
					}
				}
				else
				{
					// TODO multiple selected columns
					foreach (var column in visibleColumns)
					{
						if (column == CurrentCell.Column)
						{
							// Left of column is to the right of the viewable area or right of column is to the left of the viewable area
							if (column.Left - _hBar.Value > Width || column.Right - _hBar.Value < 0)
							{
								continue;
							}

							int left = column.Left - _hBar.Value;
							int width = column.Right - _hBar.Value - left;

							_renderer.SetBrush(CurrentCell.Column!.Emphasis
								? SystemColors.Highlight.Add(0x00550000)
								: SystemColors.Highlight);

							_renderer.FillRectangle(new Rectangle(left + 1, 1, width - 1, ColumnHeight - 1));
						}
					}
				}
			}
		}

		// TODO refactor this and DoBackGroundCallback functions.
		// Draw Gridlines and background colors using QueryItemBkColor.
		private void DrawBg(List<RollColumn> visibleColumns, Rectangle rect, int firstVisibleRow, int lastVisibleRow)
		{
			if (QueryItemBkColor is not null || QueryRowBkColor is not null)
			{
				DoBackGroundCallback(visibleColumns, rect, firstVisibleRow, lastVisibleRow);
			}

			if (GridLines)
			{
				_renderer.SetSolidPen(SystemColors.ControlLight);
				if (HorizontalOrientation)
				{
					// Columns
					for (int i = 1; i < lastVisibleRow - firstVisibleRow + 1; i++)
					{
						int x = RowsToPixels(i);
						_renderer.Line(x, 1, x, rect.Height);
					}

					// Rows
					for (int i = 0; i < visibleColumns.Count + 1; i++)
					{
						// TODO: MaxColumnWidth shouldn't be necessary
						// This also makes too many assumptions, the parameters need to drive what is being drawn
						int y = GetHColTop(i) - _vBar.Value;
						int x = RowsToPixels(0) + 1;
						_renderer.Line(x, y, rect.Width + MaxColumnWidth, y);
					}
				}
				else
				{
					// Columns
					int y = ColumnHeight + 1;
					foreach (var column in visibleColumns)
					{
						int x = column.Left - _hBar.Value;
						_renderer.Line(x, y, x, rect.Height - 1);
					}

					if (visibleColumns.Any())
					{
						int x = TotalColWidth - _hBar.Value;
						_renderer.Line(x, y, x, rect.Height - 1);
					}

					// Rows
					for (int i = 1; i < VisibleRows + 1; i++)
					{
						_renderer.Line(0, RowsToPixels(i), rect.Width + 1, RowsToPixels(i));
					}
				}
			}

			if (_selectedItems.Any())
			{
				DoSelectionBG(visibleColumns, rect);
			}
		}

		private void DoSelectionBG(List<RollColumn> visibleColumns, Rectangle rect)
		{
			var visibleRows = FirstVisibleRow.RangeTo(LastVisibleRow);
			int lastRow = -1;
			foreach (Cell cell in _selectedItems)
			{
				if (!cell.RowIndex.HasValue || !visibleRows.Contains(cell.RowIndex.Value) || !VisibleColumns.Contains(cell.Column))
				{
					continue;
				}

				Cell relativeCell = new Cell
				{
					RowIndex = cell.RowIndex - visibleRows.Start,
					Column = cell.Column
				};
				relativeCell.RowIndex -= CountLagFramesAbsolute(relativeCell.RowIndex.Value);

				var rowColor = _backColor;
				if (QueryRowBkColor != null && lastRow != cell.RowIndex.Value)
				{
					QueryRowBkColor(cell.RowIndex.Value, ref rowColor);
					lastRow = cell.RowIndex.Value;
				}

				Color cellColor = rowColor;
				QueryItemBkColor?.Invoke(cell.RowIndex.Value, cell.Column, ref cellColor);

				// Alpha layering for cell before selection
				float alpha = (float)cellColor.A / 255;
				if (cellColor.A != 255 && cellColor.A != 0)
				{
					cellColor = Color.FromArgb(rowColor.R - (int)((rowColor.R - cellColor.R) * alpha),
						rowColor.G - (int)((rowColor.G - cellColor.G) * alpha),
						rowColor.B - (int)((rowColor.B - cellColor.B) * alpha));
				}

				// Alpha layering for selection
				alpha = 0.33f;
				cellColor = Color.FromArgb(cellColor.R - (int)((cellColor.R - SystemColors.Highlight.R) * alpha),
					cellColor.G - (int)((cellColor.G - SystemColors.Highlight.G) * alpha),
					cellColor.B - (int)((cellColor.B - SystemColors.Highlight.B) * alpha));
				DrawCellBG(cellColor, relativeCell, visibleColumns, rect);
			}
		}

		// Given a cell with RowIndex in between 0 and VisibleRows, it draws the background color specified. Do not call with absolute row indices.
		private void DrawCellBG(Color color, Cell cell, List<RollColumn> visibleColumns, Rectangle rect)
		{
			int x, y, w, h;

			if (HorizontalOrientation)
			{
				x = RowsToPixels(cell.RowIndex.Value) + 1;
				if (x < MaxColumnWidth)
				{
					return;
				}

				var columnIndex = visibleColumns.IndexOf(cell.Column!);
				w = CellWidth - 1;
				y = GetHColTop(columnIndex) - _vBar.Value + 1;
				h = GetHColHeight(columnIndex) - 1;
			}
			else
			{
				y = RowsToPixels(cell.RowIndex.Value) + 1; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				if (y < ColumnHeight)
				{
					return;
				}

				x = cell.Column!.Left - _hBar.Value + 1;
				w = cell.Column.Width - 1;
				h = CellHeight - 1;
			}

			_renderer.SetBrush(color);
			_renderer.FillRectangle(new Rectangle(x, y, w, h));
		}

		// Calls QueryItemBkColor callback for all visible cells and fills in the background of those cells.
		private void DoBackGroundCallback(List<RollColumn> visibleColumns, Rectangle rect, int firstVisibleRow, int lastVisibleRow)
		{
			if (!visibleColumns.Any())
			{
				return;
			}

			int startIndex = firstVisibleRow;
			int range = Math.Min(lastVisibleRow, RowCount - 1) - startIndex + 1;

			var currentCell = new Cell();
			for (int i = 0, f = 0; f < range; i++, f++)
			{
				f += _lagFrames[i];
				var rowColor = _backColor;
				QueryRowBkColor?.Invoke(f + startIndex, ref rowColor);

				foreach (var column in visibleColumns)
				{
					var itemColor = rowColor;
					QueryItemBkColor?.Invoke(f + startIndex, column, ref itemColor);
					if (itemColor.A is not (0 or 255))
					{
						float alpha = (float)itemColor.A / 255;
						itemColor = Color.FromArgb(rowColor.R - (int)((rowColor.R - itemColor.R) * alpha),
							rowColor.G - (int)((rowColor.G - itemColor.G) * alpha),
							rowColor.B - (int)((rowColor.B - itemColor.B) * alpha));
					}
					if (itemColor != _backColor) // An easy optimization, don't draw unless the user specified something other than the default
					{
						currentCell.Column = column;
						currentCell.RowIndex = i;
						DrawCellBG(itemColor, currentCell, visibleColumns, rect);
					}
				}
			}
		}
	}
}
