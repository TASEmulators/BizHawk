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
#pragma warning disable MA0136 // multi-line string literals (passed to `[LuaMethodExample]`, which converts to host newlines)
		[LuaMethodExample("""
			local rngcall_cb_id = doom.on_prandom(function(pr_class)
				console.log("RNG advanced (class-"..pr_class.." caller)");
			end, "RNG notifier");
		""")]
#pragma warning restore MA0136
		[LuaMethod(
			name: "on_prandom",
			description: "Fires immediately after each P_Random() call by Doom. Your callback can have 1 parameter, which will be an integer identifying what kind of object or action made the RNG call.")]
		public string OnPrandom(LuaFunction luaf, string name = null)
		{
			if (Emulator is not DSDA dsda)
			{
				throw new InvalidOperationException(ERR_MSG_UNSUPPORTED_CORE);
			}

			var callbacks = dsda.RandomCallbacks;
			var nlf = _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, "OnPrandom", LogOutputCallback, CurrentFile, name: name);
			callbacks.Add(nlf.RandomCallback);
			nlf.OnRemove += () => callbacks.Remove(nlf.RandomCallback);
			return nlf.GuidStr;
		}
	}
}
