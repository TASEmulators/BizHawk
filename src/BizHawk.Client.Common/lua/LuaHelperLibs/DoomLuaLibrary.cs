using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.Doom;

using NLua;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	[Description("Functions specific to Doom games (functions may not run when a Doom game is not loaded)")]
	public sealed class DoomLuaLibrary : LuaLibraryBase
	{
		public DoomLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) { }

		public override string Name => "doom";
		private const string ERR_MSG_UNSUPPORTED_CORE = $"`doom.*` functions can only be used with {CoreNames.DSDA}";

		[RequiredService]
		private IEmulator Emulator { get; set; }

		/// <exception cref="InvalidOperationException">loaded core is not DSDA-Doom</exception>
		[LuaMethodExample("local rngcall = doom.onprandom(\r\n\tfunction()\r\n\t\tconsole.log( \"Calls the given lua function after each P_Random() call by Doom\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onprandom", "Calls the given lua function after each P_Random() call by Doom")]
		public string OnPrandom(LuaFunction luaf, string name = null)
		{
			if (Emulator is not DSDA)
			{
				throw new InvalidOperationException(ERR_MSG_UNSUPPORTED_CORE);
			}

			var callbacks = (Emulator as DSDA).RandomCallbacks;
			var nlf = _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, "OnPrandom", LogOutputCallback, CurrentFile, name: name);
			callbacks.Add(nlf.RandomCallback);
			nlf.OnRemove += () => callbacks.Remove(nlf.RandomCallback);
			return nlf.GuidStr;
		}
	}
}
