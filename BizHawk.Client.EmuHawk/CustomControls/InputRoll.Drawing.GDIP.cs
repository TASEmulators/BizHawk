using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// New GDI+ methods live here
	/// </summary>
	public partial class InputRoll
	{
		// single instance to mirror GDI implementation
		private Pen sPen = null;
		// single instance to mirror GDI implementation
		private Brush sBrush = null;

		// GDI+ uses floats for measure string
		private SizeF _charSizeF;

		#region Initialization and Destruction

		/// <summary>
		/// Initializes GDI+ related stuff
		/// (called from constructor)
		/// </summary>
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
				//_charSize = Size.Round(sizeF);
			}
		}

		private void GDIPDispose()
		{

		}

		#endregion

		#region Drawing Methods Using GDI+

		private void GDIP_OnPaint(PaintEventArgs e)
		{
			// white background
			sBrush = new SolidBrush(Color.White);
			sPen = new Pen(Color.White);

			Rectangle rect = e.ClipRectangle;
			e.Graphics.FillRectangle(sBrush, rect);
			e.Graphics.Flush();

			// Lag frame calculations
			SetLagFramesArray();

			var visibleColumns = _columns.VisibleColumns.ToList();

			if (visibleColumns.Any())
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
				int x1 = _currentX.Value - (_columnDown.Width.Value / 2);
				int y1 = _currentY.Value - (CellHeight / 2);
				int x2 = x1 + _columnDown.Width.Value;
				int y2 = y1 + CellHeight;

				sPen =  new Pen(_backColor);
				e.Graphics.DrawRectangle(sPen, x1, y1, x2, y2);
				sBrush = new SolidBrush(_foreColor);
				//e.Graphics.DrawString(_columnDown.Text, _commonFont, sBrush, (PointF)(new Point(x1 + CellWidthPadding, y1 + CellHeightPadding)));
				GDIP_DrawString(e, _columnDown.Text, _commonFont, new Point(x1 + CellWidthPadding, y1 + CellHeightPadding), _foreColor);
			}
		}

		private void GDIP_DrawCellDrag(PaintEventArgs e)
		{
			if (_draggingCell != null)
			{
				var text = "";
				int offsetX = 0;
				int offsetY = 0;
				QueryItemText?.Invoke(_draggingCell.RowIndex.Value, _draggingCell.Column, out text, ref offsetX, ref offsetY);

				Color bgColor = _backColor;
				QueryItemBkColor?.Invoke(_draggingCell.RowIndex.Value, _draggingCell.Column, ref bgColor);

				int x1 = _currentX.Value - (_draggingCell.Column.Width.Value / 2);
				int y1 = _currentY.Value - (CellHeight / 2);
				int x2 = x1 + _draggingCell.Column.Width.Value;
				int y2 = y1 + CellHeight;

				sBrush = new SolidBrush(bgColor);
				e.Graphics.FillRectangle(sBrush, x1, y1, x2 - x1, y2 - y1);
				sBrush = new SolidBrush(_foreColor);
				//e.Graphics.DrawString(text, _commonFont, sBrush, (PointF)(new Point(x1 + CellWidthPadding + offsetX, y1 + CellHeightPadding + offsetY)));
				GDIP_DrawString(e, text, _commonFont, new Point(x1 + CellWidthPadding + offsetX, y1 + CellHeightPadding + offsetY), _foreColor);
			}
		}

		private void GDIP_DrawColumnText(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			if (HorizontalOrientation)
			{
				int start = -_vBar.Value;

				sBrush = new SolidBrush(_foreColor);

				foreach (var column in visibleColumns)
				{
					var point = new Point(CellWidthPadding, start + CellHeightPadding);

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						sBrush = new SolidBrush(SystemColors.HighlightText);
						//e.Graphics.DrawString(column.Text, _commonFont, sBrush, (PointF)(point));
						GDIP_DrawString(e, column.Text, _commonFont, point, SystemColors.HighlightText);
						sBrush = new SolidBrush(_foreColor);
					}
					else
					{
						//e.Graphics.DrawString(column.Text, _commonFont, sBrush, (PointF)(point));
						GDIP_DrawString(e, column.Text, _commonFont, point, _foreColor);
					}

					start += CellHeight;
				}
			}
			else
			{
				sBrush = new SolidBrush(_foreColor);

				foreach (var column in visibleColumns)
				{
					int xPadding = CellWidthPadding + 1 - _hBar.Value;
					var point = new Point(column.Left.Value + xPadding, CellHeightPadding);

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						sBrush = new SolidBrush(SystemColors.HighlightText);
						//e.Graphics.DrawString(column.Text, _commonFont, sBrush, (PointF)(point));
						GDIP_DrawString(e, column.Text, _commonFont, point, SystemColors.HighlightText);
						sBrush = new SolidBrush(_foreColor);
					}
					else
					{
						//e.Graphics.DrawString(column.Text, _commonFont, sBrush, (PointF)(point));
						GDIP_DrawString(e, column.Text, _commonFont, point, _foreColor);
					}
				}
			}
		}

		private void GDIP_DrawData(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			// Prevent exceptions with small TAStudio windows
			if (visibleColumns.Count == 0)
			{
				return;
			}

			bool isRotated = false;

			if (QueryItemText != null)
			{
				if (HorizontalOrientation)
				{
					int startRow = FirstVisibleRow;
					int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

					sBrush = new SolidBrush(_foreColor);
					for (int i = 0, f = 0; f < range; i++, f++)
					{
						f += _lagFrames[i];
						int LastVisible = LastVisibleColumnIndex;
						for (int j = FirstVisibleColumn; j <= LastVisible; j++)
						{
							Bitmap image = null;
							int x = 0;
							int y = 0;
							int bitmapOffsetX = 0;
							int bitmapOffsetY = 0;

							QueryItemIcon?.Invoke(f + startRow, visibleColumns[j], ref image, ref bitmapOffsetX, ref bitmapOffsetY);

							if (image != null)
							{
								x = RowsToPixels(i) + CellWidthPadding + bitmapOffsetX;
								y = (j * CellHeight) + (CellHeightPadding * 2) + bitmapOffsetY;
								e.Graphics.DrawImage(image, new Point(x, y));
							}

							string text;
							int strOffsetX = 0;
							int strOffsetY = 0;
							QueryItemText(f + startRow, visibleColumns[j], out text, ref strOffsetX, ref strOffsetY);

							// Center Text
							x = RowsToPixels(i) + ((CellWidth - (int)Math.Round((text.Length * _charSizeF.Width))) / 2);
							y = (j * CellHeight) + CellHeightPadding - _vBar.Value;
							var point = new Point(x + strOffsetX, y + strOffsetY);

							var rePrep = false;
							if (j == 1)
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

							if (!string.IsNullOrWhiteSpace(text))
							{
								//_gdi.DrawString(text, point);
								if (isRotated)
								{
									SizeF sz = e.Graphics.VisibleClipBounds.Size;
									e.Graphics.TranslateTransform(sz.Width / 2, sz.Height / 2);
									e.Graphics.RotateTransform(90);
									sz = e.Graphics.MeasureString(text, _commonFont);
									e.Graphics.DrawString(text, _commonFont, sBrush, -(sz.Width / 2), -(sz.Height / 2));
								}
								else
								{
									//e.Graphics.DrawString(text, _commonFont, sBrush, (PointF)point);
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
					int startRow = FirstVisibleRow;
					int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

					sBrush = new SolidBrush(_foreColor);

					int xPadding = CellWidthPadding + 1 - _hBar.Value;
					for (int i = 0, f = 0; f < range; i++, f++) // Vertical
					{
						f += _lagFrames[i];
						int LastVisible = LastVisibleColumnIndex;
						for (int j = FirstVisibleColumn; j <= LastVisible; j++) // Horizontal
						{
							RollColumn col = visibleColumns[j];

							string text;
							int strOffsetX = 0;
							int strOffsetY = 0;
							Point point = new Point(col.Left.Value + xPadding, RowsToPixels(i) + CellHeightPadding);

							Bitmap image = null;
							int bitmapOffsetX = 0;
							int bitmapOffsetY = 0;

							QueryItemIcon?.Invoke(f + startRow, visibleColumns[j], ref image, ref bitmapOffsetX, ref bitmapOffsetY);

							if (image != null)
							{
								e.Graphics.DrawImage(image, new Point(point.X + bitmapOffsetX, point.Y + bitmapOffsetY + CellHeightPadding));
							}

							QueryItemText(f + startRow, visibleColumns[j], out text, ref strOffsetX, ref strOffsetY);

							bool rePrep = false;
							if (_selectedItems.Contains(new Cell { Column = visibleColumns[j], RowIndex = f + startRow }))
							{
								sBrush = new SolidBrush(SystemColors.HighlightText);
								isRotated = false;
								rePrep = true;
							}

							if (!string.IsNullOrWhiteSpace(text))
							{
								//e.Graphics.DrawString(text, _commonFont, sBrush, (PointF)(new Point(point.X + strOffsetX, point.Y + strOffsetY)));
								GDIP_DrawString(e, text, _commonFont, new Point(point.X + strOffsetX, point.Y + strOffsetY), new Pen(sBrush).Color);
							}

							if (rePrep)
							{
								isRotated = false;
								sBrush = new SolidBrush(_foreColor);
							}
						}
					}
				}
			}
		}

		private void GDIP_DrawColumnBg(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			sBrush = new SolidBrush(SystemColors.ControlLight);
			sPen = new Pen(Color.Black);

			if (HorizontalOrientation)
			{
				e.Graphics.FillRectangle(sBrush, 0, 0, ColumnWidth + 1, DrawHeight + 1);
				e.Graphics.DrawLine(sPen, 0, 0, 0, visibleColumns.Count * CellHeight + 1);
				e.Graphics.DrawLine(sPen, ColumnWidth, 0, ColumnWidth, visibleColumns.Count * CellHeight + 1);

				int start = -_vBar.Value;
				foreach (var column in visibleColumns)
				{
					e.Graphics.DrawLine(sPen, 1, start, ColumnWidth, start);
					start += CellHeight;
				}

				if (visibleColumns.Any())
				{
					e.Graphics.DrawLine(sPen, 1, start, ColumnWidth, start);
				}
			}
			else
			{
				int bottomEdge = RowsToPixels(0);

				// Gray column box and black line underneath
				e.Graphics.FillRectangle(sBrush, 0, 0, Width + 1, bottomEdge + 1);
				e.Graphics.DrawLine(sPen, 0, 0, TotalColWidth.Value + 1, 0);
				e.Graphics.DrawLine(sPen, 0, bottomEdge, TotalColWidth.Value + 1, bottomEdge);

				// Vertical black seperators
				for (int i = 0; i < visibleColumns.Count; i++)
				{
					int pos = visibleColumns[i].Left.Value - _hBar.Value;
					e.Graphics.DrawLine(sPen, pos, 0, pos, bottomEdge);
				}

				// Draw right most line
				if (visibleColumns.Any())
				{
					int right = TotalColWidth.Value - _hBar.Value;
					e.Graphics.DrawLine(sPen, right, 0, right, bottomEdge);
				}
			}

			// Emphasis
			foreach (var column in visibleColumns.Where(c => c.Emphasis))
			{
				sBrush = new SolidBrush(SystemColors.ActiveBorder);
				if (HorizontalOrientation)
				{
					e.Graphics.FillRectangle(sBrush, 1, visibleColumns.IndexOf(column) * CellHeight + 1, ColumnWidth - 1, ColumnHeight - 1);
				}
				else
				{
					e.Graphics.FillRectangle(sBrush, column.Left.Value + 1 - _hBar.Value, 1, column.Width.Value - 1, ColumnHeight - 1);
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

						if (CurrentCell.Column.Emphasis)
						{
							sBrush = new SolidBrush(Color.FromArgb(SystemColors.Highlight.ToArgb() + 0x00222222));
						}
						else
						{
							sBrush = new SolidBrush(SystemColors.Highlight);
						}

						e.Graphics.FillRectangle(sBrush, 1, i * CellHeight + 1, ColumnWidth - 1, ColumnHeight - 1);
					}
				}
				else
				{
					// TODO multiple selected columns
					for (int i = 0; i < visibleColumns.Count; i++)
					{
						if (visibleColumns[i] == CurrentCell.Column)
						{
							// Left of column is to the right of the viewable area or right of column is to the left of the viewable area
							if (visibleColumns[i].Left.Value - _hBar.Value > Width || visibleColumns[i].Right.Value - _hBar.Value < 0)
							{
								continue;
							}

							int left = visibleColumns[i].Left.Value - _hBar.Value;
							int width = visibleColumns[i].Right.Value - _hBar.Value - left;

							if (CurrentCell.Column.Emphasis)
							{
								sBrush = new SolidBrush(Color.FromArgb(SystemColors.Highlight.ToArgb() + 0x00550000));
							}
							else
							{
								sBrush = new SolidBrush(SystemColors.Highlight);
							}

							e.Graphics.FillRectangle(sBrush, left + 1, 1, width - 1, ColumnHeight - 1);
						}
					}
				}
			}
		}

		private void GDIP_DrawBg(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			if (UseCustomBackground && QueryItemBkColor != null)
			{
				DoBackGroundCallback(e, visibleColumns);
			}

			if (GridLines)
			{
				sPen = new Pen(SystemColors.ControlLight);
				if (HorizontalOrientation)
				{
					// Columns
					for (int i = 1; i < VisibleRows + 1; i++)
					{
						int x = RowsToPixels(i);
						e.Graphics.DrawLine(sPen, x, 1, x, DrawHeight);
					}

					// Rows
					for (int i = 0; i < visibleColumns.Count + 1; i++)
					{
						e.Graphics.DrawLine(sPen, RowsToPixels(0) + 1, i * CellHeight - _vBar.Value, DrawWidth, i * CellHeight - _vBar.Value);
					}
				}
				else
				{
					// Columns
					int y = ColumnHeight + 1;
					int? totalColWidth = TotalColWidth;
					foreach (var column in visibleColumns)
					{
						int x = column.Left.Value - _hBar.Value;
						e.Graphics.DrawLine(sPen, x, y, x, Height - 1);
					}

					if (visibleColumns.Any())
					{
						e.Graphics.DrawLine(sPen, totalColWidth.Value - _hBar.Value, y, totalColWidth.Value - _hBar.Value, Height - 1);
					}

					// Rows
					for (int i = 1; i < VisibleRows + 1; i++)
					{
						e.Graphics.DrawLine(sPen, 0, RowsToPixels(i), Width + 1, RowsToPixels(i));
					}
				}
			}

			if (_selectedItems.Any())
			{
				DoSelectionBG(e, visibleColumns);
			}
		}

		private void GDIP_DrawCellBG(PaintEventArgs e, Color color, Cell cell, List<RollColumn> visibleColumns)
		{
			int x, y, w, h;

			if (HorizontalOrientation)
			{
				x = RowsToPixels(cell.RowIndex.Value) + 1;
				w = CellWidth - 1;
				y = (CellHeight * visibleColumns.IndexOf(cell.Column)) + 1 - _vBar.Value; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				h = CellHeight - 1;
				if (x < ColumnWidth)
				{
					return;
				}
			}
			else
			{
				w = cell.Column.Width.Value - 1;
				x = cell.Column.Left.Value - _hBar.Value + 1;
				y = RowsToPixels(cell.RowIndex.Value) + 1; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				h = CellHeight - 1;
				if (y < ColumnHeight)
				{
					return;
				}
			}

			if (x > DrawWidth || y > DrawHeight)
			{
				return;
			} // Don't draw if off screen.

			var col = cell.Column.Name;
			if (color.A == 0)
			{
				sBrush = new SolidBrush(Color.FromArgb(255, color));
			}
			else
			{
				sBrush = new SolidBrush(color);
			}				
			
			e.Graphics.FillRectangle(sBrush, x, y, w, h);			
		}

		private void GDIP_DrawString(PaintEventArgs e, string text, Font font, Point point, Color color)
		{
			//TextRenderer.DrawText(e.Graphics, text, font, point, color);
			e.Graphics.DrawString(text, font, new SolidBrush(color), (PointF)point);
		}

		#endregion
	}
}
