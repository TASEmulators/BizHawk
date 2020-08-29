#nullable enable

namespace BizHawk.Client.Common
{
	public interface ICommApi : IExternalApi
	{
		HttpCommunication? HTTP { get; }

		MemoryMappedFiles? MMF { get; }

		SocketServer? Sockets { get; }

#if ENABLE_WEBSOCKETS
		WebSocketServer? WebSockets { get; }
#endif

		string? HttpTest();

		string? HttpTestGet();
	}
}
