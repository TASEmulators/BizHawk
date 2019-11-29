using System;
using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.WonderSwan;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

using NLua;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	[Description("A library for interacting with the currently loaded emulator core")]
	public sealed class EmulatorLuaLibrary : LuaLibraryBase
	{
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

		public EmulatorLuaLibrary(Lua lua)
			: base(lua) { }

		public EmulatorLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "emu";

		[LuaMethodExample("emu.displayvsync( true );")]
		[LuaMethod("displayvsync", "Sets the display vsync property of the emulator")]
		public static void DisplayVsync(bool enabled)
		{
			Global.Config.VSync = enabled;
		}

		[LuaMethodExample("emu.frameadvance( );")]
		[LuaMethod("frameadvance", "Signals to the emulator to resume emulation. Necessary for any lua script while loop or else the emulator will freeze!")]
		public void FrameAdvance()
		{
			FrameAdvanceCallback();
		}

		[LuaMethodExample("local inemufra = emu.framecount( );")]
		[LuaMethod("framecount", "Returns the current frame count")]
		public int FrameCount()
		{
			return Emulator.Frame;
		}

		[LuaMethodExample("local obemudis = emu.disassemble( 0x8000 );")]
		[LuaMethod("disassemble", "Returns the disassembly object (disasm string and length int) for the given PC address. Uses System Bus domain if no domain name provided")]
		public object Disassemble(uint pc, string name = "")
		{
			try
			{
				if (DisassemblableCore == null)
				{
					throw new NotImplementedException();
				}

				int l;
				MemoryDomain domain = MemoryDomains.SystemBus;

				if (!string.IsNullOrEmpty(name))
				{
					domain = MemoryDomains[name];
				}

				var d = DisassemblableCore.Disassemble(domain, pc, out l);
				return new { disasm = d, length = l };
			}
			catch (NotImplementedException)
			{
				Log($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDisassemblable.Disassemble)}()");
				return null;
			}
		}

		// TODO: what about 64 bit registers?
		[LuaMethodExample("local inemuget = emu.getregister( emu.getregisters( )[ 0 ] );")]
		[LuaMethod("getregister", "returns the value of a cpu register or flag specified by name. For a complete list of possible registers or flags for a given core, use getregisters")]
		public int GetRegister(string name)
		{
			try
			{
				if (DebuggableCore == null)
				{
					throw new NotImplementedException();
				}

				var registers = DebuggableCore.GetCpuFlagsAndRegisters();
				return registers.ContainsKey(name)
					? (int)registers[name].Value
					: 0;
			}
			catch (NotImplementedException)
			{
				Log($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
				return 0;
			}
		}

		[LuaMethodExample("local nlemuget = emu.getregisters( );")]
		[LuaMethod("getregisters", "returns the complete set of available flags and registers for a given core")]
		public LuaTable GetRegisters()
		{
			var table = Lua.NewTable();

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
				Log($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
			}

			return table;
		}

		[LuaMethodExample("emu.setregister( emu.getregisters( )[ 0 ], -1000 );")]
		[LuaMethod("setregister", "sets the given register name to the given value")]
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
				Log($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.SetCpuRegister)}()");
			}
		}

		[LuaMethodExample("local inemutot = emu.totalexecutedcycles( );")]
		[LuaMethod("totalexecutedcycles", "gets the total number of executed cpu cycles")]
		public long TotalExecutedycles()
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
				Log($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.TotalExecutedCycles)}()");

				return 0;
			}
		}

		[LuaMethodExample("local stemuget = emu.getsystemid( );")]
		[LuaMethod("getsystemid", "Returns the ID string of the current core loaded. Note: No ROM loaded will return the string NULL")]
		public static string GetSystemId()
		{
			return Global.Game.System;
		}

		[LuaMethodExample("if ( emu.islagged( ) ) then\r\n\tconsole.log( \"Returns whether or not the current frame is a lag frame\" );\r\nend;")]
		[LuaMethod("islagged", "Returns whether or not the current frame is a lag frame")]
		public bool IsLagged()
		{
			if (InputPollableCore != null)
			{
				return InputPollableCore.IsLagFrame;
			}

			Log($"Can not get lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			return false;
		}

		[LuaMethodExample("emu.setislagged( true );")]
		[LuaMethod("setislagged", "Sets the lag flag for the current frame. If no value is provided, it will default to true")]
		public void SetIsLagged(bool value = true)
		{
			if (InputPollableCore != null)
			{
				InputPollableCore.IsLagFrame = value;
			}
			else
			{
				Log($"Can not set lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			}
		}

		[LuaMethodExample("local inemulag = emu.lagcount( );")]
		[LuaMethod("lagcount", "Returns the current lag count")]
		public int LagCount()
		{
			if (InputPollableCore != null)
			{
				return InputPollableCore.LagCount;
			}

			Log($"Can not get lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			return 0;
		}

		[LuaMethodExample("emu.setlagcount( 50 );")]
		[LuaMethod("setlagcount", "Sets the current lag count")]
		public void SetLagCount(int count)
		{
			if (InputPollableCore != null)
			{
				InputPollableCore.LagCount = count;
			}
			else
			{
				Log($"Can not set lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			}
		}

		[LuaMethodExample("emu.limitframerate( true );")]
		[LuaMethod("limitframerate", "sets the limit framerate property of the emulator")]
		public static void LimitFramerate(bool enabled)
		{
			Global.Config.ClockThrottle = enabled;
		}

		[LuaMethodExample("emu.minimizeframeskip( true );")]
		[LuaMethod("minimizeframeskip", "Sets the autominimizeframeskip value of the emulator")]
		public static void MinimizeFrameskip(bool enabled)
		{
			Global.Config.AutoMinimizeSkipping = enabled;
		}

		[LuaMethodExample("emu.setrenderplanes( true, false );")]
		[LuaMethod("setrenderplanes", "Toggles the drawing of sprites and background planes. Set to false or nil to disable a pane, anything else will draw them")]
		public void SetRenderPlanes(params bool[] luaParam)
		{
			if (Emulator is NES nes)
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

		[LuaMethodExample("emu.yield( );")]
		[LuaMethod("yield", "allows a script to run while emulation is paused and interact with the gui/main window in realtime ")]
		public void Yield()
		{
			YieldCallback();
		}

		[LuaMethodExample("local stemuget = emu.getdisplaytype();")]
		[LuaMethod("getdisplaytype", "returns the display type (PAL vs NTSC) that the emulator is currently running in")]
		public string GetDisplayType()
		{
			return RegionableCore != null
				? RegionableCore.Region.ToString()
				: "";
		}

		[LuaMethodExample("local stemuget = emu.getboardname();")]
		[LuaMethod("getboardname", "returns (if available) the board name of the loaded ROM")]
		public string GetBoardName()
		{
			return BoardInfo != null
				? BoardInfo.BoardName
				: "";
		}

		[LuaMethod("getluacore", "returns the name of the Lua core currently in use")]
		public string GetLuaBackend()
		{
			return Lua.WhichLua;
		}
	}
}
