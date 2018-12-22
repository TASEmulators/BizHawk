using System;

namespace BizHawk.Client.ApiHawk
{
	public interface IMemEvents : IExternalApi
	{
		void AddReadCallback(Action cb, uint address, string domain);
		void AddWriteCallback(Action cb, uint address, string domain);
		void AddExecCallback(Action cb, uint address, string domain);
		void RemoveMemoryCallback(Action cb);
	}
}
