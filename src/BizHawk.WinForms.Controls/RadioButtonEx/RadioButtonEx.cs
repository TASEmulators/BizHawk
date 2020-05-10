using System.ComponentModel;
using System.Drawing;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.CheckBoxOrRadioButton"/>
	public class RadioButtonEx : RadioButtonExBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Point Location => base.Location;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		public RadioButtonEx() {}

		public RadioButtonEx(IRadioButtonReadOnlyTracker tracker) : base(tracker) => base.AutoSize = true;
	}
}
