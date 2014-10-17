using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

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
		private bool _startMarkerDrag;
		private bool _startFrameDrag;
		private bool _supressContextMenu;

		public static Color CurrentFrame_FrameCol = Color.FromArgb(0xCFEDFC);
		public static Color CurrentFrame_InputLog = Color.FromArgb(0xB5E7F7);

		public static Color GreenZone_FrameCol = Color.FromArgb(0xDDFFDD);
		public static Color GreenZone_Invalidated_FrameCol = Color.FromArgb(0xFFFFFF);
		public static Color GreenZone_InputLog = Color.FromArgb(0xC4F7C8);
		public static Color GreenZone_Invalidated_InputLog = Color.FromArgb(0xE0FBE0);

		public static Color LagZone_FrameCol = Color.FromArgb(0xFFDCDD);
		public static Color LagZone_Invalidated_FrameCol = Color.FromArgb(0xFFE9E9);
		public static Color LagZone_InputLog = Color.FromArgb(0xF0D0D2);
		public static Color LagZone_Invalidated_InputLog = Color.FromArgb(0xF7E5E5);

		public static Color Marker_FrameCol = Color.FromArgb(0xF7FFC9);

		#region Query callbacks

		private void TasView_QueryItemIcon(int index, InputRoll.RollColumn column, ref Bitmap bitmap)
		{
			var columnName = column.Name;

			if (columnName == MarkerColumnName)
			{
				if (index == Global.Emulator.Frame && index == GlobalWin.MainForm.PauseOnFrame)
				{
					bitmap = TasView.HorizontalOrientation ?
						Properties.Resources.ts_v_arrow_green_blue :
						Properties.Resources.ts_h_arrow_green_blue;
				}
				else if (index == Global.Emulator.Frame)
				{
					bitmap = TasView.HorizontalOrientation ?
						Properties.Resources.ts_v_arrow_blue :
						Properties.Resources.ts_h_arrow_blue;
				}
				else if (index == GlobalWin.MainForm.PauseOnFrame)
				{
					bitmap = TasView.HorizontalOrientation ?
						Properties.Resources.ts_v_arrow_green :
						Properties.Resources.ts_h_arrow_green;
				}
			}
		}

		private void TasView_QueryItemBkColor(int index, InputRoll.RollColumn column, ref Color color)
		{
			var columnName = column.Name;
			var record = _currentTasMovie[index];

			if (columnName == MarkerColumnName)
			{
				if (VersionInfo.DeveloperBuild) // For debugging purposes, let's visually show the state frames
				{
					color = (record.HasState ? color = Color.FromArgb(0xEEEEEE) : Color.White);
				}
				
				return;
			}

			if (columnName == FrameColumnName)
			{
				if (Global.Emulator.Frame == index)
				{
					color = CurrentFrame_FrameCol;
				}
				else if (_currentTasMovie.Markers.IsMarker(index))
				{
					color = Marker_FrameCol;
				}
				else if (record.Lagged.HasValue)
				{
					color = record.Lagged.Value ?
						LagZone_FrameCol :
						GreenZone_FrameCol;
				}
				else
				{
					color = Color.White;
				}
			}
			else
			{
				if (Global.Emulator.Frame == index)
				{
					color = CurrentFrame_InputLog;
				}
				else
				{
					if (record.Lagged.HasValue)
					{
						color = record.Lagged.Value ?
							LagZone_InputLog :
							GreenZone_InputLog;
					}
					else
					{
						color = Color.FromArgb(0xFFFEEE);
					}
				}
			}
		}

		private void TasView_QueryItemText(int index, InputRoll.RollColumn column, out string text)
		{
			try
			{
				text = string.Empty;
				var columnName = column.Name;

				if (columnName == MarkerColumnName)
				{
					// Do nothing
				}
				else if (columnName == FrameColumnName)
				{
					text = (index).ToString().PadLeft(_currentTasMovie.InputLogLength.ToString().Length, '0');
				}
				else
				{
					if (index < _currentTasMovie.InputLogLength)
					{
						text = _currentTasMovie.DisplayValue(index, columnName);
					}
					else if (Global.Emulator.Frame == _currentTasMovie.InputLogLength) // In this situation we have a "pending" frame for the user to click
					{
						text = _currentTasMovie.CreateDisplayValueForButton(
							Global.ClickyVirtualPadController,
							columnName);
					}
				}
			}
			catch (Exception ex)
			{
				text = string.Empty;
				MessageBox.Show("oops\n" + ex);
			}
		}

		#endregion

		#region Events

		private void TasView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			if (TasView.SelectedRows.Any())
			{
				var columnName = e.Column.Name;

				if (columnName == FrameColumnName)
				{
					_currentTasMovie.Markers.Add(TasView.LastSelectedIndex.Value, "");
					RefreshDialog();
					
				}
				else if (columnName != MarkerColumnName) // TODO: what about float?
				{
					foreach (var index in TasView.SelectedRows)
					{
						ToggleBoolState(index, columnName);
						GoToLastEmulatedFrameIfNecessary(TasView.SelectedRows.Min());
					}

					RefreshDialog();
				}
			}
		}

		private void TasView_ColumnRightClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			e.Column.Emphasis ^= true;

			Global.StickyXORAdapter.SetSticky(e.Column.Name, e.Column.Emphasis);

			TasView.Refresh();
		}

		private void TasView_MouseEnter(object sender, EventArgs e)
		{
			TasView.Focus();
		}

		private void TasView_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Middle)
			{
				TogglePause();
				return;
			}

			if (TasView.CurrentCell != null && TasView.CurrentCell.RowIndex.HasValue && TasView.CurrentCell.Column != null)
			{
				if (e.Button == MouseButtons.Left)
				{
					if (TasView.CurrentCell.Column.Name == MarkerColumnName)
					{
						_startMarkerDrag = true;
						GoToFrame(TasView.CurrentCell.RowIndex.Value);
					}
					else if (TasView.CurrentCell.Column.Name == FrameColumnName)
					{
						_startFrameDrag = true;
					}
					else // User changed input
					{
						var frame = TasView.CurrentCell.RowIndex.Value;
						var buttonName = TasView.CurrentCell.Column.Name;

						if (Global.MovieSession.MovieControllerAdapter.Type.BoolButtons.Contains(buttonName))
						{
							ToggleBoolState(TasView.CurrentCell.RowIndex.Value, buttonName);
							GoToLastEmulatedFrameIfNecessary(TasView.CurrentCell.RowIndex.Value);
							RefreshDialog();

							_startBoolDrawColumn = buttonName;

							if (frame < _currentTasMovie.InputLogLength)
							{
								_boolPaintState = _currentTasMovie.BoolIsPressed(frame, buttonName);
							}
							else
							{
								Global.ClickyVirtualPadController.IsPressed(buttonName);
							}
							
						}
						else
						{
							_startFloatDrawColumn = buttonName;
							_floatPaintState = _currentTasMovie.GetFloatValue(frame, buttonName);
						}
					}
				}
			}
		}

		private void TasView_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && !_supressContextMenu)
			{
				RightClickMenu.Show(TasView, e.X, e.Y);
			}
			else if (e.Button == MouseButtons.Left)
			{
				_startMarkerDrag = false;
				_startFrameDrag = false;
				_startBoolDrawColumn = string.Empty;
				_startFloatDrawColumn = string.Empty;
				_floatPaintState = 0;
			}

			_supressContextMenu = false;
		}

		private void TasView_MouseWheel(object sender, MouseEventArgs e)
		{
			if (TasView.RightButtonHeld && TasView.CurrentCell.RowIndex.HasValue)
			{
				_supressContextMenu = true;
				if (e.Delta < 0)
				{
					GoToFrame(Global.Emulator.Frame + 1);
				}
				else
				{
					if (Global.Emulator.Frame > 0)
					{
						GoToFrame(Global.Emulator.Frame - 1);
					}
				}
			}
		}

		private void TasView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (TasView.CurrentCell.RowIndex.HasValue &&
				TasView.CurrentCell != null &&
				TasView.CurrentCell.Column.Name == FrameColumnName &&
				e.Button == MouseButtons.Left)
			{
				if (Global.Config.TAStudioEmptyMarkers)
				{
					_currentTasMovie.Markers.Add(TasView.CurrentCell.RowIndex.Value, string.Empty);
					RefreshDialog();
				}
				else
				{
					CallAddMarkerPopUp(TasView.CurrentCell.RowIndex.Value);
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

			int startVal, endVal;
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

			if (_startMarkerDrag)
			{
				if (e.NewCell.RowIndex.HasValue)
				{
					GoToFrame(e.NewCell.RowIndex.Value);
				}
			}
			else if (_startFrameDrag)
			{
				if (e.OldCell.RowIndex.HasValue && e.NewCell.RowIndex.HasValue)
				{
					for (var i = startVal; i < endVal; i++)
					{
						TasView.SelectRow(i, true);
						TasView.Refresh();
					}
				}
			}
			else if (TasView.IsPaintDown && e.NewCell.RowIndex.HasValue && !string.IsNullOrEmpty(_startBoolDrawColumn))
			{
				if (e.OldCell.RowIndex.HasValue && e.NewCell.RowIndex.HasValue)
				{
					for (var i = startVal; i < endVal; i++)
					{
						SetBoolState(i, _startBoolDrawColumn, _boolPaintState); // Notice it uses new row, old column, you can only paint across a single column
						GoToLastEmulatedFrameIfNecessary(TasView.CurrentCell.RowIndex.Value);
					}

					TasView.Refresh();
				}
			}
			else if (TasView.IsPaintDown && e.NewCell.RowIndex.HasValue && !string.IsNullOrEmpty(_startFloatDrawColumn))
			{
				if (e.OldCell.RowIndex.HasValue && e.NewCell.RowIndex.HasValue)
				{
					for (var i = startVal; i < endVal; i++)
					{
						if (i < _currentTasMovie.InputLogLength) // TODO: how do we really want to handle the user setting the float state of the pending frame?
						{
							_currentTasMovie.SetFloatState(i, _startFloatDrawColumn, _floatPaintState); // Notice it uses new row, old column, you can only paint across a single column
							GoToLastEmulatedFrameIfNecessary(TasView.CurrentCell.RowIndex.Value);
						}
					}

					TasView.Refresh();
				}
			}
		}

		private void TasView_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetSplicer();
		}

		private void TasView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Left) // Ctrl + Left
			{
				GoToPreviousMarker();
			}
			else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Right) // Ctrl + Left
			{
				GoToNextMarker();
			}
			else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Up) // Ctrl + Up
			{
				GoToPreviousFrame();
			}
			else if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Down) // Ctrl + Down
			{
				GoToNextFrame();
			}
			else if (e.Control && !e.Alt && e.Shift && e.KeyCode == Keys.R) // Ctrl + Shift + R
			{
				TasView.HorizontalOrientation ^= true;
			}
		}

		#endregion
	}
}
