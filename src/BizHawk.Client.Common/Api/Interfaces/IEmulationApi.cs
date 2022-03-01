#nullable enable

using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IEmulationApi : IExternalApi
	{
		void DisplayVsync(bool enabled);
		int FrameCount();
		object? Disassemble(uint pc, string? name = null);
		ulong? GetRegister(string name);
		IReadOnlyDictionary<string, ulong> GetRegisters();
		void SetRegister(string register, int value);
		long TotalExecutedCycles();
		string GetSystemId();
		bool IsLagged();
		void SetIsLagged(bool value = true);
		int LagCount();
		void SetLagCount(int count);
		void LimitFramerate(bool enabled);
		void MinimizeFrameskip(bool enabled);
		string GetDisplayType();
		string GetBoardName();

		IGameInfo? GetGameInfo();

		IReadOnlyDictionary<string, string?> GetGameOptions();

		object? GetSettings();
		PutSettingsDirtyBits PutSettings(object settings);
		void SetRenderPlanes(params bool[] args);
	}
}
