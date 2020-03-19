using System;
using System.ComponentModel;

using NLua;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	[Description("A library for interacting with the currently loaded emulator core")]
	public sealed class EmulatorLuaLibrary : DelegatingLuaLibrary
	{
		public Action FrameAdvanceCallback { get; set; }
		public Action YieldCallback { get; set; }

		public EmulatorLuaLibrary(Lua lua)
			: base(lua) { }

		public EmulatorLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "emu";

		[LuaMethodExample("emu.displayvsync( true );")]
		[LuaMethod("displayvsync", "Sets the display vsync property of the emulator")]
		public void DisplayVsync(bool enabled) => APIs.Emu.DisplayVsync(enabled);

		[LuaMethodExample("emu.frameadvance( );")]
		[LuaMethod("frameadvance", "Signals to the emulator to resume emulation. Necessary for any lua script while loop or else the emulator will freeze!")]
		public void FrameAdvance()
		{
			FrameAdvanceCallback();
		}

		[LuaMethodExample("local inemufra = emu.framecount( );")]
		[LuaMethod("framecount", "Returns the current frame count")]
		public int FrameCount() => APIs.Emu.FrameCount();

		[LuaMethodExample("local obemudis = emu.disassemble( 0x8000 );")]
		[LuaMethod("disassemble", "Returns the disassembly object (disasm string and length int) for the given PC address. Uses System Bus domain if no domain name provided")]
		public object Disassemble(uint pc, string name = "") => APIs.Emu.Disassemble(pc, name);

		// TODO: what about 64 bit registers?
		[LuaMethodExample("local inemuget = emu.getregister( emu.getregisters( )[ 0 ] );")]
		[LuaMethod("getregister", "returns the value of a cpu register or flag specified by name. For a complete list of possible registers or flags for a given core, use getregisters")]
		public int GetRegister(string name) => (int?) APIs.Emu.GetRegister(name) ?? 0;

		[LuaMethodExample("local nlemuget = emu.getregisters( );")]
		[LuaMethod("getregisters", "returns the complete set of available flags and registers for a given core")]
		public LuaTable GetRegisters()
		{
			return APIs.Emu
				.GetRegisters()
				.ToLuaTable(Lua);
		}

		[LuaMethodExample("emu.setregister( emu.getregisters( )[ 0 ], -1000 );")]
		[LuaMethod("setregister", "sets the given register name to the given value")]
		public void SetRegister(string register, int value) => APIs.Emu.SetRegister(register, value);

		[LuaMethodExample("local inemutot = emu.totalexecutedcycles( );")]
		[LuaMethod("totalexecutedcycles", "gets the total number of executed cpu cycles")]
		public long TotalExecutedycles() => APIs.Emu.TotalExecutedCycles();

		[LuaMethodExample("local stemuget = emu.getsystemid( );")]
		[LuaMethod("getsystemid", "Returns the ID string of the current core loaded. Note: No ROM loaded will return the string NULL")]
		public string GetSystemId() => APIs.Emu.GetSystemId();

		[LuaMethodExample("if ( emu.islagged( ) ) then\r\n\tconsole.log( \"Returns whether or not the current frame is a lag frame\" );\r\nend;")]
		[LuaMethod("islagged", "Returns whether or not the current frame is a lag frame")]
		public bool IsLagged() => APIs.Emu.IsLagged();

		[LuaMethodExample("emu.setislagged( true );")]
		[LuaMethod("setislagged", "Sets the lag flag for the current frame. If no value is provided, it will default to true")]
		public void SetIsLagged(bool value = true) => APIs.Emu.SetIsLagged(value);

		[LuaMethodExample("local inemulag = emu.lagcount( );")]
		[LuaMethod("lagcount", "Returns the current lag count")]
		public int LagCount() => APIs.Emu.LagCount();

		[LuaMethodExample("emu.setlagcount( 50 );")]
		[LuaMethod("setlagcount", "Sets the current lag count")]
		public void SetLagCount(int count) => APIs.Emu.SetLagCount(count);

		[LuaMethodExample("emu.limitframerate( true );")]
		[LuaMethod("limitframerate", "sets the limit framerate property of the emulator")]
		public void LimitFramerate(bool enabled) => APIs.Emu.LimitFramerate(enabled);

		[LuaMethodExample("emu.minimizeframeskip( true );")]
		[LuaMethod("minimizeframeskip", "Sets the autominimizeframeskip value of the emulator")]
		public void MinimizeFrameskip(bool enabled) => APIs.Emu.MinimizeFrameskip(enabled);

		[LuaMethodExample("emu.setrenderplanes( true, false );")]
		[LuaMethod("setrenderplanes", "Toggles the drawing of sprites and background planes. Set to false or nil to disable a pane, anything else will draw them")]
		public void SetRenderPlanes(params bool[] luaParam) => APIs.Emu.SetRenderPlanes(luaParam);

		[LuaMethodExample("emu.yield( );")]
		[LuaMethod("yield", "allows a script to run while emulation is paused and interact with the gui/main window in realtime ")]
		public void Yield()
		{
			YieldCallback();
		}

		[LuaMethodExample("local stemuget = emu.getdisplaytype();")]
		[LuaMethod("getdisplaytype", "returns the display type (PAL vs NTSC) that the emulator is currently running in")]
		public string GetDisplayType() => APIs.Emu.GetDisplayType();

		[LuaMethodExample("local stemuget = emu.getboardname();")]
		[LuaMethod("getboardname", "returns (if available) the board name of the loaded ROM")]
		public string GetBoardName() => APIs.Emu.GetBoardName();

		[LuaMethod("getluacore", "returns the name of the Lua core currently in use")]
		public string GetLuaBackend()
		{
			return Lua.WhichLua;
		}
	}
}
