using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public sealed class MemoryEventsApi : IMemoryEvents
	{
		[RequiredService]
		private IDebuggable DebuggableCore { get; set; }

		public void AddExecCallback(MemoryCallbackDelegate cb, uint? address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable() && DebuggableCore.MemoryCallbacks.ExecuteCallbacksAvailable)
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Execute, "Plugin Hook", cb, address, null));
			}
		}

		public void AddReadCallback(MemoryCallbackDelegate cb, uint? address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Read, "Plugin Hook", cb, address, null));
			}
		}

		public void AddWriteCallback(MemoryCallbackDelegate cb, uint? address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Write, "Plugin Hook", cb, address, null));
			}
		}

		public void RemoveMemoryCallback(MemoryCallbackDelegate cb)
		{
			if (DebuggableCore.MemoryCallbacksAvailable()) DebuggableCore.MemoryCallbacks.Remove(cb);
		}
	}
}
