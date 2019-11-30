using System;
using System.ComponentModel;
using System.Collections.Generic;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.WonderSwan;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

namespace BizHawk.Client.Common
{
	[Description("A library for interacting with the currently loaded emulator core")]
	public sealed class EmuApi : IEmu
	{
		private static class EmuStatic
		{
			public static void DisplayVsync(bool enabled)
			{
				Global.Config.VSync = enabled;
			}
			public static string GetSystemId()
			{
				return Global.Game.System;
			}
			public static void LimitFramerate(bool enabled)
			{
				Global.Config.ClockThrottle = enabled;
			}

			public static void MinimizeFrameskip(bool enabled)
			{
				Global.Config.AutoMinimizeSkipping = enabled;
			}

		}
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IDebuggable DebuggableCore { get; set; }

		[OptionalService]
		private IDisassemblable DisassemblableCore { get; set; }

		[OptionalService]
		private IMemoryDomains MemoryDomains { get; set; }

		[OptionalService]
		private IInputPollable InputPollableCore { get; set; }

		[OptionalService]
		private IRegionable RegionableCore { get; set; }

		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		public Action FrameAdvanceCallback { get; set; }
		public Action YieldCallback { get; set; }

		public void DisplayVsync(bool enabled)
		{
			EmuStatic.DisplayVsync(enabled);
		}

		public void FrameAdvance()
		{
			FrameAdvanceCallback();
		}

		public int FrameCount()
		{
			return Emulator.Frame;
		}

		public object Disassemble(uint pc, string name = "")
		{
			try
			{
				if (DisassemblableCore == null)
				{
					throw new NotImplementedException();
				}

				MemoryDomain domain = MemoryDomains.SystemBus;

				if (!string.IsNullOrEmpty(name))
				{
					domain = MemoryDomains[name];
				}

				int l;
				var d = DisassemblableCore.Disassemble(domain, pc, out l);
				return new { disasm = d, length = l };
			}
			catch (NotImplementedException)
			{
				Console.WriteLine($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDisassemblable.Disassemble)}()");
				return null;
			}
		}

		public ulong? GetRegister(string name)
		{
			try
			{
				if (DebuggableCore == null)
				{
					throw new NotImplementedException();
				}

				var registers = DebuggableCore.GetCpuFlagsAndRegisters();
				ulong? value = null;
				if (registers.ContainsKey(name)) value = registers[name].Value;
				return value;
			}
			catch (NotImplementedException)
			{
				Console.WriteLine($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
				return null;
			}
		}

		public Dictionary<string, ulong> GetRegisters()
		{
			var table = new Dictionary<string, ulong>();

			try
			{
				if (DebuggableCore == null)
				{
					throw new NotImplementedException();
				}

				foreach (var kvp in DebuggableCore.GetCpuFlagsAndRegisters())
				{
					table[kvp.Key] = kvp.Value.Value;
				}
			}
			catch (NotImplementedException)
			{
				Console.WriteLine($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
			}

			return table;
		}

		public void SetRegister(string register, int value)
		{
			try
			{
				if (DebuggableCore == null)
				{
					throw new NotImplementedException();
				}

				DebuggableCore.SetCpuRegister(register, value);
			}
			catch (NotImplementedException)
			{
				Console.WriteLine($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.SetCpuRegister)}()");
			}
		}

		public long TotalExecutedCycles()
		{
			try
			{
				if (DebuggableCore == null)
				{
					throw new NotImplementedException();
				}

				return DebuggableCore.TotalExecutedCycles;
			}
			catch (NotImplementedException)
			{
				Console.WriteLine($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.TotalExecutedCycles)}()");

				return 0;
			}
		}

		public string GetSystemId()
		{
			return EmuStatic.GetSystemId();
		}

		public bool IsLagged()
		{
			if (InputPollableCore != null)
			{
				return InputPollableCore.IsLagFrame;
			}

			Console.WriteLine($"Can not get lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			return false;
		}

		public void SetIsLagged(bool value = true)
		{
			if (InputPollableCore != null)
			{
				InputPollableCore.IsLagFrame = value;
			}
			else
			{
				Console.WriteLine($"Can not set lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			}
		}

		public int LagCount()
		{
			if (InputPollableCore != null)
			{
				return InputPollableCore.LagCount;
			}

			Console.WriteLine($"Can not get lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			return 0;
		}

		public void SetLagCount(int count)
		{
			if (InputPollableCore != null)
			{
				InputPollableCore.LagCount = count;
			}
			else
			{
				Console.WriteLine($"Can not set lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			}
		}

		public void LimitFramerate(bool enabled)
		{
			EmuStatic.LimitFramerate(enabled);
		}

		public void MinimizeFrameskip(bool enabled)
		{
			EmuStatic.MinimizeFrameskip(enabled);
		}

		public void Yield()
		{
			YieldCallback();
		}

		public string GetDisplayType()
		{
			return RegionableCore != null
				? RegionableCore.Region.ToString()
				: "";
		}

		public string GetBoardName()
		{
			return BoardInfo != null
				? BoardInfo.BoardName
				: "";
		}
		public object GetSettings()
		{
			if (Emulator is GPGX gpgx)
			{
				return gpgx.GetSettings();
			}
			
			if (Emulator is LibsnesCore snes)
			{
				return snes.GetSettings();
			}
			
			if (Emulator is NES nes)
			{
				return nes.GetSettings();
			}
			
			if (Emulator is QuickNES quickNes)
			{
				return quickNes.GetSettings();
			}
			
			if (Emulator is PCEngine pce)
			{
				return pce.GetSettings();
			}
			
			if (Emulator is SMS sms)
			{
				return sms.GetSettings();
			}
			
			if (Emulator is WonderSwan ws)
			{
				return ws.GetSettings();
			}

			return null;
		}
		public bool PutSettings(object settings)
		{
			if (Emulator is GPGX gpgx)
			{
				return gpgx.PutSettings(settings as GPGX.GPGXSettings);
			}
			
			if (Emulator is LibsnesCore snes)
			{
				return snes.PutSettings(settings as LibsnesCore.SnesSettings);
			}
			
			if (Emulator is NES nes)
			{
				return nes.PutSettings(settings as NES.NESSettings);
			}
			
			if (Emulator is QuickNES quickNes)
			{
				return quickNes.PutSettings(settings as QuickNES.QuickNESSettings);
			}
			
			if (Emulator is PCEngine pce)
			{
				return pce.PutSettings(settings as PCEngine.PCESettings);
			}
			
			if (Emulator is SMS sms)
			{
				return sms.PutSettings(settings as SMS.SMSSettings);
			}

			if (Emulator is WonderSwan ws)
			{
				return ws.PutSettings(settings as WonderSwan.Settings);
			}

			return false;
		}
		public void SetRenderPlanes(params bool[] luaParam)
		{
			if (Emulator is GPGX gpgx)
			{
				var s = gpgx.GetSettings();
				s.DrawBGA = luaParam[0];
				s.DrawBGB = luaParam[1];
				s.DrawBGW = luaParam[2];
				s.DrawObj = luaParam[3];
				gpgx.PutSettings(s);

			}
			else if (Emulator is LibsnesCore snes)
			{
				var s = snes.GetSettings();
				s.ShowBG1_0 = s.ShowBG1_1 = luaParam[0];
				s.ShowBG2_0 = s.ShowBG2_1 = luaParam[1];
				s.ShowBG3_0 = s.ShowBG3_1 = luaParam[2];
				s.ShowBG4_0 = s.ShowBG4_1 = luaParam[3];
				s.ShowOBJ_0 = luaParam[4];
				s.ShowOBJ_1 = luaParam[5];
				s.ShowOBJ_2 = luaParam[6];
				s.ShowOBJ_3 = luaParam[7];
				snes.PutSettings(s);
			}
			else if (Emulator is NES nes)
			{
				// in the future, we could do something more arbitrary here.
				// but this isn't any worse than the old system
				var s = nes.GetSettings();
				s.DispSprites = luaParam[0];
				s.DispBackground = luaParam[1];
				nes.PutSettings(s);
			}
			else if (Emulator is QuickNES quicknes)
			{
				var s = quicknes.GetSettings();

				// this core doesn't support disabling BG
				bool showSp = GetSetting(0, luaParam);
				if (showSp && s.NumSprites == 0)
				{
					s.NumSprites = 8;
				}
				else if (!showSp && s.NumSprites > 0)
				{
					s.NumSprites = 0;
				}

				quicknes.PutSettings(s);
			}
			else if (Emulator is PCEngine pce)
			{
				var s = pce.GetSettings();
				s.ShowOBJ1 = GetSetting(0, luaParam);
				s.ShowBG1 = GetSetting(1, luaParam);
				if (luaParam.Length > 2)
				{
					s.ShowOBJ2 = GetSetting(2, luaParam);
					s.ShowBG2 = GetSetting(3, luaParam);
				}

				pce.PutSettings(s);
			}
			else if (Emulator is SMS sms)
			{
				var s = sms.GetSettings();
				s.DispOBJ = GetSetting(0, luaParam);
				s.DispBG = GetSetting(1, luaParam);
				sms.PutSettings(s);
			}
			else if (Emulator is WonderSwan ws)
			{
				var s = ws.GetSettings();
				s.EnableSprites = GetSetting(0, luaParam);
				s.EnableFG = GetSetting(1, luaParam);
				s.EnableBG = GetSetting(2, luaParam);
				ws.PutSettings(s);
			}
		}

		private static bool GetSetting(int index, bool[] settings)
		{
			return index >= settings.Length || settings[index];
		}
	}
}
