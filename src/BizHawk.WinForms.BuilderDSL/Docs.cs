namespace BizHawk.WinForms.BuilderDSL
{
	internal static class Docs
	{
		/// <remarks>
		/// Project convention for the order in which properties are processed/set (should also be followed in DSL usage):
		/// <list type="number">
		/// <item><description>content; sub-order: text (for Button, CheckBox, Label, TabPage...), then value/checked (for CheckBox, NUD, RadioButton, TextBox...)</description></item>
		/// <item><description>position; sub-order: fixed position, then anchor</description></item>
		/// <item><description>size; sub-order: autosize or fixed size, then inner padding, then outer padding</description></item>
		/// <item><description>appearance; sub-order: control-specific, then background colour</description></item>
		/// <item><description>behaviour; sub-order: control-specific properties, then enable/disable, then events</description></item>
		/// <item><description>children</description></item>
		/// </list>
		/// </remarks>
		public const bool OrderingConvention = true;
	}
}
