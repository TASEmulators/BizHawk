using System;
using System.Collections.Generic;
using System.IO;

using NLua;

using BizHawk.Emulation.Common;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	public sealed class MemorySavestateEmuLuaLibrary : LuaLibraryBase
	{
		public MemorySavestateEmuLuaLibrary(Lua lua)
			: base(lua) { }

		public MemorySavestateEmuLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "memorysavestate";

		[RequiredService]
		private IStatable StatableCore { get; set; }

		private readonly Dictionary<Guid, byte[]> _memorySavestates = new Dictionary<Guid, byte[]>();

		[LuaMethodExample("local mmsvstsvcst = memorysavestate.savecorestate( );")]
		[LuaMethod("savecorestate", "creates a core savestate and stores it in memory.  Note: a core savestate is only the raw data from the core, and not extras such as movie input logs, or framebuffers. Returns a unique identifer for the savestate")]
		public string SaveCoreStateToMemory()
		{
			var guid = Guid.NewGuid();
			var bytes = (byte[])StatableCore.SaveStateBinary().Clone();

			_memorySavestates.Add(guid, bytes);

			return guid.ToString();
		}

		[LuaMethodExample("memorysavestate.loadcorestate( \"3fcf120f-0778-43fd-b2c5-460fb7d34184\" );")]
		[LuaMethod("loadcorestate", "loads an in memory state with the given identifier")]
		public void LoadCoreStateFromMemory(string identifier)
		{
			var guid = new Guid(identifier);

			try
			{
				var state = _memorySavestates[guid];

				using var ms = new MemoryStream(state);
				using var br = new BinaryReader(ms);
				StatableCore.LoadStateBinary(br);
			}
			catch
			{
				Log("Unable to find the given savestate in memory");
			}
		}

		[LuaMethodExample("memorysavestate.removestate( \"3fcf120f-0778-43fd-b2c5-460fb7d34184\" );")]
		[LuaMethod("removestate", "removes the savestate with the given identifier from memory")]
		public void DeleteState(string identifier)
		{
			var guid = new Guid(identifier);
			_memorySavestates.Remove(guid);
		}

		[LuaMethodExample("memorysavestate.clearstatesfrommemory( );")]
		[LuaMethod("clearstatesfrommemory", "clears all savestates stored in memory")]
		public void ClearInMemoryStates()
		{
			_memorySavestates.Clear();
		}
	}
}
