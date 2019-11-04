using System;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Methods intentionally blank.
	/// </summary>
	public sealed class NotReallyLuaLibrary : PlatformEmuLuaLibrary
	{
		public override void CallExitEvent(LuaFile lf)
		{
		}
		public override void CallFrameAfterEvent()
		{
		}
		public override void CallFrameBeforeEvent()
		{
		}
		public override void CallLoadStateEvent(string name)
		{
		}
		public override void CallSaveStateEvent(string name)
		{
		}
		public override void Close()
		{
		}
		public override void EndLuaDrawing()
		{
		}
		public override void ExecuteString(string command)
		{
		}
		private static readonly LuaFunctionList EmptyLuaFunList = new LuaFunctionList();
		public override LuaFunctionList RegisteredFunctions => EmptyLuaFunList;
		public override void Restart(IEmulatorServiceProvider newServiceProvider)
		{
		}
		private static readonly EmuLuaLibrary.ResumeResult EmptyResumeResult = new EmuLuaLibrary.ResumeResult();
		public override EmuLuaLibrary.ResumeResult ResumeScriptFromThreadOf(LuaFile lf)
		{
			return EmptyResumeResult;
		}
		public override void SpawnAndSetFileThread(string pathToLoad, LuaFile lf)
		{
		}
		public override void StartLuaDrawing()
		{
		}
		public override void WindowClosed(IntPtr handle)
		{
		}
	}
}