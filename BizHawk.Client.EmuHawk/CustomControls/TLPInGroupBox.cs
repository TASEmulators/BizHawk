using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public class TLPInGroupBox : GroupBox
	{
		public readonly TableLayoutPanel InnerTLP = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };

		public new TableLayoutControlCollection Controls => InnerTLP.Controls;

		public TLPInGroupBox(int columns, int rows)
		{
			base.Controls.Add(InnerTLP);
			InnerTLP.ColumnCount = columns;
			for (var i = columns; i != 0; i--) InnerTLP.ColumnStyles.Add(new ColumnStyle());
			InnerTLP.RowCount = rows;
			for (var i = rows; i != 0; i--) InnerTLP.RowStyles.Add(new RowStyle());
		}
	}
}