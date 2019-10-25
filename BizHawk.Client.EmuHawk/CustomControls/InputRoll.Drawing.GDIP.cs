using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <remarks>New GDI+ methods live here</remarks>
	public partial class InputRoll
	{
		/// <remarks>instance field to mirror GDI implementation</remarks>
		private Pen sPen;

		/// <remarks>instance field to mirror GDI implementation</remarks>
		private Brush sBrush;

		/// <remarks>GDI+ uses floats to measure strings</remarks>
		private SizeF _charSizeF;

		#region Initialization and Destruction

		/// <summary>Initialises GDI+-related stuff (called from constructor)</summary>
		private void GDIPConstruction()
		{
			// HFont?
			// Rotated HFont?

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			using (var g = CreateGraphics())
			{
				_charSizeF = g.MeasureString("A", _commonFont);
//				_charSize = Size.Round(sizeF);
			}
		}

		#endregion

		#region Drawing Methods Using GDI+

		private void GDIP_OnPaint(PaintEventArgs e)
		{
			// white background
			sBrush = new SolidBrush(Color.White);
			sPen = new Pen(Color.White);

			e.Graphics.FillRectangle(sBrush, e.ClipRectangle);
			e.Graphics.Flush();

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
		}

		private void GDIP_DrawColumnDrag(PaintEventArgs e)
		{
			if (_columnDown != null && _columnDownMoved && _currentX.HasValue && _currentY.HasValue && IsHoveringOnColumnCell)
			{
				var x1 = _currentX.Value - _columnDown.Width.Value / 2;
				var y1 = _currentY.Value - CellHeight / 2;
				var x2 = x1 + _columnDown.Width.Value;
				var y2 = y1 + CellHeight;

				sPen = new Pen(_backColor);
				e.Graphics.DrawRectangle(sPen, x1, y1, x2, y2);
				sBrush = new SolidBrush(_foreColor);
//				e.Graphics.DrawString(_columnDown.Text, _commonFont, sBrush, (PointF)(new Point(x1 + CellWidthPadding, y1 + CellHeightPadding)));
				GDIP_DrawString(e, _columnDown.Text, _commonFont, new Point(x1 + CellWidthPadding, y1 + CellHeightPadding), _foreColor);
			}
		}

		private void GDIP_DrawCellDrag(PaintEventArgs e)
		{
			if (_draggingCell == null) return;

			var text = "";
			var offsetX = 0;
			var offsetY = 0;
			QueryItemText?.Invoke(_draggingCell.RowIndex.Value, _draggingCell.Column, out text, ref offsetX, ref offsetY);

			var bgColor = _backColor;
			QueryItemBkColor?.Invoke(_draggingCell.RowIndex.Value, _draggingCell.Column, ref bgColor);

			var x1 = _currentX.Value - _draggingCell.Column.Width.Value / 2;
			var y1 = _currentY.Value - CellHeight / 2;
			var x2 = x1 + _draggingCell.Column.Width.Value;
			var y2 = y1 + CellHeight;

			sBrush = new SolidBrush(bgColor);
			e.Graphics.FillRectangle(sBrush, x1, y1, x2 - x1, y2 - y1);
			sBrush = new SolidBrush(_foreColor);
//			e.Graphics.DrawString(text, _commonFont, sBrush, (PointF)(new Point(x1 + CellWidthPadding + offsetX, y1 + CellHeightPadding + offsetY)));
			GDIP_DrawString(e, text, _commonFont, new Point(x1 + CellWidthPadding + offsetX, y1 + CellHeightPadding + offsetY), _foreColor);
		}

		private void GDIP_DrawColumnText(PaintEventArgs e, IReadOnlyCollection<RollColumn> visibleColumns)
		{
			sBrush = new SolidBrush(_foreColor);
			if (HorizontalOrientation)
			{
				var start = -_vBar.Value;

				foreach (var column in visibleColumns)
				{
					var point = new Point(CellWidthPadding, start + CellHeightPadding);

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						var temp = sBrush;
						sBrush = new SolidBrush(SystemColors.HighlightText);
//						e.Graphics.DrawString(column.Text, _commonFont, sBrush, (PointF)(point));
						GDIP_DrawString(e, column.Text, _commonFont, point, SystemColors.HighlightText);
						sBrush = temp;
					}
					else
					{
//						e.Graphics.DrawString(column.Text, _commonFont, sBrush, (PointF)(point));
						GDIP_DrawString(e, column.Text, _commonFont, point, _foreColor);
					}

					start += CellHeight;
				}
			}
			else
			{
				var xPadding = CellWidthPadding + 1 - _hBar.Value;

				foreach (var column in visibleColumns)
				{
					var point = new Point(column.Left.Value + xPadding, CellHeightPadding);

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						var temp = sBrush;
						sBrush = new SolidBrush(SystemColors.HighlightText);
//						e.Graphics.DrawString(column.Text, _commonFont, sBrush, (PointF)(point));
						GDIP_DrawString(e, column.Text, _commonFont, point, SystemColors.HighlightText);
						sBrush = temp;
					}
					else
					{
//						e.Graphics.DrawString(column.Text, _commonFont, sBrush, (PointF)(point));
						GDIP_DrawString(e, column.Text, _commonFont, point, _foreColor);
					}
				}
			}
		}

		private void GDIP_DrawData(PaintEventArgs e, IReadOnlyList<RollColumn> visibleColumns)
		{
			if (visibleColumns.Count == 0) return; // Prevent exceptions with small TAStudio windows
			if (QueryItemText == null) return;

			var startRow = FirstVisibleRow;
			int LastVisible;
			if (HorizontalOrientation)
			{
				var isRotated = false;
				var fLimit = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

				sBrush = new SolidBrush(_foreColor);
				for (int i = 0, f = 0; f < fLimit; i++, f++)
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
							var x1 = RowsToPixels(i) + CellWidthPadding + bitmapOffsetX;
							var y1 = j * CellHeight + CellHeightPadding * 2 + bitmapOffsetY;
							e.Graphics.DrawImage(image, new Point(x1, y1));
						}

						string text;
						var strOffsetX = 0;
						var strOffsetY = 0;
						QueryItemText(f + startRow, visibleColumns[j], out text, ref strOffsetX, ref strOffsetY);

						// Centre Text
						var point = new Point(
							RowsToPixels(i) + (CellWidth - (int)Math.Round(text.Length * _charSizeF.Width)) / 2 + strOffsetX,
							j * CellHeight + CellHeightPadding - _vBar.Value + strOffsetY
						);

						var rePrep = false;
						if (j == 1)
						{
							if (_selectedItems.Contains(new Cell { Column = visibleColumns[j], RowIndex = i + startRow }))
							{
								isRotated = true;
								sBrush = new SolidBrush(SystemColors.HighlightText);
								rePrep = true;
							}
							else if (j == 1)
							{
								// 1. not sure about this; 2. repreps may be excess, but if we render one column at a time, we do need to change back after rendering the header
								rePrep = true;
								isRotated = true;
								sBrush = new SolidBrush(SystemColors.HighlightText);
							}
						}

						if (!string.IsNullOrWhiteSpace(text))
						{
//							_gdi.DrawString(text, point);
							if (isRotated)
							{
								var sz = e.Graphics.VisibleClipBounds.Size;
								e.Graphics.TranslateTransform(sz.Width / 2, sz.Height / 2);
								e.Graphics.RotateTransform(90);
								sz = e.Graphics.MeasureString(text, _commonFont);
								e.Graphics.DrawString(text, _commonFont, sBrush, -(sz.Width / 2), -(sz.Height / 2));
							}
							else
							{
//								e.Graphics.DrawString(text, _commonFont, sBrush, (PointF)point);
								GDIP_DrawString(e, text, _commonFont, point, new Pen(sBrush).Color);
							}
						}

						if (rePrep)
						{
							isRotated = false;
							sBrush = new SolidBrush(_foreColor);
						}
					}
				}
			}
			else
			{
				var range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;
				var xPadding = CellWidthPadding + 1 - _hBar.Value;
				sBrush = new SolidBrush(_foreColor);
				for (int i = 0, f = 0; f < range; i++, f++) // Vertical
				{
					f += _lagFrames[i];
					LastVisible = LastVisibleColumnIndex;
					for (var j = FirstVisibleColumn; j <= LastVisible; j++) // Horizontal
					{
						var col = visibleColumns[j];

						Bitmap image = null;
						var bitmapOffsetX = 0;
						var bitmapOffsetY = 0;
						QueryItemIcon?.Invoke(f + startRow, visibleColumns[j], ref image, ref bitmapOffsetX, ref bitmapOffsetY);

						var point = new Point(col.Left.Value + xPadding, RowsToPixels(i) + CellHeightPadding);
						if (image != null) e.Graphics.DrawImage(image, new Point(point.X + bitmapOffsetX, point.Y + bitmapOffsetY + CellHeightPadding));

						string text;
						var strOffsetX = 0;
						var strOffsetY = 0;
						QueryItemText(f + startRow, visibleColumns[j], out text, ref strOffsetX, ref strOffsetY);

						var rePrep = false;
						if (_selectedItems.Contains(new Cell { Column = visibleColumns[j], RowIndex = f + startRow }))
						{
							sBrush = new SolidBrush(SystemColors.HighlightText);
							rePrep = true;
						}

						if (!string.IsNullOrWhiteSpace(text))
						{
//							e.Graphics.DrawString(text, _commonFont, sBrush, (PointF)(new Point(point.X + strOffsetX, point.Y + strOffsetY)));
							GDIP_DrawString(e, text, _commonFont, new Point(point.X + strOffsetX, point.Y + strOffsetY), new Pen(sBrush).Color);
						}

						if (rePrep) sBrush = new SolidBrush(_foreColor);
					}
				}
			}
		}

		private void GDIP_DrawColumnBg(PaintEventArgs e, IReadOnlyList<RollColumn> visibleColumns)
		{
			var columnCount = visibleColumns.Count;
			sBrush = new SolidBrush(SystemColors.ControlLight);
			sPen = new Pen(Color.Black);

			if (HorizontalOrientation)
			{
				e.Graphics.FillRectangle(sBrush, 0, 0, ColumnWidth + 1, DrawHeight + 1);
				e.Graphics.DrawLine(sPen, 0, 0, 0, columnCount * CellHeight + 1);
				e.Graphics.DrawLine(sPen, ColumnWidth, 0, ColumnWidth, columnCount * CellHeight + 1);

				var iLimit = columnCount + 1;
				if (iLimit > 1)
				{
					for (int i = 0, y = -_vBar.Value; i < iLimit; i++, y += CellHeight) e.Graphics.DrawLine(sPen, 1, y, ColumnWidth, y);
				}
			}
			else
			{
				var bottomEdge = RowsToPixels(0);

				// Gray column box and black line underneath
				e.Graphics.FillRectangle(sBrush, 0, 0, Width + 1, bottomEdge + 1);
				e.Graphics.DrawLine(sPen, 0, 0, TotalColWidth.Value + 1, 0);
				e.Graphics.DrawLine(sPen, 0, bottomEdge, TotalColWidth.Value + 1, bottomEdge);

				// Vertical black seperators
				foreach (var column in visibleColumns)
				{
					var pos = column.Left.Value - _hBar.Value;
					e.Graphics.DrawLine(sPen, pos, 0, pos, bottomEdge);
				}
				if (columnCount != 0)
				{
					var right = TotalColWidth.Value - _hBar.Value;
					e.Graphics.DrawLine(sPen, right, 0, right, bottomEdge);
				}
			}

			// Emphasis
			for (var i = 0; i < columnCount; i++)
			{
				var column = visibleColumns[i];
				if (!column.Emphasis) continue; // only act on emphasised columns

				sBrush = new SolidBrush(SystemColors.ActiveBorder);
				if (HorizontalOrientation) e.Graphics.FillRectangle(sBrush, 1, i * CellHeight + 1, ColumnWidth - 1, ColumnHeight - 1);
				else e.Graphics.FillRectangle(sBrush, column.Left.Value + 1 - _hBar.Value, 1, column.Width.Value - 1, ColumnHeight - 1);
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

						sBrush = new SolidBrush(column.Emphasis
							? Color.FromArgb(SystemColors.Highlight.ToArgb() + 0x00222222) //TODO should be bitwise or?
							: SystemColors.Highlight);

						e.Graphics.FillRectangle(sBrush, 1, i * CellHeight + 1, ColumnWidth - 1, ColumnHeight - 1);
					}
				}
				else
				{
					//TODO multiple selected columns
					foreach (var column in visibleColumns)
					{
						if (column != CurrentCell.Column) continue; // only act on selected
						if (column.Left.Value - _hBar.Value > Width || column.Right.Value - _hBar.Value < 0) continue; // Left of column is to the right of the viewable area, or right of column is to the left of the viewable area

						var left = column.Left.Value - _hBar.Value;
						var width = column.Right.Value - _hBar.Value - left;
						sBrush = new SolidBrush(column.Emphasis
							? Color.FromArgb(SystemColors.Highlight.ToArgb() + 0x00550000) //TODO should be bitwise or?
							: SystemColors.Highlight);

						e.Graphics.FillRectangle(sBrush, left + 1, 1, width - 1, ColumnHeight - 1);
					}
				}
			}
		}

		private void GDIP_DrawBg(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			if (UseCustomBackground && QueryItemBkColor != null) DoBackGroundCallback(e, visibleColumns);

			if (GridLines)
			{
				sPen = new Pen(SystemColors.ControlLight);
				if (HorizontalOrientation)
				{
					// Columns
					var iLimit = VisibleRows + 1;
					for (var i = 1; i < iLimit; i++)
					{
						var x = RowsToPixels(i);
						e.Graphics.DrawLine(sPen, x, 1, x, DrawHeight);
					}

					// Rows
					var x1 = RowsToPixels(0) + 1;
					var jLimit = visibleColumns.Count + 1;
					for (var j = 0; j < jLimit; j++)
					{
						var y = j * CellHeight - _vBar.Value;
						e.Graphics.DrawLine(sPen, x1, y, DrawWidth, y);
					}
				}
				else
				{
					// Columns
					var y1 = ColumnHeight + 1;
					var y2 = Height - 1;
					foreach (var column in visibleColumns)
					{
						var x = column.Left.Value - _hBar.Value;
						e.Graphics.DrawLine(sPen, x, y1, x, y2);
					}
					if (visibleColumns.Count != 0)
					{
						var x = TotalColWidth.Value - _hBar.Value;
						e.Graphics.DrawLine(sPen, x, y1, x, y2);
					}

					// Rows
					var x2 = Width + 1;
					var iLimit = VisibleRows + 1;
					for (var i = 1; i < iLimit; i++)
					{
						var y = RowsToPixels(i);
						e.Graphics.DrawLine(sPen, 0, y, x2, y);
					}
				}
			}

			if (_selectedItems.Count != 0) DoSelectionBG(e, visibleColumns);
		}

		private void GDIP_DrawCellBG(PaintEventArgs e, Color color, Cell cell, IList<RollColumn> visibleColumns)
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

			sBrush = new SolidBrush(color.A == 0 ? Color.FromArgb(255, color) : color);
			e.Graphics.FillRectangle(sBrush, x, y, w, CellHeight - 1);
		}

		private void GDIP_DrawString(PaintEventArgs e, string text, Font font, Point point, Color color)
		{
//			TextRenderer.DrawText(e.Graphics, text, font, point, color);
			e.Graphics.DrawString(text, font, new SolidBrush(color), (PointF)point);
		}

		#endregion
	}
}
