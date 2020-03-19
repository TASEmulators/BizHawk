using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public abstract class MarginlessFLPBase : FlowLayoutPanel
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Padding Margin => base.Margin;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int TabIndex => base.TabIndex;

		protected MarginlessFLPBase() => base.Margin = Padding.Empty;

		protected static readonly Size TinySize = new Size(24, 24);
	}
}
