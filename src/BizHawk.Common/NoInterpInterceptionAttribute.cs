namespace BizHawk.Common
{
	/// <summary>
	/// Prevents passing interpolated strings to methods which may have an intercepting overload;
	/// enforced by <c>NoInterpInterceptionAnalyzer</c> in <c>BizHawk.Analyzer</c>.
	/// </summary>
	/// <remarks>
	/// So far, this is only necessary in cases where interceptors are polyfilled but the polyfill is unavailable at runtime,
	/// namely in the static constructor of the entry point class <c>Program</c> in EmuHawk/DiscoHawk.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
	public sealed class NoInterpInterceptionAttribute : Attribute;
}
