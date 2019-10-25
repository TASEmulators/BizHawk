using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.EmuHawk.CustomControls;

namespace BizHawk.Client.EmuHawk
{
	/// <remarks>GDI32.dll related methods are abstracted to here</remarks>
	public partial class InputRoll
	{
		private readonly GDIRenderer _gdi;
		private readonly IntPtr _rotatedFont;
		private readonly IntPtr _normalFont;
		private Size _charSize;

		#region Initialization and Destruction

		/// <summary>Initialises GDI-related stuff (called from constructor)</summary>
		private void GDIConstruction()
		{
			using (var g = CreateGraphics()) using (_gdi.LockGraphics(g))
				_charSize = _gdi.MeasureString("A", _commonFont); //TODO make this a property so changing it updates other values
		}

		private void GDIDispose()
		{
			_gdi.Dispose();
			GDIRenderer.DestroyHFont(_normalFont);
			GDIRenderer.DestroyHFont(_rotatedFont);
		}

		#endregion

		#region Drawing Methods Using GDI

		private void GDI_OnPaint(PaintEventArgs e)
		{
			using (_gdi.LockGraphics(e.Graphics))
			{
				_gdi.StartOffScreenBitmap(Width, Height);

				// White Background
				_gdi.SetBrush(Color.White);
				_gdi.SetSolidPen(Color.White);
				_gdi.FillRectangle(0, 0, Width, Height);

				// Lag frame calculations
				SetLagFramesArray();

				var visibleColumns = _columns.VisibleColumns.ToList();

				if (visibleColumns.Count != 0)
				{
					DrawColumnBg(e, visibleColumns);
					DrawColumnText(e, visibleColumns);
				}

				// Background
				DrawBg(e, visibleColumns);

				// Foreground
				DrawData(e, visibleColumns);

				DrawColumnDrag(e);
				DrawCellDrag(e);

				_gdi.CopyToScreen();
				_gdi.EndOffScreenBitmap();
			}
		}

		private void GDI_DrawColumnDrag()
		{
			if (_columnDown != null && _columnDownMoved && _currentX.HasValue && _currentY.HasValue && IsHoveringOnColumnCell)
			{
				var x1 = _currentX.Value - _columnDown.Width.Value / 2;
				var y1 = _currentY.Value - CellHeight / 2;
				var x2 = x1 + _columnDown.Width.Value;
				var y2 = y1 + CellHeight;

				_gdi.SetSolidPen(_backColor);
				_gdi.DrawRectangle(x1, y1, x2, y2);
				_gdi.PrepDrawString(_normalFont, _foreColor);
				_gdi.DrawString(_columnDown.Text, new Point(x1 + CellWidthPadding, y1 + CellHeightPadding));
			}
		}

		private void GDI_DrawCellDrag()
		{
			if (_draggingCell == null) return;

			var rowIndex = _draggingCell.RowIndex.Value;
			var column = _draggingCell.Column;
			var text = "";
			var offsetX = 0;
			var offsetY = 0;
			QueryItemText?.Invoke(rowIndex, column, out text, ref offsetX, ref offsetY);

			var bgColor = _backColor;
			QueryItemBkColor?.Invoke(rowIndex, column, ref bgColor);

			var draggedWidth = column.Width.Value;
			var draggedHeight = CellHeight;
			var x1 = _currentX.Value - draggedWidth / 2;
			var y1 = _currentY.Value - draggedHeight / 2;

			_gdi.SetBrush(bgColor);
			_gdi.FillRectangle(x1, y1, draggedWidth, draggedHeight);
			_gdi.PrepDrawString(_normalFont, _foreColor);
			_gdi.DrawString(text, new Point(x1 + CellWidthPadding + offsetX, y1 + CellHeightPadding + offsetY));
		}

		private void GDI_DrawColumnText(IEnumerable<RollColumn> visibleColumns)
		{
			_gdi.PrepDrawString(_normalFont, _foreColor);
			var isHoveringOnColumnCell = IsHoveringOnColumnCell;
			if (HorizontalOrientation)
			{
				var start = -_vBar.Value;
				foreach (var column in visibleColumns)
				{
					var point = new Point(CellWidthPadding, start + CellHeightPadding);
					if (isHoveringOnColumnCell && column == CurrentCell.Column)
					{
						_gdi.PrepDrawString(_normalFont, SystemColors.HighlightText);
						_gdi.DrawString(column.Text, point);
						_gdi.PrepDrawString(_normalFont, _foreColor);
					}
					else
					{
						_gdi.DrawString(column.Text, point);
					}
					start += CellHeight;
				}
			}
			else
			{
				var paddingX = 2 * CellWidthPadding - _hBar.Value; //TODO fix this CellPadding issue (2 * CellPadding vs just CellPadding)
				foreach (var column in visibleColumns)
				{
					var point = new Point(column.Left.Value + paddingX, CellHeightPadding);
					if (isHoveringOnColumnCell && column == CurrentCell.Column)
					{
						_gdi.PrepDrawString(_normalFont, SystemColors.HighlightText);
						_gdi.DrawString(column.Text, point);
						_gdi.PrepDrawString(_normalFont, _foreColor);
					}
					else
					{
						_gdi.DrawString(column.Text, point);
					}
				}
			}
		}

		private void GDI_DrawData(IReadOnlyList<RollColumn> visibleColumns)
		{
			if (visibleColumns.Count == 0) return; // Prevent exceptions with small TAStudio windows
			if (QueryItemText == null) return;

			_gdi.PrepDrawString(_normalFont, _foreColor);

			var startRow = FirstVisibleRow;
			var range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;
			int LastVisible;
			if (HorizontalOrientation)
			{
				for (int i = 0, f = 0; f < range; i++, f++)
				{
					f += _lagFrames[i];
					LastVisible = LastVisibleColumnIndex;
					for (var j = FirstVisibleColumn; j <= LastVisible; j++)
					{
						Bitmap image = null;
						var bitmapOffsetX = 0;
						var bitmapOffsetY = 0;
						QueryItemIcon?.Invoke(f + startRow, visibleColumns[j], ref image, ref bitmapOffsetX, ref bitmapOffsetY);

						if (image != null)
						{
							_gdi.DrawBitmap(
								image,
								new Point(RowsToPixels(i) + CellWidthPadding + bitmapOffsetX, j * CellHeight + CellHeightPadding * 2 + bitmapOffsetY),
								true
							);
						}

						string text;
						var strOffsetX = 0;
						var strOffsetY = 0;
						QueryItemText(f + startRow, visibleColumns[j], out text, ref strOffsetX, ref strOffsetY);

						var rePrep = j == 1;
						if (rePrep)
						{
							// 1. not sure about this; 2. repreps may be excess, but if we render one column at a time, we do need to change back after rendering the header
							_gdi.PrepDrawString(
								_rotatedFont,
								_selectedItems.Contains(new Cell { Column = visibleColumns[j], RowIndex = i + startRow })
									? SystemColors.HighlightText
									: _foreColor
							);
						}

						// Centre Text
						var point = new Point(
							RowsToPixels(i) + (CellWidth - text.Length * _charSize.Width) / 2 + strOffsetX,
							j * CellHeight + CellHeightPadding - _vBar.Value + strOffsetY
						);
						if (!string.IsNullOrWhiteSpace(text)) _gdi.DrawString(text, point);

						if (rePrep) _gdi.PrepDrawString(_normalFont, _foreColor);
					}
				}
			}
			else
			{
				var xPadding = CellWidthPadding + 1 - _hBar.Value;
				for (int i = 0, f = 0; f < range; i++, f++) // Vertical
				{
					f += _lagFrames[i];
					LastVisible = LastVisibleColumnIndex;
					var y = RowsToPixels(i) + CellHeightPadding;
					for (var j = FirstVisibleColumn; j <= LastVisible; j++) // Horizontal
					{
						var column = visibleColumns[j];
						var x = column.Left.Value + xPadding;
						Bitmap image = null;
						var bitmapOffsetX = 0;
						var bitmapOffsetY = 0;
						QueryItemIcon?.Invoke(f + startRow, column, ref image, ref bitmapOffsetX, ref bitmapOffsetY);

						if (image != null)
						{
							_gdi.DrawBitmap(
								image,
								new Point(x + bitmapOffsetX, y + bitmapOffsetY + CellHeightPadding),
								true
							);
						}

						string text;
						var strOffsetX = 0;
						var strOffsetY = 0;
						QueryItemText(f + startRow, column, out text, ref strOffsetX, ref strOffsetY);

						var rePrep = !_selectedItems.Contains(new Cell { Column = column, RowIndex = f + startRow });

						if (rePrep) _gdi.PrepDrawString(_normalFont, SystemColors.HighlightText);

						if (!string.IsNullOrWhiteSpace(text)) _gdi.DrawString(text, new Point(x + strOffsetX, y + strOffsetY));

						if (rePrep) _gdi.PrepDrawString(_normalFont, _foreColor);
					}
				}
			}
		}

		private void GDI_DrawColumnBg(IReadOnlyList<RollColumn> visibleColumns)
		{
			var columnCount = visibleColumns.Count;

			_gdi.SetBrush(SystemColors.ControlLight);
			_gdi.SetSolidPen(Color.Black);

			if (HorizontalOrientation)
			{
				_gdi.FillRectangle(0, 0, ColumnWidth + 1, DrawHeight + 1);
				_gdi.Line(0, 0, 0, columnCount * CellHeight + 1);
				_gdi.Line(ColumnWidth, 0, ColumnWidth, columnCount * CellHeight + 1);

				if (columnCount != 0) for (int i = 0, y = -_vBar.Value; i <= columnCount; i++, y += CellHeight)
				{
					_gdi.Line(1, y, ColumnWidth, y);
				}
			}
			else
			{
				var bottomEdge = RowsToPixels(0);

				// Gray column box and black line underneath
				_gdi.FillRectangle(0, 0, Width + 1, bottomEdge + 1);
				_gdi.Line(0, 0, TotalColWidth.Value + 1, 0);
				_gdi.Line(0, bottomEdge, TotalColWidth.Value + 1, bottomEdge);

				// Vertical black seperators
				foreach (var column in visibleColumns)
				{
					var x = column.Left.Value - _hBar.Value;
					_gdi.Line(x, 0, x, bottomEdge);
				}
				if (columnCount != 0)
				{
					var x = TotalColWidth.Value - _hBar.Value;
					_gdi.Line(x, 0, x, bottomEdge);
				}
			}

			// Emphasis
			var columnHeight = ColumnHeight - 1;
			if (HorizontalOrientation)
			{
				for (var i = 0; i < columnCount; i++)
				{
					if (!visibleColumns[i].Emphasis) continue; // only act on emphasised columns

					_gdi.SetBrush(SystemColors.ActiveBorder);
					_gdi.FillRectangle(1, i * CellHeight + 1, ColumnWidth - 1, columnHeight);
				}
			}
			else
			{
				foreach (var column in visibleColumns)
				{
					if (!column.Emphasis) continue; // only act on emphasised columns

					_gdi.SetBrush(SystemColors.ActiveBorder);
					_gdi.FillRectangle(column.Left.Value + 1 - _hBar.Value, 1, column.Width.Value - 1, columnHeight);
				}
			}

			// If the user is hovering over a column
			if (IsHoveringOnColumnCell)
			{
				if (HorizontalOrientation)
				{
					for (var i = 0; i < columnCount; i++)
					{
						var column = visibleColumns[i];
						if (column != CurrentCell.Column) continue; // only act on selected

						_gdi.SetBrush(column.Emphasis ? Add(SystemColors.Highlight, 0x00222222) : SystemColors.Highlight);
						_gdi.FillRectangle(1, i * CellHeight + 1, ColumnWidth - 1, columnHeight);
					}
				}
				else
				{
					//TODO multiple selected columns
					foreach (var column in visibleColumns)
					{
						if (column != CurrentCell.Column) continue; // only act on selected
						if (column.Left.Value - _hBar.Value > Width || column.Right.Value - _hBar.Value < 0) continue; // Left of column is to the right of the viewable area, or right of column is to the left of the viewable area

						_gdi.SetBrush(column.Emphasis ? Add(SystemColors.Highlight, 0x00550000) : SystemColors.Highlight);
						var left = column.Left.Value + 1;
						_gdi.FillRectangle(left - _hBar.Value, 1, column.Right.Value - left, columnHeight);
					}
				}
			}
		}

		private void GDI_DrawBg(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			if (UseCustomBackground && QueryItemBkColor != null) DoBackGroundCallback(e, visibleColumns);

			if (GridLines)
			{
				_gdi.SetSolidPen(SystemColors.ControlLight);
				if (HorizontalOrientation)
				{
					// Columns
					var iLimit = VisibleRows + 1;
					for (var i = 1; i < iLimit; i++)
					{
						var x = RowsToPixels(i);
						_gdi.Line(x, 1, x, DrawHeight);
					}

					// Rows
					var x1 = RowsToPixels(0) + 1;
					var jLimit = visibleColumns.Count + 1;
					for (var j = 0; j < jLimit; j++) _gdi.Line(x1, j * CellHeight - _vBar.Value, DrawWidth, j * CellHeight - _vBar.Value);
				}
				else
				{
					// Columns
					var y1 = ColumnHeight + 1;
					var y2 = Height - 1;
					foreach (var column in visibleColumns)
					{
						var x = column.Left.Value - _hBar.Value;
						_gdi.Line(x, y1, x, y2);
					}
					if (visibleColumns.Count != 0)
					{
						var x = TotalColWidth.Value - _hBar.Value;
						_gdi.Line(x, y1, x, y2);
					}

					// Rows
					var x2 = Width + 1;
					var iLimit = VisibleRows + 1;
					for (var i = 1; i < iLimit; i++)
					{
						var y = RowsToPixels(i);
						_gdi.Line(0, y, x2, y);
					}
				}
			}

			if (_selectedItems.Count != 0) DoSelectionBG(e, visibleColumns);
		}

		private void GDI_DrawCellBG(Color color, Cell cell, IList<RollColumn> visibleColumns)
		{
			// We can't draw without row and column, so assume they exist and fail catastrophically if they don't
			int x, y, w;
			if (HorizontalOrientation)
			{
				x = RowsToPixels(cell.RowIndex.Value) + 1;
				if (x < ColumnWidth) return;
				y = CellHeight * visibleColumns.IndexOf(cell.Column) + 1 - _vBar.Value;
				w = CellWidth - 1;
			}
			else
			{
				y = RowsToPixels(cell.RowIndex.Value) + 1;
				if (y < ColumnHeight) return;
				x = cell.Column.Left.Value - _hBar.Value + 1;
				w = cell.Column.Width.Value - 1;
			}
			if (x > DrawWidth || y > DrawHeight) return; // Don't draw if off-screen.

			_gdi.SetBrush(color);
			_gdi.FillRectangle(x, y, w, CellHeight - 1);
		}

		#endregion
	}
}
