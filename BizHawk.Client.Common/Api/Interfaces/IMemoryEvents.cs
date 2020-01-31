using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMemoryEvents : IExternalApi
	{
		void AddExecCallback(MemoryCallbackDelegate cb, uint? address, string domain);

		void AddReadCallback(MemoryCallbackDelegate cb, uint? address, string domain);

		void AddWriteCallback(MemoryCallbackDelegate cb, uint? address, string domain);

		void RemoveMemoryCallback(MemoryCallbackDelegate cb);
	}
}
