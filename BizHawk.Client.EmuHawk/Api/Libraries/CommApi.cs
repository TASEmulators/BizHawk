using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class CommApi : IComm
	{
		public Communication.HttpCommunication HTTP => GlobalWin.httpCommunication;

		public Communication.MemoryMappedFiles MemoryMappedFiles => GlobalWin.memoryMappedFiles;

		public Communication.SocketServer SocketServer => GlobalWin.socketServer;
	}
}
