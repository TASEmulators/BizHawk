using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BizHawk.API.ApiHawk
{
	/// <remarks>
	/// Changes from 2.4.2:
	/// <list type="bullet">
	/// <item><description>method <c>string GetDisplayType()</c> replaced with property <see cref="DisplayType"/> (changed return type to nullable enum)</description></item>
	/// <item><description>method <c>int FrameCount()</c> replaced with property <see cref="FrameCount"/></description></item>
	/// <item><description>methods <c>bool IsLagged()</c> and <c>void SetIsLagged(bool value = true)</c> replaced with property <see cref="IsInputPollingFrame"/> (reversed semantics)</description></item>
	/// <item><description>method <c>void LimitFramerate(bool enabled)</c> replaced with property <see cref="IsFrameRateLimited"/></description></item>
	/// <item><description>method <c>void MinimizeFrameskip(bool enabled)</c> replaced with property <see cref="IsFrameSkipMinimised"/></description></item>
	/// <item><description>method <c>void DisplayVsync(bool enabled)</c> replaced with property <see cref="IsVSyncEnabled"/></description></item>
	/// <item><description>methods <c>int LagCount()</c> and <c>void SetLagCount(int count)</c> replaced with property <see cref="NonPollingFrameCount"/> (changed return type to nullable)</description></item>
	/// <item><description>methods <c>ulong? GetRegister(string name)</c> and <c>void SetRegister(string register, int value)</c> replaced with indexer on type of property <see cref="Register"/></description></item>
	/// <item><description>method <c>Dictionary&lt;string, ulong> GetRegisters()</c> replaced with property <see cref="RegisterDict"/> (changed return type to read-only dictionary)</description></item>
	/// <item><description>method <c>string? GetSystemId()</c> replaced with property <see cref="SystemID"/></description></item>
	/// <item><description>method <c>long TotalExecutedCycles()</c> replaced with property <see cref="TotalExecutedCycles"/> (changed return type to nullable)</description></item>
	/// <item><description>non-functional method <c>void FrameAdvance()</c> and working <c>void DoFrameAdvance()</c> from <c>ClientApi</c> replaced with a single working method <see cref="DoFrameAdvance"/>, which returns a <see cref="bool">bool</see> (<see langword="true"/> if successful)</description></item>
	/// <item><description>return value of method <c>PutSettingsDirtyBits PutSettings(object settings)</c> moved to out parameter and the method now returns a <see cref="bool">bool</see> (<see langword="false"/> iff the out param is <see cref="PutSettingsDirtyBits.None"/>); see <see cref="PutSettings"/></description></item>
	/// <item><description>method <c>string GetBoardName()</c> merged into <see cref="IRomInfoLib"/> as <see cref="IRomInfoLib.LoadedRom"/>.<see cref="LoadedRomInfo.MapperName"/></description></item>
	/// <item><description>non-functional property <c>Action? FrameAdvanceCallback { get; set; }</c> removed, it was never meant to be part of the public API</description></item>
	/// <item><description>non-functional property <c>Action? YieldCallback { get; set; }</c> removed, it was never meant to be part of the public API</description></item>
	/// <item><description>non-functional method <c>void Yield()</c> removed</description></item>
	/// <item><description>method <c>object? Disassemble(uint pc, string name = "")</c> now returns tuple <c>(string, int)?</c> instead of anonymous class (as <c>object?</c>)</description></item>
	/// <item><description>method <c>object? GetSettings()</c> unchanged</description></item>
	/// <item><description>method <c>void SetRenderPlanes(params bool[] args)</c> unchanged</description></item>
	/// </list>
	/// </remarks>
	public interface IEmulationLib
	{
		DisplayType? DisplayType { get; }

		int FrameCount { get; }

		bool IsInputPollingFrame { get; set; }

		bool IsFrameRateLimited { get; set; }

		bool IsFrameSkipMinimised { get; set; }

		bool IsVSyncEnabled { get; set; }

		[DisallowNull]
		int? NonPollingFrameCount { get; set; }

		IRegisterAccess Register { get; }

		IReadOnlyDictionary<string, ulong> RegisterDict { get; }

		string? SystemID { get; }

		long? TotalExecutedCycles { get; }

		(string Disasm, int Length)? Disassemble(uint pc, string name = "");

		bool DoFrameAdvance();

		object? GetSettings();

		/// <returns><see langword="true"/> iff the changes require further action, <paramref name="requiredAfterChange"/> contains more specific flags</returns>
		bool PutSettings(object settings, out PutSettingsDirtyBits requiredAfterChange);

		void SetRenderPlanes(params bool[] args);
	}
}
