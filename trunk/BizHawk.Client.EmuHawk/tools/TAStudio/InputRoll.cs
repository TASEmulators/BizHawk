//TODO - do not constantly reference this.ForeColor and this.NormalFont. it should be a waste of time. Cache them (and be sure to respond to system messages when the user settings change)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;

namespace BizHawk.Client.EmuHawk
{
	//Row width depends on font size and padding
	//Column width is specified in column headers
	//Row width is specified for horizontal orientation
	public class InputRoll : Control
	{
		private readonly GDIRenderer Gdi;
		private readonly List<Cell> SelectedItems = new List<Cell>();

		private readonly VScrollBar VBar;
		private readonly HScrollBar HBar;

		private RollColumns _columns = new RollColumns();
		private bool _horizontalOrientation;
		private bool _programmaticallyUpdatingScrollBarValues;
		private int _maxCharactersInHorizontal = 1;

		private int _rowCount;
		private Size _charSize;

		private RollColumn _columnDown;

		private int? _currentX;
		private int? _currentY;

		// Hiding lag frames (Mainly intended for < 60fps play.)
		public int LagFramesToHide { get; set; }
		public bool HideWasLagFrames { get; set; }
		private byte[] lagFrames = new byte[100]; // Large enough value that it shouldn't ever need resizing.

		private IntPtr RotatedFont;
		private Font NormalFont;

		public InputRoll()
		{

			UseCustomBackground = true;
			GridLines = true;
			CellWidthPadding = 3;
			CellHeightPadding = 1;
			CurrentCell = null;
			ScrollMethod = "near";

			NormalFont = new Font("Courier New", 8);  // Only support fixed width

			// PrepDrawString doesn't actually set the font, so this is rather useless.
			// I'm leaving this stuff as-is so it will be a bit easier to fix up with another rendering method.
			RotatedFont = GDIRenderer.CreateRotatedHFont(Font, true);

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);

			Gdi = new GDIRenderer();

			using (var g = CreateGraphics())
			using (var LCK = Gdi.LockGraphics(g))
			{
				_charSize = Gdi.MeasureString("A", this.NormalFont); // TODO make this a property so changing it updates other values.
			}

			UpdateCellSize();
			ColumnWidth = CellWidth;
			ColumnHeight = CellHeight + 2;

			VBar = new VScrollBar
			{
				// Location gets calculated later (e.g. on resize)
				Visible = false,
				SmallChange = CellHeight,
				LargeChange = CellHeight * 20
			};

			HBar = new HScrollBar
			{
				// Location gets calculated later (e.g. on resize)
				Visible = false,
				SmallChange = 1,
				LargeChange = 20
			};

			this.Controls.Add(VBar);
			this.Controls.Add(HBar);

			VBar.ValueChanged += VerticalBar_ValueChanged;
			HBar.ValueChanged += HorizontalBar_ValueChanged;

			HorizontalOrientation = false;
			RecalculateScrollBars();
			_columns.ChangedCallback = ColumnChangedCallback;
		}

		protected override void Dispose(bool disposing)
		{
			Gdi.Dispose();

			this.NormalFont.Dispose();
			GDIRenderer.DestroyHFont(RotatedFont);

			base.Dispose(disposing);
		}

		#region Properties

		/// <summary>
		/// Gets or sets the amount of left and right padding on the text inside a cell
		/// </summary>
		[DefaultValue(3)]
		[Category("Behavior")]
		public int CellWidthPadding { get; set; }

		/// <summary>
		/// Gets or sets the amount of top and bottom padding on the text inside a cell
		/// </summary>
		[DefaultValue(1)]
		[Category("Behavior")]
		public int CellHeightPadding { get; set; }

		/// <summary>
		/// Displays grid lines around cells
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool GridLines { get; set; }

		/// <summary>
		/// Gets or sets whether the control is horizontal or vertical
		/// </summary>
		[Category("Behavior")]
		public bool HorizontalOrientation
		{
			get
			{
				return _horizontalOrientation;
			}
			set
			{
				if (_horizontalOrientation != value)
				{
					_horizontalOrientation = value;
					OrientationChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets the sets the virtual number of rows to be displayed. Does not include the column header row.
		/// </summary>
		[Category("Behavior")]
		public int RowCount
		{
			get
			{
				return _rowCount;
			}

			set
			{
				_rowCount = value;
				RecalculateScrollBars();
			}
		}

		/// <summary>
		/// Gets or sets the sets the columns can be resized
		/// </summary>
		[Category("Behavior")]
		public bool AllowColumnResize { get; set; }

		/// <summary>
		/// Gets or sets the sets the columns can be reordered
		/// </summary>
		[Category("Behavior")]
		public bool AllowColumnReorder { get; set; }

		/// <summary>
		/// Indicates whether the entire row will always be selected 
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool FullRowSelect { get; set; }

		/// <summary>
		/// Allows multiple items to be selected
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(true)]
		public bool MultiSelect { get; set; }

		/// <summary>
		/// Gets or sets whether or not the control is in input painting mode
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(false)]
		public bool InputPaintingMode { get; set; }

		/// <summary>
		/// All visible columns
		/// </summary>
		[Category("Behavior")]
		public IEnumerable<RollColumn> VisibleColumns { get { return _columns.VisibleColumns; } }

		/// <summary>
		/// Gets or sets how the InputRoll scrolls when calling ScrollToIndex.
		/// </summary>
		[DefaultValue("near")]
		[Category("Behavior")]
		public string ScrollMethod { get; set; }

		[Category("Behavior")]
		public bool AlwaysScroll { get; set; }

		/// <summary>
		/// Returns all columns including those that are not visible
		/// </summary>
		/// <returns></returns>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public RollColumns AllColumns { get { return _columns; } }

		#endregion

		#region Event Handlers

		/// <summary>
		/// Fire the QueryItemText event which requests the text for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemTextHandler QueryItemText;

		/// <summary>
		/// Fire the QueryItemBkColor event which requests the background color for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemBkColorHandler QueryItemBkColor;

		/// <summary>
		/// Fire the QueryItemIconHandler event which requests an icon for a given cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemIconHandler QueryItemIcon;

		/// <summary>
		/// SuuperW: Fire the QueryFrameLag event which checks if a given frame is a lag frame
		/// </summary>
		[Category("Virtual")]
		public event QueryFrameLagHandler QueryFrameLag;

		/// <summary>
		/// Fires when the mouse moves from one cell to another (including column header cells)
		/// </summary>
		[Category("Mouse")]
		public event CellChangeEventHandler PointedCellChanged;

		/// <summary>
		/// Occurs when a column header is clicked
		/// </summary>
		[Category("Action")]
		public event ColumnClickEventHandler ColumnClick;

		/// <summary>
		/// Occurs when a column header is right-clicked
		/// </summary>
		[Category("Action")]
		public event ColumnClickEventHandler ColumnRightClick;

		/// <summary>
		/// Occurs whenever the 'SelectedItems' property for this control changes
		/// </summary>
		[Category("Behavior")]
		public event EventHandler SelectedIndexChanged;

		/// <summary>
		/// Occurs whenever the mouse wheel is scrolled while the right mouse button is held
		/// </summary>
		[Category("Behavior")]
		public event RightMouseScrollEventHandler RightMouseScrolled;

		[Category("Property Changed")]
		[Description("Occurs when the column header has been reordered")]
		public event ColumnReorderedEventHandler ColumnReordered;

		/// <summary>
		/// Retrieve the text for a cell
		/// </summary>
		public delegate void QueryItemTextHandler(int index, RollColumn column, out string text);

		/// <summary>
		/// Retrieve the background color for a cell
		/// </summary>
		public delegate void QueryItemBkColorHandler(int index, RollColumn column, ref Color color);

		/// <summary>
		/// Retrive the image for a given cell
		/// </summary>
		public delegate void QueryItemIconHandler(int index, RollColumn column, ref Bitmap icon);

		/// <summary>
		/// SuuperW: Check if a given frame is a lag frame
		/// </summary>
		public delegate bool QueryFrameLagHandler(int index, bool hideWasLag);

		public delegate void CellChangeEventHandler(object sender, CellEventArgs e);

		public delegate void RightMouseScrollEventHandler(object sender, MouseEventArgs e);

		public delegate void ColumnClickEventHandler(object sender, ColumnClickEventArgs e);

		public delegate void ColumnReorderedEventHandler(object sender, ColumnReorderedEventArgs e);

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

		public class ColumnClickEventArgs
		{
			public ColumnClickEventArgs(RollColumn column)
			{
				Column = column;
			}

			public RollColumn Column { get; private set; }
		}

		public class ColumnReorderedEventArgs
		{
			public ColumnReorderedEventArgs(int oldDisplayIndex, int newDisplayIndex, RollColumn column)
			{
				Column = column;
				OldDisplayIndex = oldDisplayIndex;
				NewDisplayIndex = NewDisplayIndex;
			}

			public RollColumn Column { get; private set; }
			public int OldDisplayIndex { get; private set; }
			public int NewDisplayIndex { get; private set; }
		}

		#endregion

		#region Api

		public void SelectRow(int index, bool val)
		{
			if (_columns.VisibleColumns.Any())
			{
				if (val)
				{
					SelectCell(new Cell
					{
						RowIndex = index,
						Column = _columns[0]
					});
				}
				else
				{
					var items = SelectedItems.Where(cell => cell.RowIndex == index);
					SelectedItems.RemoveAll(items.Contains);
				}
			}
		}

		public void SelectAll()
		{
			var oldFullRowVal = FullRowSelect;
			FullRowSelect = true;
			for (int i = 0; i < RowCount; i++)
			{
				SelectRow(i, true);
			}

			FullRowSelect = oldFullRowVal;
		}
		public void DeselectAll()
		{
			SelectedItems.Clear();
		}
		public void TruncateSelection(int index)
		{
			SelectedItems.RemoveAll(cell => cell.RowIndex > index);
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool IsPointingAtColumnHeader
		{
			get
			{
				return IsHoveringOnColumnCell;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int? FirstSelectedIndex
		{
			get
			{
				if (SelectedRows.Any())
				{
					return SelectedRows
						.OrderBy(x => x)
						.First();
				}

				return null;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int? LastSelectedIndex
		{
			get
			{
				if (SelectedRows.Any())
				{
					return SelectedRows
						.OrderBy(x => x)
						.Last();
				}

				return null;
			}
		}

		/// <summary>
		/// The current Cell that the mouse was in.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public Cell CurrentCell { get; set; }

		/// <summary>
		/// The previous Cell that the mouse was in.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public Cell LastCell { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool IsPaintDown { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool UseCustomBackground { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int DrawHeight { get; private set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int DrawWidth { get; private set; }

		/// <summary>
		/// Sets the width of data cells when in Horizontal orientation.
		/// </summary>
		public int MaxCharactersInHorizontal
		{
			get
			{
				return _maxCharactersInHorizontal;
			}
			set
			{
				_maxCharactersInHorizontal = value;
				UpdateCellSize();
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool RightButtonHeld { get; set; }

		public string UserSettingsSerialized()
		{
			var settings = ConfigService.SaveWithType(Settings);
			return settings;
		}

		public void LoadSettingsSerialized(string settingsJson)
		{
			var settings = ConfigService.LoadWithType(settingsJson);

			// TODO: don't silently fail, inform the user somehow
			if (settings is InputRollSettings)
			{
				var rollSettings = settings as InputRollSettings;
				_columns = rollSettings.Columns;
				HorizontalOrientation = rollSettings.HorizontalOrientation;
				LagFramesToHide = rollSettings.LagFramesToHide;
				HideWasLagFrames = rollSettings.HideWasLagFrames;
			}
		}


		private InputRollSettings Settings
		{
			get
			{
				return new InputRollSettings
				{
					Columns = _columns,
					HorizontalOrientation = HorizontalOrientation,
					LagFramesToHide = LagFramesToHide,
					HideWasLagFrames = HideWasLagFrames
				};
			}
		}

		public class InputRollSettings
		{
			public RollColumns Columns { get; set; }
			public bool HorizontalOrientation { get; set; }
			public int LagFramesToHide { get; set; }
			public bool HideWasLagFrames { get; set; }
		}

		/// <summary>
		/// Gets or sets the first visible row index, if scrolling is needed
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int FirstVisibleRow
		{
			get // SuuperW: This was checking if the scroll bars were needed, which is useless because their Value is 0 if they aren't needed.
			{
				if (HorizontalOrientation)
				{
					return HBar.Value / CellWidth;
				}

				return VBar.Value / CellHeight;
			}

			set
			{
				if (HorizontalOrientation)
				{
					if (NeedsHScrollbar)
					{
						_programmaticallyUpdatingScrollBarValues = true;
						if (value * CellWidth <= HBar.Maximum)
							HBar.Value = value * CellWidth;
						else
							HBar.Value = HBar.Maximum;
						_programmaticallyUpdatingScrollBarValues = false;
					}
				}
				else
				{
					if (NeedsVScrollbar)
					{
						_programmaticallyUpdatingScrollBarValues = true;
						if (value * CellHeight <= VBar.Maximum)
							VBar.Value = value * CellHeight;
						else
							VBar.Value = VBar.Maximum;
						_programmaticallyUpdatingScrollBarValues = false;
					}
				}
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		private int LastFullyVisibleRow
		{
			get
			{
				int HalfRow = 0;
				if ((DrawHeight - ColumnHeight - 3) % CellHeight < CellHeight / 2)
					HalfRow = 1;
				return FirstVisibleRow + VisibleRows - HalfRow + CountLagFramesDisplay(VisibleRows - HalfRow);
			}
		}
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int LastVisibleRow
		{
			get
			{
				return FirstVisibleRow + VisibleRows + CountLagFramesDisplay(VisibleRows);
			}

			set
			{
				int HalfRow = 0;
				if ((DrawHeight - ColumnHeight - 3) % CellHeight < CellHeight / 2)
					HalfRow = 1;
				if (LagFramesToHide == 0)
				{
					FirstVisibleRow = Math.Max(value - (VisibleRows - HalfRow), 0);
				}
				else
				{
					if (Math.Abs(LastFullyVisibleRow - value) > VisibleRows) // Big jump
					{
						FirstVisibleRow = Math.Max(value - (ExpectedDisplayRange() - HalfRow), 0);
						SetLagFramesArray();
					}

					// Small jump, more accurate
					int lastVisible = LastFullyVisibleRow;
					do
					{
						if ((lastVisible - value) / (LagFramesToHide + 1) != 0)
							FirstVisibleRow = Math.Max(FirstVisibleRow - ((lastVisible - value) / (LagFramesToHide + 1)), 0);
						else
							FirstVisibleRow -= Math.Sign(lastVisible - value);
						SetLagFramesArray();
						lastVisible = LastFullyVisibleRow;
					} while ((lastVisible - value < 0 || lastVisible - value > lagFrames[VisibleRows - HalfRow]) && FirstVisibleRow != 0);
				}
			}
		}

		public bool IsVisible(int index)
		{
			return (index >= FirstVisibleRow) && (index <= LastFullyVisibleRow);
		}

		/// <summary>
		/// Gets the number of rows currently visible including partially visible rows.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int VisibleRows
		{
			get
			{
				if (HorizontalOrientation)
				{
					return (DrawWidth - ColumnWidth) / CellWidth;
				}

				return (DrawHeight - ColumnHeight - 3) / CellHeight; // Minus three makes it work
			}
		}

		/// <summary>
		/// Gets the first visible column index, if scrolling is needed
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int FirstVisibleColumn
		{
			get
			{
				List<RollColumn> columnList = VisibleColumns.ToList();
				if (HorizontalOrientation)
					return VBar.Value / CellHeight;
				else
					return columnList.FindIndex(c => c.Right > 0);
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int LastVisibleColumnIndex
		{
			get
			{
				List<RollColumn> columnList = VisibleColumns.ToList();
				int ret;
				if (HorizontalOrientation)
				{
					ret = (VBar.Value + DrawHeight) / CellHeight;
					if (ret >= columnList.Count)
						ret = columnList.Count - 1;
				}
				else
					ret = columnList.FindLastIndex(c => c.Left <= DrawWidth);

				return ret;
			}
		}

		public void ScrollToIndex(int index)
		{
			if (ScrollMethod == "near" && !IsVisible(index))
			{
				if (FirstVisibleRow > index)
					FirstVisibleRow = index;
				else
					LastVisibleRow = index;
			}
			if (!IsVisible(index) || AlwaysScroll)
			{
				if (ScrollMethod == "top")
					FirstVisibleRow = index;
				else if (ScrollMethod == "bottom")
					LastVisibleRow = index;
				else if (ScrollMethod == "center")
				{
					if (LagFramesToHide == 0)
						FirstVisibleRow = Math.Max(index - (VisibleRows / 2), 0);
					else
					{
						if (Math.Abs(FirstVisibleRow + CountLagFramesDisplay(VisibleRows / 2) - index) > VisibleRows) // Big jump
						{
							FirstVisibleRow = Math.Max(index - (ExpectedDisplayRange() / 2), 0);
							SetLagFramesArray();
						}

						// Small jump, more accurate
						int lastVisible = FirstVisibleRow + CountLagFramesDisplay(VisibleRows / 2);
						do
						{
							if ((lastVisible - index) / (LagFramesToHide + 1) != 0)
								FirstVisibleRow = Math.Max(FirstVisibleRow - ((lastVisible - index) / (LagFramesToHide + 1)), 0);
							else
								FirstVisibleRow -= Math.Sign(lastVisible - index);
							SetLagFramesArray();
							lastVisible = FirstVisibleRow + CountLagFramesDisplay(VisibleRows / 2);
						} while ((lastVisible - index < 0 || lastVisible - index > lagFrames[VisibleRows]) && FirstVisibleRow != 0);
					}
				}
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public IEnumerable<int> SelectedRows
		{
			get
			{
				return SelectedItems
					.Where(cell => cell.RowIndex.HasValue)
					.Select(cell => cell.RowIndex.Value)
					.Distinct();
			}
		}

		public IEnumerable<ToolStripItem> GenerateContextMenuItems()
		{
			yield return new ToolStripSeparator();

			var rotate = new ToolStripMenuItem
			{
				Name = "RotateMenuItem",
				Text = "Rotate",
				ShortcutKeyDisplayString = RotateHotkeyStr,
			};

			rotate.Click += (o, ev) =>
			{
				this.HorizontalOrientation ^= true;
			};

			yield return rotate;
		}

		public string RotateHotkeyStr
		{
			get { return "Ctrl+Shift+F"; }
		}

		#endregion

		#region Paint

		protected override void OnPaint(PaintEventArgs e)
		{
			using (var LCK = Gdi.LockGraphics(e.Graphics))
			{
				Gdi.StartOffScreenBitmap(Width, Height);

				//White Background
				Gdi.SetBrush(Color.White);
				Gdi.SetSolidPen(Color.White);
				Gdi.FillRectangle(0, 0, Width, Height);

				// Lag frame calculations
				SetLagFramesArray();

				if (_columns.VisibleColumns.Any())
				{
					DrawColumnBg(e);
					DrawColumnText(e);
				}

				//Background
				DrawBg(e);

				//Foreground
				DrawData(e);

				DrawColumnDrag(e);

				Gdi.CopyToScreen();
				Gdi.EndOffScreenBitmap();
			}
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Do nothing, and this should never be called
		}

		private void DrawColumnDrag(PaintEventArgs e)
		{
			if (_columnDown != null && _currentX.HasValue && _currentY.HasValue && IsHoveringOnColumnCell)
			{
				int x1 = _currentX.Value - (_columnDown.Width.Value / 2);
				int y1 = _currentY.Value - (CellHeight / 2);
				int x2 = x1 + _columnDown.Width.Value;
				int y2 = y1 + CellHeight;

				Gdi.SetSolidPen(this.BackColor);
				Gdi.DrawRectangle(x1, y1, x2, y2);
				Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
				Gdi.DrawString(_columnDown.Text, new Point(x1 + CellWidthPadding, y1 + CellHeightPadding));
			}
		}

		private void DrawColumnText(PaintEventArgs e)
		{
			var columns = _columns.VisibleColumns.ToList();

			if (HorizontalOrientation)
			{
				int start = 0;

				Gdi.PrepDrawString(this.RotatedFont, this.ForeColor);

				foreach (var column in columns)
				{
					var point = new Point(CellWidthPadding, start + CellHeightPadding);

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						Gdi.PrepDrawString(this.NormalFont, SystemColors.HighlightText);
						Gdi.DrawString(column.Text, point);
						Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
					}
					else
					{
						Gdi.DrawString(column.Text, point);
					}

					start += CellHeight;
				}
			}
			else
			{
				//zeromus test
				//Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
				Gdi.PrepDrawString(this.RotatedFont, this.ForeColor);

				foreach (var column in columns)
				{
					var point = new Point(column.Left.Value + 2 * CellWidthPadding - HBar.Value, CellHeightPadding); // TODO: fix this CellPadding issue (2 * CellPadding vs just CellPadding)

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						//zeromus test
						//Gdi.PrepDrawString(this.NormalFont, SystemColors.HighlightText);
						Gdi.PrepDrawString(this.RotatedFont, SystemColors.HighlightText);
						Gdi.DrawString(column.Text, point);
						//zeromus test
						//Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
						Gdi.PrepDrawString(this.RotatedFont, this.ForeColor);
					}
					else
					{
						Gdi.DrawString(column.Text, point);
					}
				}
			}
		}

		private void DrawData(PaintEventArgs e)
		{
			var columns = _columns.VisibleColumns.ToList();
			if (QueryItemText != null)
			{
				if (HorizontalOrientation)
				{
					int startRow = FirstVisibleRow;
					int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

					Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
					for (int i = 0, f = 0; f < range; i++, f++)
					{
						f += lagFrames[i];
						int LastVisible = LastVisibleColumnIndex;
						for (int j = FirstVisibleColumn; j <= LastVisible; j++)
						{
							Bitmap image = null;
							int x = 0;
							int y = 0;

							if (QueryItemIcon != null)
							{
								x = RowsToPixels(i) + CellWidthPadding;
								y = (j * CellHeight) + (CellHeightPadding * 2);

								QueryItemIcon(f + startRow, columns[j], ref image);
							}

							if (image != null)
							{
								Gdi.DrawBitmap(image, new Point(x, y), true);
							}
							else
							{
								string text;
								QueryItemText(f + startRow, columns[j], out text);

								// Center Text
								x = RowsToPixels(i) + (CellWidth - text.Length * _charSize.Width) / 2;
								y = (j * CellHeight) + CellHeightPadding;
								var point = new Point(x, y);

								var rePrep = false;
								if (SelectedItems.Contains(new Cell { Column = columns[j], RowIndex = i + startRow }))
								{
									Gdi.PrepDrawString(this.NormalFont, SystemColors.HighlightText);
									rePrep = true;
								}


								if (!string.IsNullOrWhiteSpace(text))
								{
									Gdi.DrawString(text, point);
								}

								if (rePrep)
								{
									Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
								}
							}
						}
					}
				}
				else
				{
					int startRow = FirstVisibleRow;
					int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

					Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
					int xPadding = CellWidthPadding + 1 - HBar.Value;
					for (int i = 0, f = 0; f < range; i++, f++) // Vertical
					{
						f += lagFrames[i];
						int LastVisible = LastVisibleColumnIndex;
						for (int j = FirstVisibleColumn; j <= LastVisible; j++) // Horizontal
						{
							var col = columns[j];
							if (col.Left.Value < 0 || col.Left.Value > DrawWidth)
							{
								continue;
							}

							string text;
							var point = new Point(col.Left.Value + xPadding, RowsToPixels(i) + CellHeightPadding);

							Bitmap image = null;
							if (QueryItemIcon != null)
							{
								QueryItemIcon(f + startRow, columns[j], ref image);
							}

							if (image != null)
							{
								Gdi.DrawBitmap(image, new Point(col.Left.Value, point.Y + CellHeightPadding), true);
							}
							else
							{
								QueryItemText(f + startRow, columns[j], out text);

								bool rePrep = false;
								if (SelectedItems.Contains(new Cell { Column = columns[j], RowIndex = f + startRow }))
								{
									Gdi.PrepDrawString(this.NormalFont, SystemColors.HighlightText);
									rePrep = true;
								}

								if (!string.IsNullOrWhiteSpace(text))
								{
									Gdi.DrawString(text, point);
								}

								if (rePrep)
								{
									Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
								}
							}
						}
					}
				}
			}
		}

		private void DrawColumnBg(PaintEventArgs e)
		{
			Gdi.SetBrush(SystemColors.ControlLight);
			Gdi.SetSolidPen(Color.Black);

			var columns = _columns.VisibleColumns.ToList();

			if (HorizontalOrientation)
			{
				Gdi.FillRectangle(0, 0, ColumnWidth + 1, DrawHeight + 1);
				Gdi.Line(0, 0, 0, columns.Count * CellHeight + 1);
				Gdi.Line(ColumnWidth, 0, ColumnWidth, columns.Count * CellHeight + 1);

				int start = 0;
				foreach (var column in columns)
				{
					Gdi.Line(1, start, ColumnWidth, start);
					start += CellHeight;
				}

				if (columns.Any())
				{
					Gdi.Line(1, start, ColumnWidth, start);
				}
			}
			else
			{
				int bottomEdge = RowsToPixels(0);
				//Gray column box and black line underneath
				Gdi.FillRectangle(0, 0, Width + 1, bottomEdge + 1);
				Gdi.Line(0, 0, TotalColWidth.Value + 1, 0);
				Gdi.Line(0, bottomEdge, TotalColWidth.Value + 1, bottomEdge);

				//Vertical black seperators
				for (int i = 0; i < columns.Count; i++)
				{
					int pos = columns[i].Left.Value - HBar.Value;
					Gdi.Line(pos, 0, pos, bottomEdge);
				}

				////Draw right most line
				if (columns.Any())
				{
					int right = TotalColWidth.Value - HBar.Value;
					Gdi.Line(right, 0, right, bottomEdge);
				}
			}

			// Emphasis
			foreach (var column in columns.Where(c => c.Emphasis))
			{
				Gdi.SetBrush(SystemColors.ActiveBorder);
				if (HorizontalOrientation)
				{
					Gdi.FillRectangle(1, columns.IndexOf(column) * CellHeight + 1, ColumnWidth - 1, ColumnHeight - 1);
				}
				else
				{
					Gdi.FillRectangle(column.Left.Value + 1, 1, column.Width.Value - 1, ColumnHeight - 1);
				}
			}

			// If the user is hovering over a column
			if (IsHoveringOnColumnCell)
			{
				if (HorizontalOrientation)
				{
					for (int i = 0; i < columns.Count; i++)
					{
						if (columns[i] != CurrentCell.Column)
						{
							continue;
						}

						if (CurrentCell.Column.Emphasis)
						{
							Gdi.SetBrush(Add(SystemColors.Highlight, 0x00222222));
						}
						else
						{
							Gdi.SetBrush(SystemColors.Highlight);
						}

						Gdi.FillRectangle(1, i * CellHeight + 1, ColumnWidth - 1, ColumnHeight - 1);
					}
				}
				else
				{
					//TODO multiple selected columns
					for (int i = 0; i < columns.Count; i++)
					{
						if (columns[i] == CurrentCell.Column)
						{
							//Left of column is to the right of the viewable area or right of column is to the left of the viewable area
							if (columns[i].Left.Value - HBar.Value > Width || columns[i].Right.Value - HBar.Value < 0)
							{
								continue;
							}
							int left = columns[i].Left.Value - HBar.Value;
							int width = columns[i].Right.Value - HBar.Value - left;

							if (CurrentCell.Column.Emphasis)
							{
								Gdi.SetBrush(Add(SystemColors.Highlight, 0x00550000));
							}
							else
							{
								Gdi.SetBrush(SystemColors.Highlight);
							}

							Gdi.FillRectangle(left + 1, 1, width - 1, ColumnHeight - 1);
						}
					}
				}
			}
		}

		// TODO: Make into an extension method
		private static Color Add(Color color, int val)
		{
			var col = color.ToArgb();
			col += val;
			return Color.FromArgb(col);
		}

		//TODO refactor this and DoBackGroundCallback functions.
		/// <summary>
		/// Draw Gridlines and background colors using QueryItemBkColor.
		/// </summary>
		/// <param name="e"></param>
		private void DrawBg(PaintEventArgs e)
		{
			if (QueryItemBkColor != null && UseCustomBackground)
			{
				DoBackGroundCallback(e);
			}

			if (GridLines)
			{
				var columns = _columns.VisibleColumns.ToList();

				Gdi.SetSolidPen(SystemColors.ControlLight);
				if (HorizontalOrientation)
				{
					// Columns
					for (int i = 1; i < VisibleRows + 1; i++)
					{
						var x = RowsToPixels(i);
						Gdi.Line(x, 1, x, DrawHeight);
					}

					// Rows
					for (int i = 0; i < columns.Count + 1; i++)
					{
						Gdi.Line(RowsToPixels(0) + 1, i * CellHeight, DrawWidth, i * CellHeight);
					}
				}
				else
				{
					// Columns
					int y = ColumnHeight + 1;
					foreach (var column in columns)
					{
						int x = column.Left.Value - HBar.Value;
						Gdi.Line(x, y, x, Height - 1);
					}

					if (columns.Any())
					{
						Gdi.Line(TotalColWidth.Value - HBar.Value, y, TotalColWidth.Value - HBar.Value, Height - 1);
					}

					// Rows
					for (int i = 1; i < VisibleRows + 1; i++)
					{
						Gdi.Line(0, RowsToPixels(i), Width + 1, RowsToPixels(i));
					}
				}
			}

			if (SelectedItems.Any())
			{
				DoSelectionBG(e);
			}
		}

		private void DoSelectionBG(PaintEventArgs e)
		{
			// SuuperW: This allows user to see other colors in selected frames.
			Color Highlight_Color = new Color();
			foreach (var cell in SelectedItems)
			{
				if (cell.RowIndex > LastVisibleRow || cell.RowIndex < FirstVisibleRow)
					continue;

				var relativeCell = new Cell
				{
					RowIndex = cell.RowIndex - FirstVisibleRow,
					Column = cell.Column,
				};
				relativeCell.RowIndex -= CountLagFramesAbsolute(relativeCell.RowIndex.Value);

				QueryItemBkColor(cell.RowIndex.Value, cell.Column, ref Highlight_Color);
				Highlight_Color = Color.FromArgb((Highlight_Color.R + SystemColors.Highlight.R) / 2
					, (Highlight_Color.G + SystemColors.Highlight.G) / 2
					, (Highlight_Color.B + SystemColors.Highlight.B) / 2);
				DrawCellBG(Highlight_Color, relativeCell);
			}
		}

		/// <summary>
		/// Given a cell with rowindex inbetween 0 and VisibleRows, it draws the background color specified. Do not call with absolute rowindices.
		/// </summary>
		/// <param name="color"></param>
		/// <param name="cell"></param>
		private void DrawCellBG(Color color, Cell cell)
		{
			var columns = _columns.VisibleColumns.ToList();

			int x, y, w, h;

			if (HorizontalOrientation)
			{
				x = RowsToPixels(cell.RowIndex.Value) + 1;
				w = CellWidth - 1;
				y = (CellHeight * columns.IndexOf(cell.Column)) + 1; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				h = CellHeight - 1;
				if (x < ColumnWidth) { return; }
			}
			else
			{
				w = cell.Column.Width.Value - 1;
				x = cell.Column.Left.Value - HBar.Value + 1;
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

			Gdi.SetBrush(color);
			Gdi.FillRectangle(x, y, w, h);
		}

		/// <summary>
		/// Calls QueryItemBkColor callback for all visible cells and fills in the background of those cells.
		/// </summary>
		/// <param name="e"></param>
		private void DoBackGroundCallback(PaintEventArgs e)
		{
			var columns = _columns.VisibleColumns.ToList();

			if (HorizontalOrientation)
			{
				int startIndex = FirstVisibleRow;
				int range = Math.Min(LastVisibleRow, RowCount - 1) - startIndex + 1;

				for (int i = 0, f = 0; f < range; i++, f++)
				{
					f += lagFrames[i];
					int LastVisible = LastVisibleColumnIndex;
					for (int j = FirstVisibleColumn; j <= LastVisible; j++) // TODO: Don't query all columns
					{
						var color = Color.White;
						QueryItemBkColor(f + startIndex, columns[j], ref color);

						if (color != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							var cell = new Cell()
							{
								Column = columns[j],
								RowIndex = i
							};
							DrawCellBG(color, cell);
						}
					}
				}
			}
			else
			{
				int startRow = FirstVisibleRow;
				int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

				for (int i = 0, f = 0; f < range; i++, f++) // Vertical
				{
					f += lagFrames[i];
					int LastVisible = LastVisibleColumnIndex;
					for (int j = FirstVisibleColumn; j <= LastVisible; j++) // Horizontal
					{
						var color = Color.White;
						QueryItemBkColor(f + startRow, columns[j], ref color);
						if (color != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							var cell = new Cell
							{
								Column = columns[j],
								RowIndex = i
							};
							DrawCellBG(color, cell);
						}
					}
				}
			}
		}

		#endregion

		#region Mouse and Key Events

		protected override void OnMouseMove(MouseEventArgs e)
		{
			_currentX = e.X;
			_currentY = e.Y;

			if (IsPaintDown)
			{
				if (HorizontalOrientation)
				{
					if (e.X <= ColumnWidth)
						_currentX = ColumnWidth + 2; // 2 because ColumnWidth/Height isn't correct
					else if (e.X > Width)
						_currentX = Width;
				}
				else
				{
					if (e.Y <= ColumnHeight)
						_currentY = ColumnHeight + 2;
					else if (e.Y > Height)
						_currentX = Height;
				}
			}
			var newCell = CalculatePointedCell(_currentX.Value, _currentY.Value);
			// SuuperW: Hide lag frames
			if (QueryFrameLag != null && newCell.RowIndex.HasValue)
			{
				newCell.RowIndex += CountLagFramesDisplay(newCell.RowIndex.Value);
			}
			newCell.RowIndex += FirstVisibleRow;
			if (!newCell.Equals(CurrentCell))
			{
				CellChanged(newCell);

				if (IsHoveringOnColumnCell ||
					(WasHoveringOnColumnCell && !IsHoveringOnColumnCell))
				{
					Refresh();
				}
				else if (_columnDown != null)
				{
					Refresh();
				}
			}
			else if (_columnDown != null)  // Kind of silly feeling to have this check twice, but the only alternative I can think of has it refreshing twice when pointed column changes with column down, and speed matters
			{
				Refresh();
			}

			base.OnMouseMove(e);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			CurrentCell = new Cell
			{
				Column = null,
				RowIndex = null
			};

			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			_currentX = null;
			_currentY = null;
			CurrentCell = null;
			IsPaintDown = false;
			Refresh();
			base.OnMouseLeave(e);
		}

		// TODO add query callback of whether to select the cell or not
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (IsHoveringOnColumnCell)
				{
					_columnDown = CurrentCell.Column;
				}
				else if (InputPaintingMode)
				{
					IsPaintDown = true;
				}
			}

			if (e.Button == MouseButtons.Right)
			{
				if (!IsHoveringOnColumnCell)
				{
					RightButtonHeld = true;
				}
			}

			if (e.Button == MouseButtons.Left)
			{
				if (IsHoveringOnDataCell)
				{
					if (ModifierKeys == Keys.Alt)
					{
						MessageBox.Show("Alt click logic is not yet implemented");
					}
					else if (ModifierKeys == Keys.Shift)
					{
						if (SelectedItems.Any())
						{
							if (FullRowSelect)
							{
								var selected = SelectedItems.Any(c => c.RowIndex.HasValue && CurrentCell.RowIndex.HasValue && c.RowIndex == CurrentCell.RowIndex);

								if (!selected)
								{
									var rowIndices = SelectedItems
										.Where(c => c.RowIndex.HasValue)
										.Select(c => c.RowIndex ?? -1)
										.Where(c => c >= 0) // Hack to avoid possible Nullable exceptions
										.Distinct()
										.ToList();


									var firstIndex = rowIndices.Min();
									var lastIndex = rowIndices.Max();

									if (CurrentCell.RowIndex.Value < firstIndex)
									{
										for (int i = CurrentCell.RowIndex.Value; i < firstIndex; i++)
										{
											SelectCell(new Cell
												{
													RowIndex = i,
													Column = CurrentCell.Column
												});
										}
									}
									else if (CurrentCell.RowIndex.Value > lastIndex)
									{
										for (int i = lastIndex + 1; i <= CurrentCell.RowIndex.Value; i++)
										{
											SelectCell(new Cell
											{
												RowIndex = i,
												Column = CurrentCell.Column
											});
										}
									}
									else // Somewhere in between, a scenario that can happen with ctrl-clicking, find the previous and highlight from there
									{
										var nearest = rowIndices
											.Where(x => x < CurrentCell.RowIndex.Value)
											.Max();

										for (int i = nearest + 1; i <= CurrentCell.RowIndex.Value; i++)
										{
											SelectCell(new Cell
											{
												RowIndex = i,
												Column = CurrentCell.Column
											});
										}
									}
								}
							}
							else
							{
								MessageBox.Show("Shift click logic for individual cells has not yet implemented");
							}
						}
						else
						{
							SelectCell(CurrentCell);
						}
					}
					else if (ModifierKeys == Keys.Control)
					{
						SelectCell(CurrentCell, toggle: true);
					}
					else
					{
						var hadIndex = SelectedItems.Any();
						SelectedItems.Clear();
						SelectCell(CurrentCell);

						// In this case the SelectCell did not invoke the change event since there was nothing to select
						// But we went from selected to unselected, that is a change, so catch it here
						if (hadIndex && CurrentCell.RowIndex.HasValue && CurrentCell.RowIndex > RowCount)
						{
							SelectedIndexChanged(this, new EventArgs());
						}
					}

					Refresh();
				}
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (IsHoveringOnColumnCell)
			{
				if (_columnDown != null)
				{
					DoColumnReorder();
					_columnDown = null;
					Refresh();
				}
				else if (e.Button == MouseButtons.Left)
				{
					ColumnClickEvent(ColumnAtX(e.X));
				}
				else if (e.Button == MouseButtons.Right)
				{
					ColumnRightClickEvent(ColumnAtX(e.X));
				}
			}

			_columnDown = null;
			RightButtonHeld = false;
			IsPaintDown = false;
			base.OnMouseUp(e);
		}

		private void IncrementScrollBar(ScrollBar bar, bool increment)
		{
			int newVal;
			if (increment)
			{
				newVal = bar.Value + bar.SmallChange;
				if (newVal > bar.Maximum)
				{
					newVal = bar.Maximum;
				}
			}
			else
			{
				newVal = bar.Value - bar.SmallChange;
				if (newVal < 0)
				{
					newVal = 0;
				}
			}

			_programmaticallyUpdatingScrollBarValues = true;
			bar.Value = newVal;
			_programmaticallyUpdatingScrollBarValues = false;
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (RightButtonHeld)
			{
				DoRightMouseScroll(this, e);
			}
			else
			{
				if (HorizontalOrientation)
				{
					do
					{
						IncrementScrollBar(HBar, e.Delta < 0);
						SetLagFramesFirst();
					} while (lagFrames[0] != 0 && HBar.Value != 0 && HBar.Value != HBar.Maximum);
				}
				else
				{
					do
					{
						IncrementScrollBar(VBar, e.Delta < 0);
						SetLagFramesFirst();
					} while (lagFrames[0] != 0 && VBar.Value != 0 && VBar.Value != VBar.Maximum);
				}

				if (_currentX != null)
					OnMouseMove(new MouseEventArgs(System.Windows.Forms.MouseButtons.None, 0, _currentX.Value, _currentY.Value, 0));
				Refresh();
			}
		}

		private void DoRightMouseScroll(object sender, MouseEventArgs e)
		{
			if (RightMouseScrolled != null)
			{
				RightMouseScrolled(sender, e);
			}
		}

		private void ColumnClickEvent(RollColumn column)
		{
			if (ColumnClick != null)
			{
				ColumnClick(this, new ColumnClickEventArgs(column));
			}
		}

		private void ColumnRightClickEvent(RollColumn column)
		{
			if (ColumnRightClick != null)
			{
				ColumnRightClick(this, new ColumnClickEventArgs(column));
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Control && !e.Alt && e.Shift && e.KeyCode == Keys.F) // Ctrl+Shift+F
			{
				HorizontalOrientation ^= true;
			}
			else if (!e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.PageUp) // Page Up
			{
				if (FirstVisibleRow > 0)
				{
					LastVisibleRow = FirstVisibleRow;
					Refresh();
				}
			}
			else if (!e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.PageDown) // Page Down
			{
				var totalRows = LastVisibleRow - FirstVisibleRow;
				if (totalRows <= RowCount)
				{
					var final = LastVisibleRow + totalRows;
					if (final > RowCount)
					{
						final = RowCount;
					}

					LastVisibleRow = final;
					Refresh();
				}
			}
			else if (!e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.Home) // Home
			{
				FirstVisibleRow = 0;
				Refresh();
			}
			else if (!e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.End) // End
			{
				LastVisibleRow = RowCount;
				Refresh();
			}

			base.OnKeyDown(e);
		}

		#endregion

		#region Change Events

		protected override void OnResize(EventArgs e)
		{
			RecalculateScrollBars();
			base.OnResize(e);
			Refresh();
		}

		private void OrientationChanged()
		{
			RecalculateScrollBars();

			// TODO scroll to correct positions

			if (HorizontalOrientation)
			{
				VBar.SmallChange = CellHeight;
				HBar.SmallChange = CellWidth;
				VBar.LargeChange = 10;
				HBar.LargeChange = CellWidth * 20;
			}
			else
			{
				VBar.SmallChange = CellHeight;
				HBar.SmallChange = 1;
				VBar.LargeChange = CellHeight * 20;
				HBar.LargeChange = 20;
			}

			ColumnChangedCallback();
			RecalculateScrollBars();

			Refresh();
		}

		/// <summary>
		/// Call this function to change the CurrentCell to newCell
		/// </summary>
		private void CellChanged(Cell newCell)
		{
			LastCell = CurrentCell;
			CurrentCell = newCell;

			if (PointedCellChanged != null &&
				(LastCell.Column != CurrentCell.Column || LastCell.RowIndex != CurrentCell.RowIndex))
			{
				PointedCellChanged(this, new CellEventArgs(LastCell, CurrentCell));
			}
		}

		private void VerticalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}
		}

		private void HorizontalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}
		}

		private void ColumnChangedCallback()
		{
			RecalculateScrollBars();
			if (_columns.VisibleColumns.Any())
			{
				ColumnWidth = _columns.VisibleColumns.Max(c => c.Width.Value) + CellWidthPadding * 4;
			}
		}

		#endregion

		#region Helpers

		private void DoColumnReorder()
		{
			if (_columnDown != CurrentCell.Column)
			{
				var oldIndex = _columns.IndexOf(_columnDown);
				var newIndex = _columns.IndexOf(CurrentCell.Column);

				if (ColumnReordered != null)
				{
					ColumnReordered(this, new ColumnReorderedEventArgs(oldIndex, newIndex, _columnDown));
				}

				_columns.Remove(_columnDown);
				_columns.Insert(newIndex, _columnDown);
			}
		}

		//ScrollBar.Maximum = DesiredValue + ScrollBar.LargeChange - 1
		//See MSDN Page for more information on the dumb ScrollBar.Maximum Property
		private void RecalculateScrollBars()
		{
			UpdateDrawSize();

			var columns = _columns.VisibleColumns.ToList();

			if (HorizontalOrientation)
			{
				NeedsVScrollbar = columns.Count > DrawHeight / CellHeight;
				NeedsHScrollbar = RowCount > (DrawWidth - ColumnWidth) / CellWidth;
			}
			else
			{
				NeedsVScrollbar = RowCount > DrawHeight / CellHeight;
				NeedsHScrollbar = TotalColWidth.HasValue && TotalColWidth.Value - DrawWidth + 1 > 0;
			}

			UpdateDrawSize();

			//Update VBar
			if (NeedsVScrollbar)
			{
				if (HorizontalOrientation)
				{
					VBar.Maximum = (((columns.Count() * CellHeight) - DrawHeight) / CellHeight) + VBar.LargeChange;
				}
				else
				{
					VBar.Maximum = RowsToPixels(RowCount + 1) - DrawHeight + VBar.LargeChange - 1;
				}

				VBar.Location = new Point(Width - VBar.Width, 0);
				VBar.Height = Height;
				VBar.Visible = true;
			}
			else
			{
				VBar.Visible = false;
				VBar.Value = 0;
			}

			//Update HBar
			if (NeedsHScrollbar)
			{
				if (HorizontalOrientation)
				{
					HBar.Maximum = RowsToPixels(RowCount + 1) - DrawWidth + HBar.LargeChange - 1;
				}
				else
				{
					HBar.Maximum = TotalColWidth.Value - DrawWidth + HBar.LargeChange;
				}

				HBar.Location = new Point(0, Height - HBar.Height);
				HBar.Width = Width - (NeedsVScrollbar ? (VBar.Width + 1) : 0);
				HBar.Visible = true;
			}
			else
			{
				HBar.Visible = false;
				HBar.Value = 0;
			}
		}

		private void UpdateDrawSize()
		{
			if (NeedsVScrollbar)
			{
				DrawWidth = Width - VBar.Width;
			}
			else
			{
				DrawWidth = Width;
			}
			if (NeedsHScrollbar)
			{
				DrawHeight = Height - HBar.Height;
			}
			else
			{
				DrawHeight = Height;
			}
		}

		/// <summary>
		/// If FullRowSelect is enabled, selects all cells in the row that contains the given cell. Otherwise only given cell is added.
		/// </summary>
		/// <param name="cell">The cell to select.</param>
		private void SelectCell(Cell cell, bool toggle = false)
		{
			if (cell.RowIndex.HasValue && cell.RowIndex < RowCount)
			{
				if (!MultiSelect)
				{
					SelectedItems.Clear();
				}

				if (FullRowSelect)
				{
					if (toggle && SelectedItems.Any(x => x.RowIndex.HasValue && x.RowIndex == cell.RowIndex))
					{
						var items = SelectedItems
							.Where(x => x.RowIndex.HasValue && x.RowIndex == cell.RowIndex)
							.ToList();

						foreach (var item in items)
						{
							SelectedItems.Remove(item);
						}
					}
					else
					{
						foreach (var column in _columns)
						{
							SelectedItems.Add(new Cell
							{
								RowIndex = cell.RowIndex,
								Column = column
							});
						}
					}
				}
				else
				{
					if (toggle && SelectedItems.Any(x => x.RowIndex.HasValue && x.RowIndex == cell.RowIndex))
					{
						var item = SelectedItems
							.FirstOrDefault(x => x.Equals(cell));

						if (item != null)
						{
							SelectedItems.Remove(item);
						}
					}
					else
					{
						SelectedItems.Add(CurrentCell);
					}
				}

				SelectedIndexChanged(this, new EventArgs());
			}
		}

		/// <summary>
		/// Bool that indicates if CurrentCell is a Column Cell.
		/// </summary>
		private bool IsHoveringOnColumnCell
		{
			get
			{
				return CurrentCell != null &&
					CurrentCell.Column != null &&
					!CurrentCell.RowIndex.HasValue;
			}
		}

		/// <summary>
		/// Bool that indicates if CurrentCell is a Data Cell.
		/// </summary>
		private bool IsHoveringOnDataCell
		{
			get
			{
				return CurrentCell != null &&
					CurrentCell.Column != null &&
					CurrentCell.RowIndex.HasValue;
			}
		}

		/// <summary>
		/// Bool that indicates if CurrentCell is a Column Cell.
		/// </summary>
		private bool WasHoveringOnColumnCell
		{
			get
			{
				return LastCell != null &&
					LastCell.Column != null &&
					!LastCell.RowIndex.HasValue;
			}
		}

		/// <summary>
		/// Bool that indicates if CurrentCell is a Data Cell.
		/// </summary>
		private bool WasHoveringOnDataCell
		{
			get
			{
				return LastCell != null &&
					LastCell.Column != null &&
					LastCell.RowIndex.HasValue;
			}
		}

		/// <summary>
		/// Finds the specific cell that contains the (x, y) coordinate.
		/// </summary>
		/// <remarks>The row number that it returns will be between 0 and VisibleRows, NOT the absolute row number.</remarks>
		/// <param name="x">X coordinate point.</param>
		/// <param name="y">Y coordinate point.</param>
		/// <returns>The cell with row number and RollColumn reference, both of which can be null. </returns>
		private Cell CalculatePointedCell(int x, int y)
		{
			var newCell = new Cell();
			var columns = _columns.VisibleColumns.ToList();

			// If pointing to a column header
			if (columns.Any())
			{
				if (HorizontalOrientation)
				{
					if (x >= ColumnWidth)
					{
						newCell.RowIndex = PixelsToRows(x);
					}

					int colIndex = (y / CellHeight);
					if (colIndex >= 0 && colIndex < columns.Count)
					{
						newCell.Column = columns[colIndex];
					}
				}
				else
				{
					if (y >= CellHeight)
					{
						newCell.RowIndex = PixelsToRows(y);
					}

					newCell.Column = ColumnAtX(x);
				}
			}
			return newCell;
		}

		/// <summary>
		/// A boolean that indicates if the InputRoll is too large vertically and requires a vertical scrollbar.
		/// </summary>
		private bool NeedsVScrollbar { get; set; }

		/// <summary>
		/// A boolean that indicates if the InputRoll is too large horizontally and requires a horizontal scrollbar.
		/// </summary>
		private bool NeedsHScrollbar { get; set; }

		/// <summary>
		/// Updates the width of the supplied column.
		/// <remarks>Call when changing the ColumnCell text, CellPadding, or text font.</remarks>
		/// </summary>
		/// <param name="col">The RollColumn object to update.</param>
		/// <returns>The new width of the RollColumn object.</returns>
		private int UpdateWidth(RollColumn col)
		{
			col.Width = ((col.Text.Length * _charSize.Width) + (CellWidthPadding * 4));
			return col.Width.Value;
		}

		/// <summary>
		/// Gets the total width of all the columns by using the last column's Right property.
		/// </summary>
		/// <returns>A nullable Int representing total width.</returns>
		private int? TotalColWidth
		{
			get
			{
				if (_columns.VisibleColumns.Any())
				{
					return _columns.VisibleColumns.Last().Right;
				}
				return null;
			}
		}

		/// <summary>
		/// Returns the RollColumn object at the specified visible x coordinate. Coordinate should be between 0 and Width of the InputRoll Control.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <returns>RollColumn object that contains the x coordinate or null if none exists.</returns>
		private RollColumn ColumnAtX(int x)
		{
			foreach (var column in _columns.VisibleColumns)
			{
				if (column.Left.Value - HBar.Value <= x && column.Right.Value - HBar.Value >= x)
				{
					return column;
				}
			}

			return null;
		}

		/// <summary>
		/// Converts a row number to a horizontal or vertical coordinate.
		/// </summary>
		/// <returns>A vertical coordinate if Vertical Oriented, otherwise a horizontal coordinate.</returns>
		private int RowsToPixels(int index)
		{
			if (_horizontalOrientation)
			{
				return (index * CellWidth) + ColumnWidth;
			}

			return (index * CellHeight) + ColumnHeight;
		}

		/// <summary>
		/// Converts a horizontal or vertical coordinate to a row number.
		/// </summary>
		/// <param name="pixels">A vertical coordinate if Vertical Oriented, otherwise a horizontal coordinate.</param>
		/// <returns>A row number between 0 and VisibleRows if it is a Datarow, otherwise a negative number if above all Datarows.</returns>
		private int PixelsToRows(int pixels)
		{
			if (_horizontalOrientation)
			{
				return (pixels - ColumnWidth) / CellWidth;
			}
			return (pixels - ColumnHeight) / CellHeight;
		}

		/// <summary>
		/// The width of the largest column cell in Horizontal Orientation
		/// </summary>
		private int ColumnWidth { get; set; }

		/// <summary>
		/// The height of a column cell in Vertical Orientation.
		/// </summary>
		private int ColumnHeight { get; set; }

		//Cell defaults
		/// <summary>
		/// The width of a cell in Horizontal Orientation. Only can be changed by changing the Font or CellPadding.
		/// </summary>
		private int CellWidth { get; set; }

		/// <summary>
		/// The height of a cell in Vertical Orientation. Only can be changed by changing the Font or CellPadding.
		/// </summary>
		private int CellHeight { get; set; }

		/// <summary>
		/// Call when _charSize, MaxCharactersInHorizontal, or CellPadding is changed.
		/// </summary>
		private void UpdateCellSize()
		{
			CellHeight = _charSize.Height + (CellHeightPadding * 2);
			CellWidth = (_charSize.Width * MaxCharactersInHorizontal) + (CellWidthPadding * 4); // Double the padding for horizontal because it looks better
		}

		// SuuperW: Count lag frames between FirstDisplayed and given display position
		private int CountLagFramesDisplay(int relativeIndex)
		{
			if (QueryFrameLag != null && LagFramesToHide != 0)
			{
				int count = 0;
				for (int i = 0; i <= relativeIndex; i++)
					count += lagFrames[i];

				return count;
			}
			return 0;
		}
		// Count lag frames between FirstDisplayed and given relative frame index
		private int CountLagFramesAbsolute(int relativeIndex)
		{
			if (QueryFrameLag != null && LagFramesToHide != 0)
			{
				int count = 0;
				for (int i = 0; i + count <= relativeIndex; i++)
					count += lagFrames[i];

				return count;
			}
			return 0;
		}

		private void SetLagFramesArray()
		{
			if (QueryFrameLag != null && LagFramesToHide != 0)
			{
				bool showNext = false;
				// First one needs to check BACKWARDS for lag frame count.
				SetLagFramesFirst();
				int f = lagFrames[0];
				if (QueryFrameLag(FirstVisibleRow + f, HideWasLagFrames))
					showNext = true;
				for (int i = 1; i <= VisibleRows; i++)
				{
					lagFrames[i] = 0;
					if (!showNext)
					{
						for (; lagFrames[i] < LagFramesToHide; lagFrames[i]++)
						{
							if (!QueryFrameLag(FirstVisibleRow + i + f, HideWasLagFrames))
								break;
							f++;
						}
					}
					else
					{
						if (!QueryFrameLag(FirstVisibleRow + i + f, HideWasLagFrames))
							showNext = false;
					}
					if (lagFrames[i] == LagFramesToHide && QueryFrameLag(FirstVisibleRow + i + f, HideWasLagFrames))
					{
						showNext = true;
					}
				}
			}
			else
				for (int i = 0; i <= VisibleRows; i++)
					lagFrames[i] = 0;
		}
		private void SetLagFramesFirst()
		{
			if (QueryFrameLag != null && LagFramesToHide != 0)
			{
				// Count how many lag frames are above displayed area.
				int count = 0;
				do
				{
					count++;
				} while (QueryFrameLag(FirstVisibleRow - count, HideWasLagFrames) && count <= LagFramesToHide);
				count--;
				// Count forward
				int fCount = -1;
				do
				{
					fCount++;
				} while (QueryFrameLag(FirstVisibleRow + fCount, HideWasLagFrames) && count + fCount < LagFramesToHide);
				lagFrames[0] = (byte)fCount;
			}
			else
				lagFrames[0] = 0;
		}

		// Number of displayed + hidden frames, if fps is as expected
		private int ExpectedDisplayRange()
		{
			return (VisibleRows + 1) * LagFramesToHide;
		}

		#endregion

		#region Classes

		public class RollColumns : List<RollColumn>
		{
			public RollColumn this[string name]
			{
				get
				{
					return this.SingleOrDefault(column => column.Name == name);
				}
			}

			public IEnumerable<RollColumn> VisibleColumns
			{
				get
				{
					return this.Where(c => c.Visible);
				}
			}

			public Action ChangedCallback { get; set; }

			private void DoChangeCallback()
			{
				if (ChangedCallback != null)
				{
					ChangedCallback();
				}
			}

			// TODO: this shouldn't be exposed.  But in order to not expose it, each RollColumn must have a chane callback, and all property changes must call it, it is quicker and easier to just call this when needed
			public void ColumnsChanged()
			{
				int pos = 0;

				var columns = VisibleColumns.ToList();

				for (int i = 0; i < columns.Count; i++)
				{
					columns[i].Left = pos;
					pos += columns[i].Width.Value;
					columns[i].Right = pos;
				}
				DoChangeCallback();
			}

			public new void Add(RollColumn column)
			{
				if (this.Any(c => c.Name == column.Name))
				{
					// The designer sucks, doing nothing for now
					return;
					//throw new InvalidOperationException("A column with this name already exists.");
				}

				base.Add(column);
				ColumnsChanged();
			}

			public new void AddRange(IEnumerable<RollColumn> collection)
			{
				foreach (var column in collection)
				{
					if (this.Any(c => c.Name == column.Name))
					{
						// The designer sucks, doing nothing for now
						return;

						throw new InvalidOperationException("A column with this name already exists.");
					}
				}

				base.AddRange(collection);
				ColumnsChanged();
			}

			public new void Insert(int index, RollColumn column)
			{
				if (this.Any(c => c.Name == column.Name))
				{
					throw new InvalidOperationException("A column with this name already exists.");
				}

				base.Insert(index, column);
				ColumnsChanged();
			}

			public new void InsertRange(int index, IEnumerable<RollColumn> collection)
			{
				foreach (var column in collection)
				{
					if (this.Any(c => c.Name == column.Name))
					{
						throw new InvalidOperationException("A column with this name already exists.");
					}
				}

				base.InsertRange(index, collection);
				ColumnsChanged();
			}

			public new bool Remove(RollColumn column)
			{
				var result = base.Remove(column);
				ColumnsChanged();
				return result;
			}

			public new int RemoveAll(Predicate<RollColumn> match)
			{
				var result = base.RemoveAll(match);
				ColumnsChanged();
				return result;
			}

			public new void RemoveAt(int index)
			{
				base.RemoveAt(index);
				ColumnsChanged();
			}

			public new void RemoveRange(int index, int count)
			{
				base.RemoveRange(index, count);
				ColumnsChanged();
			}

			public new void Clear()
			{
				base.Clear();
				ColumnsChanged();
			}

			public IEnumerable<string> Groups
			{
				get
				{
					return this
						.Select(x => x.Group)
						.Distinct();
				}
			}
		}

		public class RollColumn
		{
			public enum InputType { Boolean, Float, Text, Image }

			public string Group { get; set; }
			public int? Width { get; set; }
			public int? Left { get; set; }
			public int? Right { get; set; }
			public string Name { get; set; }
			public string Text { get; set; }
			public InputType Type { get; set; }
			public bool Visible { get; set; }

			/// <summary>
			/// Column will be drawn with an emphasized look, if true
			/// </summary>
			public bool Emphasis { get; set; }

			public RollColumn()
			{
				Visible = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public class Cell
		{
			public RollColumn Column { get; internal set; }
			public int? RowIndex { get; internal set; }
			public string CurrentText { get; internal set; }

			public Cell() { }

			public Cell(Cell cell)
			{
				Column = cell.Column;
				RowIndex = cell.RowIndex;
			}

			public override bool Equals(object obj)
			{
				if (obj is Cell)
				{
					var cell = obj as Cell;
					return this.Column == cell.Column && this.RowIndex == cell.RowIndex;
				}

				return base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return Column.GetHashCode() + RowIndex.GetHashCode();
			}
		}

		#endregion
	}
}
