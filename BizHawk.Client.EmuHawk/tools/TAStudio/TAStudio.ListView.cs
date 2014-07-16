using System;
using System.Drawing;
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
		private bool _rightMouseHeld = false;

		private readonly Color CurrentFrame_FrameCol = Color.FromArgb(0xCFEDFC);
		private readonly Color CurrentFrame_InputLog = Color.FromArgb(0xB5E7F7);

		private readonly Color GreenZone_FrameCol = Color.FromArgb(0xDDFFDD);
		private readonly Color GreenZone_InputLog = Color.FromArgb(0xC4F7C8);

		private readonly Color LagZone_FrameCol = Color.FromArgb(0xFFDCDD);
		private readonly Color LagZone_InputLog = Color.FromArgb(0xF0D0D2);

		private readonly Color NoState_GreenZone_FrameCol = Color.FromArgb(0xF9FFF9);
		private readonly Color NoState_GreenZone_InputLog = Color.FromArgb(0xE0FBE0);

		private readonly Color NoState_LagZone_FrameCol = Color.FromArgb(0xFFE9E9);
		private readonly Color NoState_LagZone_InputLog = Color.FromArgb(0xF0D0D2);

		private readonly Color Marker_FrameCol = Color.FromArgb(0xF7FFC9);

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
			if (index == Global.Emulator.Frame && index == _tas.InputLogLength)
			{
				if (columnName == FrameColumnName)
				{
					color = CurrentFrame_FrameCol;
				}

				color = CurrentFrame_InputLog;

				return;
			}

			var record = _tas[index];

			if (columnName == FrameColumnName)
			{
				if (Global.Emulator.Frame == index)
				{
					color = CurrentFrame_FrameCol;
				}
				else if (_tas.Markers.IsMarker(index))
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
					if (index < _tas.InputLogLength)
					{
						text = _tas.DisplayValue(index, columnName);
					}
					else if (Global.Emulator.Frame == _tas.InputLogLength) // In this situation we have a "pending" frame for the user to click
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

		private void TasView_MouseDown(object sender, MouseEventArgs e)
		{
			if (TasView.PointedCell.Row.HasValue && !string.IsNullOrEmpty(TasView.PointedCell.Column))
			{
				if (e.Button == MouseButtons.Left)
				{
					if (TasView.PointedCell.Column == MarkerColumnName)
					{
						_startMarkerDrag = true;
						GoToFrame(TasView.PointedCell.Row.Value - 1);
					}
					else if (TasView.PointedCell.Column == FrameColumnName)
					{
						_startFrameDrag = true;
					}
					else
					{
						var frame = TasView.PointedCell.Row.Value;
						var buttonName = TasView.PointedCell.Column;

						if (Global.MovieSession.MovieControllerAdapter.Type.BoolButtons.Contains(buttonName))
						{
							ToggleBoolState(TasView.PointedCell.Row.Value, TasView.PointedCell.Column);
							GoToLastEmulatedFrameIfNecessary(TasView.PointedCell.Row.Value);
							TasView.Refresh();

							_startBoolDrawColumn = TasView.PointedCell.Column;
							_boolPaintState = _tas.BoolIsPressed(frame, buttonName);
						}
						else
						{
							_startFloatDrawColumn = TasView.PointedCell.Column;
							_floatPaintState = _tas.GetFloatValue(frame, buttonName);
						}
					}
				}
				else if (e.Button == MouseButtons.Right)
				{
					_rightMouseHeld = true;
				}
			}
		}

		// TODO: move me
		// Sets either the pending frame or the tas input log
		private void ToggleBoolState(int frame, string buttonName)
		{
			if (frame < _tas.InputLogLength)
			{
				_tas.ToggleBoolState(frame, buttonName);
			}
			else if (frame == Global.Emulator.Frame && frame == _tas.InputLogLength)
			{
				Global.ClickyVirtualPadController.Toggle(buttonName);
			}
		}

		// TODO: move me
		// Sets either the pending frame or the tas input log
		private void SetBoolState(int frame, string buttonName, bool value)
		{
			if (frame < _tas.InputLogLength)
			{
				_tas.SetBoolState(frame, buttonName, value);
			}
			else if (frame == Global.Emulator.Frame && frame == _tas.InputLogLength)
			{
				Global.ClickyVirtualPadController.SetBool(buttonName, value);
			}
		}

		private void TasView_MouseUp(object sender, MouseEventArgs e)
		{
			_startMarkerDrag = false;
			_startFrameDrag = false;
			_startBoolDrawColumn = string.Empty;
			_startFloatDrawColumn = string.Empty;
			_floatPaintState = 0;
			_rightMouseHeld = false;
		}

		private void TasView_MouseWheel(object sender, MouseEventArgs e)
		{
			if (_rightMouseHeld)
			{
				if (e.Delta < 0)
				{
					GoToFrame(Global.Emulator.Frame);
				}
				else
				{
					GoToFrame(Global.Emulator.Frame - 2);
				}
			}
		}

		private void TasView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (TasView.PointedCell.Row.HasValue &&
				!string.IsNullOrEmpty(TasView.PointedCell.Column) &&
				TasView.PointedCell.Column == FrameColumnName)
			{
				CallAddMarkerPopUp(TasView.PointedCell.Row.Value);
			}
		}

		private void TasView_PointedCellChanged(object sender, TasListView.CellEventArgs e)
		{
			int startVal, endVal;
			if (e.OldCell.Row.Value < e.NewCell.Row.Value)
			{
				startVal = e.OldCell.Row.Value;
				endVal = e.NewCell.Row.Value;
			}
			else
			{
				startVal = e.NewCell.Row.Value;
				endVal = e.OldCell.Row.Value;
			}

			if (_startMarkerDrag)
			{
				if (e.NewCell.Row.HasValue)
				{
					GoToFrame(e.NewCell.Row.Value - 1);
				}
			}
			else if (_startFrameDrag)
			{
				if (e.OldCell.Row.HasValue && e.NewCell.Row.HasValue)
				{
					for (var i = startVal + 1; i <= endVal; i++)
					{
						TasView.SelectItem(i, true);
					}
				}
			}
			else if (TasView.IsPaintDown && e.NewCell.Row.HasValue && !string.IsNullOrEmpty(_startBoolDrawColumn))
			{
				if (e.OldCell.Row.HasValue && e.NewCell.Row.HasValue)
				{
					for (var i = startVal; i < endVal; i++)
					{
						SetBoolState(i, _startBoolDrawColumn, _boolPaintState); // Notice it uses new row, old column, you can only paint across a single column
						GoToLastEmulatedFrameIfNecessary(TasView.PointedCell.Row.Value);
					}

					TasView.Refresh();
				}
			}
			else if (TasView.IsPaintDown && e.NewCell.Row.HasValue && !string.IsNullOrEmpty(_startFloatDrawColumn))
			{
				if (e.OldCell.Row.HasValue && e.NewCell.Row.HasValue)
				{
					for (var i = startVal; i < endVal; i++)
					{
						if (i < _tas.InputLogLength) // TODO: how do we really want to handle the user setting the float state of the pending frame?
						{
							_tas.SetFloatState(i, _startFloatDrawColumn, _floatPaintState); // Notice it uses new row, old column, you can only paint across a single column
							GoToLastEmulatedFrameIfNecessary(TasView.PointedCell.Row.Value);
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

		#endregion
	}
}
