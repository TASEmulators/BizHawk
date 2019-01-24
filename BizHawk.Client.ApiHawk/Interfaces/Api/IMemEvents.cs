using System;

namespace BizHawk.Client.ApiHawk
{
	public interface IMemEvents : IExternalApi
	{
		void AddReadCallback(Action<uint, uint> cb, uint? address, string domain);
		void AddWriteCallback(Action<uint, uint> cb, uint? address, string domain);
		void AddExecCallback(Action<uint, uint> cb, uint? address, string domain);
		void RemoveMemoryCallback(Action<uint, uint> cb);
	}
}
