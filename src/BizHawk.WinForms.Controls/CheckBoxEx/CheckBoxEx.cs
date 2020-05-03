using System.ComponentModel;
using System.Drawing;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.CheckBoxOrRadioButton"/>
	public class CheckBoxEx : CheckBoxExBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Point Location => base.Location;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		public CheckBoxEx() => base.AutoSize = true;
	}
}
