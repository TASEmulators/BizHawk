using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		// Input Painting
		private string _startBoolDrawColumn = string.Empty;
		private string _startFloatDrawColumn = string.Empty;
		private bool _boolPaintState;
		private float _floatPaintState;
		private float _floatBackupState;
		private bool _patternPaint = false;
		private bool _startCursorDrag;
		private bool _startSelectionDrag;
		private bool _selectionDragState;
		private bool _supressContextMenu;

		// SuuperW: For editing analog input
		private string _floatEditColumn = string.Empty;
		private int _floatEditRow = -1;
		private string _floatTypedValue;
		private int _floatEditYPos = -1;
		private int floatEditRow
		{
			set
			{
				_floatEditRow = value;
				TasView.suspendHotkeys = FloatEditingMode;
			}
		}
		public bool FloatEditingMode
		{
			get { return _floatEditRow != -1; }
		}
		private List<int> _extraFloatRows = new List<int>();

		// Right-click dragging
		private string[] _rightClickInput = null;
		private string[] _rightClickOverInput = null;
		private int _rightClickFrame = -1;
		private int _rightClickLastFrame = -1;
		private bool _rightClickShift, _rightClickControl, _rightClickAlt;
		private bool _leftButtonHeld = false;
		private bool mouseButtonHeld
		{
			get { return _rightClickFrame != -1 || _leftButtonHeld; }
		}

		private bool _triggerAutoRestore; // If true, autorestore will be called on mouse up
		private bool? _autoRestorePaused = null;
		private int? _seekStartFrame = null;
		private bool _wasRecording = false;

		private Emulation.Common.ControllerDefinition controllerType
		{ get { return Global.MovieSession.MovieControllerAdapter.Definition; } }

		public AutoPatternBool[] BoolPatterns;
		public AutoPatternFloat[] FloatPatterns;

		private void JumpToGreenzone()
		{
			if (Emulator.Frame > CurrentTasMovie.LastValidFrame)
			{
				GoToLastEmulatedFrameIfNecessary(CurrentTasMovie.LastValidFrame);
			}
			else
			{
				_triggerAutoRestore = false;
			}
		}

		private void StartSeeking(int? frame)
		{
			if (!frame.HasValue)
				return;

			if (Mainform.PauseOnFrame != null)
				StopSeeking();

			_seekStartFrame = Emulator.Frame;
			Mainform.PauseOnFrame = frame.Value;
			int? diff = Mainform.PauseOnFrame - _seekStartFrame;

			Mainform.UnpauseEmulator();

			if (!_seekBackgroundWorker.IsBusy && diff.Value > TasView.VisibleRows)
				_seekBackgroundWorker.RunWorkerAsync();
		}

		public void StopSeeking()
		{
			_seekBackgroundWorker.CancelAsync();
			if (_wasRecording)
			{
				TastudioRecordMode();
				_wasRecording = false;
			}
			Mainform.PauseOnFrame = null;
			if (CurrentTasMovie != null)
				RefreshDialog();
		}

		#region Query callbacks

		// public static Color CurrentFrame_FrameCol = Color.FromArgb(0xCFEDFC); Why?
		public static Color CurrentFrame_InputLog = Color.FromArgb(0x00B5E7F7);
		public static Color SeekFrame_InputLog = Color.FromArgb(0x70B5E7F7);

		public static Color GreenZone_FrameCol = Color.FromArgb(0xDDFFDD);
		public static Color GreenZone_InputLog = Color.FromArgb(0xD2F9D3);
		public static Color GreenZone_InputLog_Stated = Color.FromArgb(0xC4F7C8);
		public static Color GreenZone_InputLog_Invalidated = Color.FromArgb(0xE0FBE0);

		public static Color LagZone_FrameCol = Color.FromArgb(0xFFDCDD);
		public static Color LagZone_InputLog = Color.FromArgb(0xF4DADA);
		public static Color LagZone_InputLog_Stated = Color.FromArgb(0xF0D0D2);
		public static Color LagZone_InputLog_Invalidated = Color.FromArgb(0xF7E5E5);

		public static Color Marker_FrameCol = Color.FromArgb(0xF7FFC9);
		public static Color AnalogEdit_Col = Color.FromArgb(0x909070); // SuuperW: When editing an analog value, it will be a gray color.

		private Bitmap ts_v_arrow_green_blue = Properties.Resources.ts_v_arrow_green_blue;
		private Bitmap ts_h_arrow_green_blue = Properties.Resources.ts_h_arrow_green_blue;
		private Bitmap ts_v_arrow_blue = Properties.Resources.ts_v_arrow_blue;
		private Bitmap ts_h_arrow_blue = Properties.Resources.ts_h_arrow_blue;
		private Bitmap ts_v_arrow_green = Properties.Resources.ts_v_arrow_green;
		private Bitmap ts_h_arrow_green = Properties.Resources.ts_h_arrow_green;

		private Bitmap icon_marker = Properties.Resources.icon_marker;
		private Bitmap icon_anchor_lag = Properties.Resources.icon_anchor_lag;
		private Bitmap icon_anchor = Properties.Resources.icon_anchor;

		private void TasView_QueryItemIcon(int index, InputRoll.RollColumn column, ref Bitmap bitmap, ref int offsetX, ref int offsetY)
		{
			var overrideIcon = GetIconOverride(index, column);

			if (overrideIcon != null)
			{
				bitmap = overrideIcon;
				return;
			}

			var columnName = column.Name;

			if (columnName == CursorColumnName)
			{
				if (index == Emulator.Frame && index == Mainform.PauseOnFrame)
				{
					bitmap = TasView.HorizontalOrientation ?
						ts_v_arrow_green_blue :
						ts_h_arrow_green_blue;
				}
				else if (index == Emulator.Frame)
				{
					bitmap = TasView.HorizontalOrientation ?
						ts_v_arrow_blue :
						ts_h_arrow_blue;
				}
				else if (index == LastPositionFrame)
				{
					bitmap = TasView.HorizontalOrientation ?
						ts_v_arrow_green :
						ts_h_arrow_green;
				}
			}
			else if (columnName == FrameColumnName)
			{
				TasMovieRecord record = CurrentTasMovie[index];
				offsetX = -3;
				offsetY = 1;

				if (CurrentTasMovie.Markers.IsMarker(index) && Settings.DenoteMarkersWithIcons)
                    bitmap = icon_marker;
				else if (record.HasState && Settings.DenoteStatesWithIcons)
				{
                    if (record.Lagged.HasValue && record.Lagged.Value)
                        bitmap = icon_anchor_lag;
                    else
                        bitmap = icon_anchor;
				}
			}
		}

		private void TasView_QueryItemBkColor(int index, InputRoll.RollColumn column, ref Color color)
		{
			Color? overrideColor = GetColorOverride(index, column);

			if (overrideColor.HasValue)
			{
				color = overrideColor.Value;
				return;
			}

			string columnName = column.Name;
            
			if (columnName == CursorColumnName)
                color = Color.FromArgb(0xFEFFFF);
            
			if (columnName == FrameColumnName)
			{
				if (Emulator.Frame != index && CurrentTasMovie.Markers.IsMarker(index) && Settings.DenoteMarkersWithBGColor)
					color = Marker_FrameCol;
				else
					color = Color.FromArgb(0x60FFFFFF);
			}
			else if (FloatEditingMode &&
				(index == _floatEditRow || _extraFloatRows.Contains(index)) &&
				columnName == _floatEditColumn)
			{ // SuuperW: Analog editing is indicated by a color change.
				color = AnalogEdit_Col;
			}

			int player = Emulator.ControllerDefinition.PlayerNumber(columnName);
			if (player != 0 && player % 2 == 0)
				color = Color.FromArgb(0x0D000000);
		}

		private void TasView_QueryRowBkColor(int index, ref Color color)
		{
			TasMovieRecord record = CurrentTasMovie[index];

			if (Mainform.IsSeeking && Mainform.PauseOnFrame == index)
			{
				color = CurrentFrame_InputLog;
			}
			else if (!Mainform.IsSeeking && Emulator.Frame == index)
			{
				color = CurrentFrame_InputLog;
			}
			else if (record.Lagged.HasValue)
			{
				if (!record.HasState && Settings.DenoteStatesWithBGColor)
                    color = record.Lagged.Value ?
                        LagZone_InputLog :
                        GreenZone_InputLog;
                else
				    color = record.Lagged.Value ?
					    LagZone_InputLog_Stated :
                        GreenZone_InputLog_Stated;
			}
			else if (record.WasLagged.HasValue)
			{
				color = record.WasLagged.Value ?
					LagZone_InputLog_Invalidated :
					GreenZone_InputLog_Invalidated;
			}
			else
			{
				color = Color.FromArgb(0xFFFEEE);
			}
		}

		private void TasView_QueryItemText(int index, InputRoll.RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			var overrideText = GetTextOverride(index, column);
			if (overrideText != null)
			{
				text = overrideText;
				return;
			}

			try
			{
				text = string.Empty;
				var columnName = column.Name;

				if (columnName == CursorColumnName)
				{
					int branchIndex = CurrentTasMovie.BranchIndexByFrame(index);
					if (branchIndex != -1)
					{
						text = branchIndex.ToString();
					}
				}
				else if (columnName == FrameColumnName)
				{
					offsetX = 7;
					text = (index).ToString().PadLeft(CurrentTasMovie.InputLogLength.ToString().Length, '0');
				}
				else
				{
					// Display typed float value (string "-" can't be parsed, so CurrentTasMovie.DisplayValue can't return it)
					if (index == _floatEditRow && columnName == _floatEditColumn)
						text = _floatTypedValue;
					else if (index < CurrentTasMovie.InputLogLength)
					{
						text = CurrentTasMovie.DisplayValue(index, columnName);
						if (column.Type == InputRoll.RollColumn.InputType.Float)
						{
							// feos: this could be cashed, but I don't notice any slowdown this way either
							Emulation.Common.ControllerDefinition.FloatRange range = Global.MovieSession.MovieControllerAdapter.Definition.FloatRanges
								[Global.MovieSession.MovieControllerAdapter.Definition.FloatControls.IndexOf(columnName)];
							if (text == range.Mid.ToString())
								text = "";
						}
					}
				}
			}
			catch (Exception ex)
			{
				text = string.Empty;
				MessageBox.Show("oops\n" + ex);
			}
		}

		// SuuperW: Used in InputRoll.cs to hide lag frames.
		private bool TasView_QueryFrameLag(int index, bool hideWasLag)
		{
			TasMovieRecord lag = CurrentTasMovie[index];
			return (lag.Lagged.HasValue && lag.Lagged.Value) || (hideWasLag && lag.WasLagged.HasValue && lag.WasLagged.Value);
		}

		#endregion

		#region Events

		private void TasView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				var columnName = e.Column.Name;

				if (columnName == FrameColumnName)
				{
					CurrentTasMovie.Markers.Add(TasView.LastSelectedIndex.Value, "");
					RefreshDialog();
				}
				else if (columnName != CursorColumnName) // TODO: what about float?
				{
					foreach (var index in TasView.SelectedRows)
					{
						CurrentTasMovie.ToggleBoolState(index, columnName);
						_triggerAutoRestore = true;
					}
					JumpToGreenzone();

					RefreshDialog();
				}
			}
		}

		private void TasView_ColumnRightClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			e.Column.Emphasis ^= true;

			UpdateAutoFire(e.Column.Name, e.Column.Emphasis);

			RefreshTasView();
		}
		private void UpdateAutoFire()
		{
			for (int i = 2; i < TasView.AllColumns.Count; i++)
				UpdateAutoFire(TasView.AllColumns[i].Name, TasView.AllColumns[i].Emphasis);
		}
		public void UpdateAutoFire(string button, bool? isOn)
		{
			if (!isOn.HasValue) // No value means don't change whether it's on or off.
				isOn = TasView.AllColumns.Find(c => c.Name == button).Emphasis;

			int index = 0;
			if (autoHoldToolStripMenuItem.Checked) index = 1;
			if (autoFireToolStripMenuItem.Checked) index = 2;
			if (controllerType.BoolButtons.Contains(button))
			{
				if (index == 0)
					index = controllerType.BoolButtons.IndexOf(button);
				else
					index += controllerType.BoolButtons.Count - 1;
				AutoPatternBool p = BoolPatterns[index];
				Global.AutofireStickyXORAdapter.SetSticky(button, isOn.Value, p);
			}
			else
			{
				if (index == 0)
					index = controllerType.FloatControls.IndexOf(button);
				else
					index += controllerType.FloatControls.Count - 1;
				float? value = null;
				if (isOn.Value) value = 0f;
				AutoPatternFloat p = FloatPatterns[index];
				Global.AutofireStickyXORAdapter.SetFloat(button, value, p);
			}
		}

		private void TasView_ColumnReordered(object sender, InputRoll.ColumnReorderedEventArgs e)
		{
			CurrentTasMovie.FlagChanges();
		}

		private void TasView_MouseEnter(object sender, EventArgs e)
		{
			if (this.ContainsFocus)
				TasView.Focus();
		}

		private void TasView_MouseDown(object sender, MouseEventArgs e)
		{
			// Clicking with left while right is held or vice versa does weird stuff
			if (mouseButtonHeld)
				return;

			if (e.Button == MouseButtons.Middle)
			{
				if (Mainform.EmulatorPaused)
				{
					TasMovieRecord record = CurrentTasMovie[LastPositionFrame];
					if (!record.Lagged.HasValue && LastPositionFrame > Emulator.Frame)
						StartSeeking(LastPositionFrame);
					else
						Mainform.UnpauseEmulator();
				}
				else
				{
					Mainform.PauseEmulator();
				}
				return;
			}

			// SuuperW: Moved these.
			if (TasView.CurrentCell == null || !TasView.CurrentCell.RowIndex.HasValue || TasView.CurrentCell.Column == null)
				return;

			int frame = TasView.CurrentCell.RowIndex.Value;
			string buttonName = TasView.CurrentCell.Column.Name;


			if (e.Button == MouseButtons.Left)
			{
				bool wasHeld = _leftButtonHeld;
				_leftButtonHeld = true;
				// SuuperW: Exit float editing mode, or re-enter mouse editing
				if (FloatEditingMode)
				{
					if (Control.ModifierKeys == Keys.Control || Control.ModifierKeys == Keys.Shift)
					{
						_extraFloatRows.Clear();
						_extraFloatRows.AddRange(TasView.SelectedRows);
						_startSelectionDrag = true;
						_selectionDragState = TasView.SelectedRows.Contains(frame);
						return;
					}
					else if (_floatEditColumn != buttonName ||
						!(_floatEditRow == frame || _extraFloatRows.Contains(frame)))
					{
						_extraFloatRows.Clear();
						floatEditRow = -1;
						RefreshTasView();
					}
					else
					{
						if (_extraFloatRows.Contains(frame))
						{
							_extraFloatRows.Clear();
							floatEditRow = frame;
							RefreshTasView();
						}
						_floatEditYPos = e.Y;
						_floatPaintState = CurrentTasMovie.GetFloatState(frame, buttonName);
						_triggerAutoRestore = true;
						JumpToGreenzone();
						return;
					}
				}

				if (TasView.CurrentCell.Column.Name == CursorColumnName)
				{
					_startCursorDrag = true;
					GoToFrame(TasView.CurrentCell.RowIndex.Value);
				}
				else if (TasView.CurrentCell.Column.Name == FrameColumnName)
				{
					if (Control.ModifierKeys == Keys.Alt && CurrentTasMovie.Markers.IsMarker(frame))
					{
						// TODO
						TasView.DragCurrentCell();
					}
					else
					{
						_startSelectionDrag = true;
						_selectionDragState = TasView.SelectedRows.Contains(frame);
					}
				}
				else // User changed input
				{
					bool wasPaused = Mainform.EmulatorPaused;

					if (Global.MovieSession.MovieControllerAdapter.Definition.BoolButtons.Contains(buttonName))
					{
						CurrentTasMovie.ChangeLog.BeginNewBatch("Paint Bool " + buttonName + " from frame " + frame);

						CurrentTasMovie.ToggleBoolState(TasView.CurrentCell.RowIndex.Value, buttonName);
						_triggerAutoRestore = true;
						JumpToGreenzone();
						RefreshDialog();

						_startBoolDrawColumn = buttonName;

						_boolPaintState = CurrentTasMovie.BoolIsPressed(frame, buttonName);
						if (applyPatternToPaintedInputToolStripMenuItem.Checked &&
							(!onlyOnAutoFireColumnsToolStripMenuItem.Checked || TasView.CurrentCell.Column.Emphasis))
						{
							BoolPatterns[controllerType.BoolButtons.IndexOf(buttonName)].Reset();
							BoolPatterns[controllerType.BoolButtons.IndexOf(buttonName)].GetNextValue();
							_patternPaint = true;
						}
						else
							_patternPaint = false;

						if (!Settings.AutoRestoreOnMouseUpOnly)
							DoTriggeredAutoRestoreIfNeeded();
					}
					else
					{
						if (frame >= CurrentTasMovie.InputLogLength)
						{
							CurrentTasMovie.SetFloatState(frame, buttonName, 0);
							RefreshDialog();
						}

						JumpToGreenzone();

						_floatPaintState = CurrentTasMovie.GetFloatState(frame, buttonName);
						if (applyPatternToPaintedInputToolStripMenuItem.Checked &&
							(!onlyOnAutoFireColumnsToolStripMenuItem.Checked || TasView.CurrentCell.Column.Emphasis))
						{
							FloatPatterns[controllerType.FloatControls.IndexOf(buttonName)].Reset();
							CurrentTasMovie.SetFloatState(frame, buttonName,
								FloatPatterns[controllerType.FloatControls.IndexOf(buttonName)].GetNextValue());
							_patternPaint = true;
						}
						else
							_patternPaint = false;


						if (e.Clicks != 2 && !Settings.SingleClickFloatEdit)
						{
							CurrentTasMovie.ChangeLog.BeginNewBatch("Paint Float " + buttonName + " from frame " + frame);
							_startFloatDrawColumn = buttonName;
						}
						else // Double-click enters float editing mode
						{
							if (_floatEditColumn == buttonName && _floatEditRow == frame)
								floatEditRow = -1;
							else
							{
								CurrentTasMovie.ChangeLog.BeginNewBatch("Float Edit: " + frame);
								_floatEditColumn = buttonName;
								floatEditRow = frame;
								_floatTypedValue = "";
								_floatEditYPos = e.Y;
								_floatBackupState = CurrentTasMovie.GetFloatState(_floatEditRow, _floatEditColumn);
							}
							RefreshDialog();
						}
					}

					// taseditor behavior
					if (!wasPaused)
						Mainform.UnpauseEmulator();
				}
			}
			else if (e.Button == System.Windows.Forms.MouseButtons.Right)
			{
				if (TasView.CurrentCell.Column.Name == FrameColumnName && frame < CurrentTasMovie.InputLogLength)
				{
					_rightClickControl = (Control.ModifierKeys | Keys.Control) == Control.ModifierKeys;
					_rightClickShift = (Control.ModifierKeys | Keys.Shift) == Control.ModifierKeys;
					_rightClickAlt = (Control.ModifierKeys | Keys.Alt) == Control.ModifierKeys;
					if (TasView.SelectedRows.Contains(frame))
					{
						_rightClickInput = new string[TasView.SelectedRows.Count()];
						_rightClickFrame = TasView.FirstSelectedIndex.Value;
						try
						{
							CurrentTasMovie.GetLogEntries().CopyTo(_rightClickFrame, _rightClickInput, 0, TasView.SelectedRows.Count());
						}
						catch { }
						if (_rightClickControl && _rightClickShift)
							_rightClickFrame += _rightClickInput.Length;
					}
					else
					{
						_rightClickInput = new string[1];
						_rightClickInput[0] = CurrentTasMovie.GetLogEntries()[frame];
						_rightClickFrame = frame;
					}
					_rightClickLastFrame = -1;

					if (_rightClickAlt || _rightClickControl || _rightClickShift)
					{
						JumpToGreenzone();
						// TODO: Turn off ChangeLog.IsRecording and handle the GeneralUndo here.
						string undoStepName = "Right-Click Edit:";
						if (_rightClickShift)
						{
							undoStepName += " Extend Input";
							if (_rightClickControl)
								undoStepName += ", Insert";
						}
						else
						{
							if (_rightClickControl)
								undoStepName += " Copy";
							else // _rightClickAlt
								undoStepName += " Move";
						}
						CurrentTasMovie.ChangeLog.BeginNewBatch(undoStepName);
					}
				}
			}
		}

		private void ClearLeftMouseStates()
		{
			_startCursorDrag = false;
			_startSelectionDrag = false;
			_startBoolDrawColumn = string.Empty;
			_startFloatDrawColumn = string.Empty;
			TasView.ReleaseCurrentCell();
			// Exit float editing if value was changed with cursor
			if (FloatEditingMode && _floatPaintState != CurrentTasMovie.GetFloatState(_floatEditRow, _floatEditColumn))
			{
				floatEditRow = -1;
				_triggerAutoRestore = true;
				JumpToGreenzone();
				DoTriggeredAutoRestoreIfNeeded();
				RefreshDialog();
			}
			_floatPaintState = 0;
			_floatEditYPos = -1;
			_leftButtonHeld = false;

			if (!FloatEditingMode && CurrentTasMovie.ChangeLog != null)
			{
				CurrentTasMovie.ChangeLog.EndBatch();
			}
		}

		private void TasView_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && !TasView.IsPointingAtColumnHeader && !_supressContextMenu && TasView.SelectedRows.Any())
			{
				if (Global.MovieSession.Movie.FrameCount < TasView.SelectedRows.Max())
				{
					// trying to be smart here
					// if a loaded branch log is shorter than selection, keep selection until you attempt to call context menu
					// you might need it when you load again the branch where this frame exists
					TasView.DeselectAll();
					RefreshTasView();
				}
				else
				{
					RightClickMenu.Show(TasView, e.X, e.Y);
				}
			}
			else if (e.Button == MouseButtons.Left)
			{
				if (FloatEditingMode && (Control.ModifierKeys == Keys.Control || Control.ModifierKeys == Keys.Shift))
				{
					_leftButtonHeld = false;
					_startSelectionDrag = false;
				}
				else
					ClearLeftMouseStates();
			}

			if (e.Button == System.Windows.Forms.MouseButtons.Right)
			{
				if (_rightClickFrame != -1)
				{
					_rightClickInput = null;
					_rightClickOverInput = null;
					_rightClickFrame = -1;
					CurrentTasMovie.ChangeLog.EndBatch();
				}
			}

			_supressContextMenu = false;

			DoTriggeredAutoRestoreIfNeeded();
		}

		private void TasView_MouseWheel(object sender, MouseEventArgs e)
		{
			if (TasView.RightButtonHeld && TasView.CurrentCell.RowIndex.HasValue)
			{
				_supressContextMenu = true;
				int notch = e.Delta / 120;
				if (notch > 1)
					notch *= 2;

				// warning: tastudio rewind hotkey/button logic is copypasted from here!
				if (Mainform.IsSeeking && !Mainform.EmulatorPaused)
				{
					Mainform.PauseOnFrame -= notch;
					// that's a weird condition here, but for whatever reason it works best
					if (notch > 0 && Emulator.Frame >= Mainform.PauseOnFrame)
					{
						Mainform.PauseEmulator();
						Mainform.PauseOnFrame = null;
						StopSeeking();
						GoToFrame(Emulator.Frame - notch);
					}
					RefreshDialog();
				}
				else
				{
					GoToFrame(Emulator.Frame - notch);
				}
			}
		}

		private void TasView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (TasView.CurrentCell.Column == null)
				return;

			if (e.Button == MouseButtons.Left)
			{
				var buttonName = TasView.CurrentCell.Column.Name;

				if (TasView.CurrentCell.RowIndex.HasValue &&
					buttonName == FrameColumnName &&
					!FloatEditingMode)
				{
					if (Settings.EmptyMarkers)
					{
						CurrentTasMovie.Markers.Add(TasView.CurrentCell.RowIndex.Value, string.Empty);
						RefreshDialog();
					}
					else
					{
						ClearLeftMouseStates();
						MarkerControl.AddMarker(false, TasView.CurrentCell.RowIndex.Value);
					}
				}
			}
		}

		private void TasView_PointedCellChanged(object sender, InputRoll.CellEventArgs e)
		{
			// TODO: think about nullability
			// For now return if a null because this happens OnEnter which doesn't have any of the below behaviors yet?
			// Most of these are stupid but I got annoyed at null crashes
			if (e.OldCell == null || e.OldCell.Column == null || e.OldCell.RowIndex == null ||
				e.NewCell == null || e.NewCell.RowIndex == null || e.NewCell.Column == null)
			{
				return;
            }

            // skip rerecord counting on drawing entirely, mouse down is enough
            // avoid introducing another global
            bool wasCountingRerecords = Global.MovieSession.Movie.IsCountingRerecords;

			int startVal, endVal;
			int frame = e.NewCell.RowIndex.Value;
			if (e.OldCell.RowIndex.Value < e.NewCell.RowIndex.Value)
			{
				startVal = e.OldCell.RowIndex.Value;
				endVal = e.NewCell.RowIndex.Value;
			}
			else
			{
				startVal = e.NewCell.RowIndex.Value;
				endVal = e.OldCell.RowIndex.Value;
			}

			if (_startCursorDrag && !Mainform.IsSeeking)
			{
				if (e.NewCell.RowIndex.HasValue)
				{
					GoToFrame(e.NewCell.RowIndex.Value);
				}
			}
			else if (_startSelectionDrag)
			{
				if (e.OldCell.RowIndex.HasValue && e.NewCell.RowIndex.HasValue)
				{
					for (var i = startVal; i <= endVal; i++)
					{
						TasView.SelectRow(i, _selectionDragState);
						if (FloatEditingMode && (Control.ModifierKeys == Keys.Control || Control.ModifierKeys == Keys.Shift))
						{
							if (_selectionDragState)
								_extraFloatRows.Add(i);
							else
								_extraFloatRows.Remove(i);
						}
					}
					SetSplicer();
				}
			}

			else if (_rightClickFrame != -1)
			{
				if (frame > CurrentTasMovie.InputLogLength - _rightClickInput.Length)
					frame = CurrentTasMovie.InputLogLength - _rightClickInput.Length;
				if (_rightClickShift)
				{
					if (_rightClickControl) // Insert
					{
						// If going backwards, delete!
						bool shouldInsert = true;
						if (startVal < _rightClickFrame)
						{ // Cloning to a previous frame makes no sense.
							startVal = _rightClickFrame - 1;
						}
						if (startVal < _rightClickLastFrame)
							shouldInsert = false;

						if (shouldInsert)
						{
							for (int i = startVal + 1; i <= endVal; i++)
								CurrentTasMovie.InsertInput(i, _rightClickInput[(i - _rightClickFrame).Mod(_rightClickInput.Length)]);
						}
						else
						{
							CurrentTasMovie.RemoveFrames(startVal + 1, endVal + 1);
						}

						_rightClickLastFrame = frame;
					}
					else // Overwrite
					{
						for (int i = startVal; i <= endVal; i++)
							CurrentTasMovie.SetFrame(i, _rightClickInput[(i - _rightClickFrame).Mod(_rightClickInput.Length)]);
					}
				}
				else
				{
					if (_rightClickControl)
					{
						for (int i = 0; i < _rightClickInput.Length; i++) // Re-set initial range, just to verify it's still there.
							CurrentTasMovie.SetFrame(_rightClickFrame + i, _rightClickInput[i]);

						if (_rightClickOverInput != null) // Restore overwritten input from previous movement
						{
							for (int i = 0; i < _rightClickOverInput.Length; i++)
								CurrentTasMovie.SetFrame(_rightClickLastFrame + i, _rightClickOverInput[i]);
						}
						else
							_rightClickOverInput = new string[_rightClickInput.Length];

						_rightClickLastFrame = frame; // Set new restore log
						CurrentTasMovie.GetLogEntries().CopyTo(frame, _rightClickOverInput, 0, _rightClickOverInput.Length);

						for (int i = 0; i < _rightClickInput.Length; i++) // Place copied input
							CurrentTasMovie.SetFrame(frame + i, _rightClickInput[i]);
					}
					else if (_rightClickAlt)
					{
						int shiftBy = _rightClickFrame - frame;
						string[] shiftInput = new string[Math.Abs(shiftBy)];
						int shiftFrom = frame;
						if (shiftBy < 0)
							shiftFrom = _rightClickFrame + _rightClickInput.Length;

						CurrentTasMovie.GetLogEntries().CopyTo(shiftFrom, shiftInput, 0, shiftInput.Length);
						int shiftTo = shiftFrom + (_rightClickInput.Length * Math.Sign(shiftBy));
						for (int i = 0; i < shiftInput.Length; i++)
							CurrentTasMovie.SetFrame(shiftTo + i, shiftInput[i]);

						for (int i = 0; i < _rightClickInput.Length; i++)
							CurrentTasMovie.SetFrame(frame + i, _rightClickInput[i]);
						_rightClickFrame = frame;
					}
				}
				if (_rightClickAlt || _rightClickControl || _rightClickShift)
				{
					JumpToGreenzone();
					_triggerAutoRestore = true;
					_supressContextMenu = true;
				}
			}
			// Left-click
			else if (TasView.IsPaintDown && e.NewCell.RowIndex.HasValue && !string.IsNullOrEmpty(_startBoolDrawColumn))
			{
                Global.MovieSession.Movie.IsCountingRerecords = false;

				if (e.OldCell.RowIndex.HasValue && e.NewCell.RowIndex.HasValue)
				{
					for (int i = startVal; i <= endVal; i++) // Inclusive on both ends (drawing up or down)
					{
						bool setVal = _boolPaintState;
						if (_patternPaint && _boolPaintState)
						{
							if (CurrentTasMovie[frame].Lagged.HasValue && CurrentTasMovie[frame].Lagged.Value)
								setVal = CurrentTasMovie.BoolIsPressed(i - 1, _startBoolDrawColumn);
							else
								setVal = BoolPatterns[controllerType.BoolButtons.IndexOf(_startBoolDrawColumn)].GetNextValue();
						}
						CurrentTasMovie.SetBoolState(i, _startBoolDrawColumn, setVal); // Notice it uses new row, old column, you can only paint across a single column
						JumpToGreenzone();
					}

					if (!Settings.AutoRestoreOnMouseUpOnly)
					{
						_triggerAutoRestore = true;
						DoTriggeredAutoRestoreIfNeeded();
					}
				}
			}

			else if (TasView.IsPaintDown && e.NewCell.RowIndex.HasValue && !string.IsNullOrEmpty(_startFloatDrawColumn))
            {
                Global.MovieSession.Movie.IsCountingRerecords = false;

				if (e.OldCell.RowIndex.HasValue && e.NewCell.RowIndex.HasValue)
				{
					for (int i = startVal; i <= endVal; i++) // Inclusive on both ends (drawing up or down)
					{
						float setVal = _floatPaintState;
						if (_patternPaint)
						{
							if (CurrentTasMovie[frame].Lagged.HasValue && CurrentTasMovie[frame].Lagged.Value)
								setVal = CurrentTasMovie.GetFloatState(i - 1, _startFloatDrawColumn);
							else
								setVal = FloatPatterns[controllerType.FloatControls.IndexOf(_startFloatDrawColumn)].GetNextValue();
						}
						CurrentTasMovie.SetFloatState(i, _startFloatDrawColumn, setVal); // Notice it uses new row, old column, you can only paint across a single column
						JumpToGreenzone();
					}

					if (!Settings.AutoRestoreOnMouseUpOnly)
					{
						_triggerAutoRestore = true;
						DoTriggeredAutoRestoreIfNeeded();
					}
				}
			}

            Global.MovieSession.Movie.IsCountingRerecords = wasCountingRerecords;

			if (mouseButtonHeld)
			{
				TasView.MakeIndexVisible(TasView.CurrentCell.RowIndex.Value); // todo: limit scrolling speed
			}
			RefreshTasView();
		}

		private void TasView_MouseMove(object sender, MouseEventArgs e)
		{
			// For float editing
			int increment = (_floatEditYPos - e.Y) / 4;
			if (_floatEditYPos == -1)
				return;

			float value = _floatPaintState + increment;
			Emulation.Common.ControllerDefinition.FloatRange range = Global.MovieSession.MovieControllerAdapter.Definition.FloatRanges
				[Global.MovieSession.MovieControllerAdapter.Definition.FloatControls.IndexOf(_floatEditColumn)];
			// Range for N64 Y axis has max -128 and min 127. That should probably be fixed in ControllerDefinition.cs.
			// SuuperW: I really don't think changing it would break anything, but adelikat isn't so sure.
			float rMax = range.Max;
			float rMin = range.Min;
			if (rMax < rMin)
			{
				rMax = range.Min;
				rMin = range.Max;
			}
			if (value > rMax)
				value = rMax;
			else if (value < rMin)
				value = rMin;

			CurrentTasMovie.SetFloatState(_floatEditRow, _floatEditColumn, value);
			_floatTypedValue = value.ToString();

			_triggerAutoRestore = true;
			JumpToGreenzone();
			DoTriggeredAutoRestoreIfNeeded();
			RefreshDialog();
		}

		private void TasView_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetSplicer();
		}

		public void AnalogIncrementByOne()
		{
			if (FloatEditingMode)
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Up));
		}

		public void AnalogDecrementByOne()
		{
			if (FloatEditingMode)
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Down));
		}

		public void AnalogIncrementByTen()
		{
			if (FloatEditingMode)
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Up | Keys.Shift));
		}

		public void AnalogDecrementByTen()
		{
			if (FloatEditingMode)
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Down | Keys.Shift));
		}

		public void AnalogMax()
		{
			if (FloatEditingMode)
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Right));
		}

		public void AnalogMin()
		{
			if (FloatEditingMode)
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Left));
		}

		public void EditAnalogProgrammatically(KeyEventArgs e)
		{
			if (!FloatEditingMode)
				return;

			float value = CurrentTasMovie.GetFloatState(_floatEditRow, _floatEditColumn);
			float prev = value;
			string prevTyped = _floatTypedValue;

			Emulation.Common.ControllerDefinition.FloatRange range = Global.MovieSession.MovieControllerAdapter.Definition.FloatRanges
				[Global.MovieSession.MovieControllerAdapter.Definition.FloatControls.IndexOf(_floatEditColumn)];

			float rMax = range.Max;
			float rMin = range.Min;
			// Range for N64 Y axis has max -128 and min 127. That should probably be fixed ControllerDefinition.cs, but I'll put a quick fix here anyway.
			if (rMax < rMin)
			{
				rMax = range.Min;
				rMin = range.Max;
			}

			// feos: typing past max digits overwrites existing value, not touching the sign
			// but doesn't handle situations where the range is like -50 through 100, where minimum is negative and has less digits
			// it just uses 3 as maxDigits there too, leaving room for typing impossible values (that are still ignored by the game and then clamped)
			int maxDigits = range.MaxDigits();
			int curDigits = _floatTypedValue.Length;
			string curMinus;
			if (_floatTypedValue.StartsWith("-"))
			{
				curDigits -= 1;
				curMinus = "-";
			}
			else
			{
				curMinus = "";
			}

			if (e.KeyCode == Keys.Right)
			{
				value = rMax;
				_floatTypedValue = value.ToString();
			}
			else if (e.KeyCode == Keys.Left)
			{
				value = rMin;
				_floatTypedValue = value.ToString();
			}
			else if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
			{
				if (curDigits >= maxDigits)
					_floatTypedValue = curMinus;
				_floatTypedValue += e.KeyCode - Keys.D0;
			}
			else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
			{
				if (curDigits >= maxDigits)
					_floatTypedValue = curMinus;
				_floatTypedValue += e.KeyCode - Keys.NumPad0;
			}
			else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
			{
				if (_floatTypedValue.StartsWith("-"))
					_floatTypedValue = _floatTypedValue.Substring(1);
				else
					_floatTypedValue = "-" + _floatTypedValue;
			}
			else if (e.KeyCode == Keys.Back)
			{
				if (_floatTypedValue == "") // Very first key press is backspace?
					_floatTypedValue = value.ToString();
				_floatTypedValue = _floatTypedValue.Substring(0, _floatTypedValue.Length - 1);
				if (_floatTypedValue == "" || _floatTypedValue == "-")
					value = 0f;
				else
					value = Convert.ToSingle(_floatTypedValue);
			}
			else if (e.KeyCode == Keys.Enter)
			{
				if (_floatEditYPos != -1)
				{
					_floatEditYPos = -1;
				}
				floatEditRow = -1;
			}
			else if (e.KeyCode == Keys.Escape)
			{
				if (_floatEditYPos != -1)
				{
					_floatEditYPos = -1;
				}
				if (_floatBackupState != _floatPaintState)
				{
					CurrentTasMovie.SetFloatState(_floatEditRow, _floatEditColumn, _floatBackupState);
					_triggerAutoRestore = Emulator.Frame > _floatEditRow;
					JumpToGreenzone();
					DoTriggeredAutoRestoreIfNeeded();
				}
				floatEditRow = -1;
			}
			else
			{
				float changeBy = 0;
				if (e.KeyCode == Keys.Up)
					changeBy = 1; // We're assuming for now that ALL float controls should contain integers.
				else if (e.KeyCode == Keys.Down)
					changeBy = -1;
				if (Control.ModifierKeys == Keys.Shift)
					changeBy *= 10;
				value += changeBy;
				if (changeBy != 0)
					_floatTypedValue = value.ToString();
			}

			if (!FloatEditingMode)
				CurrentTasMovie.ChangeLog.EndBatch();
			else
			{
				if (_floatTypedValue == "")
				{
					if (prevTyped != "")
					{
						value = 0f;
						CurrentTasMovie.SetFloatState(_floatEditRow, _floatEditColumn, value);
					}
				}
				else
				{
					if (float.TryParse(_floatTypedValue, out value)) // String "-" can't be parsed.
					{
						if (value > rMax)
							value = rMax;
						else if (value < rMin)
							value = rMin;
						_floatTypedValue = value.ToString();
						CurrentTasMovie.SetFloatState(_floatEditRow, _floatEditColumn, value);
					}
				}

				if (_extraFloatRows.Any())
				{
					foreach (int row in _extraFloatRows)
					{
						CurrentTasMovie.SetFloatState(row, _floatEditColumn, value);
					}
				}

				if (value != prev) // Auto-restore
				{
					_triggerAutoRestore = Emulator.Frame > _floatEditRow;
					JumpToGreenzone();
					DoTriggeredAutoRestoreIfNeeded();
				}
			}
			RefreshDialog();
		}

		private void TasView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Left) // Ctrl + Left
			{
				GoToPreviousMarker();
			}
			else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Right) // Ctrl + Right
			{
				GoToNextMarker();
			}

			if (FloatEditingMode &&
				e.KeyCode != Keys.Right && e.KeyCode != Keys.Left &&
				e.KeyCode != Keys.Up && e.KeyCode != Keys.Down)
				EditAnalogProgrammatically(e);

			RefreshDialog();
		}

		/// <summary>
		/// This allows arrow keys to be detected by KeyDown.
		/// </summary>
		private void TasView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
				e.IsInputKey = true;
		}
		#endregion
	}
}
