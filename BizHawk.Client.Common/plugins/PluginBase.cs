using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public abstract class PluginBase
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IMemoryDomains Domains { get; set; }

		[OptionalService]
		private IInputPollable InputPollableCore { get; set; }

		[OptionalService]
		private IDebuggable DebuggableCore { get; set; }

		public abstract void PreFrameCallback();
		public abstract void PostFrameCallback();
		public abstract void SaveStateCallback(string name);
		public abstract void LoadStateCallback(string name);
		public abstract void InputPollCallback();

		protected virtual void AddReadCallback(Action cb, uint address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Read, "Lua Hook", cb, address, null));
			}
		}
		protected virtual void AddWriteCallback(Action cb, uint address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Write, "Lua Hook", cb, address, null));
			}
		}
		protected virtual void AddExecCallback(Action cb, uint address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable() && DebuggableCore.MemoryCallbacks.ExecuteCallbacksAvailable)
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Execute, "Lua Hook", cb, address, null));
			}
		}
		protected virtual void RemoveMemoryCallback(Action cb)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Remove(cb);
			}
		}
	}
}
