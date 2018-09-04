using System;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public sealed class MemoryEventsPluginLibrary : PluginLibraryBase
	{
		[RequiredService]
		private IDebuggable DebuggableCore { get; set; }

		public MemoryEventsPluginLibrary () : base()
		{ }

		public void AddReadCallback(Action cb, uint address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Read, "Plugin Hook", cb, address, null));
			}
		}
		public void AddWriteCallback(Action cb, uint address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Write, "Plugin Hook", cb, address, null));
			}
		}
		public void AddExecCallback(Action cb, uint address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable() && DebuggableCore.MemoryCallbacks.ExecuteCallbacksAvailable)
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Execute, "Plugin Hook", cb, address, null));
			}
		}
		public void RemoveMemoryCallback(Action cb)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Remove(cb);
			}
		}
	}
}
