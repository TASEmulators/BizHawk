using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		#region Query callbacks

		private void TasView_QueryItemBkColor(int index, int column, ref Color color)
		{
			var record = _tas[index];
			if (Global.Emulator.Frame == index + 1)
			{
				color = Color.LightBlue;
			}
			else if (!record.HasState)
			{
				color = BackColor;
			}
			else
			{
				color = record.Lagged ? Color.Pink : Color.LightGreen;
			}
		}

		private void TasView_QueryItemText(int index, int column, out string text)
		{
			try
			{
				var columnName = TasView.Columns[column].Name;

				if (columnName == MarkerColumnName)
				{
					text = Global.Emulator.Frame == index + 1 ? ">" : string.Empty;
				}
				else if (columnName == FrameColumnName)
				{
					text = (index + 1).ToString().PadLeft(5, '0');
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
					//_tas.ToggleButton(TasView.PointedCell.Row.Value, TasView.PointedCell.Column);
					TasView.Refresh();

					_startDrawColumn = TasView.PointedCell.Column;
					//_startOn = _tas.IsPressed(TasView.PointedCell.Row.Value, TasView.PointedCell.Column);
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

					for (var i = startVal + 1; i <= endVal; i++)
					{
						TasView.SelectItem(i, true);
					}
				}
			}
			else if (TasView.IsPaintDown && e.NewCell.Row.HasValue && !string.IsNullOrEmpty(_startDrawColumn))
			{
				//_tas.SetBoolButton(e.NewCell.Row.Value, _startDrawColumn, /*_startOn*/ false); // Notice it uses new row, old column, you can only paint across a single column
				TasView.Refresh();
			}
		}

		private void TasView_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetSplicer();
		}

		#endregion
	}
}
