using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IEmulation : IExternalApi
	{
		string BoardName { get; }

		string DisplayType { get; }

		Action FrameAdvanceCallback { get; set; }

		int FrameCount { get; }

		bool IsLagged { get; set; }

		bool IsVSyncEnabled { get; set; }

		int LagCount { get; set; }

		bool LimitFramerate { get; set; }

		bool MinimizeFrameskip { get; set; }

		string SystemID { get; }

		long TotalExecutedCycles { get; }

		Action YieldCallback { get; set; }

		object Disassemble(uint pc, string name = null);

		void FrameAdvance();

		ulong? GetRegister(string name);

		IDictionary<string, ulong> GetRegisters();

		object GetSettings();

		bool PutSettings(object settings);

		void SetRegister(string register, int value);

		void SetRenderPlanes(params bool[] args);

		void Yield();
	}
}
