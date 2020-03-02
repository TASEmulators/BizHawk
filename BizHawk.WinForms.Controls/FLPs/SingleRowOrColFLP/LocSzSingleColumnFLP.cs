using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.SingleRowOrColFLP"/>
	public class LocSzSingleColumnFLP : MarginlessFLPBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new FlowDirection FlowDirection => base.FlowDirection;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool WrapContents => base.WrapContents;

		public LocSzSingleColumnFLP()
		{
			base.FlowDirection = FlowDirection.TopDown;
			base.WrapContents = false;
		}
	}
}
