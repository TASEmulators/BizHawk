using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		// Input Painting
		private string _startDrawColumn = string.Empty;
		private bool _boolPaintState;
		private bool _startMarkerDrag;
		private bool _startFrameDrag;

		#region Query callbacks

		private void TasView_QueryItemBkColor(int index, int column, ref Color color)
		{
			var record = _tas[index];
			if (Global.Emulator.Frame == index)
			{
				color = Color.LightBlue;
			}
			else
			{
				if (record.Lagged.HasValue)
				{
					if (record.Lagged.Value)
					{
						color = record.HasState ? Color.LightGreen :
							Color.FromArgb(Color.LightGreen.ToArgb() + 0x00111100);
					}
					else
					{
						color = record.HasState ? Color.Pink : Color.LightPink;
					}
				}
				else
				{
					color = Color.White;
				}
			}
		}

		private void TasView_QueryItemText(int index, int column, out string text)
		{
			try
			{
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
					text = _tas.DisplayValue(index, columnName);
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
				if (TasView.PointedCell.Column == MarkerColumnName)
				{
					_startMarkerDrag = true;
				}
				else if (TasView.PointedCell.Column == FrameColumnName)
				{
					_startFrameDrag = true;
				}
				else
				{
					var frame = TasView.PointedCell.Row.Value;
					var buttonName = TasView.PointedCell.Column;

					// TODO: if float, store the original value and copy that on cell chaned
					if (Global.MovieSession.MovieControllerAdapter.Type.BoolButtons.Contains(buttonName))
					{
						_tas.ToggleBoolState(TasView.PointedCell.Row.Value, TasView.PointedCell.Column);
						TasView.Refresh();

						_startDrawColumn = TasView.PointedCell.Column;
						_boolPaintState = _tas.BoolIsPressed(frame, buttonName);
					}
				}
			}
		}

		private void TasView_MouseUp(object sender, MouseEventArgs e)
		{
			_startMarkerDrag = false;
			_startFrameDrag = false;
			_startDrawColumn = string.Empty;
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
					GoToFrame(e.NewCell.Row.Value);
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
			else if (TasView.IsPaintDown && e.NewCell.Row.HasValue && !string.IsNullOrEmpty(_startDrawColumn))
			{
				if (e.OldCell.Row.HasValue && e.NewCell.Row.HasValue)
				{
					for (var i = startVal; i < endVal; i++)
					{
						_tas.SetBoolState(i, _startDrawColumn, _boolPaintState); // Notice it uses new row, old column, you can only paint across a single column
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
