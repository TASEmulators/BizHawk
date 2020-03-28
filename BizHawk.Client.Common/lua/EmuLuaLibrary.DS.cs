using System;
using System.ComponentModel;
using System.Drawing;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common.lua
{
	[Description("Functions specific to DSHawk (functions may not run when an Genesis game is not loaded)")]
	public sealed class DSLuaLibrary : DelegatingLuaLibrary
	{
		public DSLuaLibrary(Lua lua)
			: base(lua) { }

		public DSLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "ds";

		[RequiredService]
		public IEmulator Emulator { get; set; }

		[LuaMethodExample("touchStartX = ds.touchScreenStart().X")]
		[LuaMethod("touchScreenStart", "Gets the buffer coordinates that represent the start of the touch screen area. If the touch screen is not currently being displayed, nil will be returned.")]
		public Point? TouchScreenStart()
		{
			if (Emulator is MelonDS ds)
			{
				return ds.TouchScreenStart;
			}

			return null;
		}
	}
}
