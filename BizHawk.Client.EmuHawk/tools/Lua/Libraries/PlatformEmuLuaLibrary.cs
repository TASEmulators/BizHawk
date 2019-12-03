using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public abstract class PlatformEmuLuaLibrary
	{
		public readonly LuaDocumentation Docs = new LuaDocumentation();
		public abstract LuaFunctionList RegisteredFunctions { get; }
		public GuiLuaLibrary GuiLibrary => (GuiLuaLibrary) Libraries[typeof(GuiLuaLibrary)];
		protected readonly Dictionary<Type, LuaLibraryBase> Libraries = new Dictionary<Type, LuaLibraryBase>();
		public IEnumerable<LuaFile> RunningScripts => ScriptList.Where(lf => lf.Enabled);
		public readonly LuaFileList ScriptList = new LuaFileList();

		public bool IsRebootingCore { get; set; } // pretty hacky.. we dont want a lua script to be able to restart itself by rebooting the core
		public EventWaitHandle LuaWait { get; protected set; }

		public abstract void CallExitEvent(LuaFile lf);
		public abstract void CallFrameAfterEvent();
		public abstract void CallFrameBeforeEvent();
		public abstract void CallLoadStateEvent(string name);
		public abstract void CallSaveStateEvent(string name);
		public abstract void Close();
		public abstract void EndLuaDrawing();
		public abstract void ExecuteString(string command);
		public abstract void Restart(IEmulatorServiceProvider newServiceProvider);
		public abstract EmuLuaLibrary.ResumeResult ResumeScript(LuaFile lf);
		public abstract void SpawnAndSetFileThread(string pathToLoad, LuaFile lf);
		public abstract void StartLuaDrawing();
		public abstract void WindowClosed(IntPtr handle);
	}
}