using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// This in the most part now contains renderer selection logic
	/// </summary>
	public partial class InputRoll
	{
		#region Renderer-Based Logic Methods

		protected override void OnPaint(PaintEventArgs e)
		{			
			if (Renderer == RollRenderer.GDIPlus)
				GDIP_OnPaint(e);
			else if (Renderer == RollRenderer.GDI)
				GDI_OnPaint(e);
		}

		private void DrawColumnDrag(PaintEventArgs e)
		{
			if (Renderer == RollRenderer.GDIPlus)
				GDIP_DrawColumnDrag(e);
			else if (Renderer == RollRenderer.GDI)
				GDI_DrawColumnDrag(e);
		}

		private void DrawCellDrag(PaintEventArgs e)
		{
			if (Renderer == RollRenderer.GDIPlus)
				GDIP_DrawCellDrag(e);
			else if (Renderer == RollRenderer.GDI)
				GDIP_DrawCellDrag(e);
		}

		private void DrawColumnText(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			if (Renderer == RollRenderer.GDIPlus)
				GDIP_DrawColumnText(e, visibleColumns);
			else if (Renderer == RollRenderer.GDI)
				GDI_DrawColumnText(e, visibleColumns);
		}

		private void DrawData(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			if (Renderer == RollRenderer.GDIPlus)
				GDIP_DrawData(e, visibleColumns);
			else if (Renderer == RollRenderer.GDI)
				GDI_DrawData(e, visibleColumns);
		}

		private void DrawColumnBg(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			if (Renderer == RollRenderer.GDIPlus)
				GDIP_DrawColumnBg(e, visibleColumns);
			else if (Renderer == RollRenderer.GDI)
				GDI_DrawColumnBg(e, visibleColumns);
		}

		// TODO refactor this and DoBackGroundCallback functions.
		/// <summary>
		/// Draw Gridlines and background colors using QueryItemBkColor.
		/// </summary>
		private void DrawBg(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			if (Renderer == RollRenderer.GDIPlus)
				GDIP_DrawBg(e, visibleColumns);
			else if (Renderer == RollRenderer.GDI)
				GDI_DrawBg(e, visibleColumns);
		}

		/// <summary>
		/// Given a cell with rowindex inbetween 0 and VisibleRows, it draws the background color specified. Do not call with absolute rowindices.
		/// </summary>
		private void DrawCellBG(PaintEventArgs e, Color color, Cell cell, List<RollColumn> visibleColumns)
		{
			if (Renderer == RollRenderer.GDIPlus)
				GDIP_DrawCellBG(e, color, cell, visibleColumns);
			else if (Renderer == RollRenderer.GDI)
				GDI_DrawCellBG(e, color, cell, visibleColumns);
		}

		#endregion

		#region Non-Renderer-Specific Methods

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Do nothing, and this should never be called
		}

		private void DoSelectionBG(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			// SuuperW: This allows user to see other colors in selected frames.
			Color rowColor = Color.White;
			int _lastVisibleRow = LastVisibleRow;
			int lastRow = -1;
			foreach (Cell cell in _selectedItems)
			{
				if (cell.RowIndex > _lastVisibleRow || cell.RowIndex < FirstVisibleRow || !VisibleColumns.Contains(cell.Column))
				{
					continue;
				}

				Cell relativeCell = new Cell
				{
					RowIndex = cell.RowIndex - FirstVisibleRow,
					Column = cell.Column,
				};
				relativeCell.RowIndex -= CountLagFramesAbsolute(relativeCell.RowIndex.Value);

				if (QueryRowBkColor != null && lastRow != cell.RowIndex.Value)
				{
					QueryRowBkColor(cell.RowIndex.Value, ref rowColor);
					lastRow = cell.RowIndex.Value;
				}

				Color cellColor = rowColor;
				QueryItemBkColor(cell.RowIndex.Value, cell.Column, ref cellColor);

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
				DrawCellBG(e, cellColor, relativeCell, visibleColumns);
			}
		}

		/// <summary>
		/// Calls QueryItemBkColor callback for all visible cells and fills in the background of those cells.
		/// </summary>
		/// <param name="e"></param>
		private void DoBackGroundCallback(PaintEventArgs e, List<RollColumn> visibleColumns)
		{
			int startIndex = FirstVisibleRow;
			int range = Math.Min(LastVisibleRow, RowCount - 1) - startIndex + 1;
			int lastVisible = LastVisibleColumnIndex;
			int firstVisibleColumn = FirstVisibleColumn;
			// Prevent exceptions with small TAStudio windows
			if (firstVisibleColumn < 0)
			{
				return;
			}
			if (HorizontalOrientation)
			{
				for (int i = 0, f = 0; f < range; i++, f++)
				{
					f += _lagFrames[i];

					Color rowColor = Color.White;
					QueryRowBkColor?.Invoke(f + startIndex, ref rowColor);

					for (int j = firstVisibleColumn; j <= lastVisible; j++)
					{
						Color itemColor = Color.White;
						QueryItemBkColor(f + startIndex, visibleColumns[j], ref itemColor);
						if (itemColor == Color.White)
						{
							itemColor = rowColor;
						}
						else if (itemColor.A != 255 && itemColor.A != 0)
						{
							float alpha = (float)itemColor.A / 255;
							itemColor = Color.FromArgb(rowColor.R - (int)((rowColor.R - itemColor.R) * alpha),
								rowColor.G - (int)((rowColor.G - itemColor.G) * alpha),
								rowColor.B - (int)((rowColor.B - itemColor.B) * alpha));
						}

						if (itemColor != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							var cell = new Cell
							{
								Column = visibleColumns[j],
								RowIndex = i
							};
							DrawCellBG(e, itemColor, cell, visibleColumns);
						}
					}
				}
			}
			else
			{
				for (int i = 0, f = 0; f < range; i++, f++) // Vertical
				{
					f += _lagFrames[i];

					Color rowColor = Color.White;
					QueryRowBkColor?.Invoke(f + startIndex, ref rowColor);

					for (int j = FirstVisibleColumn; j <= lastVisible; j++) // Horizontal
					{
						Color itemColor = Color.White;
						QueryItemBkColor(f + startIndex, visibleColumns[j], ref itemColor);
						if (itemColor == Color.White)
						{
							itemColor = rowColor;
						}
						else if (itemColor.A != 255 && itemColor.A != 0)
						{
							float alpha = (float)itemColor.A / 255;
							itemColor = Color.FromArgb(rowColor.R - (int)((rowColor.R - itemColor.R) * alpha),
								rowColor.G - (int)((rowColor.G - itemColor.G) * alpha),
								rowColor.B - (int)((rowColor.B - itemColor.B) * alpha));
						}

						if (itemColor != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							var cell = new Cell
							{
								Column = visibleColumns[j],
								RowIndex = i
							};
							DrawCellBG(e, itemColor, cell, visibleColumns);
						}
					}
				}
			}
		}

		#endregion
	}
}
