using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMemEventsApi : IExternalApi
	{
		void AddReadCallback(MemoryCallbackDelegate cb, uint? address, string domain);
		void AddWriteCallback(MemoryCallbackDelegate cb, uint? address, string domain);
		void AddExecCallback(MemoryCallbackDelegate cb, uint? address, string domain);
		void RemoveMemoryCallback(MemoryCallbackDelegate cb);
	}
}
