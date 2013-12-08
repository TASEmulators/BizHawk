using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class TasListView : VirtualListView
	{
		public string PointedColumnName = String.Empty;
		public int? PointedRowIndex = null;

		private void CalculatePointedCell(int x, int y)
		{
			string columnName = String.Empty;

			var accumulator = 0;
			foreach (ColumnHeader column in Columns)
			{
				accumulator += column.Width;
				if (accumulator < x)
				{
					continue;
				}
				else
				{
					PointedColumnName = column.Name;
					break;
				}
			}

			var headerHeight = 24; //Are these always true? Don't know, is there a way to programmatically determine them?
			var rowHeight = 18;

			PointedRowIndex = ((y - headerHeight) / rowHeight) + this.VScrollPos;
			if (PointedRowIndex >= ItemCount)
			{
				PointedRowIndex = null;
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			PointedColumnName = String.Empty;
			PointedRowIndex = null;
			base.OnMouseLeave(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			CalculatePointedCell(e.X, e.Y);
			base.OnMouseMove(e);
		}
	}
}
