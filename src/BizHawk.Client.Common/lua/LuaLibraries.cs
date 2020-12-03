using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public abstract class LuaLibraries
	{
		public readonly LuaDocumentation Docs = new LuaDocumentation();
		public abstract LuaFunctionList RegisteredFunctions { get; }
		public abstract GuiLuaLibrary GuiLibrary { get; }
		protected readonly Dictionary<Type, LuaLibraryBase> Libraries = new Dictionary<Type, LuaLibraryBase>();
		public IEnumerable<LuaFile> RunningScripts => ScriptList.Where(lf => lf.Enabled);
		public readonly LuaFileList ScriptList = new LuaFileList();

		public bool IsRebootingCore { get; set; } // pretty hacky.. we don't want a lua script to be able to restart itself by rebooting the core

		public bool IsUpdateSupressed { get; private set;}

		public void SupressUpdate()
		{
			IsUpdateSupressed = true;
		}

		public void EnableUpdate()
		{
			IsUpdateSupressed = false;
		}

		public EventWaitHandle LuaWait { get; protected set; }

		public abstract void CallExitEvent(LuaFile lf);
		public abstract void CallFrameAfterEvent();
		public abstract void CallFrameBeforeEvent();
		public abstract void CallLoadStateEvent(string name);
		public abstract void CallSaveStateEvent(string name);
		public abstract void Close();
		public abstract void EndLuaDrawing();
		public abstract void ExecuteString(string command);

		public abstract NLuaTableHelper GetTableHelper();

		public abstract void Restart(IEmulatorServiceProvider newServiceProvider);
		public abstract (bool WaitForFrame, bool Terminated) ResumeScript(LuaFile lf);
		public abstract void SpawnAndSetFileThread(string pathToLoad, LuaFile lf);
		public abstract void StartLuaDrawing();
		public abstract void WindowClosed(IntPtr handle);

		public abstract void RunScheduledDisposes();
	}
}