using System;
using System.Collections.Generic;

namespace BizHawk.Client.ApiHawk
{
	public interface IEmu : IExternalApi
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
		bool PutSettings(object settings);
		void SetRenderPlanes(params bool[] param);
	}
}
