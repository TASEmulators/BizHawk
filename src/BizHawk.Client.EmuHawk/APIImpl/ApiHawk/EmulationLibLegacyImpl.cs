#nullable enable

using System;
using System.Collections.Generic;

using BizHawk.API.ApiHawk;
using BizHawk.API.Base;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.WonderSwan;

using DisplayType = BizHawk.API.ApiHawk.DisplayType;

namespace BizHawk.Client.EmuHawk.APIImpl.ApiHawk
{
	internal sealed class EmulationLibLegacyImpl : LibBase<GlobalsAccessAPIEnvironment>, IEmulationLib, IEmu
	{
		private IRegisterAccess? _registerAccess;

		public DisplayType? DisplayType => Env.RegionableCore?.Region;

		private IEmulator Emulator => Env.EmuCore ?? throw new NullReferenceException();

		/// <summary>Using this property to get a reference to <c>Global.Config</c> is a terrible, horrible, no good, very bad idea. That's why it's not in the <see cref="IEmulationLib">interface</see>. You'd need to go out of your way to put a helper class in this namespace to access it.</summary>
		public Config? ForbiddenConfigReference
		{
			get
			{
				ForbiddenConfigReferenceUsed = true;
				return Env.GlobalConfig;
			}
		}

		public bool ForbiddenConfigReferenceUsed { get; private set; }

		[LegacyApiHawk]
		public Action? FrameAdvanceCallback { get; set; }

		public int FrameCount => Emulator.Frame;

		public bool IsInputPollingFrame
		{
			get
			{
				if (Env.InputPollableCore != null) return !Env.InputPollableCore.IsLagFrame;
				Env.LogCallback($"Can not get lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
				return true;
			}
			set
			{
				if (Env.InputPollableCore != null) Env.InputPollableCore.IsLagFrame = !value;
				else Env.LogCallback($"Can not set lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			}
		}

		public bool IsFrameRateLimited
		{
			get => Env.GlobalConfig.ClockThrottle;
			set => Env.GlobalConfig.ClockThrottle = value;
		}

		public bool IsFrameSkipMinimised
		{
			get => Env.GlobalConfig.AutoMinimizeSkipping;
			set => Env.GlobalConfig.AutoMinimizeSkipping = value;
		}

		public bool IsVSyncEnabled
		{
			get => Env.GlobalConfig.VSync;
			set => Env.GlobalConfig.VSync = value;
		}

		public int? NonPollingFrameCount
		{
			get
			{
				if (Env.InputPollableCore != null) return Env.InputPollableCore.LagCount;
				Env.LogCallback($"Can not get lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
				return null;
			}
			set
			{
				if (Env.InputPollableCore != null) Env.InputPollableCore.LagCount = value ?? throw new NullReferenceException();
				else Env.LogCallback($"Can not set lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			}
		}

		public IRegisterAccess Register => _registerAccess ?? throw new NullReferenceException();

		public string? SystemID => Env.GlobalGame.System;

		public long? TotalExecutedCycles
		{
			get
			{
				try
				{
					if (Env.DebuggableCore == null) throw new NotImplementedException();
					return Env.DebuggableCore.TotalExecutedCycles;
				}
				catch (NotImplementedException)
				{
					Env.LogCallback($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.TotalExecutedCycles)}()");
					return null;
				}
			}
		}

		public IReadOnlyDictionary<string, ulong> RegisterDict => GetRegisters();

		[LegacyApiHawk]
		public Action? YieldCallback { get; set; }

		public EmulationLibLegacyImpl(out Action<GlobalsAccessAPIEnvironment> updateEnv) : base(out updateEnv) {}

		(string Disasm, int Length)? IEmulationLib.Disassemble(uint pc, string name)
		{
			try
			{
				if (Env.DisassemblableCore == null || Env.MemoryDomains == null) throw new NotImplementedException();
				return (
					Env.DisassemblableCore.Disassemble(
						string.IsNullOrEmpty(name) ? Env.MemoryDomains.SystemBus : Env.MemoryDomains[name],
						pc,
						out var l
					),
					l
				);
			}
			catch (NotImplementedException)
			{
				Env.LogCallback($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDisassemblable.Disassemble)}()");
				return null;
			}
		}

		[LegacyApiHawk]
		object? IEmu.Disassemble(uint pc, string name) => ((IEmulationLib) this).Disassemble(pc, name);

		[LegacyApiHawk]
		public void DisplayVsync(bool enabled) => IsVSyncEnabled = enabled;

		public bool DoFrameAdvance() => false; //TODO

		[LegacyApiHawk]
		public void FrameAdvance() => FrameAdvanceCallback?.Invoke(); // TODO did FrameAdvance actually work? HelloWorld uses ClientApi

		[LegacyApiHawk]
		int IEmu.FrameCount() => FrameCount;

		[LegacyApiHawk]
		public string GetBoardName() => Env.BoardInfo?.BoardName ?? string.Empty;

		[LegacyApiHawk]
		public string GetDisplayType() => DisplayType?.ToString() ?? string.Empty;

		[LegacyApiHawk]
		public ulong? GetRegister(string name) => Register[name];

		[LegacyApiHawk]
		public Dictionary<string, ulong> GetRegisters()
		{
			try
			{
				if (Env.DebuggableCore == null) throw new NotImplementedException();
				var table = new Dictionary<string, ulong>();
				foreach (var kvp in Env.DebuggableCore.GetCpuFlagsAndRegisters()) table[kvp.Key] = kvp.Value.Value;
				return table;
			}
			catch (NotImplementedException)
			{
				Env.LogCallback($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
				return new Dictionary<string, ulong>();
			}
		}

		public object? GetSettings() => Emulator switch
		{
			GPGX gpgx => gpgx.GetSettings(),
			LibsnesCore snes => snes.GetSettings(),
			NES nes => nes.GetSettings(),
			PCEngine pce => pce.GetSettings(),
			QuickNES quickNes => quickNes.GetSettings(),
			SMS sms => sms.GetSettings(),
			WonderSwan ws => ws.GetSettings(),
			_ => /*(object?) */null
		};

		[LegacyApiHawk]
		public string? GetSystemId() => SystemID;

		[LegacyApiHawk]
		public bool IsLagged() => !IsInputPollingFrame;

		[LegacyApiHawk]
		public int LagCount() => NonPollingFrameCount ?? 0;

		[LegacyApiHawk]
		public void LimitFramerate(bool enabled) => IsFrameRateLimited = enabled;

		[LegacyApiHawk]
		public void MinimizeFrameskip(bool enabled) => IsFrameSkipMinimised = enabled;

		protected override void PostEnvUpdate() => _registerAccess = new ApiHawkRegisterAccess(Env);

		[LegacyApiHawk]
		PutSettingsDirtyBits IEmu.PutSettings(object settings) => Emulator switch
		{
			GPGX gpgx => gpgx.PutSettings((GPGX.GPGXSettings) settings),
			LibsnesCore snes => snes.PutSettings((LibsnesCore.SnesSettings) settings),
			NES nes => nes.PutSettings((NES.NESSettings) settings),
			PCEngine pce => pce.PutSettings((PCEngine.PCESettings) settings),
			QuickNES quickNes => quickNes.PutSettings((QuickNES.QuickNESSettings) settings),
			SMS sms => sms.PutSettings((SMS.SmsSettings) settings),
			WonderSwan ws => ws.PutSettings((WonderSwan.Settings) settings),
			_ => PutSettingsDirtyBits.None
		};

		bool IEmulationLib.PutSettings(object settings, out PutSettingsDirtyBits requiredAfterChange)
			=> (requiredAfterChange = ((IEmu) this).PutSettings(settings)) != PutSettingsDirtyBits.None;

		[LegacyApiHawk]
		public void SetIsLagged(bool value) => IsInputPollingFrame = !value;

		[LegacyApiHawk]
		public void SetLagCount(int count) => NonPollingFrameCount = count;

		[LegacyApiHawk]
		public void SetRegister(string register, int value) => Register[register] = (ulong) value;

		public void SetRenderPlanes(params bool[] args)
		{
			static bool GetSetting(bool[] settings, int index) => index >= settings.Length || settings[index];
			PutSettingsDirtyBits SetBSNES(LibsnesCore core)
			{
				var s = core.GetSettings();
				s.ShowBG1_0 = s.ShowBG1_1 = GetSetting(args, 0);
				s.ShowBG2_0 = s.ShowBG2_1 = GetSetting(args, 1);
				s.ShowBG3_0 = s.ShowBG3_1 = GetSetting(args, 2);
				s.ShowBG4_0 = s.ShowBG4_1 = GetSetting(args, 3);
				s.ShowOBJ_0 = GetSetting(args, 4);
				s.ShowOBJ_1 = GetSetting(args, 5);
				s.ShowOBJ_2 = GetSetting(args, 6);
				s.ShowOBJ_3 = GetSetting(args, 7);
				return core.PutSettings(s);
			}
			PutSettingsDirtyBits SetCygne(WonderSwan ws)
			{
				var s = ws.GetSettings();
				s.EnableSprites = GetSetting(args, 0);
				s.EnableFG = GetSetting(args, 1);
				s.EnableBG = GetSetting(args, 2);
				return ws.PutSettings(s);
			}
			PutSettingsDirtyBits SetGPGX(GPGX core)
			{
				var s = core.GetSettings();
				s.DrawBGA = GetSetting(args, 0);
				s.DrawBGB = GetSetting(args, 1);
				s.DrawBGW = GetSetting(args, 2);
				s.DrawObj = GetSetting(args, 3);
				return core.PutSettings(s);
			}
			PutSettingsDirtyBits SetNesHawk(NES core)
			{
				var s = core.GetSettings();
				// in the future, we could do something more arbitrary here, but this isn't any worse than the old system
				s.DispSprites = GetSetting(args, 0);
				s.DispBackground = GetSetting(args, 1);
				return core.PutSettings(s);
			}
			PutSettingsDirtyBits SetPCEHawk(PCEngine pce)
			{
				var s = pce.GetSettings();
				s.ShowOBJ1 = GetSetting(args, 0);
				s.ShowBG1 = GetSetting(args, 1);
				if (args.Length > 2)
				{
					s.ShowOBJ2 = GetSetting(args, 2);
					s.ShowBG2 = GetSetting(args, 3);
				}
				return pce.PutSettings(s);
			}
			PutSettingsDirtyBits SetQuickNES(QuickNES quicknes)
			{
				var s = quicknes.GetSettings();
				// this core doesn't support disabling BG
				var showSp = GetSetting(args, 0);
				if (showSp && s.NumSprites == 0) s.NumSprites = 8;
				else if (!showSp && s.NumSprites > 0) s.NumSprites = 0;
				return quicknes.PutSettings(s);
			}
			PutSettingsDirtyBits SetSMSHawk(SMS sms)
			{
				var s = sms.GetSettings();
				s.DispOBJ = GetSetting(args, 0);
				s.DispBG = GetSetting(args, 1);
				return sms.PutSettings(s);
			}
			_ = Emulator switch
			{
				GPGX gpgx => SetGPGX(gpgx),
				LibsnesCore snes => SetBSNES(snes),
				NES nes => SetNesHawk(nes),
				PCEngine pce => SetPCEHawk(pce),
				QuickNES quicknes => SetQuickNES(quicknes),
				SMS sms => SetSMSHawk(sms),
				WonderSwan ws => SetCygne(ws),
				_ => (PutSettingsDirtyBits?) null
			};
		}

		[LegacyApiHawk]
		long IEmu.TotalExecutedCycles() => TotalExecutedCycles ?? 0L;

		[LegacyApiHawk]
		public void Yield() {}
	}
}
