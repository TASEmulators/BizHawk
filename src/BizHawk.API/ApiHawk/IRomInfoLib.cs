namespace BizHawk.API.ApiHawk
{
	/// <remarks>
	/// Changes from 2.4.2:
	/// <list type="bullet">
	/// <item><description>method <c>bool IsStatusBad()</c> removed</description></item>
	/// <item><description>remaining 6 methods replaced with properties on type of <see cref="LoadedRom"/></description></item>
	/// <item><description>method from <c>IEmu</c> (<c>string GetBoardName()</c>) merged into this lib</description></item>
	/// </list>
	/// </remarks>
	public interface IRomInfoLib
	{
		public LoadedRomInfo? LoadedRom { get; }
	}
}
