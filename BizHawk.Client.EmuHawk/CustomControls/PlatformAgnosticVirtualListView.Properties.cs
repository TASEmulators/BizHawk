using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A performant VirtualListView implementation that doesn't rely on native Win32 API calls
	/// (and in fact does not inherit the ListView class at all)
	/// It is an enhanced version of the work done with GDI+ rendering in InputRoll.cs
	/// -------------------------
	/// *** Public Properties ***
	/// -------------------------
	/// </summary>
	public partial class PlatformAgnosticVirtualListView
	{
		#region ListView Compatibility Properties

		/// <summary>
		/// This VirtualListView implementation doesn't really need this, but it is here for compatibility
		/// </summary>
		[Category("Behavior")]
		public int VirtualListSize
		{
			get
			{
				return _itemCount;
			}

			set
			{
				_itemCount = value;
				RecalculateScrollBars();				
			}
		}

		/// <summary>
		/// ListView compatibility property
		/// THIS DOES NOT WORK PROPERLY - AVOID!
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public System.Windows.Forms.ListView.SelectedIndexCollection SelectedIndices
		{
			// !!! does not work properly, avoid using this in the calling implementation !!!
			get
			{
				var tmpListView = new System.Windows.Forms.ListView();
				//tmpListView.VirtualMode = true;
				//var selectedIndexCollection = new System.Windows.Forms.ListView.SelectedIndexCollection(tmpListView);
				//tmpListView.VirtualListSize = ItemCount;
				for (int i = 0; i < ItemCount; i++)
				{
					tmpListView.Items.Add(i.ToString());
				}
				
				//tmpListView.Refresh();				

				if (AnyRowsSelected)
				{
					var indices = SelectedRows.ToList();
					foreach (var i in indices)
					{
						tmpListView.SelectedIndices.Add(i);
						//selectedIndexCollection.Add(i);
					}
				}

				return tmpListView.SelectedIndices; // selectedIndexCollection;
			}
		}

		/// <summary>
		/// Compatibility property
		/// With a standard ListView you can add columns in the Designer
		/// We will ignore this (but leave it here for compatibility)
		/// Columns must be added through the AddColumns() public method
		/// </summary>
		public System.Windows.Forms.ListView.ColumnHeaderCollection Columns = new System.Windows.Forms.ListView.ColumnHeaderCollection(new System.Windows.Forms.ListView());

		/// <summary>
		/// Compatibility with ListView class
		/// This is not used in this implementation
		/// </summary>
		[Category("Behavior")]
		public bool VirtualMode { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the selected item in the control remains highlighted when the control loses focus
		/// </summary>
		[Category("Behavior")]
		public bool HideSelection { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the ListView uses state image behavior that is compatible with the .NET Framework 1.1 or the .NET Framework 2.0.
		/// Here for ListView api compatibility (we dont care about this)
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public bool UseCompatibleStateImageBehavior { get; set; }

		/// <summary>
		/// Gets or sets how items are displayed in the control.
		/// Here for ListView api compatibility (we dont care about this)
		/// </summary>
		public System.Windows.Forms.View View { get; set; }

		#endregion

		#region VirtualListView Compatibility Properties

		/// <summary>
		/// Informs user that a select all event is in place, can be used in change events to wait until this is false
		/// Not used in this implementation (yet)
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool SelectAllInProgress { get; set; }

		/// <summary>
		/// Gets/Sets the selected item
		/// Here for compatibility with VirtualListView.cs
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int selectedItem
		{
			get
			{
				if (SelectedRows.Count() == 0)
				{
					return -1;
				}
				else
				{
					return SelectedRows.First();
				}
			}
			set
			{
				SelectItem(value, true);
			}
		}

		[Category("Behavior")]
		public bool BlazingFast { get; set; }

		#endregion

		#region Behavior

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
		/// Gets or sets the scrolling speed
		/// </summary>
		[Category("Behavior")]
		public int ScrollSpeed
		{
			get
			{
				if (CellHeight == 0)
					CellHeight++;
				return _vBar.SmallChange / CellHeight;
			}

			set
			{
				_vBar.SmallChange = value * CellHeight;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether columns can be resized
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(true)]
		public bool AllowColumnResize { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether columns can be reordered
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(true)]
		public bool AllowColumnReorder { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether multiple items can to be selected
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(true)]
		public bool MultiSelect { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the control is in input painting mode
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(false)]
		public bool InputPaintingMode { get; set; }

		/// <summary>
		/// Gets or sets how the InputRoll scrolls when calling ScrollToIndex.
		/// </summary>
		[DefaultValue("near")]
		[Category("Behavior")]
		public string ScrollMethod { get; set; }

		/// <summary>
		/// Gets or sets a value indicating how the Intever for the hover event
		/// </summary>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool AlwaysScroll { get; set; }

		/// <summary>
		/// Gets or sets the lowest seek interval to activate the progress bar
		/// </summary>
		[Category("Behavior")]
		public int SeekingCutoffInterval { get; set; }

		[DefaultValue(750)]
		[Category("Behavior")]
		public int HoverInterval
		{
			get { return _hoverTimer.Interval; }
			set { _hoverTimer.Interval = value; }
		}

		/// <summary>
		/// Gets or sets whether you can use right click to select things
		/// </summary>
		[Category("Behavior")]
		public bool AllowRightClickSelecton { get; set; }

		/// <summary>
		/// Gets or sets whether keys can modify selection
		/// </summary>
		[Category("Behavior")]
		public bool LetKeysModifySelection { get; set; }

		/// <summary>
		/// Gets or sets whether hot keys are suspended
		/// </summary>
		[Category("Behavior")]
		public bool SuspendHotkeys { get; set; }

		#endregion

		#region Appearance

		/// <summary>
		/// Gets or sets a value indicating whether grid lines are displayed around cells
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool GridLines { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the entire row will always be selected
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool FullRowSelect { get; set; }

		/// <summary>
		/// Gets or sets the font used for the column header text
		/// Also forces a cell size re-evaluation
		/// </summary>
		[Category("Appearance")]
		public Font ColumnHeaderFont
		{
			get
			{
				if (_columnHeaderFont == null)
				{
					ColumnHeaderFont = new Font("Arial", 8, FontStyle.Bold);
				}
					
				return _columnHeaderFont;
			}
			set
			{
				_columnHeaderFont = value;
				SetCharSize();
			}
		}
		private Font _columnHeaderFont;

		/// <summary>
		/// Gets or sets the color of the column header text
		/// </summary>
		[Category("Appearance")]
		public Color ColumnHeaderFontColor
		{
			get
			{
				if (_columnHeaderFontColor == null)
					_columnHeaderFontColor = Color.Black;
				return _columnHeaderFontColor;
			}
			set { _columnHeaderFontColor = value; }
		}
		private Color _columnHeaderFontColor;

		/// <summary>
		/// Gets or sets the background color of the column header cells
		/// </summary>
		[Category("Appearance")]
		public Color ColumnHeaderBackgroundColor
		{
			get
			{
				if (_columnHeaderBackgroundColor == null)
					_columnHeaderBackgroundColor = Color.LightGray;
				return _columnHeaderBackgroundColor;
			}
			set { _columnHeaderBackgroundColor = value; }
		}
		private Color _columnHeaderBackgroundColor;

		/// <summary>
		/// Gets or sets the background color of the column header cells when they are highlighted
		/// </summary>
		[Category("Appearance")]
		public Color ColumnHeaderBackgroundHighlightColor
		{
			get
			{
				if (_columnHeaderBackgroundHighlightColor == null)
					_columnHeaderBackgroundHighlightColor = SystemColors.HighlightText;
				return _columnHeaderBackgroundHighlightColor;
			}
			set { _columnHeaderBackgroundHighlightColor = value; }
		}
		private Color _columnHeaderBackgroundHighlightColor;

		/// <summary>
		/// Gets or sets the color of the column header outline
		/// </summary>
		[Category("Appearance")]
		public Color ColumnHeaderOutlineColor
		{
			get
			{
				if (_columnHeaderOutlineColor == null)
					_columnHeaderOutlineColor = Color.Black;
				return _columnHeaderOutlineColor;
			}
			set
			{
				_columnHeaderOutlineColor = value;
			}
		}
		private Color _columnHeaderOutlineColor;


		/// <summary>
		/// Gets or sets the font used for every row cell
		/// Also forces a cell size re-evaluation
		/// </summary>
		[Category("Appearance")]
		public Font CellFont
		{
			get
			{
				if (_cellFont == null)
				{
					CellFont = new Font("Arial", 8, FontStyle.Regular);
				}
				return _cellFont;
			}
			set
			{
				_cellFont = value;
				SetCharSize();
			}
		}
		private Font _cellFont;
		

		/// <summary>
		/// Gets or sets the color of the font used for every row cell
		/// </summary>
		[Category("Appearance")]
		public Color CellFontColor
		{
			get
			{
				if (_cellFontColor == null)
					_cellFontColor = Color.Black;
				return _cellFontColor;
			}
			set { _cellFontColor = value; }
		}
		private Color _cellFontColor;

		/// <summary>
		/// Gets or sets the background color for every row cell
		/// </summary>
		[Category("Appearance")]
		public Color CellBackgroundColor
		{
			get
			{
				if (_cellBackgroundColor == null)
					_cellBackgroundColor = Color.White;
				return _cellBackgroundColor;
			}
			set { _cellBackgroundColor = value; }
		}
		private Color _cellBackgroundColor;

		/// <summary>
		/// Gets or sets the background color for every row cell that is highlighted
		/// </summary>
		[Category("Appearance")]
		public Color CellBackgroundHighlightColor
		{
			get
			{
				if (_cellBackgroundHighlightColor == null)
					_cellBackgroundHighlightColor = Color.Blue;
				return _cellBackgroundHighlightColor;
			}
			set { _cellBackgroundHighlightColor = value; }
		}
		private Color _cellBackgroundHighlightColor;

		/// <summary>
		/// Gets or sets the color used to draw the ListView gridlines
		/// </summary>
		[Category("Appearance")]
		public Color GridLineColor
		{
			get
			{
				if (_gridLineColor == null)
					_gridLineColor = SystemColors.ControlLight;
				return _gridLineColor;
			}
			set { _gridLineColor = value; }
		}
		private Color _gridLineColor;

		/// <summary>
		/// Gets or sets the size of control's border
		/// Note: this is drawn directly onto the parent control, so large values will probably look terrible
		/// </summary>
		[Category("Appearance")]
		public int BorderSize { get; set; }

		/// <summary>
		/// Defines the absolute minimum column size (used when manually resizing columns)
		/// </summary>
		[DefaultValue(50)]
		[Category("Appearance")]
		public int MinimumColumnSize { get; set; }

		/// <summary>
		/// The padding property is disabled for this control (as this is handled internally)
		/// </summary>
		[Category("Appearance")]
		public new System.Windows.Forms.Padding Padding
		{
			get { return new System.Windows.Forms.Padding(0); }
			set { }
		}

		/// <summary>
		/// Gets or sets the color of the control's border
		/// </summary>
		[Category("Appearance")]
		public Color BorderColor
		{
			get
			{
				if (_borderColor == null)
					_borderColor = SystemColors.InactiveBorder;
				return _borderColor;
			}
			set { _borderColor = value; }
		}
		private Color _borderColor;


		#endregion

		#region API

		/// <summary>
		/// All visible columns
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IEnumerable<ListColumn> VisibleColumns => _columns.VisibleColumns;

		/// <summary>
		/// Gets or sets the sets the virtual number of rows to be displayed. Does not include the column header row.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int ItemCount
		{
			get { return _itemCount; }
			set
			{
				_itemCount = value;
				RecalculateScrollBars();
			}
		}

		/// <summary>
		/// Returns all columns including those that are not visible
		/// </summary>
		/// <returns></returns>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ListColumns AllColumns => _columns;

		/// <summary>
		/// Gets whether the mouse is currently over a column cell
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsPointingAtColumnHeader => IsHoveringOnColumnCell;

		/// <summary>
		/// Returns the index of the first selected row (null if no selection)
		/// </summary>
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

		/// <summary>
		/// Returns the index of the last selected row (null if no selection)
		/// </summary>
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
		/// Gets or sets the first visible row index, if scrolling is needed
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int FirstVisibleRow
		{
			get // SuuperW: This was checking if the scroll bars were needed, which is useless because their Value is 0 if they aren't needed.
			{
				if (CellHeight == 0) CellHeight++;
				return _vBar.Value / CellHeight;
			}

			set
			{
				if (NeedsVScrollbar)
				{
					_programmaticallyUpdatingScrollBarValues = true;
					if (value * CellHeight <= _vBar.Maximum)
					{
						_vBar.Value = value * CellHeight;
					}
					else
					{
						_vBar.Value = _vBar.Maximum;
					}

					_programmaticallyUpdatingScrollBarValues = false;
				}
			}
		}

		/// <summary>
		/// Gets the last row that is fully visible
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private int LastFullyVisibleRow
		{
			get
			{
				int halfRow = 0;
				if ((DrawHeight - ColumnHeight - 3) % CellHeight < CellHeight / 2)
				{
					halfRow = 1;
				}

				return FirstVisibleRow + VisibleRows - halfRow; // + CountLagFramesDisplay(VisibleRows - halfRow);
			}
		}

		/// <summary>
		/// Gets or sets the last visible row
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int LastVisibleRow
		{
			get
			{
				return FirstVisibleRow + VisibleRows; // + CountLagFramesDisplay(VisibleRows);
			}

			set
			{
				int halfRow = 0;
				if ((DrawHeight - ColumnHeight - 3) % CellHeight < CellHeight / 2)
				{
					halfRow = 1;
				}

				FirstVisibleRow = Math.Max(value - (VisibleRows - halfRow), 0);
			}
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
				if (CellHeight == 0) CellHeight++;
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
				if (CellHeight == 0) CellHeight++;
				var columnList = VisibleColumns.ToList();
				return columnList.FindIndex(c => c.Right > _hBar.Value);
			}
		}

		/// <summary>
		/// Gets the last visible column index, if scrolling is needed
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int LastVisibleColumnIndex
		{
			get
			{
				if (CellHeight == 0) CellHeight++;
				List<ListColumn> columnList = VisibleColumns.ToList();
				int ret;
				ret = columnList.FindLastIndex(c => c.Left <= DrawWidth + _hBar.Value);
				return ret;
			}
		}

		/// <summary>
		/// Gets or sets the current Cell that the mouse was in.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Cell CurrentCell { get; set; }

		/// <summary>
		/// Returns whether the current cell is a data cell or not
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CurrentCellIsDataCell => CurrentCell?.RowIndex != null && CurrentCell.Column != null;

		/// <summary>
		/// Gets a list of selected row indexes
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IEnumerable<int> SelectedRows
		{
			get
			{
				return _selectedItems
					.Where(cell => cell.RowIndex.HasValue)
					.Select(cell => cell.RowIndex.Value)
					.Distinct();
			}
		}

		/// <summary>
		/// Returns whether any rows are selected
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool AnyRowsSelected
		{
			get
			{
				return _selectedItems.Any(cell => cell.RowIndex.HasValue);
			}
		}

		/// <summary>
		/// Gets or sets the previous Cell that the mouse was in.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Cell LastCell { get; private set; }

		/// <summary>
		/// Gets or sets whether paint down is happening
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsPaintDown { get; private set; }

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool UseCustomBackground { get; set; }

		/// <summary>
		/// Gets or sets the current draw height
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int DrawHeight { get; private set; }

		/// <summary>
		/// Gets or sets the current draw width
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int DrawWidth { get; private set; }

		/// <summary>
		/// Gets or sets whether the right mouse button is held down
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool RightButtonHeld { get; private set; }

		#endregion
	}
}
