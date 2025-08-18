using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.SingleRowOrColFLP"/>
	public class SingleColumnFLP : MarginlessFLPBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#pragma warning disable CA1721 // method name is prop name prefixed with "Get"
		public new AutoSizeMode AutoSizeMode => base.AutoSizeMode;
#pragma warning restore CA1721

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

		public SingleColumnFLP()
		{
			base.AutoSize = true;
			base.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			base.FlowDirection = FlowDirection.TopDown;
			base.MinimumSize = TinySize;
			base.WrapContents = false;
		}
	}
}
