using System;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <remarks>Methods intentionally blank.</remarks>
	public sealed class UnixLuaLibraries : LuaLibraries
	{
		private static readonly ResumeResult EmptyResumeResult = new ResumeResult();

		public override GuiLuaLibrary GuiLibrary => null;

		public override LuaFunctionList RegisteredFunctions { get; } = new LuaFunctionList();

		public override void CallExitEvent(LuaFile lf) { }

		public override void CallFrameAfterEvent() { }

		public override void CallFrameBeforeEvent() { }

		public override void CallLoadStateEvent(string name) { }

		public override void CallSaveStateEvent(string name) { }

		public override void Close() { }

		public override void EndLuaDrawing() { }

		public override void ExecuteString(string command) { }

		public override void Restart(IEmulatorServiceProvider newServiceProvider) { }

		public override ResumeResult ResumeScript(LuaFile lf) => EmptyResumeResult;

		public override void SpawnAndSetFileThread(string pathToLoad, LuaFile lf) { }

		public override void StartLuaDrawing() { }

		public override void WindowClosed(IntPtr handle) { }
	}
}
