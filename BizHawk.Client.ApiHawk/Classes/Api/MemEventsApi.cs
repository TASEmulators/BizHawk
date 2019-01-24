using System;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.ApiHawk
{
	public sealed class MemEventsApi : IMemEvents
	{
		[RequiredService]
		private IDebuggable DebuggableCore { get; set; }

		public MemEventsApi () : base()
		{ }

		public void AddReadCallback(Action<uint, uint> cb, uint? address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Read, "Plugin Hook", cb, address, null));
			}
		}
		public void AddWriteCallback(Action<uint, uint> cb, uint? address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Write, "Plugin Hook", cb, address, null));
			}
		}
		public void AddExecCallback(Action<uint, uint> cb, uint? address, string domain)
		{
			if (DebuggableCore.MemoryCallbacksAvailable() && DebuggableCore.MemoryCallbacks.ExecuteCallbacksAvailable)
			{
				DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(domain, MemoryCallbackType.Execute, "Plugin Hook", cb, address, null));
			}
		}
		public void RemoveMemoryCallback(Action<uint, uint> cb)
		{
			if (DebuggableCore.MemoryCallbacksAvailable())
			{
				DebuggableCore.MemoryCallbacks.Remove(cb);
			}
		}
	}
}
