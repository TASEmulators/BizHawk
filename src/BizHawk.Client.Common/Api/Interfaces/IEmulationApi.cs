using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IEmulationApi : IExternalApi
	{
		Action FrameAdvanceCallback { get; set; }
		Action YieldCallback { get; set; }
		void DisplayVsync(bool enabled);
		void FrameAdvance();
		int FrameCount();
		object Disassemble(uint pc, string name = "");
		ulong? GetRegister(string name);
		Dictionary<string, ulong> GetRegisters();
		void SetRegister(string register, int value);
		long TotalExecutedCycles();
		string GetSystemId();
		bool IsLagged();
		void SetIsLagged(bool value = true);
		int LagCount();
		void SetLagCount(int count);
		void LimitFramerate(bool enabled);
		void MinimizeFrameskip(bool enabled);
		void Yield();
		string GetDisplayType();
		string GetBoardName();
		object GetSettings();
		PutSettingsDirtyBits PutSettings(object settings);
		void SetRenderPlanes(params bool[] args);
	}
}
