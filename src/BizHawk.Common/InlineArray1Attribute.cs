namespace System.Runtime.CompilerServices
{
	/// <remarks>TODO better name? maybe literally: <c>BetterInlineArrayAttribute</c></remarks>
	[AttributeUsage(AttributeTargets.Struct)]
	public sealed class InlineArray1Attribute(int length) : Attribute
	{
		public int Length
			=> length;
	}
}
