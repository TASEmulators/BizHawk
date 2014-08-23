using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

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

		public static Color NoState_GreenZone_FrameCol = Color.FromArgb(0xF9FFF9);
		public static Color NoState_GreenZone_InputLog = Color.FromArgb(0xE0FBE0);

		public static Color NoState_LagZone_FrameCol = Color.FromArgb(0xFFE9E9);
		public static Color NoState_LagZone_InputLog = Color.FromArgb(0xF0D0D2);

		public static Color Marker_FrameCol = Color.FromArgb(0xF7FFC9);

		#region Query callbacks

		private void TasView_QueryItemBkColor(int index, int column, ref Color color)
		{
			var columnName = TasView.Columns[column].Name;

			// Marker Column is white regardless
			if (columnName == MarkerColumnName)
			{
				color = Color.White;
				return;
			}

			// "pending" frame logic
			if (index == Global.Emulator.Frame && index == _currentTasMovie.InputLogLength)
			{
				if (columnName == FrameColumnName)
				{
					color = CurrentFrame_FrameCol;
				}

				color = CurrentFrame_InputLog;

				return;
			}

			var record = _currentTasMovie[index];

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
					if (record.Lagged.Value)
					{
						color = record.HasState ? GreenZone_FrameCol : NoState_GreenZone_FrameCol;
					}
					else
					{
						color = record.HasState ? LagZone_FrameCol : NoState_LagZone_InputLog;
					}
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
						if (record.Lagged.Value)
						{
							color = record.HasState ? GreenZone_InputLog : NoState_GreenZone_InputLog;
						}
						else
						{
							color = record.HasState ? LagZone_InputLog : NoState_LagZone_InputLog;
						}
					}
					else
					{
						color = (columnName == MarkerColumnName || columnName == FrameColumnName) ?
							Color.White :
							SystemColors.ControlLight;
					}
				}
			}
		}

		private void TasView_QueryItemText(int index, int column, out string text)
		{
			try
			{
				text = string.Empty;
				var columnName = TasView.Columns[column].Name;

				if (columnName == MarkerColumnName)
				{
					text = Global.Emulator.Frame == index ? ">" : string.Empty;
				}
				else if (columnName == FrameColumnName)
				{
					text = (index).ToString().PadLeft(5, '0');
				}
				else
				{
					if (index < _currentTasMovie.InputLogLength)
					{
						text = _currentTasMovie.DisplayValue(index, columnName);
					}
					else if (Global.Emulator.Frame == _currentTasMovie.InputLogLength) // In this situation we have a "pending" frame for the user to click
					{
						text = TasMovie.CreateDisplayValueForButton(
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

		private void TasView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (TasView.SelectedIndices().Any())
			{
				var columnName = TasView.Columns[e.Column].Name;

				if (columnName == FrameColumnName)
				{
					// TODO: add marker to LastSelectedIndex
				}
				else if (columnName != MarkerColumnName) // TODO: what about float?
				{
					foreach (var index in TasView.SelectedIndices())
					{
						ToggleBoolState(index, columnName);
					}

					RefreshDialog();
				}
			}
		}

		private void TasView_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Middle)
			{
				TogglePause();
				return;
			}

			if (TasView.CurrentCell.RowIndex.HasValue && !string.IsNullOrEmpty(TasView.CurrentCell.Column))
			{
				if (e.Button == MouseButtons.Left)
				{
					if (TasView.CurrentCell.Column == MarkerColumnName)
					{
						_startMarkerDrag = true;
						GoToFrame(TasView.CurrentCell.RowIndex.Value);
					}
					else if (TasView.CurrentCell.Column == FrameColumnName)
					{
						_startFrameDrag = true;
					}
					else//User changed input
					{
						var frame = TasView.CurrentCell.RowIndex.Value;
						var buttonName = TasView.CurrentCell.Column;

						if (Global.MovieSession.MovieControllerAdapter.Type.BoolButtons.Contains(buttonName))
						{
							ToggleBoolState(TasView.CurrentCell.RowIndex.Value, TasView.CurrentCell.Column);
							GoToLastEmulatedFrameIfNecessary(TasView.CurrentCell.RowIndex.Value);
							TasView.Refresh();

							if (Global.Config.TAStudioAutoRestoreLastPosition)
							{
								GlobalWin.MainForm.UnpauseEmulator();
								GlobalWin.MainForm.PauseOnFrame = Global.Emulator.Frame;
							}

							_startBoolDrawColumn = TasView.CurrentCell.Column;
							_boolPaintState = _currentTasMovie.BoolIsPressed(frame, buttonName);
						}
						else
						{
							_startFloatDrawColumn = TasView.CurrentCell.Column;
							_floatPaintState = _currentTasMovie.GetFloatValue(frame, buttonName);
						}
					}
				}
				else if (e.Button == MouseButtons.Right)
				{
					var frame = TasView.CurrentCell.RowIndex.Value;
					var buttonName = TasView.CurrentCell.Column;
					if (TasView.SelectedIndices.IndexOf(frame) != -1 && (buttonName == MarkerColumnName || buttonName == FrameColumnName))
					{
						//Disable the option to remove markers if no markers are selected (FCUEX does this).
						RemoveMarkersContextMenuItem.Enabled = _currentTasMovie.Markers.Any(m => TasView.SelectedIndices().Contains(m.Frame));
						RightClickMenu.Show(TasView, e.X, e.Y);
					}
				}
			}
		}

		private void TasView_MouseUp(object sender, MouseEventArgs e)
		{
			_startMarkerDrag = false;
			_startFrameDrag = false;
			_startBoolDrawColumn = string.Empty;
			_startFloatDrawColumn = string.Empty;
			_floatPaintState = 0;
		}

		private void TasView_MouseWheel(object sender, MouseEventArgs e)
		{
			if (TasView.RightButtonHeld && TasView.CurrentCell.RowIndex.HasValue)
			{
				if (e.Delta < 0)
				{
					GoToFrame(Global.Emulator.Frame + 1);
				}
				else
				{
					GoToFrame(Global.Emulator.Frame - 1);
				}
			}
		}

		private void TasView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (TasView.CurrentCell.RowIndex.HasValue &&
				!string.IsNullOrEmpty(TasView.CurrentCell.Column) &&
				TasView.CurrentCell.Column == FrameColumnName)
			{
				CallAddMarkerPopUp(TasView.CurrentCell.RowIndex.Value);
			}
		}

		private void TasView_PointedCellChanged(object sender, TasListView.CellEventArgs e)
		{
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
					for (var i = startVal + 1; i <= endVal; i++)
					{
						TasView.SelectItem(i, true);
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

		private void TasView_VirtualItemsSelectionRangeChanged(object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e)
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
		}

		#endregion
	}
}
