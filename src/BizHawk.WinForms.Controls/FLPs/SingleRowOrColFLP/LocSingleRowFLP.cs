using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.SingleRowOrColFLP"/>
	public class LocSingleRowFLP : MarginlessFLPBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new AutoSizeMode AutoSizeMode => base.AutoSizeMode;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new FlowDirection FlowDirection => base.FlowDirection;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size MinimumSize => base.MinimumSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool WrapContents => base.WrapContents;

		public LocSingleRowFLP()
		{
			base.AutoSize = true;
			base.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			// for some reason this breaks stuff on mono, see #4376
			if (!OSTailoredCode.IsUnixHost) base.MinimumSize = TinySize;
			base.WrapContents = false;
		}
	}
}
