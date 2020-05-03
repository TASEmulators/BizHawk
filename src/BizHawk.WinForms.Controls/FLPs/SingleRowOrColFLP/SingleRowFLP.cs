using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.SingleRowOrColFLP"/>
	public class SingleRowFLP : MarginlessFLPBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new AutoSizeMode AutoSizeMode => base.AutoSizeMode;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new FlowDirection FlowDirection => base.FlowDirection;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Point Location => base.Location;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size MinimumSize => base.MinimumSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool WrapContents => base.WrapContents;

		public SingleRowFLP()
		{
			base.AutoSize = true;
			base.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			base.MinimumSize = TinySize;
			base.WrapContents = false;
		}
	}
}
