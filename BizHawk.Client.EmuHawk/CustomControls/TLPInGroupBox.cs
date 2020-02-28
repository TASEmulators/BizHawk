using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	/// <seealso cref="FLPInGroupBox"/>
	public class TLPInGroupBox : GroupBox
	{
		public new TableLayoutControlCollection Controls => InnerTLP.Controls;

		public TableLayoutPanel InnerTLP { get; } = new TableLayoutPanel {
			AutoSize = true,
			Dock = DockStyle.Fill
		};

		public TLPInGroupBox() : base() => base.Controls.Add(InnerTLP);

		public TLPInGroupBox(int columns, int rows) : this()
		{
			InnerTLP.ColumnCount = columns;
			for (var i = columns; i != 0; i--) InnerTLP.ColumnStyles.Add(new ColumnStyle());
			InnerTLP.RowCount = rows;
			for (var i = rows; i != 0; i--) InnerTLP.RowStyles.Add(new RowStyle());
		}
	}
}
