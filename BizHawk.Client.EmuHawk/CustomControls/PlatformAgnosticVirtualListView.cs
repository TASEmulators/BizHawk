using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A performant VirtualListView implementation that doesn't rely on native Win32 API calls
	/// (and in fact does not inherit the ListView class at all)
	/// It is an enhanced version of the work done with GDI+ rendering in InputRoll.cs
	/// </summary>
	public partial class PlatformAgnosticVirtualListView : Control
	{
		private readonly SortedSet<Cell> _selectedItems = new SortedSet<Cell>(new SortCell());

		private readonly VScrollBar _vBar;
		private readonly HScrollBar _hBar;

		private readonly Timer _hoverTimer = new Timer();		

		private ListColumns _columns = new ListColumns();
		
		private bool _programmaticallyUpdatingScrollBarValues;		

		private int _itemCount;
		private Size _charSize;

		private ListColumn _columnDown;
		private ListColumn _columnSeparatorDown;

		private int? _currentX;
		private int? _currentY;

		public PlatformAgnosticVirtualListView()
		{			
			ColumnHeaderFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
			ColumnHeaderFontColor = Color.Black;
			ColumnHeaderBackgroundColor = Color.LightGray;
			ColumnHeaderBackgroundHighlightColor = SystemColors.HighlightText;
			ColumnHeaderOutlineColor = Color.Black;

			CellFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
			CellFontColor = Color.Black;
			CellBackgroundColor = Color.White;
			CellBackgroundHighlightColor = Color.Blue;

			GridLines = true;
			GridLineColor = SystemColors.ControlLight;
			
			UseCustomBackground = true;

			BorderColor = Color.DarkGray;
			BorderSize = 1;

			MinimumColumnSize = 50;

			CellWidthPadding = 3;
			CellHeightPadding = 0;
			CurrentCell = null;
			ScrollMethod = "near";			

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);			

			_vBar = new VScrollBar
			{
				// Location gets calculated later (e.g. on resize)
				Visible = false,
				SmallChange = CellHeight,
				LargeChange = CellHeight * 20
			};

			_hBar = new HScrollBar
			{
				// Location gets calculated later (e.g. on resize)
				Visible = false,
				SmallChange = CellWidth,
				LargeChange = 20
			};

			Controls.Add(_vBar);
			Controls.Add(_hBar);

			_vBar.ValueChanged += VerticalBar_ValueChanged;
			_hBar.ValueChanged += HorizontalBar_ValueChanged;

			RecalculateScrollBars();

			_columns.ChangedCallback = ColumnChangedCallback;

			_hoverTimer.Interval = 750;
			_hoverTimer.Tick += HoverTimerEventProcessor;
			_hoverTimer.Stop();
		}		

		private void HoverTimerEventProcessor(object sender, EventArgs e)
		{
			_hoverTimer.Stop();

			CellHovered?.Invoke(this, new CellEventArgs(LastCell, CurrentCell));
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		#region Pending Removal

		/*
		 * 
		
		//private readonly byte[] _lagFrames = new byte[256]; // Large enough value that it shouldn't ever need resizing. // apparently not large enough for 4K 
		
		private int _maxCharactersInHorizontal = 1;
		private bool _horizontalOrientation;

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
				//LagFramesToHide = rollSettings.LagFramesToHide;
				//HideWasLagFrames = rollSettings.HideWasLagFrames;
			}
		}

		private InputRollSettings Settings => new InputRollSettings
		{
			Columns = _columns,
			HorizontalOrientation = HorizontalOrientation,
			//LagFramesToHide = LagFramesToHide,
			//HideWasLagFrames = HideWasLagFrames
		};
		
		public class InputRollSettings
		{
			public RollColumns Columns { get; set; }
			public bool HorizontalOrientation { get; set; }
			public int LagFramesToHide { get; set; }
			public bool HideWasLagFrames { get; set; }
		}


		/// <summary>
		/// Gets or sets the width of data cells when in Horizontal orientation.
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

		/// <summary>
		/// Gets or sets a value indicating whether the control is horizontal or vertical
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
					_hBar.SmallChange = CellWidth;
					_vBar.SmallChange = CellHeight;
					ScrollSpeed = temp;
				}
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
				HorizontalOrientation ^= true;
			};

			yield return rotate;
		}

		// SuuperW: Count lag frames between FirstDisplayed and given display position
		private int CountLagFramesDisplay(int relativeIndex)
		{
			if (QueryFrameLag != null && LagFramesToHide != 0)
			{
				int count = 0;
				for (int i = 0; i <= relativeIndex; i++)
				{
					count += _lagFrames[i];
				}

				return count;
			}

			return 0;
		}

		// Count lag frames between FirstDisplayed and given relative frame index
		private int CountLagFramesAbsolute(int relativeIndex)
		{
			if (QueryFrameLag != null) // && LagFramesToHide != 0)
			{
				int count = 0;
				for (int i = 0; i + count <= relativeIndex; i++)
				{
					count += _lagFrames[i];
				}

				return count;
			}

			return 0;
		}

		private void SetLagFramesArray()
		{
			if (QueryFrameLag != null) // && LagFramesToHide != 0)
			{
				bool showNext = false;

				// First one needs to check BACKWARDS for lag frame count.
				SetLagFramesFirst();
				int f = _lagFrames[0];

				if (QueryFrameLag(FirstVisibleRow + f, HideWasLagFrames))
				{
					showNext = true;
				}

				for (int i = 1; i <= VisibleRows; i++)
				{
					_lagFrames[i] = 0;
					if (!showNext)
					{
						for (; _lagFrames[i] < LagFramesToHide; _lagFrames[i]++)
						{
							if (!QueryFrameLag(FirstVisibleRow + i + f, HideWasLagFrames))
							{
								break;
							}

							f++;
						}
					}
					else
					{
						if (!QueryFrameLag(FirstVisibleRow + i + f, HideWasLagFrames))
						{
							showNext = false;
						}
					}

					if (_lagFrames[i] == LagFramesToHide && QueryFrameLag(FirstVisibleRow + i + f, HideWasLagFrames))
					{
						showNext = true;
					}
				}
			}
			else
			{
				for (int i = 0; i <= VisibleRows; i++)
				{
					_lagFrames[i] = 0;
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
			}
			while (QueryFrameLag(FirstVisibleRow - count, HideWasLagFrames) && count <= LagFramesToHide);
			count--;

			// Count forward
			int fCount = -1;
			do
			{
				fCount++;
			}
			while (QueryFrameLag(FirstVisibleRow + fCount, HideWasLagFrames) && count + fCount < LagFramesToHide);
			_lagFrames[0] = (byte)fCount;
		}
		else
		{
			_lagFrames[0] = 0;
		}
	}

		public string RotateHotkeyStr => "Ctrl+Shift+F";

		/// <summary>
		/// Check if a given frame is a lag frame
		/// </summary>
		public delegate bool QueryFrameLagHandler(int index, bool hideWasLag);


		/// <summary>
		/// Fire the QueryFrameLag event which checks if a given frame is a lag frame
		/// </summary>
		[Category("Virtual")]
		public event QueryFrameLagHandler QueryFrameLag;

		*/

		#endregion
	}
}
