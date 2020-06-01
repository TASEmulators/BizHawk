#nullable enable

using System;
using System.Collections.Generic;

using BizHawk.API.ApiHawk;

namespace BizHawk.Client.Common
{
	[LegacyApiHawk]
	public interface IEmu : IExternalApi
	{
		[LegacyApiHawk]
		public Action? FrameAdvanceCallback { get; set; }

		[LegacyApiHawk]
		public Action? YieldCallback { get; set; }

		[LegacyApiHawk]
		public object? Disassemble(uint pc, string name = "");

		[LegacyApiHawk]
		public void DisplayVsync(bool enabled);

		[LegacyApiHawk]
		public void FrameAdvance();

		[LegacyApiHawk]
		public int FrameCount();

		[LegacyApiHawk]
		public string GetBoardName();

		[LegacyApiHawk]
		public string GetDisplayType();

		[LegacyApiHawk]
		public ulong? GetRegister(string name);

		[LegacyApiHawk]
		public Dictionary<string, ulong> GetRegisters();

		[LegacyApiHawk]
		public object? GetSettings();

		[LegacyApiHawk]
		public string? GetSystemId();

		[LegacyApiHawk]
		public bool IsLagged();

		[LegacyApiHawk]
		public int LagCount();

		[LegacyApiHawk]
		public void LimitFramerate(bool enabled);

		[LegacyApiHawk]
		public void MinimizeFrameskip(bool enabled);

		[LegacyApiHawk]
		public PutSettingsDirtyBits PutSettings(object settings);

		[LegacyApiHawk]
		public void SetIsLagged(bool value = true);

		[LegacyApiHawk]
		public void SetLagCount(int count);

		[LegacyApiHawk]
		public void SetRegister(string register, int value);

		[LegacyApiHawk]
		public void SetRenderPlanes(params bool[] args);

		[LegacyApiHawk]
		public long TotalExecutedCycles();

		[LegacyApiHawk]
		public void Yield();
	}
}
