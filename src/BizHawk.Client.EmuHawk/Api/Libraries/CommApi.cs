#nullable enable

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class CommApi : ICommApi
	{
		public HttpCommunication? HTTP => GlobalWin.httpCommunication;

		public MemoryMappedFiles? MMF => GlobalWin.memoryMappedFiles;

		public SocketServer? Sockets => GlobalWin.socketServer;

		public string? HttpTest() => HTTP == null ? null : string.Join("\n", HttpTestGet(), HTTP.SendScreenshot(), "done testing");

		public string? HttpTestGet() => HTTP?.Get(HTTP.GetUrl)?.Result;
	}
}
