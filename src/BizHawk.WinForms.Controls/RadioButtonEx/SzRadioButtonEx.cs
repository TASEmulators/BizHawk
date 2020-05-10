using System.ComponentModel;
using System.Drawing;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.CheckBoxOrRadioButton"/>
	public class SzRadioButtonEx : RadioButtonExBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Point Location => base.Location;

		public SzRadioButtonEx() {}

		public SzRadioButtonEx(IRadioButtonReadOnlyTracker tracker) : base(tracker) {}
	}
}
