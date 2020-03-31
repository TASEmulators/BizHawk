using System;
using System.ComponentModel;
using System.Drawing;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common.lua
{
	[Description("Functions specific to DSHawk (functions may not run when an NDS game is not loaded)")]
	public sealed class DSLuaLibrary : DelegatingLuaLibrary
	{
		public DSLuaLibrary(Lua lua)
			: base(lua) { }

		public DSLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "nds";

		[RequiredService]
		public IEmulator Emulator { get; set; }
	}
}
