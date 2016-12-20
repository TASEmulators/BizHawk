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
	public partial class InputRoll : Control
	{
		private readonly GDIRenderer Gdi;
		private readonly SortedSet<Cell> SelectedItems = new SortedSet<Cell>(new sortCell());

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

		public bool allowRightClickSelecton { get; set; }
		public bool letKeysModifySelection { get; set; }
		public bool suspendHotkeys { get; set; }

		private IntPtr RotatedFont;
		private readonly IntPtr NormalFont;
		private Color _foreColor;
		private Color _backColor;

		public InputRoll()
		{
			UseCustomBackground = true;
			GridLines = true;
			CellWidthPadding = 3;
			CellHeightPadding = 0;
			CurrentCell = null;
			ScrollMethod = "near";

			Font CommonFont = new Font("Arial", 8, FontStyle.Bold);
			NormalFont = GDIRenderer.CreateNormalHFont(CommonFont, 6);
			// PrepDrawString doesn't actually set the font, so this is rather useless.
			// I'm leaving this stuff as-is so it will be a bit easier to fix up with another rendering method.
			RotatedFont = GDIRenderer.CreateRotatedHFont(CommonFont, true);

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);

			Gdi = new GDIRenderer();

			using (var g = CreateGraphics())
			using (var LCK = Gdi.LockGraphics(g))
			{
				_charSize = Gdi.MeasureString("A", CommonFont); // TODO make this a property so changing it updates other values.
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
				SmallChange = CellWidth,
				LargeChange = 20
			};

			Controls.Add(VBar);
			Controls.Add(HBar);

			VBar.ValueChanged += VerticalBar_ValueChanged;
			HBar.ValueChanged += HorizontalBar_ValueChanged;

			HorizontalOrientation = false;
			RecalculateScrollBars();
			_columns.ChangedCallback = ColumnChangedCallback;

			_hoverTimer.Interval = 750;
			_hoverTimer.Tick += HoverTimerEventProcessor;
			_hoverTimer.Stop();

			_foreColor = ForeColor;
			_backColor = BackColor;
		}

		private void HoverTimerEventProcessor(object sender, EventArgs e)
		{
			_hoverTimer.Stop();

			if (CellHovered != null)
			{
				CellHovered(this, new CellEventArgs(LastCell, CurrentCell));
			}
		}

		protected override void Dispose(bool disposing)
		{
			Gdi.Dispose();

			GDIRenderer.DestroyHFont(NormalFont);
			GDIRenderer.DestroyHFont(RotatedFont);

			base.Dispose(disposing);
		}

		private Timer _hoverTimer = new Timer();

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
					int temp = ScrollSpeed;
					_horizontalOrientation = value;
					OrientationChanged();
					HBar.SmallChange = CellWidth;
					VBar.SmallChange = CellHeight;
					ScrollSpeed = temp;
				}
			}
		}

		/// <summary>
		/// Gets or sets the scrolling speed
		/// </summary>
		[Category("Behavior")]
		public int ScrollSpeed
		{
			get
			{
				if (HorizontalOrientation)
					return HBar.SmallChange / CellWidth;
				else
					return VBar.SmallChange / CellHeight;
			}
			set
			{
				if (HorizontalOrientation)
					HBar.SmallChange = value * CellWidth;
				else
					VBar.SmallChange = value * CellHeight;
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

		/// <summary>
		/// Gets or sets how the Intever for the hover event
		/// </summary>
		[Category("Behavior")]
		public bool AlwaysScroll { get; set; }

		/// <summary>
		/// Gets or sets the lowest seek interval to activate the progress bar
		/// </summary>
		[Category("Behavior")]
		public int SeekingCutoffInterval { get; set; }

		/// <summary>
		/// Returns all columns including those that are not visible
		/// </summary>
		/// <returns></returns>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RollColumns AllColumns { get { return _columns; } }


		[DefaultValue(750)]
		[Category("Behavior")]
		public int HoverInterval
		{
			get { return _hoverTimer.Interval; }
			set { _hoverTimer.Interval = value; }
		}

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
		[Category("Virtual")]
		public event QueryRowBkColorHandler QueryRowBkColor;

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
		/// Fires when a cell is hovered on
		/// </summary>
		[Category("Mouse")]
		public event HoverEventHandler CellHovered;

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

		[Category("Action")]
		[Description("Occurs when the scroll value of the visible rows change (in vertical orientation this is the vertical scroll bar change, and in horizontal it is the horizontal scroll bar)")]
		public event RowScrollEvent RowScroll;

		[Category("Action")]
		[Description("Occurs when the scroll value of the columns (in vertical orientation this is the horizontal scroll bar change, and in horizontal it is the vertical scroll bar)")]
		public event ColumnScrollEvent ColumnScroll;

		[Category("Action")]
		[Description("Occurs when a cell is dragged and then dropped into a new cell, old cell is the cell that was being dragged, new cell is its new destination")]
		public event CellDroppedEvent CellDropped;

		/// <summary>
		/// Retrieve the text for a cell
		/// </summary>
		public delegate void QueryItemTextHandler(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY);

		/// <summary>
		/// Retrieve the background color for a cell
		/// </summary>
		public delegate void QueryItemBkColorHandler(int index, RollColumn column, ref Color color);
		public delegate void QueryRowBkColorHandler(int index, ref Color color);

		/// <summary>
		/// Retrive the image for a given cell
		/// </summary>
		public delegate void QueryItemIconHandler(int index, RollColumn column, ref Bitmap icon,  ref int offsetX, ref int offsetY);

		/// <summary>
		/// SuuperW: Check if a given frame is a lag frame
		/// </summary>
		public delegate bool QueryFrameLagHandler(int index, bool hideWasLag);

		public delegate void CellChangeEventHandler(object sender, CellEventArgs e);

		public delegate void HoverEventHandler(object sender, CellEventArgs e);

		public delegate void RightMouseScrollEventHandler(object sender, MouseEventArgs e);

		public delegate void ColumnClickEventHandler(object sender, ColumnClickEventArgs e);

		public delegate void ColumnReorderedEventHandler(object sender, ColumnReorderedEventArgs e);

		public delegate void RowScrollEvent(object sender, EventArgs e);

		public delegate void ColumnScrollEvent(object sender, EventArgs e);

		public delegate void CellDroppedEvent(object sender, CellEventArgs e);

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
					IEnumerable<Cell> items = SelectedItems.Where(cell => cell.RowIndex == index);
					SelectedItems.RemoveWhere(items.Contains);
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
			SelectedItems.RemoveWhere(cell => cell.RowIndex > index);
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsPointingAtColumnHeader
		{
			get
			{
				return IsHoveringOnColumnCell;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int? FirstSelectedIndex
		{
			get
			{
				if (AnyRowsSelected)
				{
					return SelectedRows.Min();
				}

				return null;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int? LastSelectedIndex
		{
			get
			{
				if (AnyRowsSelected)
				{
					return SelectedRows.Max();
				}

				return null;
			}
		}

		/// <summary>
		/// The current Cell that the mouse was in.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Cell CurrentCell { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CurrentCellIsDataCell
		{
			get
			{
				return CurrentCell != null && CurrentCell.RowIndex.HasValue && CurrentCell.Column != null;
			}
		}

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
				_columns.ChangedCallback = ColumnChangedCallback;
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
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		public bool IsPartiallyVisible(int index)
		{
			return (index >= FirstVisibleRow) && (index <= LastVisibleRow);
		}

		/// <summary>
		/// Gets the number of rows currently visible including partially visible rows.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int FirstVisibleColumn
		{
			get
			{
				if (HorizontalOrientation)
				{
					return VBar.Value / CellHeight;
				}
				else
				{
					List<RollColumn> columnList = VisibleColumns.ToList();
					return columnList.FindIndex(c => c.Right > HBar.Value);
				}
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
					ret = columnList.FindLastIndex(c => c.Left <= DrawWidth + HBar.Value);

				return ret;
			}
		}

		private Cell DraggingCell = null;

		public void DragCurrentCell()
		{
			DraggingCell = CurrentCell;
		}

		public void ReleaseCurrentCell()
		{
			if (DraggingCell != null)
			{
				var draggedCell = DraggingCell;
				DraggingCell = null;

				if (CurrentCell != draggedCell)
				{
					if (CellDropped != null)
					{
						CellDropped(this, new CellEventArgs(draggedCell, CurrentCell));
					}
				}
			}
		}

		/// <summary>
		/// Scrolls to the given index, according to the scroll settings.
		/// </summary>
		public void ScrollToIndex(int index)
		{
			if (ScrollMethod == "near")
			{
				MakeIndexVisible(index);
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
		/// <summary>
		/// Scrolls so that the given index is visible, if it isn't already; doesn't use scroll settings.
		/// </summary>
		public void MakeIndexVisible(int index)
		{
			if (!IsVisible(index))
			{
				if (FirstVisibleRow > index)
					FirstVisibleRow = index;
				else
					LastVisibleRow = index;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		public bool AnyRowsSelected
		{
			get
			{
				return SelectedItems.Any(cell => cell.RowIndex.HasValue);
			}
		}

		public void ClearSelectedRows()
		{
			SelectedItems.Clear();
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
				HorizontalOrientation ^= true;
			};

			yield return rotate;
		}

		public string RotateHotkeyStr
		{
			get { return "Ctrl+Shift+F"; }
		}

		#endregion

		#region Mouse and Key Events

		private bool _columnDownMoved = false;
		protected override void OnMouseMove(MouseEventArgs e)
		{
			_currentX = e.X;
			_currentY = e.Y;

			if (_columnDown != null)
			{
				_columnDownMoved = true;
			}

			Cell newCell = CalculatePointedCell(_currentX.Value, _currentY.Value);
			// SuuperW: Hide lag frames
			if (QueryFrameLag != null && newCell.RowIndex.HasValue)
			{
				newCell.RowIndex += CountLagFramesDisplay(newCell.RowIndex.Value);
			}
			newCell.RowIndex += FirstVisibleRow;
			if (newCell.RowIndex < 0)
				newCell.RowIndex = 0;

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
			_hoverTimer.Stop();
			Refresh();
			base.OnMouseLeave(e);
		}

		// TODO add query callback of whether to select the cell or not
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (!GlobalWin.MainForm.EmulatorPaused && _currentX.HasValue)
			{
				// copypaste from OnMouseMove()
				Cell newCell = CalculatePointedCell(_currentX.Value, _currentY.Value);
				if (QueryFrameLag != null && newCell.RowIndex.HasValue)
				{
					newCell.RowIndex += CountLagFramesDisplay(newCell.RowIndex.Value);
				}
				newCell.RowIndex += FirstVisibleRow;
				if (newCell.RowIndex < 0)
					newCell.RowIndex = 0;

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
				else if (_columnDown != null)
				{
					Refresh();
				}
			}

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
                        // do marker drag here
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
							if (SelectedIndexChanged != null)
							{
								SelectedIndexChanged(this, new EventArgs());
							}
						}
					}

					Refresh();
				}
			}

			base.OnMouseDown(e);

			if (allowRightClickSelecton && e.Button == MouseButtons.Right)
			{
				if (!IsHoveringOnColumnCell)
				{
					_currentX = e.X;
					_currentY = e.Y;
					Cell newCell = CalculatePointedCell(_currentX.Value, _currentY.Value);
					newCell.RowIndex += FirstVisibleRow;
					CellChanged(newCell);
					SelectCell(CurrentCell);
				}
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (IsHoveringOnColumnCell)
			{
				if (_columnDown != null && _columnDownMoved)
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
			_columnDownMoved = false;
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
				if (newVal > bar.Maximum - bar.LargeChange)
				{
					newVal = bar.Maximum - bar.LargeChange;
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
			if (!suspendHotkeys)
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
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Up) // Ctrl + Up
				{
					if (SelectedRows.Any() && letKeysModifySelection)
					{
						foreach (var row in SelectedRows.ToList())
						{
							SelectRow(row - 1, true);
							SelectRow(row, false);
						}
					}
				}
				else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Down) // Ctrl + Down
				{
					if (SelectedRows.Any() && letKeysModifySelection)
					{
						foreach (var row in SelectedRows.Reverse().ToList())
						{
							SelectRow(row + 1, true);
							SelectRow(row, false);
						}
					}
				}
				else if (!e.Control && e.Shift && !e.Alt && e.KeyCode == Keys.Up) // Shift + Up
				{
					if (SelectedRows.Any() && letKeysModifySelection)
					{
						SelectRow(SelectedRows.First() - 1, true);
					}
				}
				else if (!e.Control && e.Shift && !e.Alt && e.KeyCode == Keys.Down) // Shift + Down
				{
					if (SelectedRows.Any() && letKeysModifySelection)
					{
						SelectRow(SelectedRows.Last() + 1, true);
					}
				}
				else if (!e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Up) // Up
				{
					if (FirstVisibleRow > 0)
					{
						FirstVisibleRow--;
						Refresh();
					}
				}
				else if (!e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Down) // Down
				{
					if (FirstVisibleRow < RowCount - 1)
					{
						FirstVisibleRow++;
						Refresh();
					}
				}
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

			if (CurrentCell != null && CurrentCell.Column != null && CurrentCell.RowIndex.HasValue)
			{
				_hoverTimer.Start();
			}
			else
			{
				_hoverTimer.Stop();
			}
		}

		private void VerticalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}

			if (_horizontalOrientation)
			{
				if (ColumnScroll != null)
				{
					ColumnScroll(this, e);
				}
			}
			else
			{
				if (RowScroll != null)
				{
					RowScroll(this, e);
				}
			}

			if (CurrentCell == null)
				return;
		}

		private void HorizontalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}

			if (_horizontalOrientation)
			{
				if (RowScroll != null)
				{
					RowScroll(this, e);
				}
			}
			else
			{
				if (ColumnScroll != null)
				{
					ColumnScroll(this, e);
				}
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

		// TODO: Make into an extension method
		private static Color Add(Color color, int val)
		{
			var col = color.ToArgb();
			col += val;
			return Color.FromArgb(col);
		}

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
				NeedsHScrollbar = RowCount > 1;
			}
			else
			{
				NeedsVScrollbar = RowCount > 1;
				NeedsHScrollbar = TotalColWidth.HasValue && TotalColWidth.Value - DrawWidth + 1 > 0;
			}

			UpdateDrawSize();
			if (VisibleRows > 0)
			{
				if (HorizontalOrientation)
				{
					VBar.LargeChange = DrawHeight / 2;
					HBar.Maximum = Math.Max((VisibleRows - 1) * CellHeight, HBar.Maximum);
					HBar.LargeChange = (VisibleRows - 1) * CellHeight;
				}
				else
				{
					VBar.Maximum = Math.Max((VisibleRows - 1) * CellHeight, VBar.Maximum); // ScrollBar.Maximum is dumb
					VBar.LargeChange = (VisibleRows - 1) * CellHeight;
					HBar.LargeChange = DrawWidth / 2;
				}
			}

			//Update VBar
			if (NeedsVScrollbar)
			{
				if (HorizontalOrientation)
				{
					VBar.Maximum = ((columns.Count() * CellHeight) - DrawHeight) + VBar.LargeChange;
				}
				else
				{
					VBar.Maximum = RowsToPixels(RowCount + 1) - (CellHeight * 3) + VBar.LargeChange - 1;
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
					HBar.Maximum = RowsToPixels(RowCount + 1) - (CellHeight * 3) + HBar.LargeChange - 1;
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

				if (SelectedIndexChanged != null)
				{
					SelectedIndexChanged(this, new EventArgs());
				}
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
					newCell.RowIndex = PixelsToRows(x);

					int colIndex = (y + VBar.Value) / CellHeight;
					if (colIndex >= 0 && colIndex < columns.Count)
					{
						newCell.Column = columns[colIndex];
					}
				}
				else
				{
					newCell.RowIndex = PixelsToRows(y);
					newCell.Column = ColumnAtX(x);
				}
			}

			if (!(IsPaintDown || RightButtonHeld) && newCell.RowIndex == -1)
				newCell.RowIndex = null;

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
			foreach (RollColumn column in _columns.VisibleColumns)
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
			// Using Math.Floor and float because integer division rounds towards 0 but we want to round down.
			if (_horizontalOrientation)
			{
				return (int)Math.Floor((float)(pixels - ColumnWidth) / CellWidth);
			}
			return (int)Math.Floor((float)(pixels - ColumnHeight) / CellHeight);
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

		[Browsable(false)]
		public int RowHeight { get { return CellHeight; } }

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
			{
				for (int i = 0; i <= VisibleRows; i++)
				{
					lagFrames[i] = 0;
				}
			}
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
				// no check will make it crash for user too, not sure which way of alarm we prefer. no alarm at all will cause all sorts of subtle bugs
				if (ChangedCallback == null)
					System.Diagnostics.Debug.Fail("ColumnChangedCallback has died!");
				else
					ChangedCallback();
			}

			// TODO: this shouldn't be exposed.  But in order to not expose it, each RollColumn must have a change callback, and all property changes must call it, it is quicker and easier to just call this when needed
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
			private bool _emphasis;
			public bool Emphasis
			{
				get { return _emphasis; }
				set { _emphasis = value; }
			}

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

			public bool IsDataCell
			{
				get { return Column != null && RowIndex.HasValue; }
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

		private class sortCell : IComparer<Cell>
		{
			int IComparer<Cell>.Compare(Cell a, Cell b)
			{
				Cell c1 = a as Cell;
				Cell c2 = b as Cell;
				if (c1.RowIndex.HasValue)
				{
					if (c2.RowIndex.HasValue)
					{
						int row = c1.RowIndex.Value.CompareTo(c2.RowIndex.Value);
						if (row == 0)
						{
							return c1.Column.Name.CompareTo(c2.Column.Name);
						}
						else
							return row;
					}
					else
						return 1;
				}
				else if (c2.RowIndex.HasValue)
					return -1;
				else
					return c1.Column.Name.CompareTo(c2.Column.Name);
			}
		}
		#endregion
	}
}
